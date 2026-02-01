# Portfolio Feature: Smart Extraction Pipeline

> **Decision Date:** February 1, 2026  
> **Status:** Approved for Phase 1  
> **Alternative Considered:** LLM-based description generation (deferred to future enhancement)

## Overview

The Portfolio Builder will use a **Smart Extraction Pipeline** to automatically populate project data when users select repositories. This approach avoids external AI dependencies while providing meaningful automation.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Smart Extraction Pipeline                     │
└─────────────────────────────────────────────────────────────────┘

User selects repos → Background job triggered → Extract & cache → UI updates

┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│ GitHub API   │────▶│ Extraction   │────▶│ Redis Cache  │
│ (Octokit)    │     │ Service      │     │ (1hr TTL)    │
└──────────────┘     └──────────────┘     └──────────────┘
       │                    │                     │
       ▼                    ▼                     ▼
  - Languages          - Parse README       - Store results
  - Topics             - Clean markdown     - Quick retrieval
  - Description        - Generate fallback  - Rate limit protection
  - README content
```

## 1. Tech Stack Extraction (No AI Needed)

### Data Sources

| Data | GitHub API Endpoint | Example Response |
|------|---------------------|------------------|
| Languages | `GET /repos/{owner}/{repo}/languages` | `{"TypeScript": 45000, "CSS": 12000}` |
| Topics | `repo.topics` | `["react", "typescript", "portfolio"]` |
| Dependencies | `GET /repos/{owner}/{repo}/contents/package.json` | File content (parse JSON) |

### Framework Detection Strategy

```
Dependency File          → Detected Frameworks
─────────────────────────────────────────────────
package.json             → React, Vue, Next.js, Express, etc.
*.csproj                 → ASP.NET Core, Blazor, WPF, etc.
requirements.txt         → Django, Flask, FastAPI, etc.
pyproject.toml           → Poetry-based Python projects
Cargo.toml               → Rust frameworks
go.mod                   → Go frameworks
```

### Skill Mapping File

Maintain a JSON mapping in `src/Profily.Core/Mappings/framework-mappings.json`:

```json
{
  "dependencies": {
    "react": { "name": "React", "category": "Frontend", "icon": "react" },
    "express": { "name": "Express.js", "category": "Backend", "icon": "nodejs" },
    "next": { "name": "Next.js", "category": "Framework", "icon": "nextjs" }
  },
  "packageReferences": {
    "Microsoft.AspNetCore": { "name": "ASP.NET Core", "category": "Backend", "icon": "dotnet" },
    "Microsoft.EntityFrameworkCore": { "name": "EF Core", "category": "ORM", "icon": "database" }
  }
}
```

## 2. Description Extraction (No AI Needed)

### Priority Chain

```
1. Use repo.description
   └─ IF exists AND length > 20 chars → USE IT ✓
   └─ ELSE → Continue to step 2

2. Parse README.md
   └─ Fetch via: GET /repos/{owner}/{repo}/readme
   └─ Decode Base64 content
   └─ Strip badges: regex [![.*?]\(.*?\)
   └─ Find first H1 heading → extract text
   └─ Find first paragraph after H1 → extract text
   └─ Clean markdown → plain text
   └─ IF result > 30 chars → USE IT ✓
   └─ ELSE → Continue to step 3

3. Generate fallback
   └─ "{repo.name} - A {primaryLanguage} project"
   └─ Append topics as tags if available
```

### README Parsing Rules

```
Input (README.md):
──────────────────
# My Awesome Project

[![Build Status](https://shields.io/...)](...)
[![License](https://shields.io/...)](...)

A modern web application for managing developer portfolios.

## Features
- Feature 1
- Feature 2

Output (Extracted):
───────────────────
Title: "My Awesome Project"
Description: "A modern web application for managing developer portfolios."
```

## 3. Processing Flow

### Trigger: Background Job

When user selects repos in the UI:

```
1. Frontend sends: POST /api/portfolio/repos/select
   Body: { "selectedRepoIds": ["repo1", "repo2", "repo3"] }

2. API queues background job for each new repo

3. Background job per repo:
   a. Check Redis cache (key: "repo:{owner}:{name}:extracted")
   b. If cached & fresh → return cached
   c. Else → fetch from GitHub API
   d. Run extraction pipeline
   e. Cache results (1hr TTL)
   f. Return to frontend via SignalR or polling

4. Frontend updates UI as data arrives
```

### API Rate Limit Protection

```
- GitHub API: 5000 requests/hour (authenticated)
- Strategy:
  ├─ Cache aggressively (Redis, 1hr TTL)
  ├─ Use conditional requests (If-None-Match with ETags)
  ├─ Limit tech stack analysis to top 20 repos by stars
  └─ Queue requests with exponential backoff on 429
```

## 4. User Customization

After extraction, users have **full control**:

| Field | Auto-populated | User Can |
|-------|----------------|----------|
| Title | repo.name | Edit freely |
| Description | Extracted/generated | Edit freely |
| Cover Image | None (or README screenshot) | Upload or paste URL |
| Demo URL | repo.homepage | Edit freely |
| Tech Stack | Auto-detected | Add/remove tags |
| Featured | false | Toggle on/off |
| Order | By selection order | Drag to reorder |

## 5. Data Model

```csharp
public class ExtractedProjectData
{
    // Auto-extracted (read-only reference)
    public string RepoId { get; set; }
    public string RepoName { get; set; }
    public string Owner { get; set; }
    public string? OriginalDescription { get; set; }
    public string? ExtractedDescription { get; set; }
    public string DescriptionSource { get; set; } // "repo" | "readme" | "fallback"
    public Dictionary<string, long> Languages { get; set; }
    public List<string> Topics { get; set; }
    public List<DetectedFramework> Frameworks { get; set; }
    public int Stars { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // User overrides (editable)
    public string? DisplayTitle { get; set; }
    public string? DisplayDescription { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? DemoUrl { get; set; }
    public List<string> CustomTags { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
}

public class DetectedFramework
{
    public string Name { get; set; }
    public string Category { get; set; }
    public string Icon { get; set; }
    public string Source { get; set; } // "package.json" | "csproj" | "topics"
}
```

## 6. Future Enhancement: AI Polish (Deferred)

When/if added later, AI would be an **optional enhancement**:

```
┌─────────────────────────────────────────────────────────────────┐
│  "✨ Enhance with AI" Button (Future Feature)                    │
└─────────────────────────────────────────────────────────────────┘

User clicks button → Send extracted description to AI →
AI returns polished marketing copy → User accepts or rejects

This is OPTIONAL and user-triggered, not automatic.
```

## 7. Implementation Order

```
Phase 1.1: Backend Foundation
├─ ITechStackService interface
├─ IGitHubService interface  
├─ TechStackAnalyzer implementation
├─ README parser utility
└─ Framework mappings JSON

Phase 1.2: API Endpoints
├─ GET /api/github/repos
├─ GET /api/github/repos/{id}/extract
├─ POST /api/portfolio/repos/select
└─ Background job queue setup

Phase 1.3: Frontend
├─ Repo selector with search/filter
├─ Drag-and-drop ordering (dnd-kit)
├─ Project customization modal
└─ Real-time extraction status
```

## Decision Log

| Decision | Rationale |
|----------|-----------|
| No AI for initial release | Faster, cheaper, no external dependencies, user can edit anyway |
| Background job processing | Better UX - UI remains responsive while extraction runs |
| 1hr cache TTL | Balance between freshness and rate limit protection |
| Full user edit control | Users know their projects best; auto-extraction is just a starting point |
