# Architecture Diagrams (Mermaid)

These diagrams describe the target architecture. The codebase may lag behind the target.

## System context

```mermaid
flowchart TB
  user[Developer]
  ui[Profily Web UI\n(React + TS)]
  api[Profily API\n(ASP.NET Core)]
  gh[GitHub APIs]
  pages[GitHub Pages]
  jobs[Background Jobs\n(Azure Functions or equivalent)]
  data[(User Data Store)]
  cache[(Cache)]
  assets[(Asset Storage)]

  user --> ui
  ui --> api
  api --> gh
  api --> data
  api --> cache
  api --> assets
  api --> pages
  jobs --> api
  jobs --> gh
  jobs --> data
  jobs --> assets
```

## Containers

```mermaid
flowchart LR
  subgraph Client
    ui[React + TS SPA\nBuilder + Preview]
  end

  subgraph Server
    api[ASP.NET Core Minimal API]
    jobs[Background Jobs]
  end

  subgraph External
    gh[GitHub]
    pages[GitHub Pages]
  end

  subgraph Data
    store[(User Config)]
    cache[(Cache)]
    blob[(Generated Assets)]
  end

  ui -->|HTTPS| api
  api --> gh
  api --> pages
  api --> store
  api --> cache
  api --> blob
  jobs --> api
  jobs --> gh
  jobs --> store
  jobs --> blob
```

## Local development (current)

```mermaid
flowchart LR
  browser[Browser]
  vite[Vite dev server\n:5173]
  api[ASP.NET Core API\n:5229]

  browser --> vite
  vite -->|proxy /api| api
```
