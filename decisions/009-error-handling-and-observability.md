# ADR-009: Error Handling & Observability

**Status:** Accepted  
**Date:** 2026-03-27  

---

## Context

Need standardized error handling, validation, structured logging, tracing, metrics, and health checks for the .NET API. Budget: $0 (App Insights free 5 GB/month).

## Constraints

- Single App Service instance (no distributed tracing across services — but OTEL is ready for when we add more)
- 5 GB/month App Insights free tier (~5K users at moderate logging)
- Must not log PII (emails, tokens, bio text)

---

## 1. Error Handling

### Error Response Format: RFC 7807 Problem Details

Built into .NET Minimal API. Consistent, standards-compliant error responses.

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Portfolio with id '550e8400-...' was not found"
}
```

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "errors": {
    "name": ["Name is required"],
    "startDate": ["Start date must be in the past"]
  }
}
```

### Custom Exceptions + Global Middleware

Services in Infrastructure throw typed exceptions. Global middleware catches and maps to Problem Details.

```csharp
// Profily.Core/Exceptions/
public class NotFoundException : Exception
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with id '{id}' was not found") { }
}

public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }
    public ValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred") => Errors = errors;
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ProPlanRequiredException : Exception
{
    public ProPlanRequiredException()
        : base("This feature requires a Pro plan") { }
}
```

### Exception → Problem Details Mapping

```csharp
// Global error handling middleware
app.UseExceptionHandler(error =>
{
    error.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        var (statusCode, title) = exception switch
        {
            NotFoundException     => (404, "Not Found"),
            ValidationException   => (400, "Validation Error"),
            ConflictException     => (409, "Conflict"),
            ProPlanRequiredException => (403, "Pro Plan Required"),
            _                     => (500, "Internal Server Error")
        };

        // Log 5xx as Error, 4xx as Warning
        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            logger.LogWarning("Client error {StatusCode} on {Method} {Path}: {Message}",
                statusCode, context.Request.Method, context.Request.Path, exception?.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = statusCode < 500 ? exception?.Message : "An unexpected error occurred"
        };

        // Add validation errors if applicable
        if (exception is ValidationException ve)
            problem.Extensions["errors"] = ve.Errors;

        await context.Response.WriteAsJsonAsync(problem);
    });
});
```

**Key:** 5xx errors never expose exception details to the client. 4xx errors include the user-facing message.

### Validation: FluentValidation

```csharp
// NuGet: FluentValidation.DependencyInjectionExtensions

public class CreateExperienceValidator : AbstractValidator<CreateExperienceRequest>
{
    public CreateExperienceValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty().LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after start date");
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
```

**Validation middleware** (runs before endpoint logic):

```csharp
// Extension method for endpoints
app.MapPost("/api/experiences", async (
    CreateExperienceRequest request,
    IValidator<CreateExperienceRequest> validator,
    ExperienceService service) =>
{
    var result = await validator.ValidateAsync(request);
    if (!result.IsValid)
        throw new ValidationException(result.ToDictionary());

    return await service.CreateAsync(request);
});
```

---

## 2. Observability Stack

### Architecture

```
.NET API
  │
  ├── ILogger<T>           → Logs (write in code)
  │
  └── OpenTelemetry SDK    → Traces + Metrics + Log export
          │
          ▼
    Azure Monitor Exporter
    (Azure.Monitor.OpenTelemetry.AspNetCore)
          │
          ▼
    App Insights (Free 5 GB/month)
          │
          ├── Logs        → search, filter, query (KQL)
          ├── Traces      → request flow, dependencies, latency
          ├── Metrics     → counters, histograms, dashboards
          └── Alerts      → email on errors, slow requests
```

### Setup

```csharp
// Program.cs
builder.Services.AddOpenTelemetry().UseAzureMonitor();
```

One line. Auto-instruments:
- HTTP requests (incoming + outgoing)
- EF Core queries
- HttpClient calls (GitHub API, Groq, Paymob, R2)
- ILogger output → exported as traces

**NuGet:** `Azure.Monitor.OpenTelemetry.AspNetCore`

**Config:**
```json
// appsettings.json
{
  "AzureMonitor": {
    "ConnectionString": "<app-insights-connection-string>"
  }
}
```

