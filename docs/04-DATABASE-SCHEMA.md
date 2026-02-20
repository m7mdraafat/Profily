# Profily — Database Schema

## Database: PostgreSQL (Neon Free Tier)

Connection: `postgresql://user:pass@ep-xxx.us-east-1.aws.neon.tech/profily`

---

## Entity Relationship Diagram

```
┌──────────────┐       ┌──────────────────┐       ┌──────────────┐
│    users     │       │   portfolios     │       │  templates   │
├──────────────┤       ├──────────────────┤       ├──────────────┤
│ id (PK)      │──1:N─▶│ id (PK)          │       │ id (PK)      │
│ github_id    │       │ user_id (FK)     │       │ name         │
│ username     │       │ template_id (FK) │◀─N:1──│ description  │
│ display_name │       │ customizations   │       │ html_template│
│ avatar_url   │       │ status           │       │ css_content  │
│ bio          │       │ deployed_url     │       │ js_content   │
│ ...          │       │ ...              │       │ ...          │
└──────┬───────┘       └────────┬─────────┘       └──────────────┘
       │                        │
       │ 1:N                    │ 1:N
       ▼                        ▼
┌──────────────┐       ┌──────────────────┐
│   projects   │       │  deployments     │
├──────────────┤       ├──────────────────┤
│ id (PK)      │       │ id (PK)          │
│ user_id (FK) │       │ portfolio_id (FK)│
│ name         │       │ status           │
│ language     │       │ commit_sha       │
│ topics[]     │       │ error_message    │
│ is_selected  │       │ started_at       │
│ ...          │       │ completed_at     │
└──────────────┘       └──────────────────┘
```

---

## Tables

### `users`

```sql
CREATE TABLE users (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    github_id         BIGINT UNIQUE NOT NULL,
    username          VARCHAR(100) UNIQUE NOT NULL,
    display_name      VARCHAR(200),
    avatar_url        TEXT,
    bio               TEXT,
    location          VARCHAR(200),
    company           VARCHAR(200),
    email             VARCHAR(320),
    github_url        TEXT NOT NULL,
    linkedin_url      TEXT,
    website_url       TEXT,
    access_token_enc  TEXT NOT NULL,                -- AES-256 encrypted GitHub token
    
    -- Aggregated GitHub data
    repos_count       INT DEFAULT 0,
    followers_count   INT DEFAULT 0,
    following_count   INT DEFAULT 0,
    contributions     INT DEFAULT 0,                -- this year
    top_languages     TEXT[] DEFAULT '{}',           -- ["C#", "Python", "JavaScript"]
    
    -- Inferred skills (jsonb for flexibility)
    skills            JSONB DEFAULT '[]',
    -- Example: [
    --   {"name":"C#","category":"backend","confidence":0.95,"repoCount":15,"lastUsed":"2026-02-19","displayOrder":0,"isUserEdited":false},
    --   {"name":"Python","category":"backend","confidence":0.88,"repoCount":8,"lastUsed":"2026-02-18","displayOrder":1,"isUserEdited":false}
    -- ]
    
    last_synced_at    TIMESTAMPTZ,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Indexes
CREATE UNIQUE INDEX idx_users_github_id ON users (github_id);
CREATE UNIQUE INDEX idx_users_username ON users (username);
CREATE INDEX idx_users_skills ON users USING GIN (skills);
```

---

### `projects`

```sql
CREATE TABLE projects (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id           UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    github_repo_id    BIGINT NOT NULL,
    name              VARCHAR(200) NOT NULL,
    full_name         VARCHAR(400),                  -- "m7mdraafat/VideStore.API"
    description       TEXT,
    custom_description TEXT,                          -- user override
    enhanced_description TEXT,                        -- future: DeepWiki AI
    language          VARCHAR(50),
    topics            TEXT[] DEFAULT '{}',            -- ["dotnet", "api", "clean-architecture"]
    stars             INT DEFAULT 0,
    forks             INT DEFAULT 0,
    is_fork           BOOLEAN DEFAULT FALSE,
    is_archived       BOOLEAN DEFAULT FALSE,
    html_url          TEXT NOT NULL,
    homepage_url      TEXT,
    
    -- Portfolio display
    is_selected       BOOLEAN DEFAULT FALSE,
    display_order     INT DEFAULT 0,
    
    last_pushed_at    TIMESTAMPTZ,
    synced_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    
    UNIQUE(user_id, github_repo_id)
);

-- Indexes
CREATE INDEX idx_projects_user_id ON projects (user_id);
CREATE INDEX idx_projects_selected ON projects (user_id, is_selected) WHERE is_selected = TRUE;
CREATE INDEX idx_projects_topics ON projects USING GIN (topics);
```

---

### `templates`

