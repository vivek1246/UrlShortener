using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
public class RedirectController : ControllerBase
{
    private readonly IUrlShortenerService _service;
    private readonly ILogger<RedirectController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public RedirectController(
        IUrlShortenerService service,
        ILogger<RedirectController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _service = service;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    [HttpGet("{code}")]
    [ProducesResponseType(302)]
    [ProducesResponseType(404)]
    [ProducesResponseType(410)]
    public async Task<IActionResult> RedirectToUrl(
    string code, CancellationToken ct)
    {
        if (code is "swagger" or "health" or "favicon.ico" or "api")
            return NotFound();

        var link = await _service.ResolveAsync(code, ct);

        if (link is null)
            return NotFound(new { error = $"'{code}' not found" });

        if (link.IsExpired)
            return StatusCode(410, new { error = "This link has expired" });

        var ctx = new ClickContext(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.FirstOrDefault(),
            Request.Headers.Referer.FirstOrDefault());

        var linkId = link.Id;
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider
                    .GetRequiredService<IUrlShortenerService>();
                await svc.IncrementClickAsync(linkId, ctx, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to record click for link {LinkId}", linkId);
            }
        });

        Response.Headers.Append("Cache-Control", "no-store, no-cache");
        return Redirect(link.OriginalUrl);
    }
}