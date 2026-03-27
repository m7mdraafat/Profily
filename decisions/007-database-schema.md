# ADR-007: Database Schema

**Status:** Accepted  
**Date:** 2026-03-27  

---

## Context

PostgreSQL (Neon free tier, 0.5 GB). All entities consolidated from ADRs 001-006. Schema must support: GitHub OAuth users, DB sessions, repos with incremental inference tracking, 4-category skills, experience, education, social links, portfolios with jsonb customizations, and Pro plan management.

## Constraints

- 0.5 GB storage (~5,000 users at ~100 KB/user)
- EF Core via Npgsql
- Snake_case naming convention (PostgreSQL standard)
- UUIDs for primary keys (no sequential IDs exposed in API)
- `timestamptz` for all timestamps (UTC)

---

## Schema

### users

The core identity. Created on first GitHub OAuth login.

```sql
CREATE TABLE users (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    github_id               BIGINT NOT NULL UNIQUE,
    username                VARCHAR(100) NOT NULL UNIQUE,
    display_name            VARCHAR(200),
    avatar_url              VARCHAR(500),
    bio                     TEXT,
    location                VARCHAR(200),
    company                 VARCHAR(200),
    email                   VARCHAR(300),
    github_url              VARCHAR(500) NOT NULL,
    github_token_encrypted  BYTEA NOT NULL,             -- AES-256-GCM encrypted
    repos_count             INT NOT NULL DEFAULT 0,
    followers_count         INT NOT NULL DEFAULT 0,
    contributions_this_year INT NOT NULL DEFAULT 0,
    plan                    VARCHAR(20) NOT NULL DEFAULT 'free',   -- free | pro
    plan_expires_at         TIMESTAMPTZ,                 -- NULL for free, set for pro
    last_synced_at          TIMESTAMPTZ,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_users_username ON users(username);
CREATE INDEX ix_users_github_id ON users(github_id);
```

### sessions

DB-backed auth sessions (ADR-004). Cookie value = session ID.

```sql
CREATE TABLE sessions (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id          UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at       TIMESTAMPTZ NOT NULL,
    last_accessed_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_sessions_user_id ON sessions(user_id);
CREATE INDEX ix_sessions_expires_at ON sessions(expires_at);
```

### projects

GitHub repos synced on login and re-sync. Tracks enabled state and inference hash.

```sql
CREATE TABLE projects (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id             UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    github_repo_id      BIGINT NOT NULL,
    name                VARCHAR(300) NOT NULL,
    description         TEXT,
    custom_description  TEXT,                         -- user override (P1: AI-generated)
    language            VARCHAR(100),                 -- primary language
    topics              TEXT[] DEFAULT '{}',          -- GitHub topics array
    stars               INT NOT NULL DEFAULT 0,
    forks               INT NOT NULL DEFAULT 0,
    is_fork             BOOLEAN NOT NULL DEFAULT false,
    html_url            VARCHAR(500) NOT NULL,
    homepage_url        VARCHAR(500),
    is_enabled          BOOLEAN NOT NULL DEFAULT false,   -- user toggle (max 10 enabled)
    display_order       INT NOT NULL DEFAULT 0,
    skills_hash         VARCHAR(64),                 -- SHA-256 of (languages + deps + topics) for incremental inference
    skills_inferred_at  TIMESTAMPTZ,                 -- last inference timestamp
    last_pushed_at      TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),

    UNIQUE(user_id, github_repo_id)
);

CREATE INDEX ix_projects_user_id ON projects(user_id);
CREATE INDEX ix_projects_enabled ON projects(user_id, is_enabled) WHERE is_enabled = true;
```

### user_skills

Skills grouped into 4 categories. Linked to source repo for incremental inference.

```sql
CREATE TABLE user_skills (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name            VARCHAR(100) NOT NULL,
    category        VARCHAR(50) NOT NULL,       -- languages | frameworks_and_libraries | tools_and_platforms | architectural_patterns
    confidence      DECIMAL(3,2) NOT NULL,      -- 0.30 to 0.99
    icon_filename   VARCHAR(100),               -- e.g., "csharp.svg", NULL if no icon
    source          VARCHAR(20) NOT NULL DEFAULT 'inferred',  -- inferred | manual
    source_repo_id  UUID REFERENCES projects(id) ON DELETE SET NULL,
    display_order   INT NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    UNIQUE(user_id, name, category)
);

CREATE INDEX ix_user_skills_user_id ON user_skills(user_id);
CREATE INDEX ix_user_skills_source_repo ON user_skills(source_repo_id);
```

