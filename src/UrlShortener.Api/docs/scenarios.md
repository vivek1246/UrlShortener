# Engineering Scenarios

---

## Scenario 1 — Greenfield: Core URL Shortener Service

### Requirement
Build a URL shortener from scratch with shorten, redirect, 
and analytics APIs.

### Decomposition
1. Domain models: ShortLink, ClickEvent
2. Short code generation algorithm
3. URL validation with security controls
4. Redirect endpoint with click tracking
5. Stats endpoint with daily aggregation
6. Rate limiting on shorten endpoint
7. Health check, logging, error handling

### AI-Assisted Execution
**Prompt:** "Generate ASP.NET Core 10 Web API with EF Core SQLite, 
Serilog, Swagger. Models: ShortLink (Id, ShortCode, OriginalUrl, 
ClickCount, ExpiresAt), ClickEvent (ShortLinkId, ClickedAt, 
IpAddress, UserAgent, Referer). Services: IUrlShortenerService, 
IUrlValidationService. Constraint: interface-driven, testable."

AI generated the scaffold, models, and service interfaces.

**Key decisions made over AI output:**

1. **Short code algorithm** — AI suggested MD5 hash. Rejected due 
   to collision risk. Chose Base-62 encoding of DB ID — guaranteed 
   unique, 6 chars, URL-safe.

2. **SSRF protection** — AI generated basic scheme validation. 
   I extended to block all private IP ranges, localhost, and 
   AWS metadata endpoint (169.254.169.254).

3. **Async click tracking** — AI generated synchronous tracking. 
   Changed to fire-and-forget Task.Run with new DI scope to keep 
   redirect latency minimal. Added try/catch with structured logging.

4. **302 status code** — Deliberately chosen over 301. 301 causes 
   browsers to cache permanently, bypassing analytics on repeat visits.

### Validation
- Unit tests: ShortCodeGenerator (uniqueness, length, charset, 
  edge cases), UrlValidationService (valid/invalid/private URLs)
- Integration tests: POST /shorten, GET /{code} redirect, 
  GET /stats, all error cases
- Security: SSRF test cases (localhost, 127.0.0.1, 192.168.x, 
  169.254.169.254) all return 400

### Outcome
Working prototype: shorten, redirect (302), analytics, 
rate limiting, health check, structured logging.

---

## Scenario 2 — Brownfield: Click Deduplication Fix

### Requirement
"Fix analytics — click counts are inaccurate."

### Codebase Reasoning
Examined the redirect flow end-to-end:
- `RedirectController` → fires `Task.Run` → `IncrementClickAsync`
- Some HTTP clients send two requests per user action
- Both requests hit the endpoint → both fire Task.Run → count +2

**Impact analysis:**
- `RedirectController.cs` — where to intercept
- `UrlShortenerService.IncrementClickAsync` — where count is written
- `ClickEvents` table — where raw data is stored
- `GetStatsAsync` — where count is read back

### Decomposition
1. Identify root cause — two HTTP requests per click
2. Design deduplication strategy — DB vs cache
3. Implement and validate

### AI-Assisted Execution
**Prompt:** "Fix double click counting. Two requests reach server 
per user action. Constraint: no extra DB query on hot path, 
thread-safe, works for all clients (browser/curl/Postman)."

**AI suggested:** `AnyAsync` DB check before every insert.

**I rejected this because:**
- Race condition: two concurrent requests both pass the check 
  before either inserts
- Adds DB query to every single redirect — performance regression
- Wrong layer: DB shouldn't be responsible for HTTP-level dedup

**My solution:** IMemoryCache deduplication keyed on 
`linkId:IP:UserAgentHash` with 10-second window. Cache.Set 
happens BEFORE DB write — prevents race condition. Uses existing 
infrastructure, zero overhead.

### Validation
- All 30 tests pass
- Manual test: browser visit = exactly 1 click recorded
- Different users (different IP/UA) each counted correctly
- Dedup window (10s) doesn't affect legitimate separate clicks

---

## Scenario 3 — Ambiguous: "Add proper analytics to the service"

### Requirement (as received)
"The service needs proper analytics."

### Ambiguities Identified
1. What does "analytics" mean? Click counts only? Trends? 
   Geographic data? Device breakdown?
2. What time range? All-time? Rolling window?
3. Should analytics be real-time or eventually consistent?
4. Who consumes it — internal dashboard or external API?
5. Should expired/deleted links still show historical analytics?

### Assumptions Made
1. Analytics = click counts + daily trend + top traffic sources 
   (referers). Geographic/device data excluded — requires 
   external IP lookup service, out of scope.
2. Rolling 30-day window for trends. All-time total click count 
   always shown.
3. Eventually consistent is acceptable — fire-and-forget tracking 
   means sub-second delay. Real-time would require synchronous 
   tracking, adding redirect latency.
4. External API consumer — JSON response, no dashboard UI.
5. Stats available for expired links — historical data has value 
   even after expiry.

### Decomposition
1. `StatsResponse` DTO: TotalClicks, ClicksPerDay, TopReferers, 
   IsExpired, LastAccessedAt
2. `ClickEvent` model: capture IP, UserAgent, Referer per click
3. `GetStatsAsync`: aggregate ClickEvents, never read from cache
4. DB indexes on `ClickEvents.ShortLinkId` and `ClickEvents.ClickedAt`

### AI-Assisted Execution
**Prompt:** "Generate StatsResponse with daily click aggregation 
for last 30 days and top 5 referers. Constraint: stats endpoint 
must always read fresh from DB, never from cache."

Key constraint I enforced: AI initially cached stats with the 
redirect path. I separated the concerns — cache serves redirects, 
stats always queries DB. Click count changes on every redirect; 
serving stale counts would make analytics meaningless.

### Validation
- `Stats_AfterClicks_ReturnsCorrectCount` — 3 different users, 
  3 clicks recorded
- `Stats_UnknownCode_Returns404`
- `Stats_NewLink_HasZeroClicks`
- Manual: daily breakdown and referer grouping verified via Swagger