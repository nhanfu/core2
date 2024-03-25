using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Data;
using System.Data.SqlClient;

namespace CoreAPI.Services.Sql;

public class SqlServerProvider(IDistributedCache cache, IConfiguration cfg) : ISqlProvider
{
    static readonly TSqlTokenType[] SideEffectCmd = [
        TSqlTokenType.Insert, TSqlTokenType.Update, TSqlTokenType.Delete,
            TSqlTokenType.Create, TSqlTokenType.Drop, TSqlTokenType.Alter,
            TSqlTokenType.Truncate, TSqlTokenType.MultilineComment, TSqlTokenType.SingleLineComment
    ];

    public string TenantCode { get; set; }
    public string Env { get; set; }
    public string UserId { get; set; }

    public async Task<Dictionary<string, object>[][]> ReadDataSet(string query, string connInfo, bool shouldMapToConnStr = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(connInfo);
        var sideEffect = HasSideEffect(query);
        if (sideEffect) throw new ApiException("Side effect of query is NOT allowed");
        var connStr = shouldMapToConnStr ? await GetConnStrFromKey(connInfo) : connInfo;
        var con = new SqlConnection(connStr);
        var sqlCmd = new SqlCommand
        {
            CommandType = CommandType.Text,
            CommandText = query,
            Connection = con
        };
        SqlDataReader reader = null;
        var tables = new List<Dictionary<string, object>[]>();
        try
        {
            await con.OpenAsync();
            reader = await sqlCmd.ExecuteReaderAsync();
            while (true)
            {
                var table = new List<Dictionary<string, object>>();
                while (await reader.ReadAsync())
                {
                    table.Add(ReadSqlRecord(reader));
                }
                tables.Add([.. table]);
                var next = await reader.NextResultAsync();
                if (!next) break;
            }
            return [.. tables];
        }
        catch (Exception e)
        {
            var message = $"{e.Message} {query}";
            throw new ApiException(message, e)
            {
                StatusCode = HttpStatusCode.InternalServerError,
            };
        }
        finally
        {
            if (reader is not null) await reader.DisposeAsync();
            await sqlCmd.DisposeAsync();
            await con.DisposeAsync();
        }
    }

    protected static Dictionary<string, object> ReadSqlRecord(IDataRecord reader)
    {
        var row = new Dictionary<string, object>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var val = reader[i];
            row[reader.GetName(i)] = val == DBNull.Value ? null : val;
        }
        return row;
    }

    public bool HasSideEffect(string sql, params TSqlTokenType[] allowCmds)
    {
        var finalCmd = SideEffectCmd.Except(allowCmds).ToArray();
        TSql110Parser parser = new(true);
        var fragments = parser.Parse(new StringReader(sql), out var errors);
        return fragments.ScriptTokenStream.Any(x => finalCmd.Contains(x.TokenType));
    }

    public async Task<string> GetConnStrFromKey(string connKey, string tenantCode = null, string env = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connKey);
        tenantCode = TenantCode ?? tenantCode;
        env = Env ?? env;
        var key = $"{tenantCode}_{connKey}_{env}";
        var conStr = await cache.GetStringAsync(key);
        if (conStr != null) return conStr;
        var query = $"select * from [Tenant] where TenantCode = '{tenantCode}' and ConnKey = '{connKey}' and Env = '{env}'";
        var tenantEnv = await ReadDsAs<Tenant>(query, cfg.GetConnectionString(Utils.ConnKey))
            ?? throw new ApiException($"Tenant environment NOT found {key}");
        await cache.SetStringAsync(key, tenantEnv.ConnStr, Utils.CacheTTL);
        return tenantEnv.ConnStr;
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

    public async Task<int> RunSqlCmd(string connStr, string cmdText)
    {
        if (cmdText.IsNullOrWhiteSpace()) return 0;
        if (connStr.IsNullOrWhiteSpace()) throw new ApiException("ConnStr is null")
        {
            StatusCode = HttpStatusCode.InternalServerError
        };
        SqlConnection connection = new(connStr);
        await connection.OpenAsync();
        var transaction = connection.BeginTransaction();
        var cmd = new SqlCommand
        {
            Transaction = transaction,
            Connection = connection,
            CommandText = cmdText
        };
        var anyComment = HasSqlComment(cmd.CommandText);
        if (anyComment) throw new ApiException("Comment is NOT allowed");
        try
        {
            var affected = await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return affected;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            var message = "Error occurs";
#if DEBUG
            message = $"Error occurs at {connStr} {cmdText}";
#endif
            throw new ApiException(message, e)
            {
                StatusCode = HttpStatusCode.InternalServerError
            };
        }
        finally
        {
            await transaction.DisposeAsync();
            await cmd.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    public string GetCreateOrUpdateCmd(PatchVM vm)
    {
        if (vm == null || vm.Table.IsNullOrWhiteSpace() || vm.Changes.Nothing())
        {
            throw new ApiException("Table name and change details can NOT be empty") { StatusCode = HttpStatusCode.BadRequest };
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
            return !UserServiceHelpers.SystemFields.Contains(x.Field);
        }).ToList();
        var idField = vm.Id;
        var valueFields = vm.Changes.Where(x => !UserServiceHelpers.SystemFields.Contains(x.Field.ToLower())).ToArray();
        var now = DateTimeOffset.Now.ToString(DateTimeExt.DateFormat);
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

    public bool HasSqlComment(string sql)
    {
        TSql110Parser parser = new(true);
        var fragments = parser.Parse(new StringReader(sql), out var errors);

        return fragments.ScriptTokenStream
            .Any(x => x.TokenType == TSqlTokenType.MultilineComment || x.TokenType == TSqlTokenType.SingleLineComment);
    }
}
