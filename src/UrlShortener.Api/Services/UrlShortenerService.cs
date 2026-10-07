using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Api.Data;
using UrlShortener.Api.DTOs;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services.Interfaces;

namespace UrlShortener.Api.Services;

public class UrlShortenerService : IUrlShortenerService
{
    private readonly AppDbContext _db;
    private readonly IUrlValidationService _validator;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UrlShortenerService> _logger;
    private readonly IHttpContextAccessor _http;

    public UrlShortenerService(
        AppDbContext db,
        IUrlValidationService validator,
        IMemoryCache cache,
        ILogger<UrlShortenerService> logger,
        IHttpContextAccessor http)
    {
        _db = db;
        _validator = validator;
        _cache = cache;
        _logger = logger;
        _http = http;
    }

    public async Task<ShortenResponse> ShortenAsync(
        ShortenRequest request, CancellationToken ct = default)
    {
        var validation = _validator.Validate(request.Url);
        if (!validation.IsValid)
            throw new ArgumentException(validation.ErrorMessage);

        var link = new ShortLink
        {
            OriginalUrl = request.Url,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.TtlHours.HasValue
                ? DateTime.UtcNow.AddHours(request.TtlHours.Value)
                : null
        };

        _db.ShortLinks.Add(link);
        await _db.SaveChangesAsync(ct);

        link.ShortCode = ShortCodeGenerator.FromId(link.Id);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created {ShortCode} → {Url}",
            link.ShortCode, link.OriginalUrl);

        var baseUrl = GetBaseUrl();
        return new ShortenResponse(
            link.ShortCode,
            $"{baseUrl}/{link.ShortCode}",
            link.OriginalUrl,
            link.CreatedAt,
            link.ExpiresAt);
    }

    public async Task<ShortLink?> ResolveAsync(
        string code, CancellationToken ct = default)
    {
        if (_cache.TryGetValue($"link:{code}", out ShortLink? cached))
        {
            _logger.LogDebug("Cache hit for {ShortCode}", code);
            return cached;
        }

        var link = await _db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShortCode == code, ct);

        if (link is not null && !link.IsExpired)
        {
            var cacheTtl = link.ExpiresAt.HasValue
                ? link.ExpiresAt.Value - DateTime.UtcNow
                : TimeSpan.FromMinutes(5);
            if (cacheTtl > TimeSpan.FromSeconds(10))
                _cache.Set($"link:{code}", link, cacheTtl);
        }

        return link;
    }

    public async Task IncrementClickAsync(
     long shortLinkId, ClickContext context, CancellationToken ct = default)
    {
        var dedupeKey = $"click:{shortLinkId}:" +
                        $"{context.IpAddress}:" +
                        $"{context.UserAgent?.GetHashCode()}";

        if (_cache.TryGetValue(dedupeKey, out _))
        {
            _logger.LogDebug(
                "Duplicate click ignored for link {LinkId}", shortLinkId);
            return;
        }

        _cache.Set(dedupeKey, true, TimeSpan.FromSeconds(10));

        await _db.ShortLinks
            .Where(s => s.Id == shortLinkId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ClickCount, x => x.ClickCount + 1)
                .SetProperty(x => x.LastAccessedAt, DateTime.UtcNow), ct);

        _db.ClickEvents.Add(new ClickEvent
        {
            ShortLinkId = shortLinkId,
            ClickedAt = DateTime.UtcNow,
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            Referer = context.Referer
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<StatsResponse?> GetStatsAsync(
        string code, CancellationToken ct = default)
    {
        var link = await _db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShortCode == code, ct);

        if (link is null) return null;

        var cutoff = DateTime.UtcNow.AddDays(-30);

        var rawClickDates = await _db.ClickEvents
            .AsNoTracking()
            .Where(c => c.ShortLinkId == link.Id && c.ClickedAt >= cutoff)
            .Select(c => c.ClickedAt)
            .ToListAsync(ct);

        var clicksPerDay = rawClickDates
            .GroupBy(d => d.Date)
            .Select(g => new DailyClickStat(
                g.Key.ToString("yyyy-MM-dd"),
                g.Count()))
            .OrderByDescending(d => d.Date)
            .ToList();

        var rawReferers = await _db.ClickEvents
            .AsNoTracking()
            .Where(c => c.ShortLinkId == link.Id
                     && c.Referer != null
                     && c.Referer != "")
            .Select(c => c.Referer!)
            .ToListAsync(ct);

        var topReferers = rawReferers
            .GroupBy(r => r)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        return new StatsResponse(
            link.ShortCode,
            link.OriginalUrl,
            link.ClickCount,
            link.CreatedAt,
            link.ExpiresAt,
            link.IsExpired,
            link.LastAccessedAt,
            clicksPerDay,
            topReferers);
    }

    public async Task<bool> DeleteAsync(string code, CancellationToken ct = default)
    {
        var link = await _db.ShortLinks
            .FirstOrDefaultAsync(s => s.ShortCode == code, ct);

        if (link is null) return false;

        _db.ShortLinks.Remove(link);
        await _db.SaveChangesAsync(ct);

        _cache.Remove($"link:{code}");

        _logger.LogInformation("Deleted short link {ShortCode}", code);
        return true;
    }

    private string GetBaseUrl()
    {
        var req = _http.HttpContext?.Request;
        return req is null
            ? "http://localhost:5000"
            : $"{req.Scheme}://{req.Host}";
    }
}