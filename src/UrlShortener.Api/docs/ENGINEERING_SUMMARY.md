# Engineering Summary

## What Was Built
A production-grade URL shortener service built over hours using 
Claude as an AI accelerator. The service provides URL shortening, 
redirect with analytics, click deduplication, TTL-based expiry, 
rate limiting, and health monitoring.

## Artifacts
| Artifact | Location |
|----------|----------|
| API source | src/UrlShortener.Api/ |
| Unit + integration tests | tests/UrlShortener.Tests/ |
| Architecture overview | docs/architecture.md |
| Scenario walkthroughs | docs/scenarios.md |
| AI usage log | AI_LOG.md |
| Setup instructions | README.md |

## Test Coverage
30 tests — 0 failures:
- Unit: ShortCodeGenerator, UrlValidationService
- Integration: shorten, redirect, stats, delete endpoints
- Edge cases: invalid URLs, expired links, unknown codes, 
  rate limiting, SSRF attempts

## Key Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Short code | Base-62(DB ID) | Zero collisions, 6 chars, URL-safe |
| Redirect status | 302 | Analytics require server hit on every visit |
| Cache scope | Redirect only | Stats must reflect live DB state |
| Click tracking | Fire-and-forget | Zero redirect latency impact |
| Deduplication | IMemoryCache | Thread-safe, no extra DB query |
| DB | SQLite | Zero ops, EF Core swap path to Postgres |

## Risks and Trade-offs

### Click Loss on Crash
Fire-and-forget click tracking can lose clicks if the process 
crashes between redirect and background write. Acceptable at 
this scale. Production fix: persistent queue (Azure Service Bus).

### SQLite Single-Instance
SQLite does not support horizontal scaling. Suitable for 
single-instance deployment. EF Core provider swap to PostgreSQL 
is the migration path.

### IMemoryCache Deduplication
Cache-based deduplication works correctly for single-instance. 
Multi-server deployment would require distributed cache (Redis) 
for deduplication to work across nodes.

### In-Memory Cache Cold Start
On process restart, all cached redirects are lost. First request 
for each link goes to DB. Acceptable — cache warms up quickly.

## Assumptions
- Single-instance deployment (no horizontal scaling requirement)
- SQLite sufficient for prototype scale
- Analytics eventual consistency acceptable (sub-second delay)
- No authentication required on any endpoint
- Short codes are public — anyone with the code can redirect

## Limitations
- No geographic or device analytics (requires external IP service)
- No dashboard UI — API only
- No custom alias support (could be added to ShortenRequest)
- SQLite not suitable beyond ~10k concurrent requests

## AI Usage Principle Applied
Claude was used as an accelerator within tasks. Every AI output 
was reviewed, tested, and approved before use. Key rejections:
- MD5 hash for short codes (collision risk) → Base-62 counter
- Synchronous click tracking (latency) → async fire-and-forget  
- DB-level deduplication (race condition) → cache-based dedup
- Caching stats (stale data) → always read fresh from DB

Engineer owned all correctness, security, and production 
readiness decisions throughout.