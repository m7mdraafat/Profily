# ADR-003: Template System

**Status:** Accepted  
**Date:** 2026-03-27  

---

## Context

Profily generates static HTML portfolios from user data + templates. We need to decide on the template structure, rendering engine, storage, and how templates evolve over time.

## Decisions

### Rendering Engine: Scriban

**Why Scriban:**
- Lightweight (~100 KB NuGet), purpose-built for template rendering
- Sandboxed — template authors can't execute arbitrary C# (safe for future community templates)
- Supports loops, conditionals, filters — everything needed for section-based templates
- Works with JIT (no AOT restrictions)
- Clean syntax familiar to frontend developers

**Syntax examples:**
```html
<!-- Simple value -->
<h1>{{ profile.name }}</h1>

<!-- Loop -->
{{ for project in projects }}
  <div class="project-card">
    <h3>{{ project.name }}</h3>
    <p>{{ project.description }}</p>
    {{ for tag in project.topics }}
      <span class="tag">{{ tag }}</span>
    {{ end }}
  </div>
{{ end }}

<!-- Conditional -->
{{ if profile.company }}
  <p>Currently at {{ profile.company }}</p>
{{ end }}

<!-- Filter -->
<p>{{ profile.bio | string.truncate 120 }}</p>
```

**NuGet:** `Scriban` (no additional packages needed)

### Template Structure: Section-Based

Each template is a folder with a layout wrapper and individual section files.

```
templates/{template-id}/
├── manifest.json              ← Template metadata
├── layout.html                ← Page wrapper (head, nav, footer, {{ sections }})
├── sections/
│   ├── hero.html              ← Hero/intro section
│   ├── about.html             ← About me
│   ├── projects.html          ← Featured projects grid
│   ├── skills.html            ← Tech stack (4 categories)
│   ├── experience.html        ← Work experience timeline
│   ├── education.html         ← Education entries
│   ├── contact.html           ← Contact info + social links
│   └── services.html          ← (optional) Services offered
├── css/
│   └── style.css
├── js/
│   └── main.js
├── assets/                    ← Template-specific assets (fonts, images, etc.)
└── thumbnail.png              ← Gallery preview image
```

**Why section-based over single-file:**
- Clean separation — each section is independently editable and testable
- Enables P1 section toggling — just skip rendering hidden sections
- Easier for community contributors — edit one section without touching the whole template
- Sections are composable — could mix sections from different templates in future

### manifest.json

```json
{
  "id": "3d-purple",
  "name": "3D Purple",
  "description": "Modern 3D portfolio with particle effects and purple accent theme",
  "author": "Profily",
  "version": "1.0.0",
  "thumbnail": "thumbnail.png",
  "tags": ["dark", "3d", "animated"],
  "isPro": false,
  "sections": [
    { "id": "hero", "name": "Hero", "required": true },
    { "id": "about", "name": "About Me", "required": false },
    { "id": "projects", "name": "Projects", "required": true },
    { "id": "skills", "name": "Tech Stack", "required": true },
    { "id": "experience", "name": "Experience", "required": false },
    { "id": "education", "name": "Education", "required": false },
    { "id": "contact", "name": "Contact", "required": false }
  ],
  "features": ["3D Effects", "Animated", "Dark Theme"]
}
```

- `isPro: false` — free template. Pro templates have `isPro: true`.
- `required` — section cannot be hidden (P1 toggle feature). Hero, Projects, Skills always shown.
- `sections` array defines render order.

### Rendering Pipeline

```
1. Load template manifest
2. Load layout.html
3. For each section in manifest.sections:
    a. Load sections/{id}.html
    b. Render with Scriban (inject user data context)
    c. Append to rendered sections string
4. Inject rendered sections into layout.html ({{ sections }} placeholder)
5. Inject CSS/JS references
6. Return complete HTML string
```

**Code sketch:**

```csharp
public sealed class TemplateRenderer
{
    public async Task<string> RenderAsync(string templateId, PortfolioData data)
    {
        var manifest = LoadManifest(templateId);
        var layout = LoadTemplate(templateId, "layout.html");

        var renderedSections = new StringBuilder();
        foreach (var section in manifest.Sections)
        {
            var sectionHtml = LoadTemplate(templateId, $"sections/{section.Id}.html");
            var template = Template.Parse(sectionHtml);
            var rendered = await template.RenderAsync(data.ToScriptObject());
            renderedSections.Append(rendered);
        }

        var layoutTemplate = Template.Parse(layout);
        var result = await layoutTemplate.RenderAsync(new ScriptObject
        {
            { "sections", renderedSections.ToString() },
            { "profile", data.Profile },
            { "meta", data.Meta }
        });

        return result;
    }
}
```

### Template Data Contract

The data object passed to every template:

