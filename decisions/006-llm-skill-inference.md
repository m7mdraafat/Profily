# ADR-006: LLM Skill Inference

**Status:** Accepted  
**Date:** 2026-03-27  

---

## Context

Profily's key differentiator is automated skill detection from GitHub repos. Instead of users manually typing skills (like GitFolio), we analyze their enabled repos and infer tech stack using an LLM — grouped into 4 categories with branded SVG icons.

## Constraints

- $0 LLM cost (free tiers only)
- Max 10 enabled repos per user
- Must handle 1,000 users/day
- .NET 8 JIT — plain HttpClient, no heavy SDKs
- No Redis — skills persisted in PostgreSQL

---

## LLM Provider Strategy

### 3-Tier Fallback

```
Groq (primary) → Gemini (fallback) → Rule-based (degraded)
```

| Tier | Provider | Model | RPD | TPD | Latency |
|---|---|---|---|---|---|
| Primary | Groq | `llama-3.1-8b-instant` | 14,400 | 500,000 | ~100-300ms |
| Fallback | Google | Gemini 2.5 Flash-Lite | ~1,500 | High | ~500ms-1s |
| Degraded | None (local) | Rule-based language mapping | ∞ | ∞ | <50ms |

**Fallback triggers:**
- Groq returns HTTP 429 (rate limited) or 5xx → try Gemini
- Gemini returns 429 or 5xx → fall back to rule-based
- Rule-based only maps GitHub Languages API → language skills (no frameworks, tools, or patterns)

### Integration (AOT-free, no SDK)

Both Groq and Gemini expose OpenAI-compatible REST APIs. Single `HttpClient` with swappable base URL:

```csharp
public sealed class LlmSkillInferenceService : ISkillInferenceService
{
    private readonly HttpClient _groqClient;
    private readonly HttpClient _geminiClient;

    public async Task<List<InferredSkill>> InferAsync(
        List<RepoAnalysisData> repos, CancellationToken ct)
    {
        try
        {
            return await CallLlmAsync(_groqClient, repos, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.TooManyRequests)
        {
            return await CallLlmAsync(_geminiClient, repos, ct);
        }
        catch
        {
            return FallbackRuleBasedMapping(repos);
        }
    }
}
```

---

## Inference Trigger

### Explicit "Generate" Button

User enables/disables repos in the Projects page. When ready, navigates to Skills page and clicks **"Generate"** to trigger inference.

```
Projects page:
    □ vite-react-template     [Enable]
    ■ VideStore.API            [Disable]    ← already enabled
    ■ LifeAdminAgent           [Disable]    ← already enabled
    □ BookkyStoreMVC           [Enable]
    ...

Skills page:
    ┌──────────────────────────────────────┐
    │  [✨ Generate]                       │
    │                                      │
    │  Auto-detect skills from your        │
    │  enabled repos.                      │
    └──────────────────────────────────────┘

    After generation:
    ┌──────────────────────────────────────┐
    │  Languages                           │
    │  [C#] [Python] [JavaScript]          │
    │                                      │
    │  Frameworks & Libraries              │
    │  [ASP.NET Core] [EF Core] [FastAPI]  │
    │                                      │
    │  Tools & Platforms                   │
    │  [Azure] [Docker] [PostgreSQL]       │
    │                                      │
    │  Architecture                        │
    │  [Clean Architecture] [REST APIs]    │
    │                                      │
    │  [+ Add Skill]  [✨ Re-generate]     │
    └──────────────────────────────────────┘
```

### Incremental Inference

Only new/changed repos are sent to the LLM. Previously inferred repos are skipped.

```
State tracking per repo:
    repo.skills_inferred_at: TIMESTAMPTZ | NULL
    repo.skills_hash: VARCHAR | NULL   ← hash of (languages + deps + topics)

On "Generate":
    1. Get all enabled repos
    2. For each repo:
        a. Fetch current data from GitHub (languages, deps, topics, folder structure)
        b. Compute hash of current data
        c. If hash == repo.skills_hash → skip (already inferred, data unchanged)
        d. If hash differs or NULL → include in LLM batch
    3. Send only new/changed repos to LLM
    4. Merge returned skills with existing skills:
        - Replace skills with source="inferred" from changed repos
        - Keep skills with source="manual" (user-added)
        - Keep skills from unchanged repos
    5. Persist to DB
```

**Benefits:**
- User enables 5 repos, generates. Later enables 2 more, generates again → only 2 repos sent to LLM
- Re-sync from GitHub + generate → only repos whose data actually changed get re-inferred
- Massively reduces token usage for returning users

---

## GitHub Data Extraction

For each repo included in inference, fetch:

| Data | API Call | Why |
|---|---|---|
| Languages (byte counts) | `GET /repos/{owner}/{repo}/languages` | Direct language detection |
| Repo metadata | Already synced (description, topics, stars, forks) | Context for inference |
| Dependency file | `GET /repos/{owner}/{repo}/contents/{file}` | Framework/library detection |
| Folder structure | `GET /repos/{owner}/{repo}/git/trees/main?recursive=1` | Architectural pattern detection |

