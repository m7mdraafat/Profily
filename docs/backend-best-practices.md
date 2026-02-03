# Backend Best Practices

This document outlines the production best practices applied in the Profily backend implementation.

---

## Table of Contents

1. [Architecture & Project Structure](#architecture--project-structure)
2. [Configuration Management](#configuration-management)
3. [Authentication & Authorization](#authentication--authorization)
4. [Security](#security)
5. [Database Design](#database-design)
6. [External API Integration](#external-api-integration)
7. [Caching Strategy](#caching-strategy)
8. [API Design](#api-design)
9. [Error Handling](#error-handling)
10. [Logging & Observability](#logging--observability)
11. [Dependency Injection](#dependency-injection)

---

## Architecture & Project Structure

### Clean Architecture Layers

```
Profily.sln
├── Profily.Api          # Presentation layer (endpoints, middleware)
├── Profily.Core         # Domain layer (models, interfaces, options)
└── Profily.Infrastructure  # Infrastructure layer (data access, external services)
```

| Principle | Implementation |
|-----------|----------------|
| **Dependency Inversion** | Core defines interfaces; Infrastructure implements them |
| **Separation of Concerns** | Endpoints handle HTTP; Services handle business logic |
| **Single Responsibility** | Each class has one reason to change |

### Project Dependencies

```
Api → Infrastructure → Core
Api → Core
```

- Core has zero external dependencies (pure domain)
- Infrastructure depends on Core for interfaces
- Api depends on both for composition

---

## Configuration Management

### Options Pattern

All configuration uses strongly-typed options classes:

```csharp
// ✅ Good: Strongly-typed, validated
public sealed class GitHubOptions
{
    public const string SectionName = "GitHub";
    
    [Required]
    public required string ClientId { get; init; }
    
    [Required]
    public required string ClientSecret { get; init; }
}

// ❌ Bad: Magic strings, no validation
var clientId = configuration["GitHub:ClientId"];
```

### Options Classes Location

All options in `Profily.Core/Options/`:
- `CosmosDbOptions.cs` - Database connection settings
- `GitHubOptions.cs` - OAuth configuration
- `CorsOptions.cs` - Cross-origin settings

### Secrets Management

| Environment | Method |
|-------------|--------|
| Development | `dotnet user-secrets` |
| Production | Azure Key Vault / Environment Variables |

```bash
# Development secrets (never in source control)
dotnet user-secrets set "GitHub:ClientSecret" "xxx"
dotnet user-secrets set "CosmosDb:AccountKey" "xxx"
```

### Configuration Files

| File | Purpose | In Git? |
|------|---------|---------|
| `appsettings.json` | Non-sensitive defaults | ❌ (gitignored) |
| `appsettings.template.json` | Template with placeholders | ✅ |
| `appsettings.Development.json` | Dev overrides | ❌ (gitignored) |

---

## Authentication & Authorization

### Authentication Flow

```
User → Frontend → /api/auth/github → GitHub OAuth → Callback → Cookie Set → Frontend
```

### Cookie Configuration

```csharp
options.Cookie.Name = "Profily.Auth";
options.Cookie.HttpOnly = true;           // Prevents XSS access
options.Cookie.SecurePolicy = Always;     // HTTPS only
options.Cookie.SameSite = Lax;            // CSRF protection + OAuth compat
options.ExpireTimeSpan = TimeSpan.FromDays(7);
options.SlidingExpiration = true;         // Auto-extend on activity
```

| Setting | Value | Reason |
|---------|-------|--------|
| `HttpOnly` | `true` | JavaScript cannot access cookie |
| `Secure` | `Always` | Cookie only sent over HTTPS |
| `SameSite` | `Lax` | Blocks cross-site POST, allows OAuth redirects |
| `SlidingExpiration` | `true` | Active users stay logged in |

### Claims Strategy

Minimal claims in cookie, fetch from DB for details:

```csharp
// Stored in cookie (lightweight)
new Claim(ClaimTypes.NameIdentifier, user.Id),
new Claim(ClaimTypes.Name, user.GitHubUsername),
new Claim("github_id", user.GitHubId.ToString())

// Full user data fetched from database when needed
var dbUser = await authService.GetUserByIdAsync(userId);
```

### API Authentication Behavior

```csharp
// Return 401/403 JSON instead of redirect for API calls
options.Events = new CookieAuthenticationEvents
{
    OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    }
};
```

---

## Security

### Open Redirect Prevention

```csharp
private static string? ValidateReturnUrl(string? returnUrl)
{
    // Allow relative URLs (starts with single /)
    if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
        return returnUrl;

    // Allow known safe origins only
    var safeOrigins = new[] { "http://localhost:5183" };
    // Validate against whitelist...
}
```

### CORS Configuration

```csharp
policy
    .WithOrigins(corsOptions.AllowedOrigins)  // Explicit whitelist
    .AllowCredentials()                        // Enable cookies
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));  // Cache preflight
```

| Setting | Purpose |
|---------|---------|
| `AllowCredentials` | Enables cross-origin cookies |
| `WithOrigins` | Whitelist (not `AllowAnyOrigin`) |
| `SetPreflightMaxAge` | Reduces OPTIONS requests |

### Token Storage

GitHub access tokens stored server-side in database:
- Never exposed to frontend
- Encrypted at rest (Cosmos DB)
- Used for GitHub API calls on behalf of user

---

## Database Design

### Cosmos DB Best Practices

#### Partition Strategy

```csharp
// Partition key = /id (user ID)
// All user data in same partition = efficient queries
partitionKey: new PartitionKey(user.Id)
```

#### Document Discrimination

```csharp
// Type field for different document types in same container
public string Type { get; set; } = "user";  // "user", "profileConfig", etc.
```

#### Serialization

```csharp
var clientOptions = new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    }
};
```

### Error Handling in Data Layer

```csharp
try
{
    var response = await _container.ReadItemAsync<User>(id, partitionKey);
    return response.Resource;
}
catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    _logger.LogDebug("User {UserId} not found", userId);
    return null;  // Expected case, not an error
}
```

### Cost Tracking

```csharp
_logger.LogInformation(
    "Upserted user {UserId}, RU cost: {RUCost}", 
    user.Id, 
    response.RequestCharge);  // Track Request Unit consumption
```

---

## External API Integration

### GitHub API with Octokit

We use [Octokit.NET](https://github.com/octokit/octokit.net) - the official GitHub API client:

```csharp
// ✅ Good: Create client per request with user's token
private static GitHubClient CreateClient(string accessToken)
{
    var client = new GitHubClient(new ProductHeaderValue("Profily"))
    {
        Credentials = new Credentials(accessToken)
    };
    return client;
}
```

### User Token Strategy

Use the authenticated user's access token for GitHub API calls:

| Approach | Pros | Cons |
|----------|------|------|
| **User Token** ✅ | Higher rate limits (5000/hr), access to user data | Token per user |
| App Token | Single token | Lower rate limits, no user context |

```csharp
// Get user's token from database
var user = await authService.GetUserByIdAsync(userId);
var repos = await gitHubService.GetUserRepositoriesAsync(user.AccessToken);
```

### Data Storage Strategy

| Data Type | Store in DB? | Reason |
|-----------|--------------|--------|
| Raw GitHub repos/stats | ❌ No | Changes frequently, GitHub is source of truth |
| User's selected projects | ✅ Yes | User's portfolio choices |
| Project customizations | ✅ Yes | Overridden titles, descriptions, images |
| Portfolio configuration | ✅ Yes | Theme, layout, sections |

```csharp
// ❌ Don't store: Fetched from GitHub with caching
public class GitHubRepository { ... }  // Live data
public class GitHubStats { ... }       // Live data

// ✅ Do store: User's selections and customizations
public class SelectedProject
{
    public string UserId { get; set; }        // Partition key
    public long RepoId { get; set; }          // GitHub repo ID
    public string? DisplayTitle { get; set; } // Override
    public bool IsFeatured { get; set; }
}
```

### Model Mapping

Convert external API types to our domain models:

```csharp
// ✅ Good: Map Octokit types to our clean models
private static GitHubRepository MapToGitHubRepository(Repository repo)
{
    return new GitHubRepository
    {
        Id = repo.Id,
        Name = repo.Name,
        FullName = repo.FullName,
        StarsCount = repo.StargazersCount,
        // ... only properties we need
    };
}

// ❌ Bad: Exposing Octokit types directly
return Results.Ok(octokitRepos);  // Leaks implementation details
```

---

## Caching Strategy

### In-Memory Cache for External APIs

Protect against rate limits and improve performance:

```csharp
public class GitHubService : IGitHubService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<List<GitHubRepository>> GetUserRepositoriesAsync(string accessToken)
    {
        var cacheKey = $"repos_{accessToken.GetHashCode()}";

        if (_cache.TryGetValue(cacheKey, out List<GitHubRepository>? cached))
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        // Fetch from GitHub API
        var repos = await FetchFromGitHub(accessToken);
        
        _cache.Set(cacheKey, repos, CacheDuration);
        return repos;
    }
}
```

### Cache Key Design

| Pattern | Example | Use Case |
|---------|---------|----------|
| `{type}_{userId}` | `repos_abc123` | User-specific data |
| `{type}_{owner}_{repo}` | `lang_octokit_octokit.net` | Repository-specific data |

### Cache Duration Guidelines

| Data Type | Duration | Reason |
|-----------|----------|--------|
| User repos | 10 minutes | Balance freshness vs. rate limits |
| User stats | 10 minutes | Aggregate data changes slowly |
| Repo languages | 10 minutes | Rarely changes |
| Static config | 1 hour+ | Configuration rarely changes |

### Rate Limit Protection

```csharp
// Limit expensive operations
var topRepos = ownedRepos
    .OrderByDescending(r => r.StargazersCount)
    .Take(10);  // Only fetch languages for top 10 repos

foreach (var repo in topRepos)
{
    try
    {
        var languages = await client.Repository.GetAllLanguages(repo.Owner.Login, repo.Name);
        // Process...
    }
    catch (RateLimitExceededException ex)
    {
        _logger.LogWarning("GitHub rate limit exceeded. Reset at {ResetTime}", ex.Reset);
        break;  // Stop making requests
    }
}
```

### Service Registration

```csharp
// Register memory cache
services.AddMemoryCache();

// Register GitHub service
services.AddScoped<IGitHubService, GitHubService>();
```

---

## API Design

### Minimal API Organization

Endpoints grouped in static classes with extension methods:

```csharp
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();
        
        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization();
    }
}
```

### OpenAPI Documentation

Every endpoint has:
- `WithName()` - Unique operation ID
- `WithSummary()` - Short description
- `WithDescription()` - Detailed explanation
- `WithTags()` - Grouping in Swagger UI

```csharp
group.MapGet("/me", GetCurrentUser)
    .WithName("GetCurrentUser")
    .WithSummary("Gets current authenticated user")
    .WithDescription("Returns the authenticated user's information. Returns 401 if not authenticated.")
    .RequireAuthorization();
```

### Response DTOs

Never expose domain entities directly:

```csharp
// ✅ Good: Explicit response contract
public sealed record UserInfoResponse
{
    public required string Id { get; init; }
    public required string GitHubUsername { get; init; }
    // AccessToken NOT included - security
}

// ❌ Bad: Exposing domain entity
return Results.Ok(user);  // Leaks AccessToken!
```

---

## Error Handling

### Problem Details for Errors

```csharp
return Results.Problem(
    title: "Authentication Failed",
    detail: "GitHub authentication was not successful.",
    statusCode: StatusCodes.Status401Unauthorized);
```

### Consistent Response Structure

```csharp
// Success
return Results.Ok(new AuthResponse
{
    IsAuthenticated = true,
    User = userInfo
});

// Error
return Results.Json(
    new AuthResponse { IsAuthenticated = false },
    statusCode: StatusCodes.Status401Unauthorized);
```

### HTTP Status Codes

| Code | Usage |
|------|-------|
| `200 OK` | Successful GET/POST |
| `401 Unauthorized` | Not authenticated |
| `403 Forbidden` | Authenticated but not authorized |
| `404 Not Found` | Resource doesn't exist |
| `500 Internal Server Error` | Unhandled exception |

---

## Logging & Observability

### Structured Logging

```csharp
_logger.LogInformation(
    "New user registered: {Username} (ID: {UserId})", 
    username, 
    newUser.Id);

// NOT string concatenation:
// _logger.LogInformation($"New user: {username}");  ❌
```

### Log Levels

| Level | Usage |
|-------|-------|
| `Debug` | Expected failures (user not found) |
| `Information` | Key events (login, registration) |
| `Warning` | Unexpected but handled (delete non-existent) |
| `Error` | Exceptions, failures |

### Context Enrichment

Include relevant IDs and context:

```csharp
_logger.LogInformation(
    "Upserted user {UserId} ({GitHubUsername}), RU cost: {RUCost}",
    user.Id, 
    user.GitHubUsername, 
    response.RequestCharge);
```

---

## Dependency Injection

### Service Lifetimes

| Service | Lifetime | Reason |
|---------|----------|--------|
| `CosmosClient` | Singleton | Thread-safe, connection pooling |
| `CosmosDbService` | Singleton | Stateless, uses singleton client |
| `IMemoryCache` | Singleton | Shared cache across requests |
| `AuthService` | Scoped | Per-request, may have request-specific state |
| `GitHubService` | Scoped | Uses per-user access tokens |

### Extension Methods for Registration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();  // For caching external API responses
        services.AddCosmosDb(configuration);
        services.AddGitHubAuthentication(configuration);
        services.AddProfilyCors(configuration);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGitHubService, GitHubService>();

        return services;
    }
}
```

### Interface-Based Registration

```csharp
// ✅ Good: Interface allows mocking in tests
services.AddSingleton<ICosmosDbService, CosmosDbService>();

// ❌ Bad: Concrete type, harder to test
services.AddSingleton<CosmosDbService>();
```

---

## Checklist for New Features

- [ ] Create options class in `Profily.Core/Options/`
- [ ] Create interface in `Profily.Core/Interfaces/`
- [ ] Create implementation in `Profily.Infrastructure/`
- [ ] Register in `ServiceCollectionExtensions`
- [ ] Create DTOs (not expose entities)
- [ ] Add endpoint with OpenAPI docs
- [ ] Add structured logging
- [ ] Handle errors with proper status codes
- [ ] Add secrets to `user-secrets` (dev) or Key Vault (prod)
- [ ] Update `appsettings.template.json`

### For External API Integrations

- [ ] Add caching with appropriate TTL
- [ ] Map external types to domain models
- [ ] Handle rate limits gracefully
- [ ] Decide: Store in DB or fetch with cache?
- [ ] Use user tokens when available (higher rate limits)

---

## References

- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/aspnet/core/security/)
- [Options Pattern in ASP.NET Core](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/options)
- [Azure Cosmos DB Best Practices](https://docs.microsoft.com/azure/cosmos-db/best-practices)
- [Minimal APIs Overview](https://docs.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Octokit.NET Documentation](https://octokitnet.readthedocs.io/)
- [GitHub API Rate Limits](https://docs.github.com/en/rest/rate-limit)
- [Memory Cache in ASP.NET Core](https://docs.microsoft.com/aspnet/core/performance/caching/memory)
