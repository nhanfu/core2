# Core2

Metadata-driven application core for CRM/ERP-style systems. The runtime pushes screen behavior, queries, permissions, and UI configuration into metadata so client apps stay focused on business logic instead of wiring frontend and backend integrations by hand.

## Current State
- `frontend/` is the active Vite application.
- `svc/` is the in-progress native JS runtime built on Deno + Oak.
- `sample-cli/crm/` contains sample metadata and PostgreSQL setup files.
- `CoreAPI/` is referenced by older Docker files and roadmap notes, but this checkout does not currently contain the usable ASP.NET source tree.

## Repository Layout
- `frontend/`: Vite frontend and shared `htmljs`-based UI runtime.
- `svc/`: Deno backend runtime, auth, metadata, query, patch, and file services.
- `sample-cli/crm/features/`: sample feature metadata files.
- `sample-cli/crm/db/`: PostgreSQL schema and seed scripts.
- `compose.yml`, `compose.release.yml`: container workflows that still reflect the legacy `CoreAPI` setup.
- `ROADMAP.md`: migration direction and phase priorities.

## Architecture
- Metadata defines screens, events, queries, permissions, and runtime behavior.
- Frontend components read from shared `Meta` and `Entity` state instead of owning per-screen API glue.
- Backend services load metadata, enforce permission rules, resolve runtime parameters/functions, and execute database work safely.
- The project is migrating from the legacy `.NET` runtime toward the Deno runtime in `svc/`.

## Run Locally

### Start frontend and backend together
```bash
DATABASE_URL='postgresql://postgres:YourStrongPassword123!@localhost:5432/crm' deno task dev
```

This starts:
- `svc` on the Deno watcher
- `frontend` on the Vite dev server

### Start both services without watch mode
```bash
DATABASE_URL='postgresql://postgres:YourStrongPassword123!@localhost:5432/crm' deno task start
```

This starts:
- `svc` with `deno task start`
- `frontend` by building once, then serving `frontend/dist` from a Deno static file server on port `4173`

### Deno Runtime
```bash
cd svc
DATABASE_URL='postgresql://postgres:YourStrongPassword123!@localhost:5432/crm' deno task dev
```

Default runtime port is `8000`. Health check:

```bash
curl http://localhost:8000/health
```

## Build

### Frontend production build
Use the root Deno runner:

```bash
DATABASE_URL='postgresql://postgres:YourStrongPassword123!@localhost:5432/crm' deno task start
```

### Deno runtime start without watch mode
```bash
cd svc
deno task start
```

## Test

### Frontend library tests
Use the root Deno test runner:

```bash
deno task test
```

### Deno tests
```bash
cd svc
deno test --allow-all
```

### Run repository tests from the root
```bash
deno task test
```

## Metadata Samples
- Sample CRM metadata lives under `sample-cli/crm/features/`.
- Sample PostgreSQL bootstrap scripts live under `sample-cli/crm/db/`.
- The roadmap indicates YAML will be the primary metadata format over time, while JSON samples remain present in the repo today.

## Notes
- `compose.yml` and `compose.release.yml` still point at a legacy `CoreAPI` workflow. Treat them as migration-era assets unless you update them alongside the runtime.
- Avoid hardcoding feature-specific frontend/backend glue when metadata can express the same behavior.
- The local PostgreSQL setup script is [sample-cli/crm/db/setup_local_pg.sh](/home/nhanjs/projects/core2/sample-cli/crm/db/setup_local_pg.sh).
- The backend currently requires `DATABASE_URL` to be present in the environment at startup.
