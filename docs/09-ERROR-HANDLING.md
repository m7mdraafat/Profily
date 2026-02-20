# Profily — Error Handling Strategy

## Approach: Exception-Based with Typed Exceptions

Expected errors (not found, validation, conflicts) use **typed exceptions** caught by a global middleware. Unexpected errors (bugs, infrastructure failures) are caught as generic exceptions and return a safe 500 response.

### Why This Approach

| Approach | Pros | Cons | Verdict |
|---|---|---|---|
| Exception-based | Clean handler code, well-understood in .NET | Exceptions for control flow | **Best for 15-endpoint CRUD API** |
| Result pattern | Explicit, composable | Verbose, every caller handles Result | Overkill for our scope |
| Hybrid (Result + exceptions) | Most flexible | Inconsistent — devs unsure which to use | Confusing |

For a focused API with CRUD + GitHub integration, exception-based keeps handlers clean and error handling centralized.

---

## Custom Exception Hierarchy

All live in **Profily.Core** (no external dependencies):

```
ProfilyException (abstract base)
├── NotFoundException              → 404
├── ValidationException            → 400
├── ConflictException              → 409
├── SessionExpiredException        → 401
├── RateLimitedException           → 429
├── GitHubApiException             → 502 (or 429 if rate limited)
└── DeploymentException            → 502
```

### Base Exception

```csharp
public abstract class ProfilyException : Exception
{
    public string Code { get; }
    public int HttpStatus { get; }
    public Dictionary<string, object>? Details { get; }

    protected ProfilyException(
        string code,
        string message,
        int httpStatus,
        Dictionary<string, object>? details = null)
        : base(message)
    {
        Code = code;
        HttpStatus = httpStatus;
        Details = details;
    }
}
```

### Typed Exceptions

```csharp
public class NotFoundException : ProfilyException
{
    public NotFoundException(string entity, object id)
        : base("NOT_FOUND", $"{entity} with id '{id}' not found", 404) { }
}

public class ValidationException : ProfilyException
{
    public ValidationException(string message, Dictionary<string, string>? fieldErrors = null)
        : base("VALIDATION_ERROR", message, 400,
            fieldErrors?.ToDictionary(x => x.Key, x => (object)x.Value)) { }

    public ValidationException(Dictionary<string, string> fieldErrors)
        : this("One or more validation errors occurred", fieldErrors) { }
}

public class ConflictException : ProfilyException
{
    public ConflictException(string message)
        : base("CONFLICT", message, 409) { }
}

public class SessionExpiredException : ProfilyException
{
    public SessionExpiredException()
        : base("SESSION_EXPIRED", "Your session has expired. Please sign in again.", 401) { }
}

public class RateLimitedException : ProfilyException
{
    public int RetryAfterSeconds { get; }

    public RateLimitedException(int retryAfterSeconds = 60)
        : base("RATE_LIMITED", $"Too many requests. Try again in {retryAfterSeconds} seconds.", 429,
            new Dictionary<string, object> { ["retryAfter"] = retryAfterSeconds })
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}

public class GitHubApiException : ProfilyException
{
    public int GitHubStatus { get; }
    public int? RateLimitRemaining { get; }
    public DateTimeOffset? RateLimitReset { get; }

    public GitHubApiException(
        string message,
        int githubStatus,
        int? rateLimitRemaining = null,
        DateTimeOffset? rateLimitReset = null)
        : base(
            "GITHUB_API_ERROR",
            message,
            githubStatus == 401 ? 401 : githubStatus == 403 ? 429 : 502,
            new Dictionary<string, object>
            {
                ["githubStatus"] = githubStatus,
                ["rateLimitRemaining"] = rateLimitRemaining ?? -1
            })
    {
        GitHubStatus = githubStatus;
        RateLimitRemaining = rateLimitRemaining;
        RateLimitReset = rateLimitReset;
    }
}

public class DeploymentException : ProfilyException
{
    public string Step { get; }

    public DeploymentException(string step, string message, Exception? inner = null)
        : base("DEPLOYMENT_FAILED", message, 502,
            new Dictionary<string, object> { ["step"] = step })
    {
        Step = step;
    }
}
```

