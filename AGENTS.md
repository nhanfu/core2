# Repository Guidelines

## Mission
- Build a metadata-driven core that hides frontend/backend communication details and keeps client apps focused on business logic.
- Evolve the core into an installable package so other projects can consume it directly and keep client repositories cleaner.
- Enable AI agents to generate and maintain metadata files so CRM/ERP systems can be assembled quickly from declarative definitions.

## Working Rules
- If a nested function is long and does more than one thing, extract it.
- Never fetch data in a loop.
- Prefer declarative metadata changes over feature-specific runtime branching.
- Do not ask clarifying questions unless the request is genuinely ambiguous.

## Project Structure
- `frontend/`: active Vite frontend app and shared `htmljs`-based UI library in `frontend/lib/`.
- `frontend/lib/test/`: Jest-style tests for shared frontend runtime behavior.
- `svc/`: active backend runtime on Deno + Oak.
- `sample-cli/crm/features/`: sample CRM metadata files.
- `sample-cli/crm/db/`: PostgreSQL schema, seed, and local setup scripts.
- `compose.yml`, `compose.release.yml`: container workflows that still reflect the legacy `CoreAPI` setup.
- `CoreAPI/`: legacy path referenced by roadmap and compose assets; this checkout does not currently include the usable ASP.NET source tree.
- `ROADMAP.md`: migration phases and architectural direction.

## Architecture Overview
- Frontend/backend interaction is metadata-driven, not screen-specific service glue.
- Frontend components are real classes built on `htmljs` and are responsible for their own `Render` and re-render flow.
- Runtime state is usually shared through global `Entity` and `Meta` objects loaded once and reused across components.
- Backend services load metadata, enforce permissions from metadata rules, resolve runtime parameters/functions, and execute database operations safely.
- The platform is in migration from a legacy `.NET` runtime toward the Deno runtime in `svc/`.

## Build, Test, and Development Commands
- Root dev runner: `deno task dev`
- Root start runner: `deno task start`
- Root test runner: `deno task test`
- Frontend dev flow: use the root Deno runner instead of calling `pnpm` or `bun` directly
- Frontend production build: `deno task start`
- Frontend preview build output: served by `scripts/static_server.ts` through the root Deno runner
- Deno runtime dev server: `cd svc && deno task dev`
- Deno runtime start: `cd svc && deno task start`
- Frontend tests: `deno task test`
- Deno tests: `cd svc && deno test --allow-all`

## Coding Style & Naming Conventions
- Keep files feature-scoped and avoid unrelated cross-cutting edits.
- Avoid editing vendored/generated assets unless the task explicitly requires it.
- When runtime logic becomes hard to follow, extract helpers instead of extending already-large functions.

## Testing Guidelines
- Add or update tests when changing shared runtime behavior in `frontend/lib/` or `svc/src/`.
- Frontend tests use Jest-style APIs in `frontend/lib/test/*.test.js`.
- Backend tests live beside runtime modules in `svc/src/**/*.test.ts`.
- If you change metadata transformation, query building, auth, or file services, run the relevant Deno tests before finishing.

## Commit & Pull Request Guidelines
- Use short, imperative commit subjects.
- Keep each commit scoped to one logical change.
- PRs should include:
  - What changed and why.
  - Affected areas (`frontend`, `svc`, `sample-cli`, infra, docs).
  - Verification steps and commands run.
  - Screenshots for UI work when applicable.

## Roadmap Direction
- Roadmap ownership is in `ROADMAP.md`.
- Priority order remains runtime rewrite, then database migration, then expanded YAML-first workflows.

## Security & Configuration Tips
- Do not commit real secrets.
- Prefer local environment variables for runtime configuration.
- Treat any credentials in compose files or sample scripts as local-only defaults.
