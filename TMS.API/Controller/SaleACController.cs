using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class SaleACController : GenericController<SaleAC>
    {
        private readonly DBAccountantContext db;
        public SaleACController(DBAccountantContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
            db = context;
        }

        public override Task<OdataResult<SaleAC>> Get(ODataQueryOptions<SaleAC> options)
        {
            var query = db.Sale.AsQueryable();
            return ApplyQuery(options, query);
        }

        public override async Task<OdataResult<SaleAC>> LoadById([FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config, [FromBody] string ids, [FromQuery] string FieldName)
        {
            if (ids.IsNullOrWhiteSpace())
            {
                return new OdataResult<SaleAC>();
            }
            if (!FieldName.IsNullOrWhiteSpace())
            {
                var connectionStr = _config.GetConnectionString("DBAccountant");
                using var con = new SqlConnection(connectionStr);
                var sqlCmd = new SqlCommand(GetByIds(ids, FieldName), con)
                {
                    CommandType = CommandType.Text
                };
                con.Open();
                var tables = new List<List<Dictionary<string, object>>>();
                using (var reader = await sqlCmd.ExecuteReaderAsync())
                {
                    do
                    {
                        var table = new List<Dictionary<string, object>>();
                        while (await reader.ReadAsync())
                        {
                            table.Add(Read(reader));
                        }
                        tables.Add(table);
                    } while (reader.NextResult());
                }
                return new OdataResult<SaleAC>
                {
                    value = tables[0]
                };
            }
            else
            {
                var query = GetByIds(ids);
                var data = await db.Set<SaleAC>().FromSqlRaw(query).AsNoTracking().ToListAsync();
                return new OdataResult<SaleAC>
                {
                    Query = query,
                    value = data,
                };
            }
        }

        private static string GetByIds(string ids, string FieldName = null)
        {
            var query = $"select {FieldName ?? "*"} from [Sale] where Id in ({ids})";
            return query;
        }
    }
}