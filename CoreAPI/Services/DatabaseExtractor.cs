using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace CoreAPI.Services
{
    public class DatabaseExtractor(IConfiguration configuration)
    {
        private readonly string _outputPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ai", "data", "database_dump.json");
        public void ExportDatabaseToJson()
        {
            using var connection = new SqlConnection(configuration.GetConnectionString("logistics"));
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

        private List<string> GetTableNames(SqlConnection connection)
        {
            var tables = new List<string>();
            using var command = new SqlCommand(@"SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo' 
AND TABLE_NAME <> 'sysdiagrams';", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read()) tables.Add(reader.GetString(0));
            return tables;
        }

        private List<Dictionary<string, object>> GetTableData(SqlConnection connection, string tableName)
        {
            var result = new List<Dictionary<string, object>>();
            using var command = new SqlCommand($"SELECT * FROM [{tableName}]", connection);
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
