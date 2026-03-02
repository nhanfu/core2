# Roadmap

## Principles
- Priority order is fixed: runtime rewrite, then database migration, then YAML support.
- Backward compatibility is required during roadmap execution.
- Development workflow is TDD-first (`red -> green -> refactor`) for runtime and metadata transformation logic.

### Scope

## Phase 1
- Refactor UserService.cs, it really big
- Migrate core runtime from `CoreAPI (.NET)` toward a native JS runtime on `Bun`. The new runtime is under `svc` folder.
- Preserve metadata-driven execution model (metadata defines behavior, runtime executes). See a sample yaml file in /CoreAPI/wwwroot/upload/crm/features/profile.yaml and an original JSON file in /CoreAPI/wwwroot/upload/crm/features/profile.json.
- Support YAML files as primary source code format for frontend component configs, SQL queries, DML, backend logic, etc. JSON support remains for backward compatibility.
- Integrate popular cloud services like Supabase for storage, database, and authentication instead of local setups.
- Using PostgreSQL (via Supabase) instead of SQL Server, no need to support SQL Server at this point.

## Phase 2
- To create package-first delivery for smaller client projects (another Bun/Deno project).
- AI Agents can sign in the system, act as a user, access metadata file and know how to inteact with the system.
- Build an AI-driven system that enables users to generate complete applications via natural language prompts (e.g., "create CRM system"). The system will:
- Use AI CLI tools to generate YAML-based metadata and code following the new architecture.
- Automatically deploy generated systems for clients.
- Allow clients to upload/download and modify generated code.
- Focus on YAML as the primary code format for rapid, declarative development.
- Include a billing system to charge clients for auto-generated features.
