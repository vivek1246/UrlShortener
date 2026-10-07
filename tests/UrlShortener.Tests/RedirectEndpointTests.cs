using System.Net;
using System.Net.Http.Json;
using UrlShortener.Api.DTOs;

namespace UrlShortener.Tests.IntegrationTests;

public class RedirectEndpointTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _noRedirect;
    private readonly HttpClient _following;

    public RedirectEndpointTests(TestFactory factory)
    {
        _noRedirect = factory.CreateClient(
            new() { AllowAutoRedirect = false });
        _following = factory.CreateClient();
    }

    private async Task<string> CreateLink(string url = "https://example.com")
    {
        var r = await _following.PostAsJsonAsync(
            "/api/shorten", new { url });
        var result = await r.Content.ReadFromJsonAsync<ShortenResponse>();
        return result!.ShortCode;
    }

    [Fact]
    public async Task Get_ValidCode_Returns302()
    {
        var code = await CreateLink("https://www.google.com");
        var response = await _noRedirect.GetAsync($"/{code}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString().TrimEnd('/');
        Assert.Equal("https://www.google.com", location);
    }

    [Fact]
    public async Task Get_UnknownCode_Returns404()
    {
        var response = await _noRedirect.GetAsync("/xxxxxx");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_SwaggerPath_NotHandledByRedirect()
    {
        var response = await _noRedirect.GetAsync("/swagger");
        Assert.NotEqual(HttpStatusCode.InternalServerError,
            response.StatusCode);
    }
}