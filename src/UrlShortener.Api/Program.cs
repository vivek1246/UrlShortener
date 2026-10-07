using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;
using UrlShortener.Api.Data;
using UrlShortener.Api.Filters;
using UrlShortener.Api.Middleware;
using UrlShortener.Api.Services;
using UrlShortener.Api.Services.Interfaces;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<DomainExceptionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new()
    {
        Title = "URL Shortener API",
        Version = "v1",
        Description = "Vivek Akula — Schwab G55 IC Assessment"
    }));

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite(builder.Configuration
        .GetConnectionString("Default") ?? "Data Source=urlshortener.db"));

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUrlShortenerService, UrlShortenerService>();
builder.Services.AddScoped<IUrlValidationService, UrlValidationService>();

var rl = builder.Configuration.GetSection("RateLimiting");
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("shorten", o =>
    {
        o.PermitLimit = rl.GetValue("ShortenPermitLimit", 10);
        o.Window = TimeSpan.FromMinutes(rl.GetValue("ShortenWindowMinutes", 1));
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint(
        "/swagger/v1/swagger.json", "URL Shortener v1"));
}

app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

public partial class Program { }