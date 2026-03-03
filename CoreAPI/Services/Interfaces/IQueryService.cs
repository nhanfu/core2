using Core.ViewModels;
using CoreAPI.Services.Sql;

namespace CoreAPI.Services.Interfaces
{
    public interface IQueryService
    {
        Task<SqlResult> Go(SqlViewModel sqlViewModel);
        Task<Dictionary<string, object>[][]> Gos(List<Gos> gos);
        Task<SqlResult> GoByName(SqlViewModel sqlViewModel);
        Task<SqlComResult> ComQuery(SqlViewModel vm);
        Task<Dictionary<string, object>[][]> Report(SqlViewModel vm);
        Task<Dictionary<string, object>[][]> Sql(SqlViewModel vm);
        Task<Dictionary<string, object>[][]> GetTableColumns(string tableName);
    }
}