### Dependency File Detection

Check for these files (in order, stop at first match per ecosystem):

| Ecosystem | File | What it reveals |
|---|---|---|
| .NET | `*.csproj` | NuGet packages → frameworks |
| Node.js | `package.json` | npm deps → frameworks |
| Python | `requirements.txt` or `pyproject.toml` | pip packages → frameworks |
| Go | `go.mod` | Go modules → frameworks |
| Rust | `Cargo.toml` | Crates → frameworks |
| Java | `pom.xml` or `build.gradle` | Maven/Gradle deps |
| DevOps | `Dockerfile`, `docker-compose.yml` | Docker |
| CI/CD | `.github/workflows/*.yml` | GitHub Actions |

**Only fetch dependency files that match the repo's detected languages.** If a repo is 100% Python, don't look for `package.json`.

### API Budget Per User

| Scenario | API calls | Notes |
|---|---|---|
| First generate (10 repos) | ~60 calls | 10 × (languages + metadata + deps + tree) |
| Re-generate (2 new repos) | ~12 calls | Only new repos |
| Re-generate (no changes) | 0 calls | Hash match → skip entirely |

GitHub API limit: 5,000 req/hour per authenticated user. 60 calls = 1.2% of budget.

---

## Prompt Design

```
System:
You are a skill inference engine for developer portfolios.
Analyze the developer's GitHub repos and classify their tech stack into exactly 4 categories.

Return valid JSON only:
{
  "languages": [
    { "name": "C#", "confidence": 0.95 }
  ],
  "frameworks_and_libraries": [
    { "name": "ASP.NET Core", "confidence": 0.92 }
  ],
  "tools_and_platforms": [
    { "name": "Azure", "confidence": 0.85 }
  ],
  "architectural_patterns": [
    { "name": "Clean Architecture", "confidence": 0.90 }
  ]
}

Rules:
- Languages: programming languages ONLY (from byte counts)
- Frameworks & Libraries: specific packages/frameworks (React, Entity Framework Core, FastAPI)
- Tools & Platforms: infrastructure, databases, CI/CD, cloud providers (Docker, PostgreSQL, Azure, GitHub Actions)
- Architectural Patterns: design patterns, architecture styles (Clean Architecture, CQRS, Microservices, REST APIs)
- confidence: 0.3 to 0.99 based on repo count, byte dominance, recency, stars
- Forked repos → lower confidence
- Max 10 items per category, ordered by confidence descending
- Use full display names ("ASP.NET Core" not "aspnetcore", "Entity Framework Core" not "EFCore")
- Only include skills with clear evidence in the data

User:
Analyze these repos:

Repo 1: VideStore.API
  Languages: { "C#": 85000, "HTML": 5000 }
  Description: "Full-stack e-commerce API with clean architecture"
  Topics: [dotnet, api, clean-architecture]
  Dependencies (.csproj): MediatR, FluentValidation, Microsoft.EntityFrameworkCore, Npgsql.EntityFrameworkCore.PostgreSQL
  Folders: /Domain/, /Application/, /Infrastructure/, /Api/, .github/workflows/

Repo 2: LifeAdminAgent
  Languages: { "Python": 12000, "Shell": 500 }
  Description: "AI-powered personal productivity agent"
  Topics: [ai-agent, llm, python]
  Dependencies (requirements.txt): langchain, openai, fastapi, pydantic, uvicorn
  Folders: /agents/, /tools/, /prompts/, Dockerfile

...
```

### Structured Output

Request JSON mode from Groq:

```json
{
  "model": "llama-3.1-8b-instant",
  "messages": [...],
  "response_format": { "type": "json_object" },
  "temperature": 0.1,
  "max_tokens": 1500
}
```

Low temperature (0.1) for deterministic results. JSON mode ensures parseable output.

---

## Skill Persistence

### Skill Sources

| Source | Meaning | On re-generate |
|---|---|---|
| `inferred` | Auto-detected by LLM | Replaced if repo data changed, kept if unchanged |
| `manual` | User added via "Add Skill" | Never touched by inference |

### DB Schema (Skills)

```sql
CREATE TABLE user_skills (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    category VARCHAR(50) NOT NULL,  -- languages, frameworks_and_libraries, tools_and_platforms, architectural_patterns
    confidence DECIMAL(3,2) NOT NULL,  -- 0.30 to 0.99
    icon_filename VARCHAR(100),  -- e.g., "csharp.svg", NULL if no icon
    source VARCHAR(20) NOT NULL DEFAULT 'inferred',  -- inferred | manual
    source_repo_id UUID REFERENCES projects(id),  -- which repo this was inferred from (NULL for manual)
    display_order INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),

    UNIQUE(user_id, name, category)
);

CREATE INDEX ix_user_skills_user_id ON user_skills(user_id);
```

**`source_repo_id`** — links an inferred skill to the repo it came from. When a repo is re-inferred, delete its old skills and insert new ones. Manual skills have `source_repo_id = NULL`.

