using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UrlShortener.Api.DTOs;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Produces("application/json")]
public class UrlController : ControllerBase
{
    private readonly IUrlShortenerService _service;
    private readonly ILogger<UrlController> _logger;

    public UrlController(
        IUrlShortenerService service,
        ILogger<UrlController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("api/shorten")]
    [EnableRateLimiting("shorten")]
    [ProducesResponseType(typeof(ShortenResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(429)]
    public async Task<ActionResult<ShortenResponse>> Shorten(
        [FromBody] ShortenRequest request, CancellationToken ct)
    {
        var result = await _service.ShortenAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("api/{code}/stats")]
    [ProducesResponseType(typeof(StatsResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<StatsResponse>> Stats(
        string code, CancellationToken ct)
    {
        var stats = await _service.GetStatsAsync(code, ct);

        return stats is null
            ? NotFound(new { error = $"Short link '{code}' not found" })
            : Ok(stats);
    }

    [HttpDelete("api/{code}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string code, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(code, ct);
        return deleted ? NoContent() : NotFound(new { error = $"'{code}' not found" });
    }
}