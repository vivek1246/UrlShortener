using System.Net;
using System.Net.Http.Json;
using UrlShortener.Api.DTOs;

namespace UrlShortener.Tests.IntegrationTests;

public class ShortenEndpointTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;

    public ShortenEndpointTests(TestFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task Post_ValidUrl_Returns200WithShortCode()
    {
        var response = await _client.PostAsJsonAsync("/api/shorten",
            new { url = "https://www.example.com/long/path" });

        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync<ShortenResponse>();

        Assert.NotNull(result);
        Assert.Equal(6, result.ShortCode.Length);
        Assert.Contains(result.ShortCode, result.ShortUrl);
    }

    [Fact]
    public async Task Post_InvalidUrl_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/shorten",
            new { url = "not-valid" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InternalUrl_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/shorten",
            new { url = "http://localhost/internal" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithTtl_ReturnsExpiryDate()
    {
        var response = await _client.PostAsJsonAsync("/api/shorten",
            new { url = "https://example.com", ttlHours = 48 });

        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync<ShortenResponse>();

        Assert.NotNull(result!.ExpiresAt);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }
}