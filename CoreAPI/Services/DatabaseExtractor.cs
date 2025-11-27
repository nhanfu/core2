using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace CoreAPI.Services
{
    public class DatabaseExtractor(IConfiguration configuration)
    {
        private readonly string _outputPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ai", "data", "database_dump.json");
        
        public void ExportDatabaseToJson()
        {
            using var connection = new NpgsqlConnection(configuration.GetConnectionString("logistics"));
            connection.Open();

            var tables = GetTableNames(connection);
            var dbSchema = new Dictionary<string, object>();

            foreach (var table in tables)
            {
                dbSchema[table] = GetTableData(connection, table);
            }

            string json = JsonConvert.SerializeObject(dbSchema, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(_outputPath)!);
            File.WriteAllText(_outputPath, json);
        }

        private List<string> GetTableNames(NpgsqlConnection connection)
        {
            var tables = new List<string>();
            // PostgreSQL: Changed to use information_schema.tables with schema_name = 'public'
            using var command = new NpgsqlCommand(@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_type = 'BASE TABLE'
                AND table_name NOT IN ('__EFMigrationsHistory');", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read()) tables.Add(reader.GetString(0));
            return tables;
        }

        private List<Dictionary<string, object>> GetTableData(NpgsqlConnection connection, string tableName)
        {
            var result = new List<Dictionary<string, object>>();
            // PostgreSQL: Use "double quotes" instead of [brackets]
            using var command = new NpgsqlCommand($"SELECT * FROM \"{tableName}\"", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader[i];
                result.Add(row);
            }
            return result;
        }
    }
}
