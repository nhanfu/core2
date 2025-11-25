using Core.Extensions;
using CoreAPI.Services.Sql;
using Npgsql;

namespace CoreAPI.Services;

public class DatabaseMigrationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(IConfiguration configuration, ILogger<DatabaseMigrationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task MigrateAsync()
    {
        var connectionString = _configuration.GetConnectionString("logistics");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("Connection string 'logistics' not found. Skipping migration.");
            return;
        }

        try
        {
            await EnsureDatabaseExists(connectionString);
            await CreateTablesIfNotExists(connectionString);
            _logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database migration");
            throw;
        }
    }

    private async Task EnsureDatabaseExists(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        builder.Database = "postgres"; // Connect to default postgres database

        try
        {
            using var connection = new NpgsqlConnection(builder.ToString());
            await connection.OpenAsync();

            // Check if database exists
            var checkDbQuery = $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'";
            using var checkCmd = new NpgsqlCommand(checkDbQuery, connection);
            var exists = await checkCmd.ExecuteScalarAsync();

            if (exists == null)
            {
                _logger.LogInformation($"Database '{databaseName}' does not exist. Creating...");
                var createDbQuery = $"CREATE DATABASE \"{databaseName}\"";
                using var createCmd = new NpgsqlCommand(createDbQuery, connection);
                await createCmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"Database '{databaseName}' created successfully.");
            }
            else
            {
                _logger.LogInformation($"Database '{databaseName}' already exists.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ensuring database exists: {databaseName}");
            throw;
        }
    }

    private async Task CreateTablesIfNotExists(string connectionString)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var createTablesSql = @"
-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";

-- Users table
CREATE TABLE IF NOT EXISTS ""User"" (
    ""Id"" VARCHAR(50) PRIMARY KEY,
    ""TenantCode"" VARCHAR(50),
    ""Username"" VARCHAR(255) NOT NULL,
    ""Password"" VARCHAR(255),
    ""Email"" VARCHAR(255),
    ""FullName"" VARCHAR(255),
    ""Avatar"" TEXT,
    ""PhoneNumber"" VARCHAR(50),
    ""Active"" BOOLEAN DEFAULT true,
    ""InsertedDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ""InsertedBy"" VARCHAR(50),
    ""UpdatedDate"" TIMESTAMP,
    ""UpdatedBy"" VARCHAR(50)
);

-- Tenant table
CREATE TABLE IF NOT EXISTS ""Tenant"" (
    ""Id"" VARCHAR(50) PRIMARY KEY,
    ""TenantCode"" VARCHAR(50) UNIQUE NOT NULL,
    ""Name"" VARCHAR(255) NOT NULL,
    ""Description"" TEXT,
    ""Active"" BOOLEAN DEFAULT true,
    ""InsertedDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ""InsertedBy"" VARCHAR(50),
    ""UpdatedDate"" TIMESTAMP,
    ""UpdatedBy"" VARCHAR(50)
);

-- Role table
CREATE TABLE IF NOT EXISTS ""Role"" (
    ""Id"" VARCHAR(50) PRIMARY KEY,
    ""TenantCode"" VARCHAR(50),
    ""Name"" VARCHAR(255) NOT NULL,
    ""Description"" TEXT,
    ""Active"" BOOLEAN DEFAULT true,
    ""InsertedDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ""InsertedBy"" VARCHAR(50),
    ""UpdatedDate"" TIMESTAMP,
    ""UpdatedBy"" VARCHAR(50)
);

-- UserRole table
CREATE TABLE IF NOT EXISTS ""UserRole"" (
    ""Id"" VARCHAR(50) PRIMARY KEY,
    ""TenantCode"" VARCHAR(50),
    ""UserId"" VARCHAR(50) REFERENCES ""User""(""Id""),
    ""RoleId"" VARCHAR(50) REFERENCES ""Role""(""Id""),
    ""Active"" BOOLEAN DEFAULT true,
    ""InsertedDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ""InsertedBy"" VARCHAR(50),
    ""UpdatedDate"" TIMESTAMP,
    ""UpdatedBy"" VARCHAR(50)
);

-- Feature table
CREATE TABLE IF NOT EXISTS ""Feature"" (
    ""Id"" VARCHAR(50) PRIMARY KEY,
    ""TenantCode"" VARCHAR(50),
    ""Name"" VARCHAR(255) NOT NULL,
    ""Label"" VARCHAR(255),
    ""Icon"" VARCHAR(100),
    ""ParentId"" VARCHAR(50),
    ""Active"" BOOLEAN DEFAULT true,
    ""Order"" INTEGER DEFAULT 0,
    ""InsertedDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ""InsertedBy"" VARCHAR(50),
    ""UpdatedDate"" TIMESTAMP,
    ""UpdatedBy"" VARCHAR(50)
);

-- FeaturePolicy table
CREATE TABLE IF NOT EXISTS ""FeaturePolicy"" (
    ""Id"" VARCHAR(50) PRIMARY KEY,
    ""TenantCode"" VARCHAR(50),
    ""RoleId"" VARCHAR(50) REFERENCES ""Role""(""Id""),
    ""FeatureId"" VARCHAR(50) REFERENCES ""Feature""(""Id""),
    ""CanRead"" BOOLEAN DEFAULT false,
    ""CanWrite"" BOOLEAN DEFAULT false,
    ""CanDelete"" BOOLEAN DEFAULT false,
    ""Active"" BOOLEAN DEFAULT true,
    ""InsertedDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ""InsertedBy"" VARCHAR(50),
    ""UpdatedDate"" TIMESTAMP,
    ""UpdatedBy"" VARCHAR(50)
);

-- Entity table
CREATE TABLE IF NOT EXISTS ""Entity"" (
    ""Id"" VARCHAR(50) PRIMARY KEY,
    ""TenantCode"" VARCHAR(50),
    ""Name"" VARCHAR(255) NOT NULL,
    ""Description"" TEXT,
    ""AliasFor"" VARCHAR(255),
    ""RefDetailClass"" VARCHAR(255),
    ""RefListClass"" VARCHAR(255),
    ""Namespace"" VARCHAR(255),
    ""Active"" BOOLEAN DEFAULT true,
    ""InsertedDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ""InsertedBy"" VARCHAR(50),
    ""UpdatedDate"" TIMESTAMP,
    ""UpdatedBy"" VARCHAR(50)
);

-- Create indexes
CREATE INDEX IF NOT EXISTS ""idx_user_tenantcode"" ON ""User""(""TenantCode"");
CREATE INDEX IF NOT EXISTS ""idx_user_username"" ON ""User""(""Username"");
CREATE INDEX IF NOT EXISTS ""idx_user_email"" ON ""User""(""Email"");
CREATE INDEX IF NOT EXISTS ""idx_feature_parentid"" ON ""Feature""(""ParentId"");
CREATE INDEX IF NOT EXISTS ""idx_userrole_userid"" ON ""UserRole""(""UserId"");
CREATE INDEX IF NOT EXISTS ""idx_userrole_roleid"" ON ""UserRole""(""RoleId"");
";

        try
        {
            using var command = new NpgsqlCommand(createTablesSql, connection);
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Database tables created/verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database tables");
            throw;
        }
    }
}
