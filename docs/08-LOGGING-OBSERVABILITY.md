# Profily — Logging & Observability

## Approach: Canonical Log Lines

Inspired by [Stripe's canonical log lines](https://stripe.com/blog/canonical-log-lines). One rich log line per request — contains everything needed to understand what happened.

### Why Canonical Lines

| Approach | Lines Per Request | Debug Experience | Log Cost |
|---|---|---|---|
| Traditional (many lines) | 5-20 | Grep across lines, mentally stitch | High |
| **Canonical lines (ours)** | **1** | **One line = full story** | **Low** |
| Wide events (Honeycomb) | 1 | Best, but needs $$ tooling | Medium |

---

## Log Structure

Every canonical log line follows this format:

```
{feature}.{action} | key1={Value1} key2={Value2} duration_ms={Duration}
```

### Mandatory Fields (Every Request)

| Field | Description |
|---|---|
| `event` | Event name: `auth.signup`, `deploy.completed` |
| `http.method` | GET, POST, PATCH, DELETE |
| `http.path` | /api/users/me |
| `http.status` | 200, 400, 500 |
| `user_id` | Authenticated user ID (or "anonymous") |
| `correlation_id` | Unique ID tying logs within the same request |
| `duration_ms` | Total request duration |
| `status` | `success` or `failed` |

### Feature-Specific Fields (Added During Request Execution)

Fields are accumulated as the request progresses through the handler. The canonical line is emitted once at the end.

---

## Canonical Lines by Feature

### Auth

```
auth.signup | user_id=abc username=m7mdraafat is_new_user=true repos_count=27 repos_scanned=10 skills_detected=12 top_skill=C#(95%) session_created=true status=success duration_ms=4500 http.status=200

auth.login | user_id=abc username=m7mdraafat is_new_user=false days_since_last=3 session_created=true status=success duration_ms=800 http.status=200

auth.logout | user_id=abc session_age_hours=48 status=success duration_ms=15 http.status=204

auth.failed | error=invalid_code code_length=0 status=failed duration_ms=200 http.status=400
```

### User & Sync

```
user.fetch | user_id=abc username=m7mdraafat skills_count=12 projects_count=27 has_portfolio=true status=success duration_ms=25 http.status=200

user.update | user_id=abc fields_changed=[bio,linkedinUrl] skills_reordered=true skills_added=1 skills_removed=2 status=success duration_ms=80 http.status=200

user.sync | user_id=abc username=m7mdraafat new_repos=2 removed_repos=0 repos_scanned=10 config_files_parsed=8 updated_skills=3 github_api_calls=22 rate_limit_remaining=4800 status=success duration_ms=6200 http.status=200

user.sync | user_id=abc username=m7mdraafat error=github_api_500 github_api_calls=5 status=failed duration_ms=3100 http.status=502
```

### Templates

```
template.gallery | user_authenticated=false templates_count=3 status=success duration_ms=12 http.status=200

template.demo | template_id=3d-purple cache_hit=true status=success duration_ms=8 http.status=200

template.preview | user_id=abc template_id=3d-purple sections_visible=5 skills_count=12 projects_count=6 cache_hit=false render_duration_ms=120 status=success duration_ms=150 http.status=200
```

### Portfolio

```
portfolio.created | user_id=abc portfolio_id=xyz template_id=3d-purple selected_projects=6 status=success duration_ms=45 http.status=201

portfolio.updated | user_id=abc portfolio_id=xyz changed=[sections.services.visible,hero.greeting] template_changed=false status=success duration_ms=60 http.status=200

portfolio.preview | user_id=abc portfolio_id=xyz template_id=3d-purple sections_visible=5 cache_hit=false render_duration_ms=110 status=success duration_ms=130 http.status=200
```

### Deployment

```
deploy.completed | user_id=abc portfolio_id=xyz template_id=3d-purple url=https://m7mdraafat.github.io commit_sha=def456 repo_created=false files_count=4 total_size_kb=185 pages_enabled=true github_api_calls=5 status=success duration_ms=8200 http.status=200

deploy.failed | user_id=abc portfolio_id=xyz step=git_push error=rate_limited github_status=403 rate_limit_remaining=0 rate_limit_reset=2026-02-20T13:00:00Z status=failed duration_ms=3100 http.status=502

deploy.failed | user_id=abc portfolio_id=xyz step=repo_create error=token_expired github_status=401 status=failed duration_ms=1200 http.status=401
```

---

## Implementation

### RequestLog — Accumulates Fields During a Request

```csharp
public class RequestLog : IDisposable
{
    private readonly Dictionary<string, object> _fields = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public RequestLog Set(string key, object value)
    {
        _fields[key] = value;
        return this;
    }

    public Dictionary<string, object> Build()
    {
        _fields["duration_ms"] = _stopwatch.ElapsedMilliseconds;
        return _fields;
    }

    public void Dispose() => _stopwatch.Stop();
}

// Register as scoped (one per request)
builder.Services.AddScoped<RequestLog>();
```

### Canonical Line Middleware

```csharp
app.Use(async (ctx, next) =>
{
    var log = ctx.RequestServices.GetRequiredService<RequestLog>();

    log.Set("http.method", ctx.Request.Method)
       .Set("http.path", ctx.Request.Path.ToString())
       .Set("correlation_id", Guid.NewGuid().ToString("N")[..12]);

    // Add user context if authenticated
    if (ctx.Items.TryGetValue("UserId", out var userId))
        log.Set("user_id", userId!);
    else
        log.Set("user_id", "anonymous");

    try
    {
        await next();
        log.Set("http.status", ctx.Response.StatusCode)
           .Set("status", ctx.Response.StatusCode < 400 ? "success" : "failed");
    }
    catch (Exception ex)
    {
        log.Set("http.status", 500)
           .Set("status", "failed")
           .Set("error", ex.Message)
           .Set("error.type", ex.GetType().Name);
        throw;
    }
    finally
    {
        var fields = log.Build();
        var eventName = fields.GetValueOrDefault("event", "http.request");

        // Emit ONE canonical line
        _logger.LogInformation("{Event} | {Fields}",
            eventName,
            string.Join(" ", fields.Select(f => $"{f.Key}={f.Value}")));
    }
});
```

### Usage in Handlers

```csharp
app.MapPost("/api/portfolios/{id}/deploy", async (Guid id, HttpContext ctx) =>
{
    var log = ctx.RequestServices.GetRequiredService<RequestLog>();
    log.Set("event", "deploy");

    var portfolio = await GetPortfolioAsync(id);
    log.Set("portfolio_id", portfolio.Id)
       .Set("template_id", portfolio.TemplateId);

    var files = await GenerateFilesAsync(portfolio);
    log.Set("files_count", files.Count)
       .Set("total_size_kb", files.TotalSizeKb);

    var result = await PushToGitHubAsync(user, files);
    log.Set("commit_sha", result.CommitSha)
       .Set("repo_created", result.RepoCreated)
       .Set("pages_enabled", result.PagesEnabled)
       .Set("url", result.Url)
       .Set("github_api_calls", result.ApiCalls);

    // Canonical line emitted by middleware with ALL accumulated fields
    return Results.Ok(result);
});
```

---

## Domain Events (Business Analytics)

Separate from logs — stored in PostgreSQL for product analytics:

```sql
CREATE TABLE domain_events (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    type        VARCHAR(100) NOT NULL,
    user_id     UUID REFERENCES users(id),
    data        JSONB DEFAULT '{}',
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

| Event | Data | Business Question |
|---|---|---|
| `user.signed_up` | repos_count | How active are new users on GitHub? |
| `template.selected` | templateId | Which templates are popular? |
| `portfolio.deployed` | templateId, seconds_since_signup | Conversion speed? |
| `portfolio.redeployed` | days_since_last | How often do users update? |
| `skills.user_edited` | added[], removed[] | Is auto-detection accurate? |
| `section.hidden` | section_name | Which sections are unwanted? |

---

## Log Levels Policy

| Level | In Production | When |
|---|---|---|
| **Critical** | Always on | App is broken (DB down, missing config) |
| **Error** | Always on | Operation failed, user affected |
| **Warning** | Always on | Degraded state, recovered automatically |
| **Information** | Always on | **Canonical lines live here** |
| **Debug** | Off | Development only (individual step details) |
| **Trace** | Off | Never in production |

In development, enable Debug for step-by-step tracing alongside the canonical line.

---

## Querying (Application Insights KQL)

```kusto
// All failed deployments with full context
traces
| where message startswith "deploy.failed"
| project timestamp, customDimensions

// Average time from signup to first deploy
customEvents
| where name == "portfolio.deployed"
| extend secs = toint(customDimensions.seconds_since_signup)
| summarize avg(secs), percentile(secs, 95)

// Slowest requests today
traces
| where toint(customDimensions.duration_ms) > 5000
| where timestamp > ago(24h)
| project timestamp, message

// Users whose skill detection found < 3 skills
traces
| where message startswith "auth.signup"
| where toint(customDimensions.skills_detected) < 3
| project timestamp, customDimensions.username, customDimensions.skills_detected

// Most popular templates
customEvents
| where name == "template.selected"
| summarize count() by tostring(customDimensions.templateId)

// GitHub API rate limit trend
traces
| where customDimensions.rate_limit_remaining != ""
| extend remaining = toint(customDimensions.rate_limit_remaining)
| summarize min(remaining) by bin(timestamp, 1h)
```

---

## Destination

| Environment | Destination | Retention |
|---|---|---|
| Development | Console (structured, colored) | Session |
| Production | Application Insights (free 5 GB/month) | 90 days |
| Domain Events | PostgreSQL `domain_events` table | Forever |
