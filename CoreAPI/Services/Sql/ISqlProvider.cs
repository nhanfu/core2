using Core.ViewModels;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace CoreAPI.Services.Sql
{
    public interface ISqlProvider
    {
        string Env { get; set; }
        string TenantCode { get; set; }
        string UserId { get; set; }
        public List<string> SystemFields { get; set; }

        Task<string> GetConnStrFromKey(string connKey, string tenantCode = null, string env = null);
        string GetCreateOrUpdateCmd(PatchVM vm);
        string GetUpdateCmd(PatchVM vm);
        bool HasSideEffect(string sql, params TSqlTokenType[] allowCmds);
        Task<Dictionary<string, object>[][]> ReadDataSet(string query, string connInfo = null, bool shouldMapToConnStr = false, List<WhereParamVM> paramVMs = null);
        Task<T> ReadDsAs<T>(string query, string connInfo = null) where T : class;
        Task<T[]> ReadDsAsArr<T>(string query, string connInfo = null) where T : class;
        public Task<int> RunSqlCmd(string connStr, string cmdText);
        public Task<int> RunSqlCmd(string connStr, string cmdText, Dictionary<string, object> ps);
    }
}