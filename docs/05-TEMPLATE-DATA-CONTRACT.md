# Profily — Template Data Contract

## Overview

Every portfolio template receives a standardized JSON data object. Template authors use `{{placeholders}}` to inject this data into HTML. This document defines the exact shape of that data.

---

## Full Data Object

```json
{
  "profile": {
    "name": "Mohamed Raafat",
    "username": "m7mdraafat",
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
    "contributionsThisYear": 715
  },

  "skills": [
    {
      "name": "C#",
      "category": "backend",
      "confidence": 95,
      "repoCount": 15,
      "displayOrder": 0
    },
    {
      "name": "Python",
      "category": "backend",
      "confidence": 88,
      "repoCount": 8,
      "displayOrder": 1
    },
    {
      "name": "REST APIs",
      "category": "backend",
      "confidence": 92,
      "repoCount": 10,
      "displayOrder": 7
    }
  ],

  "projects": [
    {
      "name": "VideStore.API",
      "description": "Full-stack e-commerce API with clean architecture",
      "language": "C#",
      "topics": ["dotnet", "api", "clean-architecture"],
      "stars": 1,
      "forks": 0,
      "url": "https://github.com/m7mdraafat/VideStore.API",
      "homepageUrl": null,
      "displayOrder": 0
    }
  ],

  "sections": {
    "hero": {
      "visible": true,
      "greeting": "Hello, I Am",
      "roleTexts": ["Software Engineer", "Backend Developer", "AI Agent Builder"]
    },
    "about": {
      "visible": true,
      "title": "About Me",
      "subtitle": "Get To Know",
      "description": "I'm a software engineer at Microsoft...",
      "stats": [
        { "label": "Software Engineer", "value": "Microsoft", "icon": "briefcase" },
        { "label": "27+ Repositories", "value": "", "icon": "code" },
        { "label": "715+ Contributions", "value": "This Year", "icon": "git-commit" }
      ]
    },
    "services": {
      "visible": true,
      "title": "My Services",
      "subtitle": "What I Offer",
      "items": [
        {
          "title": "Backend Development",
          "description": "Building robust, scalable APIs and microservices...",
          "icon": "code",
          "tags": ["C# / .NET", "REST APIs", "SQL Server"]
        },
        {
          "title": "AI & Agents",
          "description": "Developing AI-powered applications...",
          "icon": "cpu",
          "tags": ["Python", "AI Agents", "LLMs"]
        }
      ]
    },
    "projects": {
      "visible": true,
      "title": "Featured Projects",
      "subtitle": "My Recent Work",
      "showFilters": true,
      "filters": ["All", "Backend", "AI / Agents"]
    },
    "skills": {
      "visible": true,
      "title": "Tech Stack",
      "subtitle": "My Abilities",
      "showProgressBars": true
    },
    "contact": {
      "visible": true,
      "title": "Contact Me",
      "subtitle": "Get In Touch",
      "description": "Have a project in mind? Let's work together...",
      "showForm": true,
      "showEmail": true,
      "showLocation": true,
      "showAvailability": true
    }
  },

  "socials": [
    { "platform": "github", "url": "https://github.com/m7mdraafat", "label": "GitHub" },
    { "platform": "linkedin", "url": "https://linkedin.com/in/...", "label": "LinkedIn" },
    { "platform": "leetcode", "url": "https://leetcode.com/u/mo_raafat/", "label": "LeetCode" },
    { "platform": "facebook", "url": "https://facebook.com/...", "label": "Facebook" }
  ],

  "meta": {
    "generatedAt": "2026-02-20T12:00:00Z",
    "templateId": "3d-purple",
    "profilyUrl": "https://profily.dev",
    "year": 2026
  }
}
```

---

## Placeholder Syntax

Templates use a simple placeholder syntax (no library, AOT-compatible):

### Simple Values
```html
<h1>{{profile.name}}</h1>
<p>{{profile.bio}}</p>
<img src="{{profile.avatarUrl}}" alt="{{profile.name}}">
```

### Conditionals
```html
{{#if sections.services.visible}}
<section class="services">
  <h2>{{sections.services.title}}</h2>
  ...
</section>
{{/if}}

{{#if profile.linkedinUrl}}
<a href="{{profile.linkedinUrl}}">LinkedIn</a>
{{/if}}
```

### Loops
```html
{{#each skills}}
<div class="skill-item">
  <span class="skill-name">{{name}}</span>
  <div class="skill-bar">
    <div class="skill-progress" data-width="{{confidence}}"></div>
  </div>
</div>
{{/each}}

{{#each projects}}
<article class="project-card">
  <h3>{{name}}</h3>
  <p>{{description}}</p>
  <a href="{{url}}">View Code</a>
  {{#each topics}}
  <span class="tag">{{.}}</span>
  {{/each}}
</article>
{{/each}}
```

