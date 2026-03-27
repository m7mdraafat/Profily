# ADR-001: Product Requirements & User Stories

**Status:** Accepted  
**Date:** 2026-03-23  

---

## 1. Problem Statement

Developers spend hours building portfolio websites from scratch or settling for generic templates that don't reflect their actual skills. Their GitHub profile already contains everything needed — projects, languages, contributions — but no tool automatically transforms this data into a polished, deployable portfolio.

**Existing alternatives and their gaps:**

| Product | Gap |
|---|---|
| GitHub Profile README | Not a real website, limited design |
| GitFolio | No auto-detected tech stack, skills are fully manual |
| peerlist.io | Manual data entry, not GitHub-driven |
| read.cv | Designer-focused, not developer-centric |
| polywork.com | Social network, not a portfolio builder |
| gitfolio (CLI) | Outdated, no templates, CLI only |
| resume.github.io | Resume format, not portfolio |

## 2. Product Vision

**One-line:** Sign in with GitHub → pick a template → fill your dashboard → publish a portfolio at `username.profily.dev` in under 5 minutes.

Profily auto-imports your GitHub profile and repos, infers your tech stack using an LLM (grouped into Languages, Frameworks & Libraries, Tools & Platforms, and Architectural Patterns — each with branded SVG icons), lets you add experience, education, and social links, then publishes to `username.profily.dev` and/or GitHub Pages.

**Key differentiator vs GitFolio:** Automated skill extraction from active repos. You don't manually type "React" — the LLM detects it from your `package.json` and repo structure.

## 3. Target Audience

- Junior to mid-level developers building their online presence
- CS students preparing for job applications
- Developers who want a portfolio but don't want to build one from scratch
- Anyone with a GitHub account who wants to showcase their work

## 4. Product Model

Dashboard-based builder (not a linear wizard). User has a persistent dashboard with sidebar navigation. Every section is independently editable. Portfolio has **draft** and **published** states.

### Dashboard Sections

```
Sidebar:
├── Overview          — Profile stats, quick glance at portfolio completeness
├── Personal Info     — Edit name, bio, avatar, email, location
├── Projects          — All repos (paginated), toggle enable/disable (max 10 enabled)
├── Experience        — Manual CRUD (title, company, dates, description)
├── Education         — Manual CRUD (degree, school, dates, description)
├── Social Links      — GitHub auto-filled, add LinkedIn, Twitter, LeetCode, etc.
├── Skills            — LLM-inferred from enabled repos, grouped by category, add more with icon search
├── Templates         — Pick one, preview with your real data
└── Preview           — Full-page preview of generated portfolio

Bottom:
├── Share Portfolio
└── Visit Your Portfolio (link to published URL)
```

### User Flow

```
First visit:
    Landing Page → "Sign in with GitHub"
        → GitHub OAuth → fetch profile + all repos
        → Template Gallery (pick one)
        → Dashboard / Overview
        → User fills sections (any order):
            - Enable up to 10 repos (triggers LLM skill inference)
            - Review/edit inferred skills (add more with icon search)
            - Add experience, education, social links
            - Edit personal info
        → Preview portfolio
        → Publish (draft → published)
            → Live at username.profily.dev
            → Optionally also deploy to GitHub Pages

Return visit:
    Sign in → Dashboard → Edit anything → Publish changes
```

### Draft vs Published

| State | Behavior |
|---|---|
| **Draft** | Changes saved to database. Portfolio not regenerated. Visitor sees last published version (or nothing if never published). |
| **Published** | Static HTML generated from current dashboard data + template. Uploaded to hosting. Visitor sees this version. |

User edits freely in draft mode. Clicks "Publish" when ready. This avoids regenerating HTML on every keystroke and prevents visitors from seeing half-finished edits.

### Deployment Targets

| Target | URL | Mechanism | Default |
|---|---|---|---|
| **Profily subdomain** | `username.profily.dev` | Cloudflare Workers + R2 | Yes (primary) |
| **GitHub Pages** | `username.github.io` | Git tree API push | Optional (user-initiated) |

Both can be active simultaneously. Profily subdomain is the default. GitHub Pages is an export option for users who want to own the repo.

## 5. Pricing Plans