Connection string stored as environment variable in App Service — not in source code.

---

## 3. Logging

### Log Levels

| Level | What | Production default |
|---|---|---|
| `Debug` | Developer detail (SQL, cache hits, serialization) | OFF |
| `Information` | Normal operations | ON |
| `Warning` | Unexpected but handled | ON |
| `Error` | Something failed | ON |
| `Critical` | App is broken | ON |

### What to Log

| Event | Level | Example |
|---|---|---|
| HTTP request completed | `Information` | `"HTTP GET /api/users/me → 200 in 45ms"` (auto by OTEL) |
| User login | `Information` | `"User {Username} logged in"` |
| User logout | `Information` | `"User {Username} logged out"` |
| Portfolio published | `Information` | `"Portfolio published for {Username} in {Duration}ms"` |
| GitHub Pages exported | `Information` | `"GitHub Pages deployed for {Username}"` |
| LLM inference completed | `Information` | `"Skill inference for {Username}: {Provider}, {TokenCount} tokens, {Duration}ms, {SkillCount} skills"` |
| GitHub API call | `Information` | `"GitHub API {Endpoint} → {StatusCode}, rate limit remaining: {Remaining}"` |
| Paymob webhook received | `Information` | `"Paymob webhook: {EventType} for user {UserId}, tx: {TransactionId}"` |
| Slow request (>2s) | `Warning` | Auto-detected via middleware |
| Validation failure | `Warning` | `"Validation failed on {Endpoint}: {Errors}"` |
| LLM fallback triggered | `Warning` | `"Groq rate limited, falling back to Gemini for {Username}"` |
| GitHub rate limit low (<100) | `Warning` | `"GitHub rate limit low: {Remaining}/5000 for {Username}"` |
| Unhandled exception | `Error` | Full stack trace + request context |
| Webhook HMAC verification failed | `Error` | `"Paymob HMAC verification failed from {IP}"` |
| DB connection failed | `Critical` | `"Cannot connect to PostgreSQL: {Error}"` |
| Missing encryption key | `Critical` | `"GITHUB_TOKEN_ENCRYPTION_KEY not configured"` |
| Missing required config | `Critical` | `"PAYMOB_HMAC_SECRET not configured"` |

### What NOT to Log

- Request/response bodies (PII: user bio, email, descriptions)
- GitHub access tokens (encrypted or raw)
- Paymob HMAC secrets
- Full webhook payloads (stored in `payment_events` table, not logs)
- User passwords (we don't have any, but principle stands)

### Logging Configuration

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "System.Net.Http.HttpClient": "Warning"
    }
  }
}
```

Suppress noisy framework logs. Only our application code logs at `Information`.

---

## 4. Custom Metrics

Registered via OpenTelemetry's `Meter` API:

```csharp
public static class ProfilyMetrics
{
    private static readonly Meter Meter = new("Profily.Api");

    // Counters
    public static readonly Counter<long> LlmInferences =
        Meter.CreateCounter<long>("profily.llm.inferences", description: "Total LLM inference calls");

    public static readonly Counter<long> LlmTokensUsed =
        Meter.CreateCounter<long>("profily.llm.tokens_used", description: "Total LLM tokens consumed");

    public static readonly Counter<long> PortfolioPublishes =
        Meter.CreateCounter<long>("profily.portfolio.publishes", description: "Total portfolio publishes");

    public static readonly Counter<long> GitHubApiCalls =
        Meter.CreateCounter<long>("profily.github.api_calls", description: "Total GitHub API calls");

    // Histograms
    public static readonly Histogram<double> LlmInferenceDuration =
        Meter.CreateHistogram<double>("profily.llm.inference_duration_ms", "ms", "LLM inference latency");

    public static readonly Histogram<double> PublishDuration =
        Meter.CreateHistogram<double>("profily.portfolio.publish_duration_ms", "ms", "Portfolio publish latency");
}
```

**Usage:**

```csharp
var sw = Stopwatch.StartNew();
var skills = await llmService.InferAsync(repos, ct);
sw.Stop();

