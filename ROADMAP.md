# Roadmap

## Principles
- Priority order is fixed: runtime rewrite, then database migration, then YAML support.
- Backward compatibility is not required during roadmap execution.
- Development workflow is TDD-first (`red -> green -> refactor`) for runtime and metadata transformation logic.
- Goal is package-first delivery for smaller client projects.

### Scope

## Phase 1
- Move core runtime from `CoreAPI (.NET)` toward a native JS runtime on `bun`. The new runtime is under `svc` folder.
- Preserve metadata-driven execution model (metadata defines behavior, runtime executes). See sample files in \CoreAPI\wwwroot\upload\crm\features\profile.json and svc/test/profile.yaml
- Support YAML files as primary source code format for frontend component configs, SQL queries, DML, backend logic, etc. JSON support remains for backward compatibility.
- Finish the JS runtime implementation to fully execute YAML-defined features.
- Integrate popular cloud services like Supabase for storage, database, and authentication instead of local setups.
- Using PostgreSQL (via Supabase) instead of SQL Server, no need to support SQL Server.
- Add comprehensive tests for the runtime and metadata execution.

## Phase 2
Build an AI-driven system that enables users to generate complete applications via natural language prompts (e.g., "create CRM system"). The system will:
- Use AI CLI tools to generate YAML-based metadata and code following the new architecture.
- Automatically deploy generated systems for clients.
- Allow clients to upload/download and modify generated code.
- Focus on YAML as the primary code format for rapid, declarative development.
- Include a billing system to charge clients for auto-generated features.

### Deliverables
- Installable runtime package with documentation for client project integration.
- Metadata execution engine for query/action/event flows, supporting YAML-defined components, SQL queries, DML, and backend logic.
- Permission enforcement pipeline based on metadata rules.
- Integration with Supabase for cloud storage, database, and authentication.
- Comprehensive test suite for runtime execution, metadata parsing, and cloud service integrations.
- Test harness for metadata transformation and runtime execution.

### Success Metrics
- Runtime package is installable and runs in 1 sample client apps.
- At least 80% unit test coverage for metadata transformation, permission evaluation, and runtime execution modules.
- 100% pass rate on critical-path integration tests (sign-in via Supabase, metadata load, query execute, save flow).
- Successful integration with Supabase for database, storage, and authentication in production-like environments.
- No data fetching inside loops in runtime implementation.
