namespace UrlShortener.Api.DTOs;

public record ShortenResponse(
    string ShortCode,
    string ShortUrl,
    string OriginalUrl,
    DateTime CreatedAt,
    DateTime? ExpiresAt);