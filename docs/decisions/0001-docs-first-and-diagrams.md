# ADR 0001: Docs-first + Architecture Diagrams

## Status

Accepted

## Context

Profily has a large target scope (profile generator + portfolio generator + deployment + background jobs). Early code scaffolding is intentionally minimal.

Without a written target architecture, it’s easy to drift from the goal or to implement features in the wrong order.

## Decision

- Keep a docs folder as the source of truth.
- Use Mermaid diagrams in Markdown as the canonical architecture diagrams.
- Keep optional Draw.io files for higher-fidelity visuals (sources + exports).
- Maintain an explicit “current implementation status” table in the architecture overview.

## Consequences

- Documentation becomes part of the build process (kept updated as the code changes).
- Diagrams remain easy to edit/review in PRs.
- The team (even if it’s just one person) has a stable reference for sequencing and boundaries.
