# GitHub Profile Builder - Architecture Design

## Overview

A flexible, template-based GitHub Profile README generator where:
- **Sections** are modular building blocks (header, stats, tech stack, etc.)
- **Section Styles** are different visual implementations of the same section
- **Templates** are curated combinations of sections + pre-selected styles
- **Generated Output** includes README.md, GitHub workflows, and assets

---

## Version Roadmap

| Version | Features |
|---------|----------|
| **V1 (Current)** | Templates, Sections, Styles (admin-managed), User configs, Deploy |
| **V2 (Future)** | Community contributions, Versioning, Admin approval workflow |

---

## Current Backend Analysis

### Existing Architecture

```
Profily.Core/              # Domain Layer
├── Interfaces/
│   ├── IAuthService.cs
│   ├── ICosmosDbService.cs
│   └── IGitHubService.cs
├── Models/
│   ├── User.cs
│   ├── Auth/UserInfoResponse.cs
│   └── GitHub/
│       ├── GitHubRepository.cs
│       ├── GitHubStats.cs
│       └── LanguageStat.cs
└── Options/
    ├── CosmosDbOptions.cs
    ├── GitHubOptions.cs
    └── CorsOptions.cs

Profily.Infrastructure/    # Infrastructure Layer
├── Data/
│   └── CosmosDbService.cs
├── Services/
│   └── AuthService.cs
├── GitHub/
│   └── GitHubService.cs
└── Extensions/
    ├── ServiceCollectionExtensions.cs
    ├── AuthenticationExtensions.cs
    └── CorsExtensions.cs

Profily.Api/               # API Layer
├── Program.cs
└── Endpoints/
    ├── AuthEndpoints.cs
    └── GitHubEndpoints.cs
```

### Current Capabilities

| Feature | Status | Details |
|---------|--------|---------|
| User Authentication | ✅ | GitHub OAuth with JWT |
| User Storage | ✅ | Cosmos DB |
| GitHub Repos | ✅ | `GET /api/github/repos` |
| GitHub Stats | ✅ | `GET /api/github/stats` |
| Language Stats | ✅ | `GET /api/github/repos/{owner}/{repo}/languages` |

### Database Architecture

- **Cosmos DB** with single container
- **Partition Key**: `/userId`
- **Document Type Discriminator**: `type` field
- Documents: `{ type: "user", ... }`

---

## V1 Backend Design

### Data Model (Cosmos DB Documents)

All documents stored in single container with `type` discriminator.

#### 1. Section Definition
```json
{
  "id": "section-header",
  "type": "section",
  "userId": "system",
  "slug": "header",
  "displayName": "Header / Title",
  "description": "Dynamic SVG header with your name",
  "icon": "👋",
  "sortOrder": 0,
  "requiredDataFields": ["username", "displayName"],
  "isActive": true,
  "createdAt": "2026-02-03T00:00:00Z",
  "updatedAt": "2026-02-03T00:00:00Z"
}
```

#### 2. Section Style
```json
{
  "id": "style-header-capsule-render",
  "type": "sectionStyle",
  "userId": "system",
  "sectionId": "section-header",
  "slug": "capsule-render",
  "displayName": "3D Capsule Wave",
  "description": "Animated 3D wave header using capsule-render",
  "previewImageUrl": "/previews/header-capsule.png",
  
  "markdownTemplate": "<p align=\"center\">\n  <img src=\"https://capsule-render.vercel.app/api?type=waving&color={{theme.gradient}}&height=200&section=header&text={{displayName}}&fontSize=50&fontColor={{theme.textColor}}&animation=fadeIn\" />\n</p>\n\n<p align=\"center\">\n  <em>{{tagline}}</em>\n</p>",
  
  "workflows": [],
  "assets": [],
  "configSchema": {
    "type": "object",
    "properties": {
      "animationType": {
        "type": "string",
        "enum": ["fadeIn", "scaleIn", "twinkling"],
        "default": "fadeIn"
      }
    }
  },
  
  "isActive": true,
  "createdAt": "2026-02-03T00:00:00Z",
  "updatedAt": "2026-02-03T00:00:00Z"
}
```

