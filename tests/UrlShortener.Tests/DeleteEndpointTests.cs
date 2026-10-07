using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using UrlShortener.Api.DTOs;

namespace UrlShortener.Tests
{
    public class DeleteEndpointTests : IClassFixture<TestFactory>
    {
        private readonly HttpClient _client;
        private readonly HttpClient _noRedirect;

        public DeleteEndpointTests(TestFactory factory)
        {
            _client = factory.CreateClient();
            _noRedirect = factory.CreateClient(
                new() { AllowAutoRedirect = false });
        }

        [Fact]
        public async Task Delete_ExistingCode_Returns204()
        {
            var shorten = await _client.PostAsJsonAsync("/api/shorten",
                new { url = "https://example.com" });
            var result = await shorten.Content.ReadFromJsonAsync<ShortenResponse>();

            var response = await _client.DeleteAsync($"/api/{result!.ShortCode}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Delete_UnknownCode_Returns404()
        {
            var response = await _client.DeleteAsync("/api/xxxxxx");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ThenRedirect_Returns404()
        {
            var shorten = await _client.PostAsJsonAsync("/api/shorten",
                new { url = "https://example.com" });
            var result = await shorten.Content.ReadFromJsonAsync<ShortenResponse>();
            var code = result!.ShortCode;

            await _client.DeleteAsync($"/api/{code}");

            var redirect = await _noRedirect.GetAsync($"/{code}");
            Assert.Equal(HttpStatusCode.NotFound, redirect.StatusCode);
        }
    }
}
