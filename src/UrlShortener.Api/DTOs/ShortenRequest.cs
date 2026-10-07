using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.DTOs;

public record ShortenRequest
{
    [Required(ErrorMessage = "URL is required")]
    public string Url { get; init; } = string.Empty;

    [Range(1, 8760, ErrorMessage = "TTL must be 1–8760 hours")]
    public int? TtlHours { get; init; }
}