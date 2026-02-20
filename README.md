# Profily

> Sign in with GitHub → Pick a template → Get a deployed portfolio in under 5 minutes.

**Profily** automatically extracts your profile, projects, skills, and tech stack from GitHub, lets you customize with stunning templates, and deploys to GitHub Pages in one click.

## Features

- **Zero manual data entry** — everything comes from your GitHub account
- **Skill inference** — auto-detects your tech stack from repos, dependencies, and config files
- **Stunning templates** — 3D effects, animations, dark themes (not generic Bootstrap)
- **One-click deploy** — push to `username.github.io` instantly
- **Edit anytime** — come back and update your portfolio from the platform

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8 Minimal API (Native AOT) |
| Frontend | React 18 + TypeScript + Vite |
| Database | PostgreSQL (Neon) |
| Cache | Redis (Azure) |
| Storage | Cloudflare R2 |
| Auth | GitHub OAuth + HttpOnly Cookie Sessions |
| Hosting | Azure App Service (API) + Azure Static Web Apps (SPA) |
| Deploy Target | GitHub Pages |

## Project Structure

```
profily/
├── apps/
│   ├── api/                    # .NET 8 Backend (Clean Architecture)
│   │   ├── src/
│   │   │   ├── Profily.Core/           # Entities, interfaces, DTOs
│   │   │   ├── Profily.Infrastructure/ # EF Core, GitHub, Redis, R2
│   │   │   └── Profily.Api/            # Endpoints, middleware
│   │   └── tests/
│   └── web/                    # React TypeScript Frontend
│       └── src/
├── templates/                  # Portfolio HTML templates
├── docs/                       # Documentation
└── docker-compose.yml          # Local dev infrastructure
```

## Quick Start

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- Docker Desktop

### Setup

```bash
# 1. Clone
git clone https://github.com/m7mdraafat/profily.git
cd profily

# 2. Environment
cp .env.example .env
# Edit .env with your GitHub OAuth App credentials

# 3. Infrastructure
docker-compose up -d

# 4. Backend
cd apps/api/src/Profily.Api
dotnet run

# 5. Frontend
cd apps/web
npm install && npm run dev
```

See [docs/07-SETUP-GUIDE.md](docs/07-SETUP-GUIDE.md) for detailed instructions.

## Documentation

| Doc | Description |
|---|---|
| [01-PRD](docs/01-PRD.md) | Product requirements & user stories |
| [02-Technical Design](docs/02-TECHNICAL-DESIGN.md) | Architecture, AOT, auth flow |
| [03-API Specification](docs/03-API-SPECIFICATION.md) | Endpoints, request/response schemas |
| [04-Database Schema](docs/04-DATABASE-SCHEMA.md) | PostgreSQL tables & indexes |
| [05-Template Data Contract](docs/05-TEMPLATE-DATA-CONTRACT.md) | JSON shape for templates |
| [06-Skill Mappings](docs/06-SKILL-MAPPINGS.md) | Package → skill inference mappings |
| [07-Setup Guide](docs/07-SETUP-GUIDE.md) | Local development setup |
| [08-Logging](docs/08-LOGGING-OBSERVABILITY.md) | Canonical log lines strategy |
| [09-Error Handling](docs/09-ERROR-HANDLING.md) | Exception hierarchy & error flow |

## License

MIT
