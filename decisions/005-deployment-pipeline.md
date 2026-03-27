# ADR-005: Deployment Pipeline

**Status:** Accepted  
**Date:** 2026-03-27  

---

## Context

Two distinct deployment concerns:
1. **App deployment** — shipping our code (API + frontend) to production
2. **Portfolio deployment** — publishing a user's portfolio to `profily.dev` and/or GitHub Pages

We also need to decide on environment strategy (staging vs prod).

## Constraints

- $0 budget (Azure App Service F1, Cloudflare Pages free)
- Solo developer — minimize operational overhead
- Monorepo: `apps/api` (.NET) and `apps/web` (React) in same repo

---

## 1. Environment Strategy: Prod Only (No Staging)

| What | How |
|---|---|
| Development | Local Docker Compose (PostgreSQL) + `dotnet run` + `npm run dev` |
| CI | GitHub Actions — build, test, lint on every PR |
| Frontend preview | Cloudflare Pages preview deploys (automatic per PR) |
| Production | Single environment, deploy on merge to `main` |
| Incomplete features | Feature flags (DB-backed or config-based) |

**Why no staging:**
- F1 only allows 1 app per region per subscription
- Solo developer — staging adds operational overhead with little value
- GitHub Actions CI + local testing catches issues before prod
- Feature flags hide incomplete work from users

**Add staging when:** revenue justifies B1 ($13/month) and team grows beyond 1 person.

### Feature Flags

Simple DB-backed feature flags for hiding incomplete features:

```csharp
// Check in endpoint or service
if (await featureFlags.IsEnabledAsync("github-pages-export"))
{
    // Show the button / allow the action
}
```

```sql
CREATE TABLE feature_flags (
    name VARCHAR(100) PRIMARY KEY,
    is_enabled BOOLEAN NOT NULL DEFAULT false,
    description TEXT
);
```

No external service needed. Toggle a DB row to enable/disable features in production.

---

## 2. App Deployment (CI/CD)

### Frontend: Cloudflare Pages (Auto-Deploy)

```
Push to main (apps/web changed)
    → Cloudflare Pages detects change
    → Runs: npm install && npm run build
    → Deploys to app.profily.dev
    → ~30 seconds

PR opened/updated
    → Cloudflare Pages builds preview
    → Preview URL: {branch}.profily-web.pages.dev
    → Review before merge
```

**Configuration:**
- Build command: `cd apps/web && npm run build`
- Output directory: `apps/web/dist`
- Production branch: `main`

### Backend: GitHub Actions → Azure App Service

```yaml
# .github/workflows/api-deploy.yml
name: Deploy API

on:
  push:
    branches: [main]
    paths: ['apps/api/**']

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore apps/api/src/Profily.Api/Profily.Api.csproj

      - name: Build
        run: dotnet build apps/api/src/Profily.Api/Profily.Api.csproj -c Release --no-restore

      - name: Test
        run: dotnet test apps/api/tests/ -c Release --no-build

      - name: Publish
        run: dotnet publish apps/api/src/Profily.Api/Profily.Api.csproj -c Release -o publish

      - name: Deploy to Azure
        uses: azure/webapps-deploy@v3
        with:
          app-name: profily-api
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
          package: publish
```

**Triggers:**
- Only runs when `apps/api/**` files change (ignores frontend/docs changes)
- Runs tests before deploy — failed tests block deployment
- Uses Azure publish profile (stored as GitHub secret)

### CI on Pull Requests

```yaml
# .github/workflows/ci.yml
name: CI

on:
  pull_request:
    branches: [main]

jobs:
  api:
    if: contains(github.event.pull_request.changed_files, 'apps/api')
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet build apps/api/src/Profily.Api/Profily.Api.csproj
      - run: dotnet test apps/api/tests/

  web:
    if: contains(github.event.pull_request.changed_files, 'apps/web')
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: cd apps/web && npm ci
      - run: cd apps/web && npm run lint
      - run: cd apps/web && npm run build
```

---

## 3. Portfolio Deployment: Profily Subdomain

### Publish Flow

```
User clicks "Publish" in dashboard
    │
    ▼
API: TemplateRenderer.RenderAsync(templateId, userData)
    → Generates: index.html, css/style.css, js/main.js
    │
    ▼
API: R2StorageClient.UploadPortfolioAsync(username, files)
    → PUT portfolios/{username}/index.html
    → PUT portfolios/{username}/css/style.css
    → PUT portfolios/{username}/js/main.js
    │
    ▼
API: Update DB → portfolio.status = "published", portfolio.published_at = now()
    │
    ▼
SSE: event: publish-status, data: { "status": "live", "url": "https://m7mdraafat.profily.dev" }
```

### Cloudflare Worker (Serves `*.profily.dev`)

```javascript
// worker.js
export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    const hostname = url.hostname;

    // Extract username from subdomain
    // e.g., m7mdraafat.profily.dev → m7mdraafat
    const parts = hostname.split('.');
    if (parts.length < 3) {
      return new Response('Not Found', { status: 404 });
    }
    const username = parts[0];

    // Skip non-portfolio subdomains
    if (username === 'app' || username === 'www' || username === 'api') {
      return fetch(request);
    }

    // Determine file path
    let path = url.pathname === '/' ? '/index.html' : url.pathname;
    const key = `portfolios/${username}${path}`;

    // Fetch from R2
    const object = await env.PORTFOLIO_BUCKET.get(key);
    if (!object) {
      return new Response('Not Found', { status: 404 });
    }

    // Set content type
    const contentTypes = {
      '.html': 'text/html',
      '.css': 'text/css',
      '.js': 'application/javascript',
      '.svg': 'image/svg+xml',
      '.png': 'image/png',
      '.jpg': 'image/jpeg',
    };
    const ext = path.substring(path.lastIndexOf('.'));
    const contentType = contentTypes[ext] || 'application/octet-stream';

    return new Response(object.body, {
      headers: {
        'Content-Type': contentType,
        'Cache-Control': 'public, max-age=3600',  // 1 hour cache
        'X-Profily-User': username,
      },
    });
  },
};
```

