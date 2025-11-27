# Docker Compose Migration Summary

## ✅ Completed Tasks

### 1. Docker Configuration Files Created

#### Frontend (`frontend/dockerfile-new`)
- Multi-stage build with Node 20 Alpine
- Vite build process
- Nginx Alpine for serving static files
- Custom nginx.conf with API proxy support

#### CoreAPI (`CoreAPI/dockerfile-new`)
- Multi-stage build with .NET 9 SDK
- Build and publish optimized for production
- ASP.NET Core runtime
- Exposed on port 8080

#### Docker Compose (`docker-compose-new.yml`)
- PostgreSQL 16 Alpine database
- CoreAPI service with health check dependencies
- Frontend service with Nginx
- Named volumes for data persistence
- Bridge network for service communication

### 2. Database Migration (SQL Server → PostgreSQL)

#### Package Updates (`CoreAPI.csproj`)
- ✅ Removed: `Microsoft.EntityFrameworkCore.SqlServer`
- ✅ Added: `Npgsql.EntityFrameworkCore.PostgreSQL v9.0.3`
- ✅ Kept: `Microsoft.SqlServer.DacFx` (for SQL parsing/security)

#### Connection String (`appsettings.json`)
**Before:**
```json
"logistics": "Server=sqlserver,1433;Database=crm;User Id=sa;Password=YourStrongPassword123!;"
```

**After:**
```json
"logistics": "Host=localhost;Port=5432;Database=crm;Username=postgres;Password=YourStrongPassword123!;"
```

#### New PostgreSQL Provider (`PostgreSqlProvider.cs`)
- Created new provider implementing `ISqlProvider`
- Uses Npgsql for PostgreSQL connections
- Adapted SQL syntax for PostgreSQL (double quotes for identifiers)
- Transaction support with commit/rollback
- Parameter binding with `NpgsqlCommand`

#### Program.cs Updates
- Registered `PostgreSqlProvider` in DI container
- Changed default provider from `SqlServerProvider` to `PostgreSqlProvider`

### 3. Supporting Files

- `.dockerignore` files for both frontend and CoreAPI
- `nginx.conf` for frontend routing and API proxy
- `DOCKER_SETUP.md` - Comprehensive setup and usage guide

## 🚀 How to Run

### First Time Setup

```powershell
# Navigate to project root
cd d:\Projects\TSN\core2

# Restore NuGet packages (important!)
cd CoreAPI
dotnet restore
cd ..

# Build and start all services
docker-compose -f docker-compose-new.yml up -d --build
```

### Verify Services

```powershell
# Check all services are running
docker-compose -f docker-compose-new.yml ps

# View logs
docker-compose -f docker-compose-new.yml logs -f
```

### Access Services

- **Frontend**: http://localhost
- **API**: http://localhost:8080
- **PostgreSQL**: localhost:5432

## 📝 Important Notes

### Database Connection Strings

**From Host Machine:**
```
Host=localhost;Port=5432;Database=crm;Username=postgres;Password=YourStrongPassword123!;
```

**From Docker Containers (API):**
```
Host=db;Port=5432;Database=crm;Username=postgres;Password=YourStrongPassword123!;
```

### Environment Variables in docker-compose-new.yml

The API service uses environment variables to override appsettings.json:
- `ConnectionStrings__logistics` - Uses `db` as hostname (Docker service name)
- This ensures the API container can reach PostgreSQL

### Key Differences: SQL Server vs PostgreSQL

1. **Identifier Quoting**:
   - SQL Server: `[TableName]`, `[FieldName]`
   - PostgreSQL: `"TableName"`, `"FieldName"`

2. **Connection Libraries**:
   - SQL Server: `System.Data.SqlClient` → `SqlConnection`
   - PostgreSQL: `Npgsql` → `NpgsqlConnection`

3. **UPSERT Syntax**:
   - SQL Server: `MERGE` statement
   - PostgreSQL: `INSERT ... ON CONFLICT ... DO UPDATE`

