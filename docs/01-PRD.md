# Profily — Product Requirements Document

## 1. Problem Statement

Developers spend hours building portfolio websites from scratch or settling for generic templates that don't reflect their actual skills. Their GitHub profile already contains everything needed — projects, languages, contributions — but there's no tool that automatically transforms this data into a polished, deployable portfolio.

**Existing alternatives and their gaps:**

| Product | Gap |
|---|---|
| GitHub Profile README | Not a real website, limited design |
| peerlist.io | Manual data entry, not GitHub-driven |
| read.cv | Designer-focused, not developer-centric |
| polywork.com | Social network, not a portfolio builder |
| gitfolio | Outdated, no templates, CLI only |
| resume.github.io | Resume format, not portfolio |

## 2. Product Vision

**One-line:** Sign in with GitHub → pick a template → get a deployed portfolio in under 5 minutes.

**Profily** automatically extracts your profile, projects, skills, and tech stack from GitHub, lets you customize and preview with stunning templates, and deploys to GitHub Pages in one click.

## 3. Target Audience

- Junior to mid-level developers building their online presence
- CS students preparing for job applications
- Developers who want a portfolio but don't want to build one from scratch
- Anyone with a GitHub account who wants to showcase their work

## 4. Core User Stories

### MVP (Launch)

| ID | Story | Priority |
|---|---|---|
| US-01 | As a user, I can sign in with my GitHub account | P0 |
| US-02 | As a user, I can see my profile data auto-populated from GitHub | P0 |
| US-03 | As a user, I can see my skills/tech stack auto-detected from my repos | P0 |
| US-04 | As a user, I can browse and pick a portfolio template | P0 |
| US-05 | As a user, I can preview my portfolio with my real data | P0 |
| US-06 | As a user, I can edit my bio, skills, and featured projects | P0 |
| US-07 | As a user, I can toggle sections on/off (hide services, etc.) | P0 |
| US-08 | As a user, I can deploy my portfolio to GitHub Pages in one click | P0 |
| US-09 | As a user, I can come back and update my portfolio anytime | P0 |
| US-10 | As a user, I can re-sync my GitHub data to pick up new repos | P1 |

### Post-MVP

| ID | Story | Priority |
|---|---|---|
| US-11 | As a user, I can reorder sections via drag & drop | P2 |
| US-12 | As a user, I can get AI-generated project descriptions | P2 |
| US-13 | As a user, I can see analytics on my portfolio (views, clicks) | P3 |
| US-14 | As a user, I can use a custom domain | P3 |
| US-15 | As a user, I can add custom sections (certifications, blog) | P3 |

## 5. MVP Scope

### In Scope

- GitHub OAuth login/logout
- Auto-detect: profile info, repositories, languages, skills (via config file parsing)
- 3 templates at launch (3D Purple, Minimal Clean, Developer Terminal)
- Live preview with real user data
- Section editor: toggle visibility, edit text, select projects, reorder skills
- One-click deploy to GitHub Pages (`username.github.io`)
- Return and update portfolio from platform
- Mobile-responsive generated portfolios

### Out of Scope (Post-MVP)

- Custom domains
- DeepWiki/AI-powered project descriptions
- Analytics dashboard
- Inline portfolio editing (edit directly on the deployed site)
- Premium plans / payment processing
- Multiple portfolios per user
- Blog / CMS features
- Drag & drop section reordering
- Custom CSS overrides

## 6. User Flow

```
Landing Page → "Sign in with GitHub" 
    → GitHub OAuth consent screen
    → Redirect back with profile + repos loaded (< 5 sec)
    → Template Gallery (pick one)
    → Editor / Preview (side by side)
        - Edit sections (toggle, text, projects, skills)
        - Preview updates in real-time
    → "Deploy to GitHub Pages" button
    → Deploying... (< 10 sec)
    → "Your portfolio is live at username.github.io!" 
    → Share / go to dashboard
```

## 7. Success Metrics

| Metric | Target (Month 1) |
|---|---|
| Signup → Deploy conversion | > 60% |
| Time from signup to deployed portfolio | < 5 minutes |
| Return users who update portfolio | > 20% |
| User satisfaction (would recommend) | > 4/5 |

## 8. Competitive Advantage

1. **Zero manual data entry** — everything comes from GitHub
2. **Skill inference** — we figure out your tech stack, you just confirm
3. **One-click deploy** — no CLI, no manual hosting setup
4. **Stunning templates** — 3D effects, animations, not generic Bootstrap
5. **Free forever** — $0 for core features, premium for power users later
