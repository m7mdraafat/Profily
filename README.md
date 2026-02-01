# Profily

Work-in-progress scaffolding for the Profily platform.

## Current scaffold

- Backend: .NET 8 Minimal API in [src/Profily.Api](src/Profily.Api)
- Shared contracts/models: [src/Profily.Core](src/Profily.Core)
- Infrastructure implementations: [src/Profily.Infrastructure](src/Profily.Infrastructure)
- Frontend: Vite + React + TypeScript in [src/profily-web](src/profily-web)

## Run locally

### Backend API

```powershell
cd src/Profily.Api
dotnet run
```

- Swagger: http://localhost:5229/swagger (or https://localhost:7246/swagger)
- Health: http://localhost:5229/api/health

### Frontend

```powershell
cd src/profily-web
npm install
npm run dev
```

The frontend dev server proxies `/api/*` to `http://localhost:5229`.

## Docs

See [docs/project_idea.md](docs/project_idea.md) for the product vision.