### Cache Strategy

| Asset | Cache-Control | Bust on re-publish? |
|---|---|---|
| `index.html` | `public, max-age=3600` (1 hour) | Yes — Cloudflare cache purge API |
| `css/style.css` | `public, max-age=86400` (1 day) | Yes — filename hash or purge |
| `js/main.js` | `public, max-age=86400` (1 day) | Yes — filename hash or purge |

On re-publish, the API calls Cloudflare's cache purge API to invalidate the old version:

```csharp
// After uploading new files to R2
await cloudflareClient.PurgeCacheAsync($"https://{username}.profily.dev/*");
```

### 404 Handling

When a username doesn't exist in R2 (no portfolio published), the Worker returns a plain `404 Not Found`. No redirect, no custom page (keep it simple).

---

## 4. Portfolio Deployment: GitHub Pages (Pro Only)

### Export Flow

```
User clicks "Export to GitHub Pages" (separate button from Publish)
    │
    ▼
API: Check user.isPro → if not, return 403
    │
    ▼
API: TemplateRenderer.RenderAsync(templateId, userData)
    → Same render code as profily.dev publish
    │
    ▼
API: GitHubPagesDeployer.DeployAsync(user, files)
    │
    ├── 1. GET /repos/{username}/{username}.github.io
    │       → 404? Create repo: POST /user/repos
    │
    ├── 2. Create blobs for each file:
    │       POST /repos/{owner}/{repo}/git/blobs
    │       → index.html, css/style.css, js/main.js
    │
    ├── 3. Create tree:
    │       POST /repos/{owner}/{repo}/git/trees
    │
    ├── 4. Create commit:
    │       POST /repos/{owner}/{repo}/git/commits
    │
    ├── 5. Update ref:
    │       PATCH /repos/{owner}/{repo}/git/refs/heads/main
    │
    └── 6. Enable Pages (if not already):
            PUT /repos/{owner}/{repo}/pages
            { "source": { "branch": "main", "path": "/" } }

Total GitHub API calls: 5-7
Total time: ~5-10 seconds

SSE events during deploy:
    → { "status": "creating_repo" }
    → { "status": "pushing_files" }
    → { "status": "enabling_pages" }
    → { "status": "live", "url": "https://m7mdraafat.github.io" }
```

### GitHub API Rate Limits

| Limit | Value | Impact |
|---|---|---|
| Authenticated requests | 5,000/hour per user | Each deploy uses ~7 calls. No concern. |
| Repo creation | 50 repos/hour | First deploy creates repo. One-time. |

Uses the user's own encrypted GitHub token — rate limits are per-user, not shared.

---

## 5. SSE Events

Both publish and GitHub Pages export stream progress via SSE:

```
Event types:
    publish-status    → profily.dev publish progress
    export-status     → GitHub Pages export progress
    sync-status       → GitHub re-sync progress
    inference-status  → LLM skill inference progress
```

```
data format:
{
    "type": "publish-status",
    "status": "generating" | "uploading" | "purging_cache" | "live" | "error",
    "message": "Generating HTML...",
    "url": "https://m7mdraafat.profily.dev"  // only on "live"
}
```

---

## R2 Bucket Structure

```
profily-storage/
├── portfolios/
│   ├── m7mdraafat/
│   │   ├── index.html
│   │   ├── css/
│   │   │   └── style.css
│   │   └── js/
│   │       └── main.js
│   ├── johndoe/
│   │   ├── index.html
│   │   ├── css/
│   │   │   └── style.css
│   │   └── js/
│   │       └── main.js
│   └── ...
├── templates/                  ← Future: when templates move to R2
└── icons/                      ← Future: SVG skill icons
```

---

## DNS Configuration

```
Cloudflare DNS for profily.dev:

*.profily.dev    → CNAME → worker route (Cloudflare Worker)
app.profily.dev  → CNAME → profily-web.pages.dev (Cloudflare Pages)
profily.dev      → A/CNAME → landing page (Cloudflare Pages or redirect)
```

**Note:** `app.profily.dev` is excluded from the Worker wildcard — Cloudflare routes exact matches before wildcards.

---

## Key Decisions Summary

| # | Decision | Rationale |
|---|---|---|
| 1 | Prod only, no staging | Solo dev, $0 budget. Local Docker + CI + preview deploys + feature flags cover the gap. |
| 2 | Feature flags (DB-backed) | Hide incomplete features in prod. Simple table, no external service. |
| 3 | GitHub Actions for API CI/CD | Natural fit for GitHub-hosted repo. Path-filtered triggers avoid unnecessary deploys. |
| 4 | Cloudflare Pages auto-deploy for frontend | Built-in Git integration, preview deploys per PR, zero config. |
| 5 | Separate files (not inlined) for portfolios | CSS/JS as separate files. Worker handles multiple paths per username. |
| 6 | 404 for non-existent usernames | Simple. No custom "build yours" page (MVP). |
| 7 | Cache + purge on re-publish | 1-hour cache on HTML, 1-day on assets. Purge via Cloudflare API on re-publish. |
| 8 | "Publish" and "Export to GitHub Pages" are separate buttons | Publish always goes to profily.dev. GitHub Pages is an independent Pro action. |
| 9 | SSE for all long operations | Publish, export, sync, inference — all stream progress events. |
