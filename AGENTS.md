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
- `compose.yml`: container workflows
- `ROADMAP.md`: migration phases and architectural direction

## Architecture Overview
- Frontend/backend interaction is metadata-driven, not screen-specific service glue.
- Frontend components are real classes built on `htmljs` and are responsible for their own `render` and re-render flow.
- Runtime state is usually shared through global `entity` and `meta` objects loaded once and reused across components.
- Backend services load metadata, enforce permissions from metadata rules, resolve runtime parameters/functions, and execute database operations safely.

## Build, Test, and Development Commands
- Root start runner: `deno task start`
- Root test runner: `deno task test`

## Coding Style & Naming Conventions
- Keep files feature-scoped and avoid unrelated cross-cutting edits.
- Avoid editing vendored/generated assets unless the task explicitly requires it.
- When runtime logic becomes hard to follow, extract helpers instead of extending already-large functions.

## Testing Guidelines
- Add or update tests when changing shared runtime behavior in `frontend/lib/` or `svc/src/`.
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
- Full roadmap is in `ROADMAP.md` & Configuration Tips
- Do not ask about security concerns unless explicitly asked by the user.
- Do not commit real secrets.
- Prefer local environment variables for runtime configuration.
- Treat any credentials in compose files or sample scripts as local-only defaults.