| | Free | Pro ($10/year) |
|---|---|---|
| Portfolios | 1 | Multiple (swappable with one click) |
| Templates | 1 basic | All templates (current + future) |
| Hosting | `username.profily.dev` | `username.profily.dev` |
| GitHub Pages export | ❌ | ✅ |
| Branding | "Built with Profily" footer | Removed |
| Skills inference (LLM) | ✅ | ✅ |
| Experience / Education | ✅ | ✅ |
| Max enabled repos | 10 | 10 |

**Payment provider (v1):** Paymob (Egyptian company). Supports cards, Fawry, Vodafone Cash, InstaPay. Direct payout to Egyptian bank account.

**Payment provider (v2):** Add LemonSqueezy as Merchant of Record for global users (cards, PayPal, Apple Pay). Route by user location: Egypt/MENA → Paymob, everywhere else → LemonSqueezy.

**Revenue per Pro user:** $10 - ~$0.25 (Paymob fees) = ~$9.75/year

## 6. User Stories

### MVP (P0)

| ID | Story |
|---|---|
| US-01 | As a user, I can sign in with my GitHub account |
| US-02 | As a user, I can see my profile data auto-populated from GitHub (name, bio, avatar, location, stats) |
| US-03 | As a user, I can see all my GitHub repos in a paginated list, with each repo disabled by default |
| US-04 | As a user, I can enable up to 10 repos to feature in my portfolio |
| US-05 | As a user, I can see my skills auto-detected from my enabled repos, grouped into: Languages, Frameworks & Libraries, Tools & Platforms, and Architectural Patterns — each with branded SVG icons |
| US-06 | As a user, I can review and edit inferred skills (add, remove, reorder within each category) |
| US-07 | As a user, I can search and add skills manually (with icon auto-matched) |
| US-08 | As a user, I can edit my personal info (name, bio, email, location) |
| US-09 | As a user, I can add, edit, and delete work experience entries |
| US-10 | As a user, I can add, edit, and delete education entries |
| US-11 | As a user, I can add and manage social links (GitHub auto-filled, add others) |
| US-12 | As a user, I can browse and pick a portfolio template from a gallery |
| US-13 | As a user, I can preview my portfolio with my real data before publishing |
| US-14 | As a user, I can publish my portfolio to `username.profily.dev` |
| US-15 | As a user, I can come back and update my portfolio anytime (draft/published states) |
| US-16 | As a user, I can re-sync my GitHub data to pick up new repos |
| US-17 | As a user, I can upgrade to Pro ($10/year) via Paymob |
| US-18 | As a Pro user, I can access all templates |
| US-19 | As a Pro user, I can export/deploy my portfolio to GitHub Pages |
| US-20 | As a Pro user, I can remove Profily branding from my portfolio |

### Post-MVP (P1)

| ID | Story |
|---|---|
| US-21 | As a user, I can get AI-generated project descriptions from repo READMEs |
| US-22 | As a user, I can toggle sections on/off (hide experience, education, etc.) |
| US-23 | As a user, I can reorder sections via drag & drop |
| US-24 | As a Pro user, I can create multiple portfolios and swap the active one |
| US-25 | As a user, I can pay via LemonSqueezy (global payment methods) |

### Future (P2/P3)

| ID | Story |
|---|---|
| US-26 | As a user, I can see analytics on my portfolio (views, clicks) |
| US-27 | As a user, I can use a fully custom domain (e.g., `mohamedraafat.dev`) |
| US-28 | As a user, I can add custom sections (certifications, blog posts) |

## 7. MVP Scope

### In Scope

- GitHub OAuth login/logout
- Dashboard with sidebar navigation (Overview, Personal Info, Projects, Experience, Education, Social Links, Skills, Templates, Preview)
- Auto-import: profile info, all repositories
- Projects page: paginated repo list, enable/disable toggle, max 10 enabled
- Experience: manual CRUD (title, company, start/end date, description)
- Education: manual CRUD (degree, school, start/end date, description)
- Social Links: GitHub auto-filled, manual add for LinkedIn, Twitter, LeetCode, Facebook, etc.
- LLM-based skill inference from enabled repos, grouped into 4 categories
- Branded SVG icons for each skill/tool/framework
- Manual skill add with icon search
- Template gallery (3 templates at launch; 1 free, rest locked to Pro)
- Full-page preview with real user data
- Draft/published states
- Publish to `username.profily.dev` (Cloudflare Workers + R2)
- GitHub Pages export (Pro only)
- Re-sync GitHub data
- Mobile-responsive generated portfolios
- Pricing page + Paymob payment integration ($10/year Pro plan)
- Feature gating (free vs Pro)
- "Built with Profily" footer on free portfolios

