using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using DuckDB.NET.Data;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace CoreAPI.Services.Sql
{
    public class DuckDbProvider(IDistributedCache cache, IConfiguration cfg) : ISqlProvider
    {
        public string Env { get; set; }
        public string TenantCode { get; set; }
        public string UserId { get; set; }
        public List<string> SystemFields { get; set; }

        private const string DUCK = "duck_";
        static readonly TSqlTokenType[] SideEffectCmd = [
            TSqlTokenType.Insert, TSqlTokenType.Update, TSqlTokenType.Delete,
            TSqlTokenType.Create, TSqlTokenType.Drop, TSqlTokenType.Alter,
            TSqlTokenType.Truncate, TSqlTokenType.MultilineComment, TSqlTokenType.SingleLineComment
        ];

        public async Task<string> GetConnStrFromKey(string connKey, string tenantCode = null, string env = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connKey);
            tenantCode = TenantCode ?? tenantCode;
            env = Env ?? env;
            var key = $"{tenantCode}_{connKey}_{env}";
            var conStr = await cache.GetStringAsync(key);
            if (conStr != null) return conStr;
            var query = $"select * from [Tenant] where TenantCode = '{tenantCode}' and ConnKey = '{connKey}' and Env = '{env}'";
            var tenantEnv = await ReadDsAs<Tenant>(query, cfg.GetConnectionString(DUCK + Utils.ConnKey))
                ?? throw new ApiException($"Tenant environment NOT found {key}");
            await cache.SetStringAsync(key, tenantEnv.ConnStr, Utils.CacheTTL);
            return tenantEnv.ConnStr;
        }

        public string GetCreateOrUpdateCmd(PatchVM vm)
        {
            if (vm == null || vm.Table.IsNullOrWhiteSpace() || vm.Changes.Nothing())
            {
                throw new ApiException("Table name and change details can NOT be empty")
                {
                    StatusCode = HttpStatusCode.BadRequest
                };
            }
            if (vm.Id is null)
            {
                throw new ApiException("Id cannot be null") { StatusCode = HttpStatusCode.BadRequest };
            }

            vm.Table = Utils.RemoveWhiteSpace(vm.Table);
            vm.Changes = vm.Changes.Where(x =>
            {
                if (x.Field.IsNullOrWhiteSpace()) throw new ApiException($"Field name can NOT be empty") { StatusCode = HttpStatusCode.BadRequest };
                x.Field = Utils.RemoveWhiteSpace(x.Field);
                x.Value = x.Value?.Replace("'", "''");
                x.OldVal = x.OldVal?.Replace("'", "''");
                return !SystemFields.Contains(x.Field);
            }).ToList();
            var idField = vm.Id;
            var valueFields = vm.Changes.Where(x => !SystemFields.Contains(x.Field.ToLower())).ToArray();
            var now = DateTime.Now.ToString(DateTimeExt.DateFormat);
            var oldId = idField?.OldVal;
            if (oldId is not null)
            {
                var update = valueFields.Combine(x => x.Value is null ? $"[{x.Field}] = null" : $"[{x.Field}] = N'{x.Value}'");
                if (update.IsNullOrWhiteSpace()) return null;
                return @$"update [{vm.Table}] set {update}, 
                UpdatedBy = '{UserId ?? 1.ToString()}', UpdatedDate = '{now}' where Id = '{oldId}';";
            }
            else
            {
                valueFields = valueFields.Where(x => x.Field != "Active").ToArray();
                var fields = valueFields.Combine(x => $"[{x.Field}]");
                var values = valueFields.Combine(x => x.Value is null ? "null" : $"N'{x.Value}'");
                if (fields.IsNullOrWhiteSpace() || values.IsNullOrWhiteSpace()) return null;
                return @$"insert into [{vm.Table}] ([Id], [Active], [InsertedBy], [InsertedDate], {fields})
                    values ('{idField.Value}', 1, '{UserId ?? 1.ToString()}', '{now}', {values});";
            }
        }

        public bool HasSideEffect(string sql, params TSqlTokenType[] allowCmds)
        {
            var finalCmd = SideEffectCmd.Except(allowCmds).ToArray();
            TSql110Parser parser = new(true);
            var fragments = parser.Parse(new StringReader(sql), out var errors);
            return fragments.ScriptTokenStream.Any(x => finalCmd.Contains(x.TokenType));
        }

        public async Task<Dictionary<string, object>[][]> ReadDataSet(string query, string connInfo, bool shouldMapToConnStr = false, List<WhereParamVM> paramVMs = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(query);
            ArgumentException.ThrowIfNullOrWhiteSpace(connInfo);
            var sideEffect = HasSideEffect(query);
            if (sideEffect) throw new ApiException("Side effect of query is NOT allowed");
            var connStr = shouldMapToConnStr ? await GetConnStrFromKey(connInfo) : connInfo;

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
            return [.. tables];
        }

        public async Task<T> ReadDsAs<T>(string query, string connInfo) where T : class
        {
            var ds = await ReadDataSet(query, connInfo);
            if (ds.Length == 0 || ds[0].Length == 0) return null;
            return ds[0][0].MapTo<T>();
        }

        public async Task<T[]> ReadDsAsArr<T>(string query, string connInfo) where T : class
        {
            var ds = await ReadDataSet(query, connInfo);
            if (ds.Length == 0 || ds[0].Length == 0) return [];
            return ds[0].Select(x => x.MapTo<T>()).ToArray();
        }

        public Task<int> RunSqlCmd(string connStr, string cmdText)
        {
            using var connection = new DuckDBConnection(connStr);
            connection.Open();

            using var duckDbCommand = connection.CreateCommand();
            duckDbCommand.CommandText = cmdText;
            duckDbCommand.ExecuteNonQuery();
            return Task.FromResult(1);
        }

        public async Task TransferFromSqlServer(ISqlProvider sqlServer, ISqlProvider duckDb)
        {
            var tableQuery = "SELECT s.name as SchemaName, t.name as TableName from sys.tables t inner join sys.schemas s ON t.schema_id = s.schema_id";
            var connStr = await sqlServer.GetConnStrFromKey(Utils.ConnKey);
            var duckConnStr = cfg.GetConnectionString(DUCK + Utils.ConnKey);
            var ds = await sqlServer.ReadDsAsArr<TableSchema>(tableQuery, connStr);
            var tasks = ds.Select(async table =>
            {
                var dataQuery = $"select * from [{table.TableName}]";
                var row = (await sqlServer.ReadDataSet(dataQuery, connStr))[0];
                return row.Select(cell =>
                {
                    var insertCmd = GetCreateOrUpdateCmd(new PatchVM
                    {
                        Table = table.TableName,
                        DataConn = duckConnStr,
                        Changes = cell.Select(x => new PatchDetail { Field = x.Key, Value = x.Value?.ToString() }).ToList()
                    });
                    return insertCmd;
                }).Combine(";");
            });
            await Task.WhenAll(tasks);
            var cmd = tasks.Select(x => x.Result).Combine(";");
            await RunSqlCmd(duckConnStr, cmd);
        }

        public string GetUpdateCmd(PatchVM vm)
        {
            if (vm == null || vm.Table.IsNullOrWhiteSpace() || vm.Changes.Nothing())
            {
                throw new ApiException("Table name and change details can NOT be empty")
                {
                    StatusCode = HttpStatusCode.BadRequest
                };
            }
            if (vm.Id is null)
            {
                throw new ApiException("Id cannot be null") { StatusCode = HttpStatusCode.BadRequest };
            }

            vm.Table = Utils.RemoveWhiteSpace(vm.Table);
            vm.Changes = vm.Changes.Where(x =>
            {
                if (x.Field.IsNullOrWhiteSpace()) throw new ApiException($"Field name can NOT be empty") { StatusCode = HttpStatusCode.BadRequest };
                x.Field = Utils.RemoveWhiteSpace(x.Field);
                x.Value = x.Value?.Replace("'", "''");
                x.OldVal = x.OldVal?.Replace("'", "''");
                return !SystemFields.Contains(x.Field);
            }).ToList();
            var idField = vm.Id;
            var valueFields = vm.Changes.Where(x => !SystemFields.Contains(x.Field.ToLower())).ToArray();
            var now = DateTime.Now.ToString(DateTimeExt.DateFormat);
            var oldId = idField?.Value;
            var update = valueFields.Combine(x => x.Value is null ? $"[{x.Field}] = null" : $"[{x.Field}] = N'{x.Value}'");
            if (update.IsNullOrWhiteSpace()) return null;
            return @$"update [{vm.Table}] set {update}, 
                UpdatedBy = '{UserId ?? 1.ToString()}', UpdatedDate = '{now}' where Id = '{oldId}';";
        }

        public Task<int> RunSqlCmd(string connStr, string cmdText, Dictionary<string, object> ps)
        {
            throw new NotImplementedException();
        }
    }

    internal class TableSchema
    {
        public string SchemaName { get; set; }
        public string TableName { get; set; }
    }
}
