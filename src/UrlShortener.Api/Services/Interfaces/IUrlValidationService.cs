namespace UrlShortener.Api.Services.Interfaces
{
    public interface IUrlValidationService
    {
        ValidationResult Validate(string url);
    }

    public record ValidationResult(bool IsValid, string? ErrorMessage = null);
}
