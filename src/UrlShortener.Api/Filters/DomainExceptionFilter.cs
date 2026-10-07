using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UrlShortener.Api.Filters;

public class DomainExceptionFilter : IActionFilter
{
    private readonly ILogger<DomainExceptionFilter> _logger;

    public DomainExceptionFilter(ILogger<DomainExceptionFilter> logger)
        => _logger = logger;

    public void OnActionExecuting(ActionExecutingContext context) { }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception is ArgumentException ex)
        {
            _logger.LogWarning("Validation error: {Message}", ex.Message);

            context.Result = new BadRequestObjectResult(
                new { error = ex.Message });

            context.ExceptionHandled = true;
        }
    }
}