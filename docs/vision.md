# Vision

Profily is a platform that helps developers generate two things from their GitHub identity and a small amount of curated input:

1) A GitHub profile experience (README + dynamic visuals)
2) A portfolio website (static site) that can be deployed to GitHub Pages

## Product goals

- Make it fast to go from “I have repos” to “I have a polished developer presence”.
- Keep the output customizable (themes/templates) without requiring the user to hand-code.
- Support repeatable updates (scheduled rebuilds / webhook triggers).
- Produce artifacts users can own: Markdown, SVGs, and static website files.

## Non-goals (for early iterations)

- A full CMS or blogging platform (only simple markdown sync/export).
- Hosting user-generated dynamic sites (portfolio output is static).
- Supporting every source control provider (GitHub-first).

## Roadmap framing (Now / Next / Later)

### Now (foundation)

- Repository scaffolding, local dev story, docs, architecture diagrams.
- A minimal API surface with health + placeholders.

### Next (first real value)

- Portfolio generator MVP: template rendering with a small set of fields + local preview.
- Deploy to GitHub Pages (via generated workflow / gh-pages branch).

### Later (full platform)

- Profile generator MVP: README + SVG generation + theme selection.
- GitHub OAuth, repository selection, tech stack detection.
- Background jobs + webhooks + persistence/caching.

## Success criteria

- A user can generate and deploy a portfolio in minutes.
- Changes are reproducible: rerun generation and get the same output.
- The architecture stays understandable as features grow.