#### 3. Template
```json
{
  "id": "template-developer-pro",
  "type": "template",
  "userId": "system",
  "slug": "developer-pro",
  "displayName": "Developer Pro",
  "description": "Full-featured profile with metrics, 3D contributions, and stats",
  "icon": "🚀",
  "previewImageUrl": "/previews/template-developer-pro.png",
  
  "theme": {
    "id": "dark",
    "name": "Dark",
    "primary": "#58a6ff",
    "secondary": "#8b949e",
    "background": "#0d1117",
    "gradient": "0:EEFF00,100:a]82da",
    "textColor": "#ffffff"
  },
  
  "sections": [
    { "sectionId": "section-header", "styleId": "style-header-capsule-render", "order": 0, "enabled": true, "config": {} },
    { "sectionId": "section-badges", "styleId": "style-badges-shields", "order": 1, "enabled": true, "config": {} },
    { "sectionId": "section-snake", "styleId": "style-snake-dark", "order": 2, "enabled": true, "config": {} },
    { "sectionId": "section-about", "styleId": "style-about-bullets", "order": 3, "enabled": true, "config": {} },
    { "sectionId": "section-skills", "styleId": "style-skills-icons", "order": 4, "enabled": true, "config": {} },
    { "sectionId": "section-stats", "styleId": "style-stats-combined", "order": 5, "enabled": true, "config": {} },
    { "sectionId": "section-repos", "styleId": "style-repos-cards", "order": 6, "enabled": true, "config": {} },
    { "sectionId": "section-contact", "styleId": "style-contact-badges", "order": 7, "enabled": true, "config": {} },
    { "sectionId": "section-footer", "styleId": "style-footer-wave", "order": 8, "enabled": true, "config": {} }
  ],
  
  "isOfficial": true,
  "isActive": true,
  "usageCount": 0,
  "createdAt": "2026-02-03T00:00:00Z",
  "updatedAt": "2026-02-03T00:00:00Z"
}
```

#### 4. User Profile Config
```json
{
  "id": "config-{userId}",
  "type": "profileConfig",
  "userId": "{userId}",
  
  "templateId": "template-developer-pro",
  
  "theme": {
    "id": "dark",
    "primary": "#58a6ff",
    "secondary": "#8b949e",
    "background": "#0d1117"
  },
  
  "sections": [
    { "sectionId": "section-header", "styleId": "style-header-capsule-render", "order": 0, "enabled": true, "config": {} },
    { "sectionId": "section-stats", "styleId": "style-stats-cards", "order": 1, "enabled": true, "config": {} }
  ],
  
  "content": {
    "displayName": "Mohamed Raafat",
    "tagline": "Full Stack Developer | Open Source Enthusiast",
    "aboutMe": "Passionate about building great products...",
    "customSkills": ["C#", "TypeScript", "Python", "Azure"],
    "pinnedRepoNames": ["Profily", "awesome-project"]
  },
  
  "socialLinks": {
    "linkedin": "https://linkedin.com/in/m7mdraafat",
    "twitter": "https://twitter.com/m7mdraafat",
    "email": "email@example.com"
  },
  
  "preferences": {
    "showOpenToWork": false,
    "showProfileViews": true
  },
  
  "isDraft": true,
  "lastDeployedAt": null,
  "lastDeployedConfigSnapshot": null,
  
  "createdAt": "2026-02-03T00:00:00Z",
  "updatedAt": "2026-02-03T00:00:00Z"
}
```

#### 5. Deploy History
```json
{
  "id": "deploy-{guid}",
  "type": "deployHistory",
  "userId": "{userId}",
  
  "configSnapshot": { /* full config at deploy time */ },
  "templateId": "template-developer-pro",
  
  "result": {
    "success": true,
    "commitSha": "abc123def456",
    "repoUrl": "https://github.com/username/username",
    "filesDeployed": ["README.md", ".github/workflows/snake.yml"]
  },
  
  "createdAt": "2026-02-03T00:00:00Z"
}
```

---

## V1 C# Models

### New Files to Create