### Nested Loops
```html
{{#each sections.services.items}}
<div class="service-card">
  <h3>{{title}}</h3>
  <p>{{description}}</p>
  {{#each tags}}
  <span>{{.}}</span>
  {{/each}}
</div>
{{/each}}
```

---

## Template Manifest

Each template includes a `manifest.json`:

```json
{
  "id": "3d-purple",
  "name": "3D Purple",
  "version": "1.0.0",
  "description": "Modern 3D portfolio with particle effects, morphing geometry, and purple accent theme",
  "author": "Profily Team",
  "features": ["3D Effects", "Animated", "Dark Theme", "Particle Background"],
  "thumbnail": "thumbnail.png",
  "sections": ["hero", "about", "services", "projects", "skills", "contact"],
  "defaultCustomizations": {
    "sections": {
      "hero": { "visible": true, "greeting": "Hello, I Am" },
      "about": { "visible": true, "title": "About Me", "subtitle": "Get To Know" },
      "services": { "visible": true, "title": "My Services", "subtitle": "What I Offer" },
      "projects": { "visible": true, "title": "Featured Projects", "subtitle": "My Recent Work", "showFilters": true },
      "skills": { "visible": true, "title": "Tech Stack", "subtitle": "My Abilities", "showProgressBars": true },
      "contact": { "visible": true, "title": "Contact Me", "subtitle": "Get In Touch", "showForm": true }
    }
  },
  "externalDependencies": [
    "https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js",
    "https://cdnjs.cloudflare.com/ajax/libs/gsap/3.12.5/gsap.min.js",
    "https://cdnjs.cloudflare.com/ajax/libs/gsap/3.12.5/ScrollTrigger.min.js"
  ],
  "fonts": [
    "https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700;800;900&family=Fira+Code:wght@400;500;600&display=swap"
  ]
}
```

---

## Sample Data for Demos

Templates need sample data for the gallery demo. Located at `templates/_shared/sample-data.json`:

```json
{
  "profile": {
    "name": "Alex Johnson",
    "username": "alexjohnson",
    "avatarUrl": "/demo/avatar-placeholder.svg",
    "bio": "Full-stack developer passionate about building elegant solutions",
    "location": "San Francisco, CA",
    "company": "Tech Corp",
    "email": "alex@example.com",
    "githubUrl": "https://github.com/alexjohnson",
    "linkedinUrl": "https://linkedin.com/in/alexjohnson",
    "reposCount": 42,
    "followersCount": 150,
    "contributionsThisYear": 1200
  },
  "skills": [
    { "name": "TypeScript", "category": "frontend", "confidence": 95, "displayOrder": 0 },
    { "name": "React", "category": "frontend", "confidence": 92, "displayOrder": 1 },
    { "name": "Node.js", "category": "backend", "confidence": 90, "displayOrder": 2 },
    { "name": "Python", "category": "backend", "confidence": 88, "displayOrder": 3 },
    { "name": "PostgreSQL", "category": "database", "confidence": 85, "displayOrder": 4 },
    { "name": "Docker", "category": "devops", "confidence": 83, "displayOrder": 5 },
    { "name": "AWS", "category": "devops", "confidence": 80, "displayOrder": 6 },
    { "name": "GraphQL", "category": "backend", "confidence": 78, "displayOrder": 7 }
  ],
  "projects": [
    {
      "name": "CloudSync Pro",
      "description": "Real-time cloud synchronization platform with end-to-end encryption",
      "language": "TypeScript",
      "topics": ["react", "node", "websockets"],
      "stars": 234,
      "url": "#",
      "displayOrder": 0
    },
    {
      "name": "ML Pipeline Runner",
      "description": "Automated machine learning pipeline with drag-and-drop workflow builder",
      "language": "Python",
      "topics": ["machine-learning", "fastapi", "docker"],
      "stars": 156,
      "url": "#",
      "displayOrder": 1
    },
    {
      "name": "DevBoard",
      "description": "Developer productivity dashboard with GitHub integration and analytics",
      "language": "TypeScript",
      "topics": ["next-js", "prisma", "tailwind"],
      "stars": 89,
      "url": "#",
      "displayOrder": 2
    },
    {
      "name": "SecureVault API",
      "description": "Zero-knowledge encrypted secrets manager with team sharing capabilities",
      "language": "Go",
      "topics": ["security", "api", "encryption"],
      "stars": 67,
      "url": "#",
      "displayOrder": 3
    }
  ]
}
```

---

## Adding a New Template

1. Create folder: `templates/{template-id}/`
2. Add `manifest.json` with metadata
3. Create `layout.html` (nav, footer, `{{sections}}` placeholder)
4. Create `sections/*.html` for each section
5. Add `css/style.css` and `js/main.js`
6. Generate `thumbnail.png` (1200×800)
7. Pre-render demo HTML with sample data
8. Insert row into `templates` table
9. Upload all files to Cloudflare R2
