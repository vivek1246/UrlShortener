using System.Net;
using System.Net.Http.Json;
using UrlShortener.Api.DTOs;

namespace UrlShortener.Tests.IntegrationTests;

public class AnalyticsTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    private readonly TestFactory _factory;

    public AnalyticsTests(TestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(
            new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Stats_AfterClicks_ReturnsCorrectCount()
    {
        var shorten = await _client.PostAsJsonAsync("/api/shorten",
            new { url = "https://example.com" });
        var shortened = await shorten.Content
            .ReadFromJsonAsync<ShortenResponse>();
        var code = shortened!.ShortCode;

        var client1 = _factory.CreateClient();
        client1.DefaultRequestHeaders.Add("User-Agent", "TestBrowser/1.0");

        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Add("User-Agent", "TestBrowser/2.0");

        var client3 = _factory.CreateClient();
        client3.DefaultRequestHeaders.Add("User-Agent", "TestBrowser/3.0");

        await client1.GetAsync($"/{code}");
        await client2.GetAsync($"/{code}");
        await client3.GetAsync($"/{code}");

        var deadline = DateTime.UtcNow.AddSeconds(5);
        StatsResponse? stats = null;
        while (DateTime.UtcNow < deadline)
        {
            var statsResponse = await _client.GetAsync($"/api/{code}/stats");
            stats = await statsResponse.Content
                .ReadFromJsonAsync<StatsResponse>();
            if (stats?.TotalClicks == 3) break;
            await Task.Delay(100);
        }

        Assert.Equal(3, stats!.TotalClicks);
    }
    [Fact]
    public async Task Stats_UnknownCode_Returns404()
    {
        var response = await _client.GetAsync("/api/xxxxxx/stats");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Stats_NewLink_HasZeroClicks()
    {
        var shorten = await _client.PostAsJsonAsync("/api/shorten",
            new { url = "https://example.com" });
        var shortened = await shorten.Content
            .ReadFromJsonAsync<ShortenResponse>();

        var stats = await _client.GetAsync(
            $"/api/{shortened!.ShortCode}/stats");
        var result = await stats.Content
            .ReadFromJsonAsync<StatsResponse>();

        Assert.Equal(0, result!.TotalClicks);
        Assert.Empty(result.ClicksPerDay);
    }
}