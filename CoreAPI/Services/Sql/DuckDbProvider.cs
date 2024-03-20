using DuckDB.NET.Data;

namespace CoreAPI.Services.Sql
{
    public class DuckDbProvider()
    {
        public Dictionary<string, object>[][] ReadDataSet(string query, string connStr)
        {
            using var duckDBConnection = new DuckDBConnection(connStr);
            duckDBConnection.Open();
            var command = duckDBConnection.CreateCommand();
            command.CommandText = query;

            var queryResult = command.ExecuteReader();

            var tables = new List<Dictionary<string, object>[]>();

            while (queryResult.Read())
            {
                var table = new Dictionary<string, object>();
                for (int index = 0; index < queryResult.FieldCount; index++)
                {
                    table.Add(queryResult.GetName(index), queryResult.GetValue(index));
                }
            }
            return tables.ToArray();
        }

    }
}
