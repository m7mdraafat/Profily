# Architecture Overview

This is the target architecture (long-term) and the current implementation status.

## Target architecture (long-term)

- Web UI (React + TypeScript)
  - Builders (profile + portfolio)
  - Template gallery
  - Live preview
- API (ASP.NET Core Minimal APIs)
  - Template rendering (README/HTML)
  - Asset generation (SVG)
  - Deployment helpers (GitHub Pages / profile repo)
  - Auth (GitHub OAuth)
- Background jobs (Azure Functions or equivalent)
  - Scheduled refresh / rebuild
  - Webhook handlers
- Data
  - Persistent user config (e.g., Cosmos DB)
  - Cache (e.g., Redis)
  - Asset storage (e.g., Blob storage)

See the diagrams in [diagrams.md](diagrams.md).

## Current implementation status

| Area | Status | Notes |
|---|---:|---|
| Repo scaffolding | Implemented | .NET solution + React Vite app |
| API health | Implemented | [src/Profily.Api/Program.cs](../../src/Profily.Api/Program.cs) exposes `/api/health` |
| Portfolio generator | Planned | Templates + rendering + deploy |
| Profile generator | Planned | README + SVG generation |
| GitHub integration | Planned | OAuth + API access + repo selection |
| Background jobs | Planned | Timers + webhooks |
| Persistence/caching | Planned | Data model + storage |
