# Roadmap

## Principles
- Priority order is fixed: runtime rewrite, then database migration, then YAML support.
- Backward compatibility is not required during roadmap execution.
- Development workflow is TDD-first (`red -> green -> refactor`) for runtime and metadata transformation logic.
- Goal is package-first delivery for smaller client projects.

### Scope
- Move core runtime from `CoreAPI (.NET)` toward a native JS runtime on `bun`. The new runtime is under `svc` folder.
- Preserve metadata-driven execution model (metadata defines behavior, runtime executes). See an sample files in \CoreAPI\wwwroot\upload\crm\features\profile.json
- Support yaml files beside json files, so the user can review code easier
- Using PostgreSQL instead of SQL Server, no need to support SQL Server
- Support initial data loading from meta attributes, not only by Entity ID.

### Deliverables
- Installable runtime package with documentation for client project integration.
- Metadata execution engine for query/action/event flows.
- Permission enforcement pipeline based on metadata rules.
- Test harness for metadata transformation and runtime execution.

### Success Metrics
- Runtime package is installable and runs in at least 2 sample client apps.
- At least 80% unit test coverage for metadata transformation and permission evaluation modules.
- 100% pass rate on critical-path integration tests (sign-in, metadata load, query execute, save flow).
- No data fetching inside loops in runtime implementation.