**`ON DELETE SET NULL` for `source_repo_id`:** If a repo is deleted (user removes it from GitHub), the skill stays but loses its repo link. User can still see/edit it as a manual skill.

### experiences

Work experience entries. Manual CRUD.

```sql
CREATE TABLE experiences (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title         VARCHAR(200) NOT NULL,
    company       VARCHAR(200) NOT NULL,
    start_date    DATE NOT NULL,
    end_date      DATE,                        -- NULL = current position
    is_current    BOOLEAN NOT NULL DEFAULT false,
    description   TEXT,
    display_order INT NOT NULL DEFAULT 0,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_experiences_user_id ON experiences(user_id);
```

### educations

Education entries. Manual CRUD.

```sql
CREATE TABLE educations (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    degree        VARCHAR(200) NOT NULL,
    school        VARCHAR(200) NOT NULL,
    start_date    DATE NOT NULL,
    end_date      DATE,
    description   TEXT,
    display_order INT NOT NULL DEFAULT 0,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_educations_user_id ON educations(user_id);
```

### social_links

Freeform platform + URL. GitHub auto-filled on sync.

```sql
CREATE TABLE social_links (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    platform      VARCHAR(50) NOT NULL,         -- github, linkedin, twitter, leetcode, facebook, medium, kaggle, etc.
    url           VARCHAR(500) NOT NULL,
    icon_filename VARCHAR(100),                 -- e.g., "github.svg"
    display_order INT NOT NULL DEFAULT 0,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),

    UNIQUE(user_id, platform)
);

CREATE INDEX ix_social_links_user_id ON social_links(user_id);
```

### portfolios

One per user (MVP). Stores template choice, status, and section customizations as jsonb.

```sql
CREATE TABLE portfolios (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE UNIQUE,
    template_id     VARCHAR(50) NOT NULL,        -- e.g., "3d-purple"
    status          VARCHAR(20) NOT NULL DEFAULT 'draft',  -- draft | published
    customizations  JSONB NOT NULL DEFAULT '{}', -- section-level overrides
    deployed_url    VARCHAR(500),                -- e.g., "https://m7mdraafat.profily.dev"
    github_pages_url VARCHAR(500),              -- e.g., "https://m7mdraafat.github.io"
    published_at    TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

**`UNIQUE(user_id)`** — one portfolio per user in MVP. P1 adds multiple (swap `UNIQUE` for an `is_active` flag).

#### customizations jsonb structure

```json
{
  "hero": {
    "greeting": "Hello, I Am",
    "role_texts": ["Software Engineer", "Backend Developer"]
  },
  "about": {
    "title": "About Me",
    "subtitle": "Get To Know",
    "description": "Custom about text..."
  },
  "projects": {
    "title": "Featured Projects",
    "subtitle": "My Recent Work"
  },
  "skills": {
    "title": "Tech Stack",
    "subtitle": "My Abilities"
  },
  "experience": {
    "title": "Experience",
    "subtitle": "My Journey"
  },
  "education": {
    "title": "Education",
    "subtitle": "Academic Background"
  },
  "contact": {
    "title": "Contact Me",
    "subtitle": "Get In Touch"
  }
}
```

**Why jsonb:**
- Flexible — add new section overrides without migrations
- Templates consume data as a JSON object — matches naturally
- Always loaded as a whole (no need for individual section queries)
- PostgreSQL jsonb supports indexing if we ever need to query into it

**What goes in customizations vs dedicated columns:**
- `template_id`, `status`, `deployed_url` → dedicated columns (queried, filtered)
- Section titles, greetings, descriptions → jsonb (template-specific, loaded as blob)

### feature_flags

Simple feature toggle (ADR-005).

```sql
CREATE TABLE feature_flags (
    name        VARCHAR(100) PRIMARY KEY,
    is_enabled  BOOLEAN NOT NULL DEFAULT false,
    description TEXT
);
```

### payment_events

Payment webhook log for debugging and support (ADR-008).

```sql
CREATE TABLE payment_events (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID REFERENCES users(id) ON DELETE SET NULL,
    paymob_order_id VARCHAR(100),
    transaction_id  VARCHAR(100),
    event_type      VARCHAR(50) NOT NULL,    -- payment_success | payment_failed | refund | subscription_inactive
    amount_cents    INT,
    currency        VARCHAR(10),
    raw_payload     JSONB,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_payment_events_user_id ON payment_events(user_id);
CREATE UNIQUE INDEX ix_payment_events_transaction ON payment_events(transaction_id);
```

---

## Entity Relationship Diagram

```
users (1)
  │
  ├── (1:N) sessions
  │
  ├── (1:N) projects
  │       │
  │       └── (1:N) user_skills  [via source_repo_id]
  │
  ├── (1:N) user_skills  [via user_id]
  │
  ├── (1:N) experiences
  │
  ├── (1:N) educations
  │
  ├── (1:N) social_links
  │
  ├── (1:1) portfolios
  │
  └── (1:N) payment_events

feature_flags (standalone)
```

---

## Storage Estimate

| Table | Avg row size | Rows per user | Per user |
|---|---|---|---|
| users | ~500 B | 1 | 500 B |
| sessions | ~100 B | 1-3 | 300 B |
| projects | ~400 B | 28 avg | 11.2 KB |
| user_skills | ~200 B | 15-25 | 5 KB |
| experiences | ~300 B | 2-5 | 1.5 KB |
| educations | ~250 B | 1-3 | 750 B |
| social_links | ~150 B | 3-6 | 900 B |
| portfolios | ~2 KB (jsonb) | 1 | 2 KB |
| **Total** | | | **~22 KB** |

**Neon free tier: 0.5 GB = 512,000 KB / 22 KB ≈ ~23,000 users.** Well beyond the 5,000 estimate. Plenty of room.

---

## EF Core Considerations

### DbContext

```csharp
public sealed class ProfilyDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<SocialLink> SocialLinks => Set<SocialLink>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
}
```

### Notable EF Core mappings

```csharp
// PostgreSQL array type for topics
builder.Property(p => p.Topics)
    .HasColumnType("text[]");

