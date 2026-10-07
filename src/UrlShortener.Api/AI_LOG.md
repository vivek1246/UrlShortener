# AI Usage Log — URL Shortener
**Tool:** Claude (claude.ai)  
**Principle:** AI accelerates execution. Engineer owns all decisions, 
reviews all output, and approves before applying.

---

## Entry #1 — Project Scaffolding

**Prompt intent:** Generate initial ASP.NET Core project structure 
with EF Core, Serilog, and Swagger configured.

**What AI generated:** Basic Program.cs with service registrations, 
AppDbContext, and middleware pipeline.

**What I changed:** 
- Moved `AddControllers()` with filter registration — AI initially 
  registered it twice (redundant call). Removed duplicate.
- Added `public partial class Program {}` explicitly for 
  WebApplicationFactory test support.

**Decision:** Keep middleware order as ExceptionHandling → 
RequestLogging → RateLimiter. AI had RateLimiter before logging 
which would miss rate-limited requests in logs.

---

## Entry #2 — Short Code Generation

**Prompt intent:** Generate a collision-free short code algorithm.

**AI suggested:** MD5 hash of URL, take first 6 chars.

**I rejected this because:**
- Hash collisions possible — two different URLs could produce 
  same 6-char prefix
- Same URL always produces same hash — no deduplication support
- Not URL-safe without encoding

**My decision:** Base-62 encoding of auto-incremented DB ID.
- ID is guaranteed unique by DB — zero collision risk
- Deterministic and reversible
- Pads to 6 chars with 'a' for consistent URL length

**Traceability:** AI generated the Base-62 charset and loop logic. 
I added the `PadLeft(6, 'a')` and the guard for `id <= 0`.

---

## Entry #3 — URL Validation / SSRF Protection

**Prompt intent:** Validate URLs before shortening, block internal 
and private addresses.

**AI generated:** Basic HTTP/HTTPS scheme check only.

**What I added (engineering judgment):**
- Block `localhost`, `127.0.0.1`, `0.0.0.0`
- Block AWS metadata endpoint `169.254.254.169`
- Block all RFC-1918 private IP ranges (10.x, 192.168.x, 172.16-31.x)
- Block known malicious domains list

**Rationale:** A URL shortener that can be used to proxy requests 
to internal infrastructure is a critical SSRF vulnerability. This 
is a security requirement, not optional.

---

## Entry #4 — Async Click Tracking

**Prompt intent:** Record click analytics without blocking the 
redirect response.

**AI generated:** Synchronous click recording before redirect — 
added latency to every redirect.

**My decision:** Fire-and-forget via `Task.Run` with a new DI scope.
- Redirect returns 302 immediately
- Click recorded asynchronously in background
- Added try/catch with error logging — AI's version had no 
  error handling, silent failures in production are unacceptable

**Trade-off documented:** Fire-and-forget can lose clicks if process 
crashes mid-write. Acceptable for this scale. Production alternative 
would be a persistent queue (e.g., Azure Service Bus).

---

## Entry #5 — Memory Cache for Redirect Hot Path

**Prompt intent:** Cache frequently accessed short links to reduce 
DB load on redirects.

**AI generated:** Cache everything including stats queries.

**I corrected this — key design decision:**
- Cache ONLY used for redirect hot path (`ResolveAsync`)
- Stats endpoint (`GetStatsAsync`) always reads fresh from DB
- Reason: ClickCount changes on every redirect. Caching stats 
  would serve stale counts — defeats the purpose of the endpoint.
- Cache TTL capped to link's own ExpiresAt to prevent serving 
  expired links from cache

**Design principle applied:** Cache things that change rarely 
(destination URL). Never cache things that change on every 
request (click count).

---

## Entry #6 — Rate Limiting

**Prompt intent:** Protect the shorten endpoint from abuse.

**AI generated:** Hardcoded rate limit values in code.

**What I changed:** Moved limits to `appsettings.json` 
(`ShortenPermitLimit`, `ShortenWindowMinutes`) so they're 
configurable per environment without redeployment.

---

## Entry #7 — Test Infrastructure

**Prompt intent:** Set up integration tests with isolated DB 
per test run.

**Issue discovered:** Background `Task.Run` in redirect handler 
uses a separate DI scope with its own DbContext. Multiple DbContext 
instances sharing one SQLite connection caused thread lock — 
clicks were silently not recorded in tests.

**Fix I applied:** Named shared-cache SQLite database with a 
keep-alive connection. Each DbContext opens its own independent 
connection to the named DB. Independent connections = no locking. 
Shared cache = all connections see same data.

**What I validated:** Tests confirmed `Stats_AfterClicks` 
returning correct count after fix.

---

## Entry #8 — DELETE Endpoint

**Prompt intent:** Add ability to deactivate short links.

**AI generated:** Basic delete from DB.

**What I added:**
- Cache eviction on delete (`_cache.Remove`) — without this, 
  deleted links would still redirect for up to 5 minutes from cache
- Proper 204 No Content response (not 200)
- 404 if code not found

---

## Entry #9 — Click Deduplication

**Prompt intent:** Fix double-count issue — some clients trigger 
two HTTP requests per user action.

**AI first suggested:** DB-level deduplication with AnyAsync 
before every insert.

**I rejected this because:**
- Race condition — two concurrent requests both pass the check 
  before either inserts
- Extra DB query on every single redirect — performance cost

**My solution:** IMemoryCache deduplication keyed on 
`linkId + IP + UserAgent hash` with 10-second window.
- Cache.Set happens BEFORE DB write — prevents race condition
- Uses existing IMemoryCache — zero extra infrastructure
- Thread-safe by design
- Legitimate clicks from different users (different IP/UA) 
  always counted correctly

---

## Summary of AI Usage

| Area | AI Contribution | Engineer Decision |
|------|----------------|-------------------|
| Project structure | Generated scaffold | Fixed duplicate registrations |
| Short code | Suggested MD5 hash | Rejected — chose Base-62 counter |
| URL validation | Basic scheme check | Added full SSRF protection |
| Click tracking | Sync recording | Changed to async fire-and-forget |
| Caching | Cache everything | Restricted to redirect path only |
| Rate limiting | Hardcoded values | Moved to configuration |
| Test isolation | Basic in-memory DB | Fixed SQLite concurrency issue |
| DELETE endpoint | Basic DB delete | Added cache eviction |
| Deduplication | DB AnyAsync check | Rejected — used cache-based dedup |