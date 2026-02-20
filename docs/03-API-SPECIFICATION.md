# Profily — API Specification

Base URL: `https://profily-api.azurewebsites.net/api`

## Authentication

All endpoints except `POST /auth/callback`, `GET /auth/github`, `GET /templates*`, and `GET /health` require an authenticated session.

Authentication uses **HttpOnly cookies** with Redis-backed sessions:
- Login sets a `session` cookie automatically
- Browser sends it on every request — no manual headers needed
- No `Authorization` header required

```
Cookie: session=<session_id>
```

## Error Response Format

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Human-readable error message",
    "details": [
      { "field": "name", "message": "Name is required" }
    ]
  }
}
```

Standard HTTP status codes: 200, 201, 204, 400, 401, 403, 404, 409, 429, 500

---

## Endpoint Summary

```
Auth (2):
  POST   /auth/callback               Exchange GitHub OAuth code for JWT
  DELETE /auth/logout                  End session

User (3):
  GET    /users/me                     Profile + skills + projects (single resource)
  PATCH  /users/me                     Update profile, skills, project overrides
  POST   /users/me/sync               Re-fetch from GitHub

Templates (3):
  GET    /templates                    Template gallery
  GET    /templates/{id}/demo          Demo with sample data
  GET    /templates/{id}/preview       Preview with user's real data

Portfolios (5):
  POST   /portfolios                   Create portfolio
  GET    /portfolios/{id}              Get portfolio config
  PATCH  /portfolios/{id}              Update customizations + selected projects
  GET    /portfolios/{id}/preview      Rendered HTML preview
  POST   /portfolios/{id}/deploy       Deploy to GitHub Pages

Events (1):
  GET    /events/stream                SSE real-time updates

Health (1):
  GET    /health                       Health check

Total: 15 endpoints
```

---

## Auth Endpoints

### `POST /auth/callback`
Exchanges GitHub OAuth code for access token, creates/updates user, returns JWT.

**Request:**
```json
{
  "code": "github_oauth_code",
  "state": "csrf_state_token"
}
```

**Response: `200 OK`**

Sets HttpOnly cookie and returns user profile:

```
Set-Cookie: session=<uuid>; HttpOnly; Secure; SameSite=Lax; Path=/api; Max-Age=604800
```

```json
{
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "m7mdraafat",
    "displayName": "Mohamed Raafat",
    "avatarUrl": "https://avatars.githubusercontent.com/u/141199348",
    "isNewUser": true
  }
}
```

**Errors:** `400` invalid code, `502` GitHub API unavailable

---

### `DELETE /auth/logout`
Deletes Redis session and clears the cookie.

**Response:** `204 No Content`

```
Set-Cookie: session=; HttpOnly; Secure; SameSite=Lax; Path=/api; Max-Age=0
```

---

## User Endpoints

### `GET /users/me`
Single resource: returns profile, skills, projects, and portfolio reference.

**Response: `200 OK`**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "m7mdraafat",
  "displayName": "Mohamed Raafat",
  "avatarUrl": "https://avatars.githubusercontent.com/u/141199348",
  "bio": "I would love to change the world, but they won't give me the source code",
  "location": "Egypt",
  "company": "Microsoft",
  "email": "m7mdraafat2003@gmail.com",
  "githubUrl": "https://github.com/m7mdraafat",
  "linkedinUrl": "https://linkedin.com/in/mohamed-raafat-701290252",
  "websiteUrl": null,
  "reposCount": 27,
  "followersCount": 73,
  "contributionsThisYear": 715,
  "lastSyncedAt": "2026-02-20T10:30:00Z",

  "skills": [
    {
      "name": "C#",
      "category": "backend",
      "confidence": 95,
      "repoCount": 15,
      "displayOrder": 0,
      "isUserEdited": false
    },
    {
      "name": "Python",
      "category": "backend",
      "confidence": 88,
      "repoCount": 8,
      "displayOrder": 1,
      "isUserEdited": false
    }
  ],

  "projects": [
    {
      "id": "proj-uuid-1",
      "name": "VideStore.API",
      "description": "Full-stack e-commerce API with clean architecture",
      "customDescription": null,
      "language": "C#",
      "topics": ["dotnet", "api", "clean-architecture"],
      "stars": 1,
      "forks": 0,
      "isFork": false,
      "htmlUrl": "https://github.com/m7mdraafat/VideStore.API",
      "homepageUrl": null,
      "isSelected": true,
      "displayOrder": 0,
      "lastPushedAt": "2026-01-15T00:00:00Z"
    }
  ],

  "portfolio": {
    "id": "portfolio-uuid",
    "templateId": "3d-purple",
    "status": "deployed",
    "deployedUrl": "https://m7mdraafat.github.io",
    "lastDeployedAt": "2026-02-20T12:00:00Z"
  }
}
```

