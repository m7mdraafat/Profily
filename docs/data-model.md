# Data Model (Target)

This describes the long-term data model. It is intentionally implementation-agnostic.

## Core entities

### User

- `id`
- `githubUsername`
- `createdAt`, `updatedAt`

### ProfileConfig

- `userId`
- `theme`
- `sections[]` (order + per-section settings)
- `lastGeneratedAt`

### PortfolioConfig

- `userId`
- `templateId`
- `projects[]` (selected + overrides)
- `links` (socials/contact)
- `resume` (JSON Resume or equivalent)
- `blogSource` (repo/gist + sync settings)
- `customDomain` (optional)
- `lastGeneratedAt`

### SelectedProject

- `repoId`, `owner`, `repoName`
- `displayTitle?`, `displayDescription?`
- `coverImageUrl?`, `demoUrl?`
- `tags[]`
- `isFeatured`, `displayOrder`
- cached metadata: `stars`, `primaryLanguage`, `repoUrl`, `lastUpdated`

### Template

- `id`, `name`, `type` (profile/portfolio)
- `version`
- `assets` (css/images)

### GeneratedAsset

- `id`
- `userId`
- `kind` (svg/html/zip/etc)
- `location` (blob path/url)
- `createdAt`

## Storage notes (non-binding)

- User configs fit well into a document DB.
- Generated assets are best stored outside the DB (blob/object storage).
- Cache is for GitHub API responses and expensive renders.
