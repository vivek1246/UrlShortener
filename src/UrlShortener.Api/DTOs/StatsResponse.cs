namespace UrlShortener.Api.DTOs;

public record StatsResponse(
    string ShortCode,
    string OriginalUrl,
    int TotalClicks,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    bool IsExpired,
    DateTime? LastAccessedAt,
    IEnumerable<DailyClickStat> ClicksPerDay,
    IEnumerable<string> TopReferers);

public record DailyClickStat(string Date, int Count);