**Notes:**
- `portfolio` is `null` if user hasn't created one yet
- `projects` includes ALL repos (synced from GitHub), `isSelected` marks featured ones
- `skills` are auto-detected + user-edited, sorted by `displayOrder`

---

### `PATCH /users/me`
Update profile fields, skills, and project overrides. Only send fields you want to change.

**Request (partial update):**
```json
{
  "displayName": "Mohamed Raafat",
  "bio": "Software engineer building scalable systems...",
  "location": "Cairo, Egypt",
  "email": "m7mdraafat2003@gmail.com",
  "linkedinUrl": "https://linkedin.com/in/mohamed-raafat-701290252",
  "websiteUrl": "https://m7mdraafat.github.io",

  "skills": [
    { "name": "C#", "category": "backend", "confidence": 95, "displayOrder": 0 },
    { "name": "Python", "category": "backend", "confidence": 90, "displayOrder": 1 },
    { "name": "Docker", "category": "devops", "confidence": 80, "displayOrder": 5 },
    { "name": "GraphQL", "category": "backend", "confidence": 70, "displayOrder": 8 }
  ],

  "projectOverrides": [
    { "id": "proj-uuid-1", "customDescription": "E-commerce REST API with JWT auth, CQRS, and Stripe integration" }
  ]
}
```

**Response: `200 OK`** — returns full updated user object (same shape as `GET /users/me`).

**Validation:**
- `skills` array replaces ALL skills when provided (send the complete list)
- `skills[].confidence` must be 1-99
- `skills[].category` must be one of: `frontend`, `backend`, `database`, `devops`, `ai`, `mobile`, `tools`
- `projectOverrides` only updates `customDescription` — does not change GitHub-synced fields
- Profile fields are individually optional (partial update)

---

### `POST /users/me/sync`
Re-fetches all data from GitHub: profile, repos, languages. Re-runs skill inference. Preserves user edits (skills marked `isUserEdited: true` are kept).

**Response: `200 OK`**
```json
{
  "syncedAt": "2026-02-20T14:00:00Z",
  "changes": {
    "newRepos": 2,
    "removedRepos": 0,
    "updatedSkills": 3,
    "profileFieldsUpdated": ["followersCount", "contributionsThisYear"]
  }
}
```

**Rate limit:** 3 requests per 10 minutes.

---

## Template Endpoints

### `GET /templates`
List all available templates. **No auth required.**

**Response: `200 OK`**
```json
{
  "templates": [
    {
      "id": "3d-purple",
      "name": "3D Purple",
      "description": "Modern 3D portfolio with particle effects, morphing geometry, and purple accent theme",
      "thumbnailUrl": "https://r2.profily.dev/templates/3d-purple/thumbnail.png",
      "features": ["3D Effects", "Animated", "Dark Theme"],
      "sections": ["hero", "about", "services", "projects", "skills", "contact"],
      "isPremium": false
    },
    {
      "id": "minimal-clean",
      "name": "Minimal Clean",
      "description": "Clean, fast-loading minimal portfolio with subtle animations",
      "thumbnailUrl": "https://r2.profily.dev/templates/minimal-clean/thumbnail.png",
      "features": ["Minimal", "Fast", "Light/Dark"],
      "sections": ["hero", "about", "projects", "skills", "contact"],
      "isPremium": false
    }
  ]
}
```

---

### `GET /templates/{id}/demo`
Returns pre-rendered demo HTML with sample data. **No auth required.**

**Response: `200 OK`**
```json
{
  "html": "<!DOCTYPE html><html>...",
  "templateId": "3d-purple"
}
```

---

### `GET /templates/{id}/preview`
Returns rendered HTML with the current user's real data + default customizations. **Auth required.**

**Response: `200 OK`**
```json
{
  "html": "<!DOCTYPE html><html>...",
  "templateId": "3d-purple",
  "generatedAt": "2026-02-20T12:00:00Z"
}
```

**Errors:** `404` template not found

---

## Portfolio Endpoints

### `POST /portfolios`
Create a new portfolio. One per user (MVP).

**Request:**
```json
{
  "templateId": "3d-purple",
  "selectedProjectIds": ["proj-uuid-1", "proj-uuid-2", "proj-uuid-3", "proj-uuid-4"]
}
```

