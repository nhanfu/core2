using Core.ViewModels;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace CoreAPI.Services.Sql
{
    public interface ISqlProvider
    {
        string Env { get; set; }
        string TenantCode { get; set; }
        string UserId { get; set; }

        Task<string> GetConnStrFromKey(string connKey, string tenantCode = null, string env = null);
        string GetCreateOrUpdateCmd(PatchVM vm);
        bool HasSideEffect(string sql, params TSqlTokenType[] allowCmds);
        Task<Dictionary<string, object>[][]> ReadDataSet(string query, string connInfo, bool shouldMapToConnStr = false);
        Task<T> ReadDsAs<T>(string query, string connInfo) where T : class;
        Task<T[]> ReadDsAsArr<T>(string query, string connInfo) where T : class;
        public Task<int> RunSqlCmd(string connStr, string cmdText);
    }
}