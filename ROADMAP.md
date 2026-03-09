# Roadmap

## Principles
- Priority order is fixed: runtime rewrite, then database migration, then YAML support.
- Backward compatibility is required during roadmap execution.
- Development workflow is TDD-first (`red -> green -> refactor`) for runtime and metadata transformation logic.

### Scope

## Phase 1
- Refactor UserService.cs, it's really big - done
- Let user code calc query using Jint runtime, the function is declared at json file in JSScript property, not to use CalcFinalQuery - done
- Support yaml, similar to json. See a sample yaml file in /CoreAPI/wwwroot/upload/crm/features/profile.yaml and an original JSON file in /CoreAPI/wwwroot/upload/crm/features/profile.json - done
    - The system should convert yaml to json before sending to the client.
    - All properties start with _ should be clear before sending to the client
- Using PostgreSQL (via Supabase) instead of SQL Server, no need to support SQL Server at this point - done
    - Convert all scripts in CoreAPI/wwwroot/upload/crm/db folder to postgreSQL scripts
    - Use PostgreSqlProvider instead of SqlServerProvider in Program.cs
    - Replace all sql server query to postgreSQL query
- Migrate core runtime from `CoreAPI (.NET)` toward a native JS runtime on `Deno`. The new runtime is under `svc` folder - working
- Add connection pool to PostgreSqlProvider, so that we don't have to create too many connection to the database - in ReadDataSet

## Phase 2
- Build an AI-driven system that enables users to generate complete applications via natural language prompts (e.g., "create CRM system"). The system will:
  - Use AI CLI tools to generate YAML-based metadata and code following the new architecture.
  - Automatically deploy generated systems for clients.
  - Allow clients to upload/download and modify generated code.
  - Focus on YAML as the primary code format for rapid, declarative development.
  - Include a billing system to charge clients for auto-generated features.
- AI Agents can sign in the system, act as a user, access metadata file and know how to inteact with the system.