4. **Transaction Methods**:
   - SQL Server: Synchronous `Commit()`/`Rollback()`
   - PostgreSQL: Async `CommitAsync()`/`RollbackAsync()`

## 🔧 Next Steps

### 1. Run dotnet restore (Important!)

```powershell
cd CoreAPI
dotnet restore
```

This will resolve the compile errors related to `Microsoft.SqlServer.TransactSql.ScriptDom`.

### 2. Database Schema Migration

You'll need to create the PostgreSQL schema. Options:

**Option A: EF Migrations (if using EF Core)**
```powershell
dotnet ef migrations add InitialMigration
dotnet ef database update
```

**Option B: SQL Script**
- Export your SQL Server schema
- Convert to PostgreSQL syntax
- Run against PostgreSQL

**Option C: Use pgloader**
```bash
# Install pgloader
# Create config file to migrate from SQL Server to PostgreSQL
pgloader mssql://sa:password@sqlserver/crm postgresql://postgres:password@localhost/crm
```

### 3. Test the Application

```powershell
# Start services
docker-compose -f docker-compose-new.yml up -d

# Watch logs
docker-compose -f docker-compose-new.yml logs -f api

# Test API endpoint
curl http://localhost:8080/api/health

# Test frontend
# Open browser: http://localhost
```

### 4. Production Checklist

Before deploying to production:

- [ ] Change default passwords in docker-compose-new.yml
- [ ] Use Docker secrets for sensitive data
- [ ] Enable HTTPS/SSL
- [ ] Configure proper CORS policies
- [ ] Set up database backups
- [ ] Configure logging/monitoring
- [ ] Optimize PostgreSQL settings (shared_buffers, work_mem, etc.)
- [ ] Set up connection pooling
- [ ] Add health check endpoints
- [ ] Configure resource limits in docker-compose

## 🐛 Troubleshooting

### Package Restore Issues

If you see compile errors related to `TSqlTokenType` or `TSql110Parser`:

```powershell
cd CoreAPI
dotnet clean
dotnet restore
dotnet build
```

### Database Connection Failed

1. Check PostgreSQL is running:
   ```powershell
   docker-compose -f docker-compose-new.yml ps db
   ```

2. Check logs:
   ```powershell
   docker-compose -f docker-compose-new.yml logs db
   ```

3. Test connection from host:
   ```powershell
   docker exec -it core2-postgres psql -U postgres -d crm
   ```

### API Not Starting

1. Ensure database health check passed
2. Check connection string environment variable
3. View API logs for details:
   ```powershell
   docker-compose -f docker-compose-new.yml logs api
   ```

## 📁 Files Created/Modified

### Created:
- `docker-compose-new.yml`
- `frontend/dockerfile-new`
- `frontend/nginx.conf`
- `frontend/.dockerignore`
- `CoreAPI/dockerfile-new`
- `CoreAPI/.dockerignore`
- `CoreAPI/Services/Sql/PostgreSqlProvider.cs`
- `DOCKER_SETUP.md`
- `DOCKER_MIGRATION_SUMMARY.md` (this file)

### Modified:
- `CoreAPI/CoreAPI.csproj` - Added Npgsql packages
- `CoreAPI/appsettings.json` - PostgreSQL connection string
- `CoreAPI/Program.cs` - Registered PostgreSqlProvider

## 🔄 Rollback to SQL Server

If you need to revert:

1. Update `docker-compose-new.yml` - use SQL Server image
2. Restore `appsettings.json` connection string
3. Update `Program.cs`:
   ```csharp
   services.AddScoped<ISqlProvider, SqlServerProvider>();
   ```
4. Update `CoreAPI.csproj` - restore SQL Server packages

## 📖 Additional Resources

- [PostgreSQL Docker Official Image](https://hub.docker.com/_/postgres)
- [Npgsql Documentation](https://www.npgsql.org/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET PostgreSQL Guide](https://docs.microsoft.com/ef/core/providers/npgsql/)

---

**Status**: ✅ All configuration files created and ready
**Next Action**: Run `dotnet restore` in CoreAPI folder, then test with `docker-compose up`