```
Profily.Core/
├── Models/
│   └── Profile/
│       ├── Section.cs
│       ├── SectionStyle.cs
│       ├── ProfileTemplate.cs
│       ├── ProfileConfig.cs
│       ├── DeployHistory.cs
│       └── DTOs/
│           ├── TemplateDto.cs
│           ├── SectionDto.cs
│           ├── ProfileConfigDto.cs
│           └── GeneratedProfileDto.cs
├── Interfaces/
│   ├── IProfileService.cs
│   ├── ITemplateService.cs
│   └── IReadmeGeneratorService.cs

Profily.Infrastructure/
├── Services/
│   ├── ProfileService.cs
│   ├── TemplateService.cs
│   └── ReadmeGeneratorService.cs
├── Data/
│   └── SeedData/
│       ├── SectionSeeder.cs
│       ├── StyleSeeder.cs
│       └── TemplateSeeder.cs

Profily.Api/
└── Endpoints/
    └── ProfileEndpoints.cs
```

### Core Models

```csharp
// Profily.Core/Models/Profile/Section.cs
public class Section
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "section";
    
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "system";
    
    [JsonPropertyName("slug")]
    public required string Slug { get; set; }
    
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
    
    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
    
    [JsonPropertyName("requiredDataFields")]
    public List<string> RequiredDataFields { get; set; } = [];
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

```csharp
// Profily.Core/Models/Profile/SectionStyle.cs
public class SectionStyle
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "sectionStyle";
    
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "system";
    
    [JsonPropertyName("sectionId")]
    public required string SectionId { get; set; }
    
    [JsonPropertyName("slug")]
    public required string Slug { get; set; }
    
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("previewImageUrl")]
    public string? PreviewImageUrl { get; set; }
    
    [JsonPropertyName("markdownTemplate")]
    public required string MarkdownTemplate { get; set; }
    
    [JsonPropertyName("workflows")]
    public List<WorkflowDefinition>? Workflows { get; set; }
    
    [JsonPropertyName("assets")]
    public List<AssetDefinition>? Assets { get; set; }
    
    [JsonPropertyName("configSchema")]
    public JsonDocument? ConfigSchema { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class WorkflowDefinition
{
    [JsonPropertyName("path")]
    public required string Path { get; set; }
    
    [JsonPropertyName("template")]
    public required string Template { get; set; }
}

public class AssetDefinition
{
    [JsonPropertyName("path")]
    public required string Path { get; set; }
    
    [JsonPropertyName("content")]
    public required string Content { get; set; }
    
    [JsonPropertyName("type")]
    public required string Type { get; set; } // "svg", "png", "gif"
}
```

```csharp
// Profily.Core/Models/Profile/ProfileTemplate.cs
public class ProfileTemplate
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "template";
    
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "system";
    
    [JsonPropertyName("slug")]
    public required string Slug { get; set; }
    
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
    
    [JsonPropertyName("previewImageUrl")]
    public string? PreviewImageUrl { get; set; }
    
    [JsonPropertyName("theme")]
    public required ThemeConfig Theme { get; set; }
    
    [JsonPropertyName("sections")]
    public required List<TemplateSectionConfig> Sections { get; set; }
    
    [JsonPropertyName("isOfficial")]
    public bool IsOfficial { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
    
    [JsonPropertyName("usageCount")]
    public int UsageCount { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ThemeConfig
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("primary")]
    public required string Primary { get; set; }
    
    [JsonPropertyName("secondary")]
    public required string Secondary { get; set; }
    
    [JsonPropertyName("background")]
    public required string Background { get; set; }
    
    [JsonPropertyName("gradient")]
    public string? Gradient { get; set; }
    
    [JsonPropertyName("textColor")]
    public string? TextColor { get; set; }
}

public class TemplateSectionConfig
{
    [JsonPropertyName("sectionId")]
    public required string SectionId { get; set; }
    
    [JsonPropertyName("styleId")]
    public required string StyleId { get; set; }
    
    [JsonPropertyName("order")]
    public int Order { get; set; }
    
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
    
    [JsonPropertyName("config")]
    public Dictionary<string, object>? Config { get; set; }
}
```

```csharp
// Profily.Core/Models/Profile/ProfileConfig.cs
public class ProfileConfig
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "profileConfig";
    
    [JsonPropertyName("userId")]
    public required string UserId { get; set; }
    
    [JsonPropertyName("templateId")]
    public string? TemplateId { get; set; }
    
    [JsonPropertyName("theme")]
    public required ThemeConfig Theme { get; set; }
    
    [JsonPropertyName("sections")]
    public required List<TemplateSectionConfig> Sections { get; set; }
    
    [JsonPropertyName("content")]
    public required ProfileContent Content { get; set; }
    
    [JsonPropertyName("socialLinks")]
    public SocialLinks? SocialLinks { get; set; }
    
    [JsonPropertyName("preferences")]
    public ProfilePreferences Preferences { get; set; } = new();
    
    [JsonPropertyName("isDraft")]
    public bool IsDraft { get; set; } = true;
    
    [JsonPropertyName("lastDeployedAt")]
    public DateTime? LastDeployedAt { get; set; }
    
    [JsonPropertyName("lastDeployedConfigSnapshot")]
    public JsonDocument? LastDeployedConfigSnapshot { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ProfileContent
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
    
    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }
    
    [JsonPropertyName("aboutMe")]
    public string? AboutMe { get; set; }
    
    [JsonPropertyName("customSkills")]
    public List<string>? CustomSkills { get; set; }
    
    [JsonPropertyName("pinnedRepoNames")]
    public List<string>? PinnedRepoNames { get; set; }
}

