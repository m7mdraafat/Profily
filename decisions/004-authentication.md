# ADR-004: Authentication & Sessions

**Status:** Accepted  
**Date:** 2026-03-27  

---

## Context

Profily uses GitHub OAuth for login and needs to authenticate API requests from the React SPA (hosted on Cloudflare Pages at `app.profily.dev`) to the .NET API (hosted on Azure App Service at `profily-api.azurewebsites.net`). We also use SSE for real-time progress updates, which constrains our auth approach.

## Constraints

- **Cross-origin:** frontend and API on different domains
- **SSE required:** `EventSource` API cannot set custom HTTP headers — only cookies are sent automatically
- **No Redis:** sessions must be DB-backed
- **GitHub OAuth:** user authenticates via GitHub, we store their GitHub access token to call GitHub API on their behalf

## Decision: HttpOnly Cookie + DB Sessions

JWT was simpler for cross-domain, but SSE forces cookies. Since we need cookies anyway, we use them as the sole auth mechanism — no hybrid approach.

### Why not JWT

| Issue | Impact |
|---|---|
| SSE incompatible | `EventSource` can't set `Authorization` header. Would need a hybrid auth (JWT for REST, cookies for SSE) — messy. |
| No true logout | JWT can't be revoked. A leaked token is valid until expiry. |
| Token storage dilemma | localStorage = XSS risk. In-memory = lost on refresh. Both have tradeoffs. |

### Why cookies

| Benefit | Detail |
|---|---|
| SSE compatible | Browser sends cookies automatically on `EventSource` connection |
| True logout | Delete session row in DB → token is dead instantly |
| XSS-immune | HttpOnly cookies can't be read by JavaScript |
| Zero frontend auth code | No interceptors, no token refresh, no storage management |

---

## OAuth Flow

```
Browser                         API                         GitHub
  │                              │                            │
  │── GET app.profily.dev ──────▶│                            │
  │   (React SPA loads)          │                            │
  │                              │                            │
  │── Click "Sign in with GitHub"│                            │
  │── GET /api/auth/github ─────▶│                            │
  │                              │── Build OAuth URL ─────────│
  │◀── 302 Redirect ────────────│   (client_id, scope,       │
  │                              │    state, redirect_uri)    │
  │── User approves on GitHub ──▶│                            │
  │                              │                            │
  │◀── Redirect to callback ────│◀── code + state ──────────│
  │── GET /api/auth/callback ───▶│                            │
  │   ?code=xxx&state=yyy        │                            │
  │                              │── POST /access_token ─────▶│
  │                              │◀── access_token ──────────│
  │                              │── GET /user ──────────────▶│
  │                              │◀── GitHub profile ────────│
  │                              │                            │
  │                              │── Create/update user in DB │
  │                              │── Encrypt & store GitHub   │
  │                              │   token in DB              │
  │                              │── Create session row in DB │
  │                              │                            │
  │◀── Set-Cookie: session=uuid ─│                            │
  │◀── 302 Redirect to SPA ─────│                            │
  │   (app.profily.dev/dashboard)│                            │
  │                              │                            │
  │── All future requests ──────▶│                            │
  │   Cookie sent automatically  │── Lookup session in DB     │
  │   by browser                 │── Load user from session   │
```

### OAuth Scopes

| Scope | Why |
|---|---|
| `read:user` | Profile info (name, bio, avatar, email, location) |
| `repo` | Create/push to `username.github.io` (Pro: GitHub Pages deploy) |

**Note:** `repo` scope is broad (read/write all repos). We only use it for creating/pushing to `username.github.io`. We could use `public_repo` instead, but it wouldn't work for users with private repos they want to showcase. This is the same scope GitFolio and similar tools request.

---

## Session Management

### Session Table

```sql
CREATE TABLE sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at TIMESTAMPTZ NOT NULL,
    last_accessed_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_sessions_expires_at ON sessions(expires_at);
```

### Cookie Settings

```
Set-Cookie: session={uuid};
    HttpOnly;          ← JS can't read it (XSS protection)
    Secure;            ← HTTPS only
    SameSite=None;     ← Required for cross-origin (Cloudflare Pages → Azure)
    Path=/api;         ← Only sent to API endpoints
    Max-Age=604800;    ← 7 days
    Domain=            ← Not set (defaults to API domain)
```

**`SameSite=None` + `Secure` required** because frontend (`app.profily.dev`) and API (`profily-api.azurewebsites.net`) are on different origins. Without `SameSite=None`, the browser won't send the cookie cross-origin.

### Session Lifecycle