```sql
CREATE TABLE templates (
    id                VARCHAR(50) PRIMARY KEY,       -- "3d-purple", "minimal-clean"
    name              VARCHAR(100) NOT NULL,
    description       TEXT,
    thumbnail_url     TEXT,
    demo_html         TEXT,                           -- pre-rendered with sample data
    
    -- Template content (stored in R2, URL references here)
    layout_url        TEXT,                           -- R2 URL for layout.html
    css_url           TEXT,                           -- R2 URL for style.css
    js_url            TEXT,                           -- R2 URL for main.js
    sections_urls     JSONB DEFAULT '{}',             -- {"hero": "r2://...", "about": "r2://..."}
    
    features          TEXT[] DEFAULT '{}',            -- ["3D Effects", "Animated", "Dark Theme"]
    available_sections TEXT[] DEFAULT '{}',           -- ["hero","about","services","projects","skills","contact"]
    is_premium        BOOLEAN DEFAULT FALSE,
    is_active         BOOLEAN DEFAULT TRUE,
    sort_order        INT DEFAULT 0,
    
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

---

### `portfolios`

```sql
CREATE TABLE portfolios (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id           UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    template_id       VARCHAR(50) NOT NULL REFERENCES templates(id),
    
    -- All user customizations stored as jsonb
    customizations    JSONB DEFAULT '{
        "sections": {
            "hero":     {"visible": true},
            "about":    {"visible": true},
            "services": {"visible": true},
            "projects": {"visible": true},
            "skills":   {"visible": true},
            "contact":  {"visible": true}
        }
    }',
    
    status            VARCHAR(20) NOT NULL DEFAULT 'draft',   -- draft, deployed
    deployed_url      TEXT,                                    -- https://username.github.io
    last_deployed_at  TIMESTAMPTZ,
    
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    
    -- One portfolio per user (MVP)
    UNIQUE(user_id)
);

-- Indexes
CREATE INDEX idx_portfolios_user_id ON portfolios (user_id);
CREATE INDEX idx_portfolios_template_id ON portfolios (template_id);
```

---

### `deployments`

```sql
CREATE TABLE deployments (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    portfolio_id      UUID NOT NULL REFERENCES portfolios(id) ON DELETE CASCADE,
    
    status            VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, deploying, success, failed
    commit_sha        VARCHAR(40),
    deployed_url      TEXT,
    error_message     TEXT,
    
    -- Files deployed (for audit/rollback, optional)
    file_count        INT DEFAULT 0,
    
    started_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at      TIMESTAMPTZ
);

-- Indexes
CREATE INDEX idx_deployments_portfolio_id ON deployments (portfolio_id);
CREATE INDEX idx_deployments_status ON deployments (status) WHERE status IN ('pending', 'deploying');
```

---

## Sample Data

### Users
```sql
INSERT INTO users (id, github_id, username, display_name, avatar_url, bio, location, company, email, github_url, repos_count, followers_count, contributions, top_languages, skills, access_token_enc)
VALUES (
    '550e8400-e29b-41d4-a716-446655440000',
    141199348,
    'm7mdraafat',
    'Mohamed Raafat',
    'https://avatars.githubusercontent.com/u/141199348',
    'I would love to change the world, but they won''t give me the source code',
    'Egypt',
    'Microsoft',
    'm7mdraafat2003@gmail.com',
    'https://github.com/m7mdraafat',
    27,
    73,
    715,
    ARRAY['C#', 'Python', 'JavaScript', 'C++', 'HTML'],
    '[
        {"name":"C#","category":"backend","confidence":0.95,"repoCount":15,"displayOrder":0,"isUserEdited":false},
        {"name":"Python","category":"backend","confidence":0.88,"repoCount":8,"displayOrder":1,"isUserEdited":false},
        {"name":"C++","category":"backend","confidence":0.85,"repoCount":5,"displayOrder":2,"isUserEdited":false},
        {"name":"JavaScript","category":"frontend","confidence":0.82,"repoCount":4,"displayOrder":3,"isUserEdited":false},
        {"name":"Azure","category":"devops","confidence":0.85,"repoCount":6,"displayOrder":4,"isUserEdited":false},
        {"name":"Docker","category":"devops","confidence":0.80,"repoCount":5,"displayOrder":5,"isUserEdited":false},
        {"name":"SQL Server","category":"database","confidence":0.87,"repoCount":7,"displayOrder":6,"isUserEdited":false},
        {"name":"REST APIs","category":"backend","confidence":0.92,"repoCount":10,"displayOrder":7,"isUserEdited":false}
    ]',
    'ENCRYPTED_TOKEN_HERE'
);
```

---

## Storage Estimates

| Table | Avg Row Size | At 1k Users | At 33k Users (Neon limit) |
|---|---|---|---|
| users | ~2 KB | 2 MB | 66 MB |
| projects | ~500 B | 13.5 MB (27 avg repos) | 445 MB |
| portfolios | ~1 KB | 1 MB | 33 MB |
| templates | ~5 KB | ~50 KB (10 templates) | ~50 KB |
| deployments | ~200 B | 600 KB (3 deploys avg) | 20 MB |
| **Total** | | **~17 MB** | **~564 MB** |

Neon free limit: 512 MB. We'd hit the limit around **~30k users**. At that scale, revenue should justify upgrading.
