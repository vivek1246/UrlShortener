using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UrlShortener.Api.Models;

public class ShortLink
{
    public long Id { get; set; }

    [Required, MaxLength(10)]
    public string ShortCode { get; set; } = string.Empty;

    [Required, MaxLength(2048)]
    public string OriginalUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ClickCount { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [NotMapped]
    public bool IsExpired =>
        ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
}