public class SocialLinks
{
    [JsonPropertyName("linkedin")]
    public string? Linkedin { get; set; }
    
    [JsonPropertyName("twitter")]
    public string? Twitter { get; set; }
    
    [JsonPropertyName("youtube")]
    public string? Youtube { get; set; }
    
    [JsonPropertyName("discord")]
    public string? Discord { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("website")]
    public string? Website { get; set; }
    
    [JsonPropertyName("leetcode")]
    public string? Leetcode { get; set; }
    
    [JsonPropertyName("resume")]
    public string? Resume { get; set; }
}

public class ProfilePreferences
{
    [JsonPropertyName("showOpenToWork")]
    public bool ShowOpenToWork { get; set; }
    
    [JsonPropertyName("showProfileViews")]
    public bool ShowProfileViews { get; set; } = true;
}
```

```csharp
// Profily.Core/Models/Profile/DeployHistory.cs
public class DeployHistory
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "deployHistory";
    
    [JsonPropertyName("userId")]
    public required string UserId { get; set; }
    
    [JsonPropertyName("configSnapshot")]
    public required JsonDocument ConfigSnapshot { get; set; }
    
    [JsonPropertyName("templateId")]
    public string? TemplateId { get; set; }
    
    [JsonPropertyName("result")]
    public required DeployResult Result { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DeployResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("commitSha")]
    public string? CommitSha { get; set; }
    
    [JsonPropertyName("repoUrl")]
    public string? RepoUrl { get; set; }
    
    [JsonPropertyName("filesDeployed")]
    public List<string>? FilesDeployed { get; set; }
}
```

---

## V1 API Endpoints

### Template & Section APIs (Read-only)
```http
GET  /api/templates                    # List all active templates
GET  /api/templates/{slug}             # Get template with sections & styles
GET  /api/templates/{slug}/preview     # Get preview with sample data

GET  /api/sections                     # List all sections with their styles
GET  /api/sections/{slug}/styles       # Get styles for a section
```

### User Profile APIs (Authenticated)
```http
GET  /api/profile/config               # Get user's saved config (or null)
PUT  /api/profile/config               # Save/update config (auto-save)
DELETE /api/profile/config             # Reset config

POST /api/profile/preview              # Generate preview from current config
POST /api/profile/deploy               # Deploy to GitHub

GET  /api/profile/deploys              # Get deploy history
GET  /api/profile/deploys/{id}         # Get specific deploy details
```

---

## V1 Services

### Interfaces

```csharp
// ITemplateService.cs
public interface ITemplateService
{
    Task<List<ProfileTemplate>> GetActiveTemplatesAsync(CancellationToken ct = default);
    Task<ProfileTemplate?> GetTemplateBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<Section>> GetAllSectionsAsync(CancellationToken ct = default);
    Task<List<SectionStyle>> GetStylesForSectionAsync(string sectionId, CancellationToken ct = default);
}

// IProfileService.cs
public interface IProfileService
{
    Task<ProfileConfig?> GetUserConfigAsync(string userId, CancellationToken ct = default);
    Task<ProfileConfig> SaveUserConfigAsync(ProfileConfig config, CancellationToken ct = default);
    Task DeleteUserConfigAsync(string userId, CancellationToken ct = default);
    Task<List<DeployHistory>> GetDeployHistoryAsync(string userId, CancellationToken ct = default);
    Task<DeployHistory> RecordDeployAsync(DeployHistory deploy, CancellationToken ct = default);
}

