# Roadmap

## Principles
- Priority order is fixed: runtime rewrite, then database migration, then YAML support.
- Backward compatibility is not required during roadmap execution.
- Development workflow is TDD-first (`red -> green -> refactor`) for runtime and metadata transformation logic.
- Goal is package-first delivery for smaller client projects.

## Phase 1: Runtime Rewrite (Now)
### Scope
- Move core runtime from `CoreAPI (.NET)` toward a native JS runtime on `Bun`.
- Keep security-oriented execution patterns inspired by `Deno`.
- Preserve metadata-driven execution model (metadata defines behavior, runtime executes).
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

## Phase 2: Database Migration to PostgreSQL (Next)
### Scope
- Replace SQL Server as primary database with PostgreSQL.
- Update metadata-to-SQL/DML transformation for PostgreSQL dialect.
- Migrate local and deployment stack defaults to PostgreSQL.

### Deliverables
- PostgreSQL schema/migration scripts for core metadata and runtime data paths.
- Updated query builder/transformer behavior for PostgreSQL syntax and parameterization.
- Updated local compose/deployment configuration.

### Success Metrics
- 100% of core runtime integration tests pass on PostgreSQL.
- All metadata-generated CRUD flows run on PostgreSQL without manual SQL edits.
- End-to-end test suite runtime on PostgreSQL is within 20% of SQL Server baseline.

## Phase 3: YAML Support (Later)
### Scope
- Add YAML input support alongside JSON for metadata authoring and review.
- Maintain one normalized runtime metadata object after parsing.

### Deliverables
- YAML parser/loader wired into metadata load pipeline.
- Validation rules shared across JSON and YAML inputs.
- Documentation and examples for YAML metadata files.

### Success Metrics
- Equivalent JSON and YAML metadata produce identical normalized runtime metadata.
- 100% pass rate for parser and validation tests on both formats.
- At least 3 representative metadata feature files validated in YAML (form, list/query, action/event).
