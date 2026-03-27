# ADR-002: Tech Stack & Architecture

**Status:** Accepted  
**Date:** 2026-03-27  

---

## Context

We need to select the full tech stack for Profily — a dashboard-based portfolio builder with LLM skill inference, dual deployment targets (profily.dev + GitHub Pages), and a Free/Pro pricing model. Budget is $0/month at launch, upgrading when Pro revenue comes in.

## Constraints

- $0 infrastructure cost at launch
- Founder has experience with .NET, React, Azure
- Must support: GitHub OAuth, LLM API calls, static file generation, Paymob webhooks
- No Redis — use DB + in-memory cache
- Clean Architecture (3 projects) in a monorepo

---

## Decisions

### Backend: .NET 8 Minimal API (JIT)

**Why JIT over AOT:**
- No library restrictions (full EF Core, reflection-based JSON, any NuGet package)
- Simpler development — no source generators required
- Cold start on F1 (~5-8 sec) is acceptable for a dashboard app (not a public API with millisecond SLA)
- Can switch to AOT later if cold start becomes a real problem

**Why Minimal API over Controllers:**
- Less boilerplate, fewer files
- Better for a small-to-medium API (~15-20 endpoints)
- First-class .NET 8 support

### Frontend: React 18 + TypeScript + Vite

- Dashboard SPA with sidebar navigation
- TypeScript for type safety
- Vite for fast dev builds
- No SSR needed — dashboard is behind auth

### Database: Neon PostgreSQL (Free Tier)

| Aspect | Detail |
|---|---|
| Provider | Neon |
| Engine | PostgreSQL 16 |
| Free tier | 0.5 GB storage, 1 compute |
| Sleep behavior | Suspends after 5 min inactivity, ~1 sec cold wake |
| EF Core | Npgsql.EntityFrameworkCore.PostgreSQL |
| Capacity | ~5,000 users at 100 KB avg per user |