### Icon Matching

LLM returns skill names. We match to SVG icons by normalized lookup:

```csharp
// Icon lookup table (bundled in app)
var iconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["C#"] = "csharp.svg",
    ["ASP.NET Core"] = "dotnet.svg",
    ["Python"] = "python.svg",
    ["React"] = "react.svg",
    ["Docker"] = "docker.svg",
    ["PostgreSQL"] = "postgresql.svg",
    ["Clean Architecture"] = null,  // No icon for patterns
    // ... 200+ mappings from devicon/svgl
};

string? iconFile = iconMap.GetValueOrDefault(skill.Name);
```

If no icon match → render as text-only badge. User can still manually add the skill.

---

## SSE Progress Events

```
User clicks "Generate"
    → SSE event: { "type": "inference-status", "status": "fetching", "message": "Fetching repo data..." }
    → SSE event: { "type": "inference-status", "status": "analyzing", "message": "Analyzing 3 new repos..." }
    → SSE event: { "type": "inference-status", "status": "inferring", "message": "Detecting skills..." }
    → SSE event: { "type": "inference-status", "status": "complete", "message": "Found 18 skills", "skillCount": 18 }
```

If fallback triggers:
```
    → SSE event: { "type": "inference-status", "status": "fallback", "message": "Using backup model..." }
```

---

## Capacity Planning

### Token Usage (Incremental Model)

| Scenario | Input tokens | Output tokens | Total |
|---|---|---|---|
| First generate (10 repos) | ~5,000-8,000 | ~500-1,000 | ~6,000-9,000 |
| Re-generate (2 new repos) | ~1,000-1,600 | ~200-400 | ~1,200-2,000 |
| Re-generate (no changes) | 0 | 0 | 0 (skipped) |

### Daily Capacity (Groq Free)

| Metric | Value |
|---|---|
| RPD | 14,400 |
| TPD | 500,000 |
| Avg tokens per first inference | ~7,500 |
| Max first-time inferences per day | ~66 (TPD-limited) |
| Avg tokens per incremental inference | ~1,500 |
| Max incremental inferences per day | ~333 |

**Realistic daily mix (1,000 users):**
- ~100 new users (first generate): ~750K tokens ← exceeds TPD
- ~200 returning users (incremental): ~300K tokens
- ~700 returning users (no changes): 0 tokens

**Problem:** 100 new users × 7,500 tokens = 750K, which exceeds Groq's 500K TPD.

**Mitigations:**
1. **Overflow to Gemini** — when Groq TPD is hit, route to Gemini (has its own independent limit)
2. **Reduce input** — send only top 5 dependency lines per repo (not full package.json)
3. **Truncate folder structure** — only send top-level folders, not recursive tree
4. **Realistic expectation** — 100 new users/day is an optimistic first month. Likely 10-30 new users/day initially, well within limits.

At **30 new users/day**: 30 × 7,500 = 225K tokens. Comfortable within 500K TPD.

---

## Rule-Based Fallback (Degraded Mode)

When both Groq and Gemini are unavailable:

```csharp
public List<InferredSkill> FallbackRuleBasedMapping(List<RepoAnalysisData> repos)
{
    var skills = new List<InferredSkill>();

    // Languages from GitHub API (deterministic)
    foreach (var (language, bytes) in repos.SelectMany(r => r.Languages))
    {
        skills.Add(new InferredSkill
        {
            Name = language,    // "C#", "Python", "JavaScript"
            Category = "languages",
            Confidence = CalculateLanguageConfidence(bytes, totalBytes)
        });
    }

    // No framework, tool, or pattern detection in fallback mode
    return skills.DistinctBy(s => s.Name).OrderByDescending(s => s.Confidence).ToList();
}
```

User sees only languages. A banner says: "Full skill detection temporarily unavailable. You can add skills manually."

---

## Key Decisions Summary

| # | Decision | Rationale |
|---|---|---|
| 1 | Groq primary, Gemini fallback, rule-based degraded | 3-tier resilience. Groq has best free RPD. Gemini as independent backup. |
| 2 | Explicit "Generate" button (not auto on toggle) | One LLM call per batch. Predictable, user-controlled. |
| 3 | Incremental inference (skip unchanged repos) | Hash-based change detection. Massively reduces token usage for returning users. |
| 4 | Merge strategy (preserve manual skills) | `source: inferred` vs `source: manual`. Re-inference only touches inferred skills. |
| 5 | Skill linked to source repo | `source_repo_id` enables per-repo re-inference without losing skills from other repos. |
| 6 | 4 categories including architectural patterns | Languages, Frameworks & Libraries, Tools & Platforms, Architectural Patterns. Folder structure provides pattern signals. |
| 7 | JSON mode + low temperature (0.1) | Deterministic, parseable output. |
| 8 | Icon matching by name lookup | 200+ icon mappings. No icon = text-only badge. |
| 9 | SSE for inference progress | Stream status: fetching → analyzing → inferring → complete. |
