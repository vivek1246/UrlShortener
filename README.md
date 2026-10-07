# URL Shortener Service

A production-grade URL shortener built with ASP.NET Core (.NET 10), 
EF Core, SQLite, and Serilog.

## Tech Stack
- ASP.NET Core 10 — Web API
- Entity Framework Core + SQLite — persistence
- IMemoryCache — redirect hot-path caching
- Serilog — structured logging
- xUnit + WebApplicationFactory — integration & unit tests
- Swashbuckle — Swagger UI

## Prerequisites
- .NET 10 SDK — https://dotnet.microsoft.com/download

## Run the API
```bash
cd src/UrlShortener.Api
dotnet run
```
Swagger UI: https://localhost:7230/swagger

## Run Tests
```bash
dotnet test
```
Expected: total: 30, failed: 0

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/shorten | Shorten a URL |
| GET | /{code} | Redirect to original URL |
| GET | /api/{code}/stats | Get click analytics |
| DELETE | /api/{code} | Delete a short link |
| GET | /health | Health check |

## Example Usage

### Shorten a URL
```bash
curl -X POST https://localhost:7230/api/shorten \
  -H "Content-Type: application/json" \
  -d '{"url": "https://www.example.com/long/path"}'
```

### Shorten with Expiry
```bash
curl -X POST https://localhost:7230/api/shorten \
  -H "Content-Type: application/json" \
  -d '{"url": "https://www.example.com", "ttlHours": 48}'
```

### Redirect
```bash
curl -v https://localhost:7230/aaaaab --insecure
# Returns 302 → original URL
# Use browser or curl — not Swagger (Swagger cannot follow redirects)
```

### Get Stats
```bash
curl https://localhost:7230/api/aaaaab/stats
```

### Delete
```bash
curl -X DELETE https://localhost:7230/api/aaaaab
```

## Configuration (appsettings.json)
```json
{
  "RateLimiting": {
    "ShortenPermitLimit": 10,
    "ShortenWindowMinutes": 1
  }
}
```

## Reset Database
Stop the app, delete `src/UrlShortener.Api/urlshortener.db`, restart.
Database is auto-created on startup via EnsureCreated().