---

## Error Response Format

Every error returns a consistent JSON shape:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable message for the UI",
    "details": { }
  }
}
```

### Examples

**404 Not Found:**
```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "Portfolio with id 'abc-123' not found",
    "details": null
  }
}
```

**400 Validation:**
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": {
      "templateId": "Template 'invalid-id' does not exist",
      "selectedProjectIds": "At least 1 project must be selected"
    }
  }
}
```

**409 Conflict:**
```json
{
  "error": {
    "code": "CONFLICT",
    "message": "You already have a portfolio. Update it instead."
  }
}
```

**429 Rate Limited (our API):**
```json
{
  "error": {
    "code": "RATE_LIMITED",
    "message": "Too many requests. Try again in 45 seconds.",
    "details": {
      "retryAfter": 45
    }
  }
}
```

**429 GitHub Rate Limited:**
```json
{
  "error": {
    "code": "GITHUB_API_ERROR",
    "message": "GitHub API rate limit exceeded",
    "details": {
      "githubStatus": 403,
      "rateLimitRemaining": 0
    }
  }
}
```

**502 Deployment Failed:**
```json
{
  "error": {
    "code": "DEPLOYMENT_FAILED",
    "message": "Failed to push files to GitHub",
    "details": {
      "step": "git_push"
    }
  }
}
```

**500 Internal Error (bugs — never expose internals):**
```json
{
  "error": {
    "code": "INTERNAL_ERROR",
    "message": "An unexpected error occurred. Please try again."
  }
}
```

---

## Global Error Handling Middleware

Lives in **Profily.Api** — catches all exceptions and produces consistent responses:

```csharp
public static class ErrorHandlingMiddleware
{
    public static void UseErrorHandling(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            try
            {
                await next();
            }
            catch (ProfilyException ex)
            {
                // Expected errors — log via canonical line, return structured response
                var log = ctx.RequestServices.GetRequiredService<RequestLog>();
                log.Set("error", ex.Message)
                   .Set("error.code", ex.Code)
                   .Set("status", "failed");

                if (ex is GitHubApiException ghEx)
                {
                    log.Set("github_status", ghEx.GitHubStatus)
                       .Set("rate_limit_remaining", ghEx.RateLimitRemaining ?? -1);
                }

                if (ex is DeploymentException deployEx)
                {
                    log.Set("deploy_step", deployEx.Step);
                }

                ctx.Response.StatusCode = ex.HttpStatus;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = ex.Code,
                        message = ex.Message,
                        details = ex.Details
                    }
                });
            }
            catch (Exception ex)
            {
                // Unexpected errors — log full exception, return safe response
                var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                    ctx.Request.Method, ctx.Request.Path);

                var log = ctx.RequestServices.GetRequiredService<RequestLog>();
                log.Set("error", ex.Message)
                   .Set("error.type", ex.GetType().Name)
                   .Set("status", "failed");

                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = "INTERNAL_ERROR",
                        message = "An unexpected error occurred. Please try again."
                    }
                });
            }
        });
    }
}
```

---

## Usage in Service Layer

Services throw typed exceptions. No try/catch in endpoints.