// jsonb for customizations
builder.Property(p => p.Customizations)
    .HasColumnType("jsonb");

// Encrypted GitHub token stored as bytea
builder.Property(u => u.GitHubTokenEncrypted)
    .HasColumnType("bytea");
```

### Snake Case Convention

Using `EFCore.NamingConventions` NuGet package:

```csharp
optionsBuilder.UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention();
```

`UserSkill` → `user_skills`, `DisplayOrder` → `display_order`, etc.

---

## Indexes Summary

| Table | Index | Purpose |
|---|---|---|
| users | `ix_users_username` | Lookup by username (portfolio serving) |
| users | `ix_users_github_id` | OAuth login lookup |
| sessions | `ix_sessions_user_id` | Find user's sessions |
| sessions | `ix_sessions_expires_at` | Cleanup expired sessions |
| projects | `ix_projects_user_id` | List user's repos |
| projects | `ix_projects_enabled` | Partial index on enabled repos (inference) |
| user_skills | `ix_user_skills_user_id` | Load user's skills |
| user_skills | `ix_user_skills_source_repo` | Delete skills when repo re-inferred |
| experiences | `ix_experiences_user_id` | Load user's experience |
| educations | `ix_educations_user_id` | Load user's education |
| social_links | `ix_social_links_user_id` | Load user's links |

---

## Migration Strategy

- EF Core migrations via `dotnet ef migrations add`
- `ProfilyDbContextFactory` for design-time migration generation
- Migrations applied on deploy via `dotnet ef database update` in GitHub Actions pipeline
- No manual SQL scripts — EF Core is the single source of truth

---

## Key Decisions Summary

| # | Decision | Rationale |
|---|---|---|
| 1 | UUIDs for all PKs | No sequential IDs in API responses. Prevents enumeration. |
| 2 | Snake_case naming | PostgreSQL convention. `EFCore.NamingConventions` handles mapping. |
| 3 | `jsonb` for portfolio customizations | Flexible, no migration needed for new section overrides, matches template consumption pattern. |
| 4 | Freeform social links (varchar platform) | Users have unpredictable platforms. No enum limitation. |
| 5 | `plan` + `plan_expires_at` on users (no subscription table) | Simple for MVP with one plan. Paymob webhook updates these two columns. |
| 6 | `skills_hash` on projects | Enables incremental inference — skip unchanged repos. |
| 7 | `source` + `source_repo_id` on user_skills | Merge strategy: re-inference replaces inferred skills, preserves manual skills. |
| 8 | Partial index on enabled projects | Only index the max 10 enabled repos per user — smaller, faster index. |
| 9 | `ON DELETE CASCADE` on all user FK | Delete user → everything goes. Clean. |
| 10 | `ON DELETE SET NULL` on source_repo_id | Deleted repo doesn't delete the skill — just unlinks it. |
