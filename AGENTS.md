# Repository Guidelines

## Mission
- Build a metadata-driven core that hides frontend/backend communication details and keeps client apps focused on business logic.
- Evolve the core into an installable package so other projects can consume it directly and keep client repositories cleaner.
- AI Agents can generate meta files easily to complete any CRM/ERP in minutes. They can sign in the system, access metadata file and know how to inteact with the system.

## Instruction
- If a nested function is long and does more than one thing, extract it
- Never fetching data in a loop
- Don't ask clarifying questions unless the request is genuinely ambiguous — the user knows the scope and expects execution, not dialogue

## Project Structure & Module Organization
- `CoreAPI/`: ASP.NET Core 9 backend (`Program.cs`, controllers, services, models, middleware).
- `frontend/`: Vite-based frontend app (`src/` for app code, `lib/` for shared UI framework utilities).
- `frontend/lib/test/`: Jest-style unit tests for UI library components.
- `scripts/`: maintenance and deployment scripts (`deploy.sh`, JSON/id migration helpers).
- `compose.yml`: local multi-service stack (SQL Server, API, frontend).
- `Core.sln`: solution entry point for backend development in IDEs.

## Architecture Overview (Meta-Driven Runtime)
- This project centralizes frontend/backend interaction through metadata, not hardcoded service glue.
- Frontend components are real classes built on `htmljs` (native DOM + fluent API). Each component:
  - stores state in a global `Entity` object (one property per component),
  - reads behavior/config from `Meta`,
  - must implement its own `Render`/re-render flow.
- `Entity` and `Meta` are typically loaded once in `EditableComponent.LoadFeatureAndRender`, then shared by runtime components.
- Frontend mostly consumes metadata JSON from server, then fetches data and renders UI without repetitive per-screen service code.
- Backend responsibilities: load/save metadata files, enforce permissions from metadata rules, transform metadata parameters/functions into SQL/DML, and execute safely with permission checks.
- Metadata defines what to do (components config, queries, events), while core runtime defines how to execute it.

## Build, Test, and Development Commands
- Frontend dev server: `cd frontend && pnpm run dev`
- Frontend production build: `cd frontend && pnpm run build1`
- Frontend preview build output: `cd frontend && pnpm run serve`
- Backend run locally: `cd CoreAPI && dotnet run --urls="http://localhost:8080"`
- Backend restore/build: `cd CoreAPI && dotnet restore && dotnet build`
- Docker local stack: `docker compose up --build`

## Coding Style & Naming Conventions
- C# code: 4-space indentation, `PascalCase` for types/methods/properties, `camelCase` for locals/parameters.
- JavaScript/React code in `frontend/`: prefer 2-space indentation in JSX/JS files, `PascalCase` for components (`AppComponent.jsx`), `camelCase` for functions/variables.
- Keep files feature-scoped (forms in `src/forms`, reusable primitives in `frontend/lib`).
- Avoid editing vendored/generated assets under `frontend/public/` unless explicitly updating those bundles.
- For metadata work, prefer declarative updates in meta files over custom per-feature runtime branching.

## Testing Guidelines
- Frontend unit tests live in `frontend/lib/test/*.test.js` and use Jest-style APIs (`describe`, `test`, `expect`).
- Run tests manually (no committed script yet): `cd frontend && npx jest lib/test --runInBand`
- Add tests next to affected library behavior when changing `frontend/lib/*` logic.

## Commit & Pull Request Guidelines
- Recent history favors short, imperative commit subjects (example: `Remove websocket, add AuthService`).
- Use one logical change per commit; keep subject lines concise and specific.
- PRs should include:
  - What changed and why.
  - Affected areas (`CoreAPI`, `frontend`, `scripts`, infra).
  - Verification steps (commands run, API endpoints checked, screenshots for UI changes).
  - Linked issue/task when available.

## Roadmap Direction
- Roadmap ownership is in `ROADMAP.md` (phases, priorities, and success metrics).

## Security & Configuration Tips
- Do not commit real secrets; use local overrides in `CoreAPI/appsettings.Development.json` and environment variables.
- Treat SQL credentials in `compose.yml` as local defaults only; replace for shared/staging environments.