```csharp
public class PortfolioService
{
    public async Task<Portfolio> GetAsync(Guid userId, Guid portfolioId)
    {
        var portfolio = await _db.Portfolios
            .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId);

        if (portfolio is null)
            throw new NotFoundException("Portfolio", portfolioId);

        return portfolio;
    }

    public async Task<Portfolio> CreateAsync(Guid userId, CreatePortfolioRequest req)
    {
        // Validation
        if (string.IsNullOrEmpty(req.TemplateId))
            throw new ValidationException(new() { ["templateId"] = "Template is required" });

        // Conflict check
        var exists = await _db.Portfolios.AnyAsync(p => p.UserId == userId);
        if (exists)
            throw new ConflictException("You already have a portfolio. Update it instead.");

        // Foreign key check
        var template = await _db.Templates.FindAsync(req.TemplateId)
            ?? throw new NotFoundException("Template", req.TemplateId);

        var portfolio = new Portfolio { UserId = userId, TemplateId = req.TemplateId };
        _db.Portfolios.Add(portfolio);
        await _db.SaveChangesAsync();

        return portfolio;
    }

    public async Task<DeploymentResult> DeployAsync(Guid userId, Guid portfolioId)
    {
        var portfolio = await GetAsync(userId, portfolioId);
        var token = await _tokenService.DecryptAsync(userId);

        try
        {
            var files = await _renderer.GenerateAsync(portfolio);
            return await _github.PushToPages(token, username, files);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new GitHubApiException("GitHub API rate limit exceeded", 403,
                rateLimitRemaining: 0);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new GitHubApiException("GitHub token expired. Please sign in again.", 401);
        }
        catch (HttpRequestException ex)
        {
            throw new DeploymentException("git_push",
                $"Failed to push to GitHub: {ex.Message}", ex);
        }
    }
}
```

## Usage in Endpoints

Endpoints stay clean — zero error handling code:

```csharp
app.MapGet("/api/portfolios/{id}", async (Guid id, PortfolioService svc, HttpContext ctx) =>
{
    var userId = ctx.GetUserId();
    var portfolio = await svc.GetAsync(userId, id);
    return Results.Ok(portfolio);
});

app.MapPost("/api/portfolios", async (CreatePortfolioRequest req, PortfolioService svc, HttpContext ctx) =>
{
    var userId = ctx.GetUserId();
    var portfolio = await svc.CreateAsync(userId, req);
    return Results.Created($"/api/portfolios/{portfolio.Id}", portfolio);
});

app.MapPost("/api/portfolios/{id}/deploy", async (Guid id, PortfolioService svc, HttpContext ctx) =>
{
    var userId = ctx.GetUserId();
    var result = await svc.DeployAsync(userId, id);
    return Results.Ok(result);
});
```

---

## Error Flow Diagram

```
Endpoint calls Service method
    │
    ├── Happy path → returns data → Results.Ok(data)
    │
    └── Error → throws ProfilyException
              │
              ▼
        Error Middleware catches
              │
              ├── Sets RequestLog fields (error, code, status=failed)
              ├── Sets HTTP status from exception
              ├── Writes JSON error response
              │
              ▼
        Canonical Line Middleware emits:
            portfolio.create | user_id=abc error=Portfolio already exists error.code=CONFLICT status=failed duration_ms=45 http.status=409
```

---

## Frontend Error Handling

React consumes errors consistently:

```typescript
// API client with error parsing
async function apiCall<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`/api${path}`, {
    ...options,
    credentials: 'include',  // send session cookie
    headers: { 'Content-Type': 'application/json', ...options?.headers },
  });

  if (!res.ok) {
    const body = await res.json();
    const error = body.error;

    switch (error.code) {
      case 'SESSION_EXPIRED':
        window.location.href = '/login';
        break;
      case 'RATE_LIMITED':
        toast.warning(`Too many requests. Retry in ${error.details.retryAfter}s`);
        break;
      case 'VALIDATION_ERROR':
        throw new ValidationError(error.message, error.details);
      case 'NOT_FOUND':
        throw new NotFoundError(error.message);
      default:
        toast.error(error.message);
        throw new ApiError(error.code, error.message);
    }
  }

  return res.json();
}

// Usage — clean
const portfolio = await apiCall<Portfolio>('/portfolios', {
  method: 'POST',
  body: JSON.stringify({ templateId: '3d-purple', selectedProjectIds: [...] }),
});
```

---

## Rules

1. **Services throw, endpoints don't catch** — middleware handles everything
2. **One exception type per HTTP status** — predictable mapping
3. **Never expose internals in 500 errors** — log the full exception server-side, return generic message to client
4. **Always include `error.code`** — frontend switches on code, not message (messages can change)
5. **Canonical line includes error context** — no separate error log needed
6. **GitHub API errors map to our exceptions** — never let raw HttpRequestException reach the client
