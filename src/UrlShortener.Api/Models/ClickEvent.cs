using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.Models;

public class ClickEvent
{
    public int Id { get; set; }
    public long ShortLinkId { get; set; }
    public ShortLink ShortLink { get; set; } = null!;
    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(2048)]
    public string? Referer { get; set; }
}