# Profily — Technical Design Document

## 1. Architecture Overview

### System Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                     Client Layer                              │
│                                                              │
│   ┌────────────────────┐    ┌─────────────────────────────┐ │
│   │  React SPA (Vite)  │    │  Generated Static Portfolio │ │
│   │  Azure Static Web  │    │  GitHub Pages               │ │
│   │  Apps (Free)       │    │  (username.github.io)       │ │
│   └─────────┬──────────┘    └─────────────────────────────┘ │
└─────────────┼────────────────────────────────────────────────┘
              │ REST API + SSE
┌─────────────▼────────────────────────────────────────────────┐
│                     API Layer                                 │
│                                                              │
│   ┌──────────────────────────────────────────────────────┐  │
│   │  .NET 8 Minimal API (Native AOT)                     │  │
│   │  Azure App Service F1 (Free)                         │  │
│   │                                                       │  │
│   │  Endpoints:                                           │  │
│   │    /api/auth/*        — GitHub OAuth                  │  │
│   │    /api/users/*       — Profile + skills + projects   │  │
│   │    /api/templates/*   — Template gallery & preview    │  │
│   │    /api/portfolios/*  — CRUD & deploy                 │  │
│   │    /api/events/stream — SSE real-time updates         │  │
│   └──────────────────────────────────────────────────────┘  │
└──────┬──────────────┬──────────────┬─────────────────────────┘
       │              │              │
┌──────▼──────┐ ┌─────▼──────┐ ┌────▼──────────┐ ┌────────────┐
│ PostgreSQL  │ │  Redis     │ │ Cloudflare R2 │ │ GitHub API │
│ Neon Free   │ │  Azure     │ │ Free (10 GB)  │ │ (Octokit)  │
│ (0.5 GB)    │ │  Free      │ │               │ │            │
│             │ │  (250 MB)  │ │ Templates     │ │ Profile    │
│ Users       │ │            │ │ Thumbnails    │ │ Repos      │
│ Portfolios  │ │ API cache  │ │ Assets        │ │ Languages  │
│ Projects    │ │ Rate limit │ │               │ │ Files      │
│ Templates   │ │ Sessions   │ │               │ │ Deploy     │
└─────────────┘ └────────────┘ └───────────────┘ └────────────┘
```

### Monorepo Structure

```
profily/
├── apps/
│   ├── api/                          # .NET 8 Backend (Clean Architecture)
│   │   ├── src/
│   │   │   ├── Profily.Core/         # Domain entities, interfaces, value objects
│   │   │   ├── Profily.Infrastructure/ # EF Core, GitHub client, Redis, R2
│   │   │   └── Profily.Api/          # Endpoints, middleware, Program.cs
│   │   ├── tests/
│   │   │   ├── Profily.UnitTests/
│   │   │   ├── Profily.IntegrationTests/
│   │   │   └── Profily.ArchTests/
│   │   ├── Profily.sln
│   │   └── Dockerfile
│   │
│   └── web/                          # React TypeScript Frontend
│       ├── src/
│       │   ├── components/
│       │   ├── pages/
│       │   ├── hooks/
│       │   ├── services/
│       │   ├── stores/
│       │   └── types/
│       ├── package.json
│       ├── tsconfig.json
│       └── vite.config.ts
│
├── templates/                        # Portfolio templates
│   ├── 3d-purple/
│   ├── minimal-clean/
│   ├── developer-terminal/
│   └── _shared/
│
├── docs/                             # Documentation
│
├── .github/
│   └── workflows/
│       ├── api-ci.yml
│       ├── web-ci.yml
│       └── deploy.yml
│
├── docker-compose.yml                # Local dev (PostgreSQL + Redis)
├── .gitignore
└── README.md
```

## 2. Tech Stack

| Layer | Technology | Justification |
|---|---|---|
| **Backend** | .NET 8 Minimal API + Native AOT | C# expertise, ~200ms cold start on free tier |
| **Frontend** | React 18 + TypeScript + Vite | Fast dev experience, strong typing |
| **Database** | PostgreSQL (Neon Free) | jsonb for flexible data, arrays for tags, free forever |
| **Cache** | Azure Cache for Redis (Free) | GitHub API caching, rate limit counters |
| **File Storage** | Cloudflare R2 (Free 10 GB) | Templates, thumbnails, S3-compatible |
| **Frontend Hosting** | Azure Static Web Apps (Free) | Built-in CI/CD, custom domains |
| **API Hosting** | Azure App Service F1 (Free) | .NET native, AOT eliminates cold start issue |
| **Deployment Target** | GitHub Pages | Free, users already have GitHub accounts |
| **Auth** | GitHub OAuth 2.0 + HttpOnly Cookie + Redis Sessions | Secure, automatic, SSE-compatible, zero frontend auth code |
| **Template Engine** | String placeholder replacement | AOT-compatible, zero dependencies |
| **Real-time** | Server-Sent Events (SSE) | Server→client only, no SignalR cost, native browser API |
| **Monitoring** | Application Insights (Free 5GB) | Azure-native, .NET first-class support |

**Total monthly cost: $0**

## 3. Clean Architecture (.NET Backend)

```
┌─────────────────────────────────────────────────┐
│                  Profily.Api                      │
│         (Endpoints, Middleware, Program.cs)       │
│         Depends on: Core, Infrastructure         │
└───────────────────┬─────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────┐
│              Profily.Infrastructure               │
│   (EF Core, GitHub Client, Redis, R2, Scrapers)  │
│   Depends on: Core                               │
│   Implements: Core interfaces                    │
└───────────────────┬─────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────┐
│                 Profily.Core                      │
│       (Entities, Interfaces, Value Objects,      │
│        Enums, Exceptions, DTOs)                  │
│       Depends on: NOTHING                        │
└─────────────────────────────────────────────────┘
```

### Profily.Core (Domain Layer)
- Zero external dependencies (no NuGet packages)
- Contains: entities, interfaces (repos + services), value objects, enums, DTOs, exceptions
- Defines the contracts that Infrastructure and Api implement or use

### Profily.Infrastructure (Data + External Services)
- References only Profily.Core
- Contains: EF Core DbContext, repository implementations, GitHub API client, Redis cache, R2 storage client, skill inference engine, template renderer, deployment service
- All external I/O lives here

### Profily.Api (HTTP Surface)
- References both Core and Infrastructure
- Contains: Minimal API endpoint definitions, middleware, DI registration, Program.cs
- Thin layer — delegates to Core services via interfaces

## 4. Native AOT Strategy

### Why AOT
- Azure F1 sleeps after 20 min → cold start ~200ms (AOT) vs ~8 sec (JIT)
- Memory: ~50-80 MB (AOT) vs ~150-200 MB (JIT) — F1 has 1 GB shared
- Single-file deployment: ~35 MB native binary

### AOT Constraints & Solutions

| Constraint | Solution |
|---|---|
| No runtime reflection | System.Text.Json source generators |
| No dynamic code gen | Manual mapping (no AutoMapper) |
| Scriban won't work | String placeholder replacement (`{{name}}` → value) |
| Some EF Core limits | Stick to basic LINQ, avoid complex projections |
| No Hangfire | Inline operations (deploy < 10 sec, no background jobs) |

### JSON Source Generators (Required for AOT)

```csharp
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(UserProfileResponse))]
[JsonSerializable(typeof(List<InferredSkillDto>))]
[JsonSerializable(typeof(List<TemplateListDto>))]
[JsonSerializable(typeof(PortfolioResponse))]
[JsonSerializable(typeof(CreatePortfolioRequest))]
[JsonSerializable(typeof(PreviewResponse))]
[JsonSerializable(typeof(DeploymentStatusResponse))]
internal partial class ProfilyJsonContext : JsonSerializerContext { }
```

## 5. Authentication Flow

```
Browser                      API                      GitHub
  │                           │                         │
  │── GET /auth/github ──────▶│                         │
  │◀── 302 Redirect ─────────│── Redirect to ─────────▶│
  │── User approves ─────────┼─────────────────────────▶│
  │◀── Redirect with code ───│◀── code ────────────────│
  │── POST /auth/callback ──▶│                         │
  │   { code }               │── POST /access_token ──▶│
  │                           │◀── access_token ───────│
  │                           │── GET /user ───────────▶│
  │                           │◀── profile data ───────│
  │                           │── Encrypt & store token │
  │                           │── Create Redis session  │
  │◀── Set-Cookie: session ──│                         │
  │◀── { user profile }  ────│                         │
  │                           │                         │
  │── All future requests ──▶│                         │
  │   Cookie sent auto by browser (HttpOnly, Secure)   │
```

**OAuth Scopes Required:**
- `read:user` — profile info
- `repo` — create/push to `username.github.io`
- `read:org` — organization membership (optional)

**Session Management:**
- HttpOnly cookie with session ID (JS cannot access it)
- Session stored in Redis with 7-day sliding expiration
- Every request automatically extends the session
- Logout deletes session from Redis + clears cookie
- No JWT, no token rotation, no refresh interceptors

**Token Security:**
- GitHub access token encrypted with AES-256 before storing in PostgreSQL
- Encryption key stored in environment variable
- Session ID is opaque (UUID) — no user data embedded

**CSRF Protection:**
- `SameSite=Lax` cookie attribute prevents cross-site requests
- Origin header check on POST/PATCH/DELETE requests
- No CSRF tokens needed

**Why Cookie over JWT:**
- SSE (`EventSource`) cannot set custom headers — cookies are sent automatically
- True server-side logout (delete Redis session) — JWT can't be revoked
- Zero frontend auth code — browser handles cookie storage and sending
- More secure — HttpOnly cookies are immune to XSS token theft

## 6. Skill Inference Pipeline

```
Step 1: Languages API (instant)
  GitHub API → languages per repo → byte counts
  → Top languages ranked by total bytes across all repos

Step 2: Config File Parsing (5-15 sec)
  Fetch from top 10 repos (by stars + recency):
    *.csproj → NuGet packages → frameworks/tools
    package.json → npm dependencies → frameworks/tools
    requirements.txt → pip packages → frameworks/tools
    Dockerfile → Docker
    .github/workflows/* → GitHub Actions
  
Step 3: Repo Metadata
  Topics → additional skill signals
  Is fork? → lower confidence
  Stars + recency → weight skills

Step 4: Confidence Scoring
  confidence = f(repoCount, totalBytes, recency, isFork, stars)
  Category auto-assigned (frontend, backend, devops, database, ai, tools)

Step 5: Present to User
  "We detected these skills. Edit, reorder, or remove as needed."
```

## 7. Template System

### Template Structure
```
templates/{template-id}/
├── manifest.json           # Metadata: name, description, features, sections
├── sections/
│   ├── hero.html          # {{profile.name}}, {{profile.bio}}, etc.
│   ├── about.html
│   ├── services.html
│   ├── projects.html
│   ├── skills.html
│   └── contact.html
├── layout.html             # Wrapper: nav, footer, {{sections}} placeholder
├── css/style.css
├── js/main.js
└── thumbnail.png
```

### Rendering Pipeline
```
1. Load template files from R2 storage (cached in Redis)
2. For each visible section (per user config):
   a. Load section HTML
   b. Replace {{placeholders}} with user data
   c. Handle loops: {{#each projects}}...{{/each}}
3. Insert rendered sections into layout.html
4. Return complete HTML string
```

### Placeholder Syntax (AOT-compatible, no library)
```
Simple:     {{profile.name}}
Loops:      {{#each skills}}...{{/each}}
Conditionals: {{#if sections.services.visible}}...{{/if}}
```

Implemented as recursive string replacement in C# — no Scriban, no Razor, no runtime compilation.

## 8. GitHub Pages Deployment

```
1. Check if repo exists: GET /repos/{username}/{username}.github.io
2. If not: POST /user/repos → create {username}.github.io
3. Generate all static files (HTML, CSS, JS)
4. Build git tree: POST /repos/{owner}/{repo}/git/trees
5. Create commit: POST /repos/{owner}/{repo}/git/commits
6. Update ref: PATCH /repos/{owner}/{repo}/git/refs/heads/main
7. Enable Pages: PUT /repos/{owner}/{repo}/pages (source: main branch)
8. Return deployed URL

Total API calls: 4-6
Total time: < 10 seconds
No background job needed.
```

## 9. Real-Time Updates (SSE)

For deployment status and future deep analysis notifications:

```
Client:  const events = new EventSource('/api/events/stream');
         events.addEventListener('deploy-status', handler);

Server:  [HttpGet("/api/events/stream")]
         async Task StreamEvents(CancellationToken ct)
         {
             Response.ContentType = "text/event-stream";
             while (!ct.IsCancellationRequested) {
                 var events = await GetPendingEvents(userId);
                 foreach (var e in events)
                     await Response.WriteAsync($"event: {e.Type}\ndata: {e.Json}\n\n");
                 await Response.Body.FlushAsync(ct);
                 await Task.Delay(1000, ct);
             }
         }
```

## 10. Caching Strategy

| Data | Cache TTL | Key Pattern |
|---|---|---|
| GitHub user profile | 5 min | `gh:user:{username}` |
| GitHub repos list | 5 min | `gh:repos:{username}` |
| GitHub languages | 10 min | `gh:langs:{username}:{repo}` |
| Template HTML (from R2) | 1 hour | `tpl:{templateId}:layout` |
| Template demo HTML | 1 hour | `tpl:{templateId}:demo` |
| Rendered preview | 1 min | `preview:{portfolioId}:{hash}` |
| Rate limit counter | 1 min | `rl:{ip}` |
| User session | 7 days (sliding) | `session:{sessionId}` |

## 11. Security

- **GitHub tokens:** AES-256 encrypted at rest, decrypted only when calling GitHub API
- **Session:** HttpOnly + Secure + SameSite=Lax cookie, session stored in Redis (7-day sliding expiry)
- **CSRF:** SameSite=Lax + Origin header check on mutating requests
- **Input sanitization:** All user-provided text HTML-escaped before template injection
- **CORS:** Allow only `profily.dev` origin (or Azure Static Web Apps domain)
- **Rate limiting:** 100 req/min per IP, 20 req/min for deploy endpoint
- **CSP headers:** On generated portfolios to prevent XSS
- **OAuth state parameter:** CSRF protection on OAuth flow

## 12. Cost Breakdown

| Service | Provider | Cost | Limit |
|---|---|---|---|
| .NET API (AOT) | Azure App Service F1 | $0 | 60 CPU min/day, 1GB RAM |
| React SPA | Azure Static Web Apps | $0 | 100 GB bandwidth |
| PostgreSQL | Neon Free | $0 | 0.5 GB, ~33k users |
| Redis | Azure Cache Free | $0 | 250 MB, 256 connections |
| File Storage | Cloudflare R2 | $0 | 10 GB, 1M reads/month |
| Monitoring | Application Insights | $0 | 5 GB/month |
| DNS | Cloudflare | $0 | Unlimited |
| **Total** | | **$0/month** | **Permanently** |
