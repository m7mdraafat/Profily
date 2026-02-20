# Profily — Development Setup Guide

## Prerequisites

| Tool | Version | Download |
|---|---|---|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download/dotnet/8.0 |
| Node.js | 20+ | https://nodejs.org |
| Docker Desktop | Latest | https://docker.com/products/docker-desktop |
| Git | Latest | https://git-scm.com |
| VS Code | Latest | https://code.visualstudio.com |

### VS Code Extensions (Recommended)
- C# Dev Kit
- ESLint
- Prettier
- Thunder Client (API testing)
- Docker

---

## 1. Clone Repository

```bash
git clone https://github.com/m7mdraafat/profily.git
cd profily
```

---

## 2. GitHub OAuth App Setup

1. Go to: https://github.com/settings/developers
2. Click **"New OAuth App"**
3. Fill in:
   - **Application name:** `Profily Dev`
   - **Homepage URL:** `http://localhost:5173`
   - **Authorization callback URL:** `http://localhost:5000/api/auth/callback`
4. Click **"Register application"**
5. Copy **Client ID**
6. Generate and copy **Client Secret**

---

## 3. Environment Configuration

```bash
# Copy example env file
cp .env.example .env
```

Edit `.env`:
```env
# GitHub OAuth
GITHUB_CLIENT_ID=your_client_id_here
GITHUB_CLIENT_SECRET=your_client_secret_here

# Database (local Docker PostgreSQL)
DATABASE_URL=Host=localhost;Port=5432;Database=profily;Username=profily;Password=profily_dev_123

# Redis (local Docker)
# Redis (sessions + cache)
REDIS_CONNECTION=localhost:6379

# Token Encryption (for GitHub access tokens stored in DB)
ENCRYPTION_KEY=your-32-char-encryption-key-here

# Cloudflare R2 (optional for local dev — uses local file system fallback)
R2_ACCOUNT_ID=
R2_ACCESS_KEY=
R2_SECRET_KEY=
R2_BUCKET=profily-templates

# Environment
ASPNETCORE_ENVIRONMENT=Development
CORS_ORIGIN=http://localhost:5173
```

---

## 4. Start Infrastructure (Docker)

```bash
docker-compose up -d
```

This starts:
- **PostgreSQL** on port `5432`
- **Redis** on port `6379`

`docker-compose.yml`:
```yaml
services:
  db:
    image: postgres:16-alpine
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: profily
      POSTGRES_PASSWORD: profily_dev_123
      POSTGRES_DB: profily
    volumes:
      - pgdata:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

volumes:
  pgdata:
```

Verify:
```bash
docker ps
# Should show both containers running
```

---

## 5. Backend Setup

```bash
cd apps/api

# Restore packages
dotnet restore

# Apply database migrations
dotnet ef database update --project src/Profily.Infrastructure --startup-project src/Profily.Api

# Run the API
cd src/Profily.Api
dotnet run
```

API will be available at: `http://localhost:5000`

Health check: `curl http://localhost:5000/api/health`

### Run Tests
```bash
cd apps/api
dotnet test
```

---

## 6. Frontend Setup

```bash
cd apps/web

# Install dependencies
npm install

# Start dev server
npm run dev
```

Frontend will be available at: `http://localhost:5173`

### Available Scripts
```bash
npm run dev          # Start dev server (Vite)
npm run build        # Production build
npm run preview      # Preview production build
npm run lint         # Run ESLint
npm run type-check   # TypeScript type checking
```

---

## 7. Seed Template Data

After the API is running, seed the initial templates:

```bash
cd apps/api
dotnet run --project tools/Profily.Seeder
```

This loads:
- 3 starter templates (3d-purple, minimal-clean, developer-terminal)
- Sample data for template demos

---

## 8. Local Development Workflow

### Full Stack (Recommended)
```bash
# Terminal 1: Infrastructure
docker-compose up -d

# Terminal 2: Backend API
cd apps/api/src/Profily.Api && dotnet run

# Terminal 3: Frontend
cd apps/web && npm run dev

# Terminal 4 (optional): Watch tests
cd apps/api && dotnet watch test
```

### API Only
```bash
docker-compose up -d
cd apps/api/src/Profily.Api && dotnet watch run
```

### Frontend Only (API already running)
```bash
cd apps/web && npm run dev
```

---

## 9. Project Structure Quick Reference

```
profily/
├── apps/
│   ├── api/                              # .NET 8 Backend
│   │   ├── src/
│   │   │   ├── Profily.Core/             # Entities, Interfaces, DTOs, Enums
│   │   │   ├── Profily.Infrastructure/   # EF Core, GitHub, Redis, R2, Inference
│   │   │   └── Profily.Api/              # Endpoints, Middleware, Program.cs
│   │   ├── tests/
│   │   │   ├── Profily.UnitTests/
│   │   │   ├── Profily.IntegrationTests/
│   │   │   └── Profily.ArchTests/
│   │   └── tools/
│   │       └── Profily.Seeder/           # Template & seed data loader
│   │
│   └── web/                              # React TypeScript Frontend
│       └── src/
│           ├── components/               # Reusable UI components
│           ├── pages/                    # Route pages
│           ├── hooks/                    # Custom React hooks
│           ├── services/                 # API client functions
│           ├── stores/                   # State management (Zustand)
│           └── types/                    # TypeScript types
│
├── templates/                            # Portfolio HTML templates
│   ├── 3d-purple/
│   ├── minimal-clean/
│   ├── developer-terminal/
│   └── _shared/sample-data.json
│
├── docs/                                 # Documentation
├── docker-compose.yml
├── .env.example
└── README.md
```

---

## 10. Common Issues

### Port already in use
```bash
# Find and kill process on port 5000
npx kill-port 5000
# Or change port in launchSettings.json
```

### Docker containers won't start
```bash
docker-compose down -v    # Remove volumes (resets DB)
docker-compose up -d      # Restart fresh
```

### EF Core migration errors
```bash
cd apps/api
dotnet ef migrations add InitialCreate --project src/Profily.Infrastructure --startup-project src/Profily.Api
dotnet ef database update --project src/Profily.Infrastructure --startup-project src/Profily.Api
```

### GitHub OAuth redirect mismatch
- Ensure callback URL in GitHub OAuth App settings exactly matches:
  `http://localhost:5000/api/auth/callback`
- No trailing slash

### AOT build (for production testing)
```bash
cd apps/api/src/Profily.Api
dotnet publish -c Release
# Binary at: bin/Release/net8.0/publish/Profily.Api
```

Note: Local development uses JIT (normal `dotnet run`), not AOT. AOT is only for production deployment. Build with AOT takes 2-3 minutes.

---

## 11. Deployment

### Backend → Azure App Service

```bash
# Build AOT binary
cd apps/api/src/Profily.Api
dotnet publish -c Release

# Deploy via Azure CLI
az webapp up --name profily-api --resource-group rg-profily --plan profily-plan --sku F1
```

### Frontend → Azure Static Web Apps

```bash
cd apps/web
npm run build

# Deploy via SWA CLI
npx @azure/static-web-apps-cli deploy ./dist
```

### CI/CD

GitHub Actions workflows in `.github/workflows/` handle:
- `api-ci.yml` — build, test, deploy .NET API
- `web-ci.yml` — build, deploy React SPA
- Triggered on push to `main` branch