**Response: `201 Created`**
```json
{
  "id": "portfolio-uuid",
  "templateId": "3d-purple",
  "status": "draft",
  "selectedProjectIds": ["proj-uuid-1", "proj-uuid-2", "proj-uuid-3", "proj-uuid-4"],
  "customizations": {
    "sections": {
      "hero": { "visible": true, "greeting": "Hello, I Am", "roleTexts": [] },
      "about": { "visible": true, "title": "About Me", "subtitle": "Get To Know" },
      "services": { "visible": true, "title": "My Services", "subtitle": "What I Offer" },
      "projects": { "visible": true, "title": "Featured Projects", "subtitle": "My Recent Work", "showFilters": true },
      "skills": { "visible": true, "title": "Tech Stack", "subtitle": "My Abilities", "showProgressBars": true },
      "contact": { "visible": true, "title": "Contact Me", "subtitle": "Get In Touch", "showForm": true }
    }
  },
  "deployedUrl": null,
  "lastDeployedAt": null,
  "createdAt": "2026-02-20T12:00:00Z"
}
```

**Errors:** `409` portfolio already exists (one per user), `404` template not found

---

### `GET /portfolios/{id}`
Get portfolio configuration.

**Response: `200 OK`** — same shape as POST response.

---

### `PATCH /portfolios/{id}`
Update portfolio: change template, customizations, selected projects.

**Request (partial — send only what changed):**
```json
{
  "templateId": "minimal-clean",
  "selectedProjectIds": ["proj-uuid-1", "proj-uuid-2"],
  "customizations": {
    "sections": {
      "services": { "visible": false },
      "hero": {
        "greeting": "Hey, I'm",
        "roleTexts": ["Software Engineer", "Backend Developer", "AI Builder"]
      },
      "about": {
        "title": "Who Am I",
        "description": "Custom about me text..."
      },
      "skills": {
        "showProgressBars": false
      }
    }
  }
}
```

**Response: `200 OK`** — returns full updated portfolio object.

**Notes:**
- `customizations.sections` is **deep merged** — sending `{ "services": { "visible": false } }` doesn't reset other sections
- `selectedProjectIds` replaces the full selection when provided
- Changing `templateId` resets customizations to that template's defaults

---

### `GET /portfolios/{id}/preview`
Renders full HTML using: user profile + user skills + selected projects + customizations.

**Response: `200 OK`**
```json
{
  "html": "<!DOCTYPE html><html lang='en'>...",
  "generatedAt": "2026-02-20T12:05:00Z"
}
```

Frontend displays this in a sandboxed `<iframe srcdoc="...">`.

---

### `POST /portfolios/{id}/deploy`
Deploys the portfolio to GitHub Pages (`username.github.io`). Synchronous — completes in < 10 seconds.

**Response: `200 OK`**
```json
{
  "status": "success",
  "url": "https://m7mdraafat.github.io",
  "commitSha": "abc123def456",
  "deployedAt": "2026-02-20T12:06:08Z"
}
```

**Errors:**
- `400` portfolio has no selected projects
- `401` GitHub token expired (need re-auth)
- `409` deployment already in progress
- `502` GitHub API error

---

## Events Endpoint (SSE)

### `GET /events/stream`
Server-Sent Events stream for real-time notifications. **Auth required (cookie sent automatically).**

**Headers Sent:**
```
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive
```

**Event Types:**

```
event: deploy-status
data: {"status":"success","url":"https://m7mdraafat.github.io","commitSha":"abc123"}

event: sync-complete
data: {"newRepos":2,"updatedSkills":1,"syncedAt":"2026-02-20T14:00:00Z"}
```

**Client Usage (no auth code needed — cookie is automatic):**
```javascript
// Cookie sent automatically by the browser
const events = new EventSource('/api/events/stream');
events.addEventListener('deploy-status', (e) => {
  const data = JSON.parse(e.data);
  if (data.status === 'success') showSuccessToast(data.url);
});
```

---

## Health Check

### `GET /health`
**No auth required.**

**Response: `200 OK`**
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "checks": {
    "database": "ok",
    "redis": "ok",
    "github": "ok"
  }
}
```

---

## Rate Limits

| Endpoint Group | Limit | Window |
|---|---|---|
| `POST /auth/*` | 10 requests | 1 minute |
| `GET /users/me` | 60 requests | 1 minute |
| `PATCH /users/me` | 20 requests | 1 minute |
| `POST /users/me/sync` | 3 requests | 10 minutes |
| `GET /templates/*` | 60 requests | 1 minute |
| `*/preview` | 30 requests | 1 minute |
| `POST /*/deploy` | 5 requests | 5 minutes |
| All other | 100 requests | 1 minute |

**Rate limit headers:**
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1708430460
```

**When exceeded:** `429 Too Many Requests`
```json
{
  "error": {
    "code": "RATE_LIMITED",
    "message": "Too many requests. Try again in 45 seconds.",
    "retryAfter": 45
  }
}
```