ProfilyMetrics.LlmInferences.Add(1, new("provider", "groq"));
ProfilyMetrics.LlmTokensUsed.Add(totalTokens);
ProfilyMetrics.LlmInferenceDuration.Record(sw.ElapsedMilliseconds);
```

### Metric Dashboard (App Insights)

| Metric | Visualization | Alert threshold |
|---|---|---|
| `profily.llm.inferences` by provider | Pie chart (groq vs gemini vs fallback) | Fallback > 20% of total |
| `profily.llm.tokens_used` | Daily cumulative line chart | > 400K/day (approaching Groq TPD limit) |
| `profily.llm.inference_duration_ms` p95 | Line chart | p95 > 3,000ms |
| `profily.portfolio.publishes` | Counter per day | — |
| `profily.github.api_calls` | Counter per hour | > 4,000/hour (approaching limit) |
| HTTP request duration p95 | Line chart | p95 > 5,000ms |
| HTTP 5xx rate | Line chart | > 1% of requests |

---

## 5. Health Checks

### Endpoints

```
GET /health          → 200 OK (lightweight, for load balancer / uptime monitor)
GET /health/ready    → 200 OK / 503 (detailed, for debugging)
```

### Setup

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql", tags: ["ready"])
    .AddCheck<R2HealthCheck>("cloudflare-r2", tags: ["ready"])
    .AddCheck<PaymobHealthCheck>("paymob", tags: ["ready"])
    .AddCheck<GitHubApiHealthCheck>("github-api", tags: ["ready"])
    .AddCheck<GroqHealthCheck>("groq", tags: ["ready"]);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false  // No checks, just returns 200
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteDetailedResponse
});
```

### Component Health Status

| Component | Check | Healthy | Degraded | Unhealthy |
|---|---|---|---|---|
| **PostgreSQL** | `SELECT 1` | ✅ responds | — | ❌ Connection failed |
| **Cloudflare R2** | `HEAD` known object | ✅ 200 | — | ❌ Timeout/error |
| **Paymob** | `GET` auth endpoint | ✅ responds | ⚠️ Slow (>2s) | ❌ Unreachable |
| **GitHub API** | Rate limit endpoint | ✅ remaining > 100 | ⚠️ remaining < 100 | ❌ Unreachable |
| **Groq** | Lightweight completion | ✅ responds | ⚠️ Slow (>2s) | ❌ Unreachable |

### Overall Status Logic

- Any `Unhealthy` where component is **PostgreSQL** → response is `503 Service Unavailable`
- Any `Unhealthy` where component is **non-critical** (R2, Paymob, GitHub, Groq) → response is `200 OK` with `Degraded` status
- All `Healthy` → `200 OK`

### Response Format

```json
{
  "status": "Degraded",
  "totalDuration": "00:00:00.125",
  "checks": {
    "postgresql": { "status": "Healthy", "duration": "00:00:00.012" },
    "cloudflare-r2": { "status": "Healthy", "duration": "00:00:00.045" },
    "paymob": { "status": "Healthy", "duration": "00:00:00.089" },
    "github-api": { "status": "Degraded", "description": "Rate limit low: 45/5000", "duration": "00:00:00.067" },
    "groq": { "status": "Healthy", "duration": "00:00:00.102" }
  }
}
```

---

## Key Decisions Summary

| # | Decision | Rationale |
|---|---|---|
| 1 | RFC 7807 Problem Details | .NET standard, built into Minimal API. Frontend handles errors uniformly. |
| 2 | Custom exceptions + global middleware | Clean separation. Services throw, middleware maps to HTTP responses. |
| 3 | 5xx never expose details to client | Security. Internal errors return generic message. |
| 4 | FluentValidation | Complex nested objects (experience, education, portfolio). Cleaner than manual if/else. |
| 5 | ILogger + OpenTelemetry + App Insights | Simplest stack. One NuGet, one line setup. Logs + traces + metrics out of the box. |
| 6 | No Serilog | ILogger is sufficient for single-service app. OTEL handles export. Less dependencies. |
| 7 | Custom metrics via OTEL Meter API | Track LLM usage, token consumption, publish latency — business-critical metrics. |
| 8 | Health checks with degraded vs unhealthy | DB down = unhealthy (503). External service down = degraded (200). App still works in degraded mode. |
| 9 | Don't log PII | No request bodies, no tokens, no emails in logs. |