**Why Neon over alternatives:**
- Neon vs Supabase: Neon sleeps after 5 min (better than Supabase's 7-day pause/deletion risk)
- Neon vs Azure SQL Free: PostgreSQL ecosystem (jsonb, arrays) is more flexible. Azure SQL Free could be revoked. Neon is purpose-built for serverless PostgreSQL.
- Neon vs Turso: PostgreSQL + EF Core is a proven stack. Turso (SQLite) has limited EF Core support.

**Upgrade path:** Neon Pro ($19/month) when revenue justifies it — always-on, no sleep, 10 GB storage.

### Cache: IMemoryCache (No Redis)

| What to cache | Strategy |
|---|---|
| LLM skill responses | Persisted in DB (skills table). No separate cache needed. |
| GitHub API responses | IMemoryCache with 5-10 min expiry. Adequate for single instance. |
| Sessions | DB-backed sessions (cookie → session row in PostgreSQL). |
| Template HTML | IMemoryCache with 1 hour expiry. |

**Why no Redis:**
- Single App Service instance — in-memory cache is sufficient
- Skills are persisted to DB on inference — no volatile cache needed
- One less service to manage, one less free-tier limit to worry about
- Add Redis later if we scale to multiple instances

### API Hosting: Azure App Service F1 (Free)

| Aspect | Detail |
|---|---|
| Cost | $0 |
| RAM | 1 GB (shared) |
| CPU | 60 minutes/day |
| Storage | 1 GB |
| Sleep | After 20 min inactivity |
| Cold start | ~5-8 sec (JIT) |
| Custom domain | Not on F1 (use default azurewebsites.net) |

**Known limitations:**
- Sleeps after 20 min → first request after sleep takes ~5-8 sec
- 60 CPU min/day → sufficient for <1K users/day (dashboard requests are lightweight)
- No custom domain on F1 → frontend calls `profily-api.azurewebsites.net`

**Upgrade path:** B1 ($13/month) — always on, custom domain, 1.75 GB RAM.

### Frontend Hosting: Cloudflare Pages (Free)

| Aspect | Detail |
|---|---|
| Cost | $0 |
| Bandwidth | Unlimited |
| Builds | 500/month |
| Custom domains | ✅ (e.g., `app.profily.dev`) |
| Preview deploys | ✅ (per branch) |

**Why Cloudflare Pages over Azure Static Web Apps:**
- Already using Cloudflare for portfolio hosting (Workers + R2) — same ecosystem
- Unlimited bandwidth (Azure SWA: 100 GB)
- Better global CDN edge network

### Portfolio Hosting: Cloudflare Workers + R2

| Aspect | Detail |
|---|---|
| Workers | Serves `*.profily.dev` — extracts username from subdomain, fetches HTML from R2 |
| R2 | Stores generated portfolio static files (HTML, CSS, JS) |
| Workers free | 100K requests/day |
| R2 free | 10 GB storage, 10M reads/month |
| SSL | Cloudflare proxy — wildcard cert for `*.profily.dev` |

### File Storage: Cloudflare R2

Stores:
- Generated portfolio HTML/CSS/JS files (`portfolios/{username}/`)
- Template assets (`templates/{template-id}/`)
- SVG skill icons (`icons/`)

---

## Architecture

### Clean Architecture (3 Projects)

```
apps/api/src/
├── Profily.Core/              → Domain layer (zero dependencies)
│   ├── Entities/              → User, Portfolio, Project, Skill, Experience, Education
│   ├── Interfaces/            → IRepository<T>, IUnitOfWork, ISkillInferenceService, etc.
│   ├── Enums/                 → PortfolioStatus, SkillCategory, PlanType
│   ├── ValueObjects/          → InferredSkill, SocialLink
│   └── Exceptions/            → NotFoundException, ValidationException, etc.
│
├── Profily.Infrastructure/    → Data + External Services
│   ├── Data/                  → EF Core DbContext, configurations, migrations
│   ├── Repositories/          → Repository implementations
│   ├── Services/
│   │   ├── GitHub/            → GitHubApiClient (OAuth, repos, languages, file contents)
│   │   ├── LLM/               → GroqClient, GeminiClient (skill inference)
│   │   ├── Storage/           → R2StorageClient (portfolio files, templates)
│   │   ├── Payment/           → PaymobClient (webhooks, subscription status)
│   │   └── Deployment/        → PortfolioPublisher, GitHubPagesDeployer
│   └── InfrastructureServiceRegistration.cs
│
└── Profily.Api/               → HTTP Surface
    ├── Endpoints/             → Auth, Users, Projects, Skills, Templates, Portfolios, Payments
    ├── Middleware/             → ErrorHandling, RateLimiting, Auth
    └── Program.cs
```

**Dependency direction:** Api → Infrastructure → Core. Core depends on nothing.

### Monorepo Structure

```
profily/
├── apps/
│   ├── api/                   → .NET 8 Backend
│   │   ├── src/
│   │   │   ├── Profily.Api/
│   │   │   ├── Profily.Core/
│   │   │   └── Profily.Infrastructure/
│   │   ├── tests/
│   │   └── Profily.slnx
│   │
│   └── web/                   → React Frontend
│       ├── src/
│       │   ├── components/
│       │   ├── pages/         → Dashboard pages (Overview, Projects, Skills, etc.)
│       │   ├── hooks/
│       │   ├── services/      → API client
│       │   ├── stores/        → State management
│       │   └── types/
│       ├── package.json
│       └── vite.config.ts
│
├── templates/                 → Portfolio templates (HTML/CSS/JS)
├── icons/                     → SVG skill icons
├── decisions/                 → ADRs
├── docs/                      → Documentation
├── docker-compose.yml         → Local dev (PostgreSQL)
└── README.md
```

### System Diagram

```
┌──────────────────────────────────────────────────┐
│              Cloudflare Pages (Free)              │
│              React SPA Dashboard                  │
│              app.profily.dev                      │
└──────────────────┬───────────────────────────────┘
                   │ REST API
┌──────────────────▼───────────────────────────────┐
│          Azure App Service F1 (Free)              │
│          .NET 8 Minimal API (JIT)                 │
│          profily-api.azurewebsites.net             │
│                                                   │
│   ┌───────────┬───────────┬──────────┬─────────┐ │
│   │ GitHub    │ Groq /    │ Paymob   │ R2      │ │
│   │ OAuth +   │ Gemini    │ Payment  │ Storage │ │
│   │ API       │ (Skills)  │ Webhooks │ Client  │ │
│   └───────────┴───────────┴──────────┴─────────┘ │
└──────────┬───────────────────────────────────────┘
           │
┌──────────▼──────┐    ┌────────────────────────────┐
│ Neon PostgreSQL │    │ Cloudflare                  │
│ (Free 0.5 GB)   │    │                             │
│                  │    │ Workers → serves            │
│ Users            │    │   *.profily.dev             │
│ Portfolios       │    │                             │
│ Projects         │    │ R2 → stores                 │
│ Skills           │    │   portfolio HTML files      │
│ Experience       │    │   template assets           │
│ Education        │    │   SVG skill icons           │
│ Sessions         │    │                             │
│ Payments         │    │ Pages → hosts               │
└──────────────────┘    │   React SPA                 │
                        └────────────────────────────┘
```

---

## Cost Summary

| Component | Provider | Cost |
|---|---|---|
| .NET API | Azure App Service F1 | $0 |
| React SPA | Cloudflare Pages | $0 |
| Database | Neon PostgreSQL | $0 |
| Portfolio hosting | Cloudflare Workers + R2 | $0 |
| LLM inference | Groq free tier | $0 |
| Domain | `profily.dev` | ~$12/year |
| **Total** | | **~$1/month** |

## Upgrade Path (When Revenue Justifies)

| Component | Free → Paid | Cost |
|---|---|---|
| API hosting | F1 → B1 | +$13/month |
| Database | Neon Free → Pro | +$19/month |
| Add Redis | Azure Cache Free or Upstash | +$0-10/month |
| **Total upgraded** | | **~$33-43/month** |

Breakeven: ~4-5 Pro subscribers/month covers the upgraded infra.
