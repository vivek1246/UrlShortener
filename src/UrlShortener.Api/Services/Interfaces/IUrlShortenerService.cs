using UrlShortener.Api.DTOs;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public interface IUrlShortenerService
{
    Task<ShortenResponse> ShortenAsync(ShortenRequest request,
        CancellationToken ct = default);

    Task<ShortLink?> ResolveAsync(string code,
        CancellationToken ct = default);
    Task IncrementClickAsync(long shortLinkId, ClickContext context,
        CancellationToken ct = default);

    Task<StatsResponse?> GetStatsAsync(string code,
        CancellationToken ct = default);
    Task<bool> DeleteAsync(string code, CancellationToken ct = default);
}

public record ClickContext(
    string? IpAddress,
    string? UserAgent,
    string? Referer);