### Out of Scope (Post-MVP)

- AI-generated project descriptions (P1)
- Section toggle visibility (P1)
- Drag & drop section reordering (P1)
- Multiple swappable portfolios (P1, Pro only)
- LemonSqueezy global payments (P1)
- Fully custom domains (P2)
- Analytics dashboard (P2)
- Blog / CMS features
- Custom CSS overrides
- Inline portfolio editing (edit directly on the deployed site)

## 8. Success Metrics

| Metric | Target (Month 1) |
|---|---|
| Signup → Publish conversion | > 50% |
| Time from signup to published portfolio | < 10 minutes |
| Return users who update portfolio | > 25% |
| Free → Pro conversion | > 5% |
| User satisfaction (would recommend) | > 4/5 |

## 9. Competitive Advantage

1. **LLM skill inference** — auto-detects tech stack from repos with branded icons, grouped into 4 categories. No manual typing.
2. **Zero data entry for projects** — repos come from GitHub, enable the ones you want
3. **Instant hosting** — `username.profily.dev` live in seconds + GitHub Pages export (Pro)
4. **Stunning templates** — 3D effects, animations, not generic Bootstrap
5. **Complete portfolio** — projects + skills + experience + education + social links in one place
6. **$10/year Pro** — affordable for students, sustainable for us

## 10. Key Decisions

| # | Decision | Rationale |
|---|---|---|
| 1 | Dashboard-based builder, not a linear wizard | Users return and edit sections independently. Matches GitFolio's proven UX. |
| 2 | All repos shown, disabled by default, max 10 enabled | User opts in, not out. Bounds LLM token usage. Forces curation. |
| 3 | Skills LLM-inferred from enabled repos only | User controls their identity. Avoids noise from forks/experiments. |
| 4 | 4 skill categories with branded SVG icons | Languages / Frameworks & Libraries / Tools & Platforms / Architectural Patterns. More meaningful than flat list with progress bars. |
| 5 | Experience + Education in MVP | Adds real resume value. Differentiates from GitHub-only tools. |
| 6 | Draft/published states | Prevents visitors seeing half-finished edits. Reduces unnecessary HTML regeneration and hosting writes. |
| 7 | Profily subdomain as primary, GitHub Pages as Pro feature | We control the hosting experience. GitHub Pages export is a natural Pro upgrade trigger. |
| 8 | Template first, then dashboard | User picks visual style immediately after sign-in. Gets excited to see their data in a template. Can change later from dashboard. |
| 9 | Free/Pro split with $10/year | 1 template + branding = free. All templates + no branding + GitHub Pages = Pro. Sustainable revenue from day one. |
| 10 | Paymob for v1, LemonSqueezy for v2 | Based in Egypt — Paymob supports local methods (Fawry, Vodafone Cash, InstaPay). Add LemonSqueezy later for global cards/PayPal. |
| 11 | Section toggling moved to P1 | Reduces dashboard complexity for MVP. All sections visible by default. |
| 12 | AI project descriptions moved to P1 | LLM integration already exists. Low-effort to add after MVP. |

## 11. Cost & Revenue Model

### Costs

| Item | Cost | Notes |
|---|---|---|
| Domain (`profily.dev`) | ~$12/year | Only non-zero infra cost |
| Cloudflare Workers | $0 | 100K req/day free |
| Cloudflare R2 | $0 | 10GB free (1K users × 300KB = 300MB) |
| Cloudflare DNS + SSL | $0 | Wildcard `*.profily.dev` |
| LLM inference (Groq) | $0 | 14,400 RPD free |
| Paymob | ~2.5% per transaction | ~$0.25 per $10 payment |
| **Total fixed** | **~$1/month** | |

### Revenue (projections)

| Users | Pro conversion (5%) | Annual revenue |
|---|---|---|
| 100 | 5 | $50 |
| 500 | 25 | $250 |
| 1,000 | 50 | $500 |
| 5,000 | 250 | $2,500 |