| Event | Action |
|---|---|
| Login | Create session row, set cookie (7-day expiry) |
| Any request | Lookup session by cookie value, update `last_accessed_at`, extend `expires_at` (sliding expiration) |
| Logout | Delete session row, clear cookie (`Max-Age=0`) |
| Expired | Background cleanup job deletes expired sessions (or lazy deletion on lookup) |

### Sliding Expiration

Every authenticated request extends the session by 7 days. Active users never get logged out. Inactive users expire after 7 days.

```csharp
// Middleware pseudo-code
var session = await db.Sessions.FindAsync(sessionId);
if (session == null || session.ExpiresAt < DateTime.UtcNow)
    return Unauthorized();

session.LastAccessedAt = DateTime.UtcNow;
session.ExpiresAt = DateTime.UtcNow.AddDays(7);
await db.SaveChangesAsync();
```

---

## Cross-Origin Configuration

### CORS (API side)

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProfilyCors", policy =>
    {
        policy.WithOrigins("https://app.profily.dev")
              .AllowCredentials()        // Required for cookies
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

**Key:** `AllowCredentials()` is required. Without it, the browser won't send cookies cross-origin. Cannot use `AllowAnyOrigin()` with `AllowCredentials()` — must specify exact origin.

### Frontend (fetch calls)

```typescript
// Every API call must include credentials
fetch('https://profily-api.azurewebsites.net/api/users/me', {
    credentials: 'include'  // Sends cookies cross-origin
});

// SSE also gets cookies automatically
const events = new EventSource(
    'https://profily-api.azurewebsites.net/api/events/stream',
    { withCredentials: true }
);
```

---

## GitHub Token Security

The user's GitHub access token (from OAuth) is stored encrypted in the DB. It's only decrypted when calling GitHub API on the user's behalf.

```
Encryption: AES-256-GCM
Key: Environment variable (GITHUB_TOKEN_ENCRYPTION_KEY)
Storage: users.github_token_encrypted (bytea column)
```

**Why encrypt:**
- If DB is compromised, attacker gets encrypted blobs, not raw tokens
- GitHub tokens have `repo` scope — a leak would give write access to all user repos
- AES-256-GCM provides authenticated encryption (detects tampering)

---

## CSRF Protection

| Protection | How |
|---|---|
| `SameSite=None` | Doesn't prevent CSRF by itself (it allows cross-site!) |
| Origin header check | API verifies `Origin` header matches `https://app.profily.dev` on mutating requests (POST, PATCH, DELETE) |
| Cookie `Path=/api` | Cookie only sent to `/api/*` paths |

**Why `SameSite=None` doesn't protect against CSRF:**
`SameSite=None` means the cookie IS sent on cross-origin requests — that's why we need it (our frontend IS cross-origin). But it also means any malicious site could make requests with the cookie. The **Origin header check** is the actual CSRF protection.

```csharp
// Middleware for mutating requests
if (HttpMethods.IsPost(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method))
{
    var origin = request.Headers.Origin.ToString();
    if (origin != "https://app.profily.dev")
        return Results.StatusCode(403);
}
```

---

## SSE Authentication

SSE uses the same cookie-based auth. No special handling needed.

```csharp
app.MapGet("/api/events/stream", async (HttpContext ctx, CancellationToken ct) =>
{
    // Session already validated by auth middleware (cookie sent automatically)
    var userId = ctx.GetUserId();

    ctx.Response.ContentType = "text/event-stream";
    ctx.Response.Headers.CacheControl = "no-cache";
    ctx.Response.Headers.Connection = "keep-alive";

    while (!ct.IsCancellationRequested)
    {
        var events = await GetPendingEvents(userId);
        foreach (var e in events)
        {
            await ctx.Response.WriteAsync($"event: {e.Type}\ndata: {e.Json}\n\n", ct);
        }
        await ctx.Response.Body.FlushAsync(ct);
        await Task.Delay(1000, ct);
    }
});
```

---

## Key Decisions Summary

| # | Decision | Rationale |
|---|---|---|
| 1 | HttpOnly cookie + DB sessions (not JWT) | SSE requires cookies. True logout. XSS-immune. Zero frontend auth code. |
| 2 | `SameSite=None; Secure` | Required for cross-origin cookies (Cloudflare Pages → Azure App Service) |
| 3 | Origin header check for CSRF | `SameSite=None` doesn't prevent CSRF. Origin check does. |
| 4 | 7-day sliding expiration | Active users stay logged in. Inactive users expire. |
| 5 | AES-256-GCM for GitHub token | Encrypted at rest. Only decrypted when calling GitHub API. |
| 6 | Sessions in PostgreSQL (not Redis) | One less service. Session lookup adds ~1-2ms per request — acceptable. |
| 7 | `repo` OAuth scope | Needed for GitHub Pages deploy. Same as GitFolio and competitors. |
