# Diviner Workflow

## Operating Model

The main agent owns planning, integration, and quality gates. Subagents should be used for bounded research, isolated implementation slices, and independent review. Each delegated task must have clear ownership and must report changed files or findings.

## Milestones

1. Development environment: compile, deploy, and verify a blank BaseLib-required mod.
2. Mod identity: define Diviner theme, mechanics, scope, and content budget.
3. Runtime foundation: logging, shared constants, localization helpers, BaseLib registration pattern.
4. First playable slice: one small mechanic plus one card/relic/power path.
5. Content expansion: cards, relics, powers, visuals, balancing fixtures.
6. Workshop packaging: release build, manifest audit, image/icon audit, changelog, smoke test.

## Current Design Docs

- `docs/feasibility-analysis.md`: risk matrix for destiny, divination, reward manipulation, and forecast providers.
- `docs/implementation-plan.md`: phased build plan and QA gates.
- `docs/card-pool-v0.md`: first full card-pool draft.
- `docs/relics-potions-v0.md`: first relic and potion draft.

## Quality Gates

- `./scripts/verify-env.sh` passes before build work.
- `./scripts/build.sh` passes before code review.
- Manifest dependency versions match the package references.
- Local build deploys `Diviner.dll`, `Diviner.json`, `Diviner.pck`, and BaseLib files.
- Any gameplay change includes a smoke-test note and localization coverage.
- Workshop-ready releases use `Release` configuration and a clean manifest.