```json
{
  "profile": {
    "name": "Mohamed Raafat",
    "username": "m7mdraafat",
    "avatarUrl": "https://avatars.githubusercontent.com/u/...",
    "bio": "I would love to change the world...",
    "location": "Egypt",
    "company": "Microsoft",
    "email": "m7mdraafat2003@gmail.com",
    "githubUrl": "https://github.com/m7mdraafat",
    "reposCount": 27,
    "followersCount": 73,
    "contributionsThisYear": 715
  },

  "projects": [
    {
      "name": "VideStore.API",
      "description": "Full-stack e-commerce API with clean architecture",
      "language": "C#",
      "topics": ["dotnet", "api", "clean-architecture"],
      "stars": 1,
      "forks": 0,
      "url": "https://github.com/m7mdraafat/VideStore.API",
      "displayOrder": 0
    }
  ],

  "skills": {
    "languages": [
      { "name": "C#", "icon": "csharp.svg", "displayOrder": 0 }
    ],
    "frameworksAndLibraries": [
      { "name": "ASP.NET Core", "icon": "dotnet.svg", "displayOrder": 0 }
    ],
    "toolsAndPlatforms": [
      { "name": "Azure", "icon": "azure.svg", "displayOrder": 0 }
    ],
    "architecturalPatterns": [
      { "name": "Clean Architecture", "icon": null, "displayOrder": 0 }
    ]
  },

  "experience": [
    {
      "title": "Software Engineer",
      "company": "Microsoft",
      "startDate": "2025-06",
      "endDate": null,
      "isCurrent": true,
      "description": "Building scalable backend systems...",
      "displayOrder": 0
    }
  ],

  "education": [
    {
      "degree": "Bachelor of Computer Science",
      "school": "Cairo University",
      "startDate": "2021-09",
      "endDate": "2025-06",
      "description": null,
      "displayOrder": 0
    }
  ],

  "socials": [
    { "platform": "github", "url": "https://github.com/m7mdraafat", "icon": "github.svg" },
    { "platform": "linkedin", "url": "https://linkedin.com/in/...", "icon": "linkedin.svg" },
    { "platform": "leetcode", "url": "https://leetcode.com/u/mo_raafat/", "icon": "leetcode.svg" }
  ],

  "sections": {
    "hero": { "visible": true, "greeting": "Hello, I Am" },
    "about": { "visible": true },
    "projects": { "visible": true },
    "skills": { "visible": true },
    "experience": { "visible": true },
    "education": { "visible": true },
    "contact": { "visible": true }
  },

  "meta": {
    "generatedAt": "2026-03-27T12:00:00Z",
    "templateId": "3d-purple",
    "profilyUrl": "https://profily.dev",
    "year": 2026,
    "isPro": false
  }
}
```

**Notes:**
- `meta.isPro` controls whether "Built with Profily" footer renders
- `skills` is grouped into 4 categories (not a flat list)
- `skills[].icon` is the SVG filename — `null` if no icon exists (render text-only badge)
- `sections[].visible` is always `true` in MVP. P1 adds toggling.
- `experience` and `education` are empty arrays if user hasn't added any — template handles empty state

### Template Storage

**MVP: Bundled in app**

```
apps/api/src/Profily.Api/
└── Templates/                  ← Embedded or content files
    ├── 3d-purple/
    ├── minimal-clean/
    └── developer-terminal/
```

Templates ship with the API deployment. Adding a new template requires redeploying.

**Future: Cloudflare R2**

```
R2 bucket:
└── templates/
    ├── 3d-purple/
    │   ├── manifest.json
    │   ├── layout.html
    │   ├── sections/...
    │   ├── css/...
    │   └── thumbnail.png
    ├── minimal-clean/
    └── developer-terminal/
```

Migration path:
1. Move template files from app bundle to R2
2. `TemplateLoader` interface stays the same — swap `FileSystemLoader` for `R2Loader`
3. Cache loaded templates in IMemoryCache (1 hour TTL)
4. New templates can be added without redeploying the API

### Preview vs Publish

Same rendering code, different output:

```
Preview:
    User clicks "Preview"
    → API renders HTML (TemplateRenderer.RenderAsync)
    → Returns HTML string in API response
    → Frontend renders in <iframe>

Publish:
    User clicks "Publish"
    → API renders HTML (same code path)
    → Uploads to R2: portfolios/{username}/index.html
    → Uploads CSS/JS alongside: portfolios/{username}/css/style.css
    → Cloudflare Worker serves at username.profily.dev

GitHub Pages (Pro):
    User clicks "Deploy to GitHub Pages"
    → API renders HTML (same code path)
    → Pushes files via GitHub Git Tree API to {username}.github.io repo
```

### Launch Templates (3)

| Template | Style | Free/Pro |
|---|---|---|
| **3D Purple** | Dark theme, particle effects, purple accents, GSAP animations | Free |
| **Minimal Clean** | Light/dark toggle, clean typography, subtle animations | Pro |
| **Developer Terminal** | Terminal aesthetic, monospace font, green-on-black, typing effects | Pro |

1 free template. Users see all 3 in gallery (Pro templates shown with lock icon). Upgrading unlocks them instantly.

### Future: Community Templates

Structure for community contributions:

```
templates/
├── official/               ← Profily-maintained templates
│   ├── 3d-purple/
│   └── minimal-clean/
└── community/              ← Community PRs
    └── retro-pixel/
        ├── manifest.json   ← Must follow schema
        ├── layout.html
        ├── sections/
        └── thumbnail.png
```

Community templates submitted via GitHub PR → reviewed → merged → available in gallery. `manifest.json` schema validation ensures all required sections and fields exist.

---

## Key Decisions Summary

| # | Decision | Rationale |
|---|---|---|
| 1 | Scriban rendering engine | Lightweight, sandboxed, loop/conditional support, safe for community templates |
| 2 | Section-based template structure | Enables P1 section toggling, cleaner separation, easier community contributions |
| 3 | Bundled in app (MVP), R2 later | Simple now, same interface — swap loader implementation when needed |
| 4 | Same render code for preview and publish | No divergence between what user sees in preview and what gets deployed |
| 5 | 1 free template, 2 Pro | Natural upgrade trigger — user sees locked templates in gallery |
| 6 | Community templates via GitHub PRs (future) | Low-friction contribution model, version controlled, reviewed before merge |