// IReadmeGeneratorService.cs
public interface IReadmeGeneratorService
{
    Task<GeneratedProfile> GenerateAsync(
        ProfileConfig config,
        GitHubStats stats,
        List<GitHubRepository> repos,
        CancellationToken ct = default);
}

// IGitHubDeployService.cs (extend existing IGitHubService)
public interface IGitHubDeployService
{
    Task<DeployResult> DeployProfileAsync(
        string accessToken,
        string username,
        GeneratedProfile profile,
        CancellationToken ct = default);
}
```

### Generated Profile DTO

```csharp
public class GeneratedProfile
{
    public required string Readme { get; set; }
    public List<WorkflowFile> Workflows { get; set; } = [];
    public List<AssetFile> Assets { get; set; } = [];
}

public class WorkflowFile
{
    public required string Path { get; set; }
    public required string Content { get; set; }
}

public class AssetFile
{
    public required string Path { get; set; }
    public required string Content { get; set; }
    public required string Type { get; set; }
}
```

---

## V1 Seed Data

Initial sections, styles, and templates will be seeded on application startup.

### Sections to Seed
| Section ID | Slug | Display Name |
|------------|------|--------------|
| section-header | header | Header / Title |
| section-badges | badges | Profile Badges |
| section-about | about | About Me |
| section-skills | skills | Tech Stack |
| section-stats | stats | GitHub Stats |
| section-streak | streak | Contribution Streak |
| section-languages | languages | Top Languages |
| section-snake | snake | Contribution Snake |
| section-3d | 3d-contrib | 3D Contributions |
| section-repos | repos | Top Repositories |
| section-contact | contact | Contact Links |
| section-footer | footer | Footer |

### Styles to Seed (per section)
| Section | Styles |
|---------|--------|
| header | typing-svg, capsule-render, simple-text |
| badges | shields-io, profile-views, social-badges |
| about | bullet-list, quote-style |
| skills | icons-grid, shields-badges, simple-list |
| stats | stats-card, combined, trophy |
| streak | streak-card |
| languages | top-langs-compact, top-langs-donut |
| snake | green-snake, dark-snake, ocean-snake |
| 3d-contrib | 3d-profile, skyline-link |
| repos | repo-cards, table-format, list-format |
| contact | badges-row, icons-row |
| footer | wave-svg, simple-line |

### Templates to Seed
1. **developer-pro** - Full featured (all sections)
2. **minimal-clean** - Simple (header, about, stats, skills, contact)
3. **creative-fun** - Animated (header, badges, snake, skills, stats, footer)

---

## Implementation Order (V1)

### Phase 1: Core Models & Data Layer
- [ ] Create Profile models (Section, SectionStyle, ProfileTemplate, ProfileConfig, DeployHistory)
- [ ] Update ICosmosDbService with generic query methods
- [ ] Create seed data classes

### Phase 2: Services
- [ ] Implement TemplateService
- [ ] Implement ProfileService
- [ ] Create markdown template engine
- [ ] Implement ReadmeGeneratorService

### Phase 3: GitHub Deploy
- [ ] Extend GitHubService with deploy capabilities
- [ ] Create/update repository
- [ ] Push files (README, workflows, assets)

### Phase 4: API Endpoints
- [ ] Template endpoints (read-only)
- [ ] Profile config endpoints (CRUD)
- [ ] Preview endpoint
- [ ] Deploy endpoint

### Phase 5: Seed Data
- [ ] Section definitions with markdown templates
- [ ] Style variations
- [ ] Template presets

### Phase 6: Frontend Update
- [ ] Update to use new API
- [ ] Remove hardcoded templates/sections
- [ ] Add style selector per section

---

## Future: V2 Additions

When community contributions are added:

```csharp
// Additional models for V2
public class ContributionSubmission { ... }
public class StyleVersion { ... }
public class TemplateVersion { ... }

// Additional endpoints
POST /api/contributions/styles
POST /api/contributions/templates
GET  /api/admin/contributions/pending
POST /api/admin/contributions/{id}/approve
```

The V1 schema is designed to be forward-compatible with versioning.