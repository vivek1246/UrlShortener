# Architecture Overview

## System Components

Client (Browser / API)
│
▼
┌─────────────────────────────────────┐
│ ASP.NET Core 10 API │
│ │
│ ┌─────────────┐ ┌───────────────┐ │
│ │UrlController│ │RedirectControl│ │
│ │ POST /shorten│ │ GET /{code} │ │
│ │ GET /stats │ │ │ │
│ │ DELETE │ └───────┬───────┘ │
│ └──────┬──────┘ │ │
│ │ │ │
│ ┌──────▼────────────────▼───────┐ │
│ │ UrlShortenerService │ │
│ │ - ShortenAsync │ │
│ │ - ResolveAsync (+ cache) │ │
│ │ - IncrementClickAsync │ │
│ │ - GetStatsAsync │ │
│ │ - DeleteAsync │ │
│ └──────┬───────────────┬────────┘ │
│ │ │ │
│ ┌──────▼──────┐ ┌──────▼───────┐ │
│ │ AppDbContext│ │IMemoryCache │ │
│ │ ShortLinks │ │redirect path │ │
│ │ ClickEvents│ │5-min TTL │ │
│ └──────┬──────┘ └──────────────┘ │
│ │ │
└─────────┼───────────────────────────┘
│
┌─────▼──────┐
│ SQLite DB │
└────────────┘


## Request Flows

### Shorten Flow

POST /api/shorten
→ UrlValidationService (SSRF + scheme check)
→ Insert ShortLink (ClickCount=0, ExpiresAt optional)
→ Generate ShortCode via Base-62(Id)
→ Return ShortUrl


### Redirect Flow

GET /{code}
→ ResolveAsync → IMemoryCache check → DB if miss
→ Check IsExpired → 410 if expired
→ Fire-and-forget: IncrementClickAsync (new scope)
→ Cache deduplication check (10s window)
→ ExecuteUpdateAsync ClickCount+1
→ Insert ClickEvent (IP, UserAgent, Referer)
→ Return 302 + Cache-Control: no-store


### Stats Flow

GET /api/{code}/stats
→ Always reads fresh from DB (never cached)
→ Aggregates ClickEvents by day (last 30 days)
→ Groups top 5 referers
→ Returns StatsResponse


## Key Design Decisions

### Base-62 over UUID/Hash
UUID short codes are 36 chars — not short. MD5 hash has collision 
risk. Base-62 encoding of auto-incremented DB ID gives guaranteed 
uniqueness, 6-char codes, and URL-safe characters.

### 302 over 301
301 is permanent — browsers cache it forever. Once cached, the 
browser never hits our server again — we lose all analytics. 
302 ensures every redirect goes through our server so clicks 
are always counted.

### Cache Only the Redirect Path
Redirect is the hot path — potentially millions of hits per day. 
Stats is called occasionally. Caching stats would serve stale 
click counts. Cache only what makes sense: the destination URL 
(rarely changes), not the click count (changes on every hit).

### Fire-and-forget Click Tracking
Adding click tracking synchronously would add DB write latency 
to every redirect. Fire-and-forget returns 302 immediately and 
records the click in the background. Trade-off: possible click 
loss on process crash. Acceptable at this scale.

### SQLite for Persistence
Single-instance deployment — SQLite is sufficient, zero ops 
overhead, and ships with the app. Migration path to PostgreSQL 
is straightforward via EF Core provider swap.

## Reliability Features
- Rate limiting: 10 requests/minute on POST /shorten (configurable)
- Health check: /health endpoint with DB connectivity check
- Global exception middleware: all unhandled exceptions return 500 
  with structured JSON, never raw stack traces
- SSRF protection: private IPs, localhost, metadata endpoints blocked
- Request logging middleware: every request logged with method, 
  path, status, duration