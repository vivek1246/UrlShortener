using UrlShortener.Api.Services;

namespace UrlShortener.Tests.UnitTests;

public class UrlValidationServiceTests
{
    private readonly UrlValidationService _sut = new();

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("http://example.com/path?q=123")]
    public void Validate_ValidUrls_ReturnValid(string url)
        => Assert.True(_sut.Validate(url).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://files.example.com")]
    public void Validate_InvalidUrls_ReturnError(string url)
        => Assert.False(_sut.Validate(url).IsValid);

    [Theory]
    [InlineData("http://localhost/admin")]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://192.168.1.1")]
    [InlineData("http://10.0.0.1")]
    public void Validate_PrivateUrls_BlockedWithMessage(string url)
    {
        var result = _sut.Validate(url);
        Assert.False(result.IsValid);
        Assert.Contains("Private", result.ErrorMessage);
    }
}