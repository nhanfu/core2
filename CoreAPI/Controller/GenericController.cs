using Core.Enums;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuppeteerSharp;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Dynamic.Core;
using System.Reflection;
using Core.Exceptions;
using Core.Models;
using Core.Services;
using FileIO = System.IO.File;
using Utils = Core.Extensions.Utils;

namespace Core.Controllers
{
    [Authorize]
    public class GenericController<T> : ControllerBase where T : class
    {
        protected const string IdField = "Id";
        protected const string InsertedByField = "InsertedBy";
        protected DbContext ctx;
        protected IServiceProvider _serviceProvider;
        protected IConfiguration _config;
        public IWebHostEnvironment _host;

        /// <summary>
        /// Current UserId
        /// </summary>
        protected string UserId { get; private set; }
        /// <summary>
        /// All roles of the current user including inherited roles
        /// </summary>
        public List<string> AllRoleIds { get; private set; }
        /// <summary>
        /// All roles assign (not including inherited roles)
        /// </summary>
        public List<string> RoleIds { get; private set; }
        /// <summary>
        /// The vendor of the current user
        /// </summary>
        public string VendorId { get; private set; }

        protected readonly IHttpContextAccessor _httpContext;
        protected readonly UserService _userSvc;
        protected readonly TaskService _taskService;
        protected readonly EntityService _entitySvc;
        public GenericController(DbContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor)
        {
            ctx = context;
            _httpContext = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _userSvc = _httpContext.HttpContext.RequestServices.GetService(typeof(UserService)) as UserService;
            _entitySvc = entityService;
            _taskService = _httpContext.HttpContext.RequestServices.GetService(typeof(TaskService)) as TaskService;
            _serviceProvider = _httpContext.HttpContext.RequestServices.GetService(typeof(IServiceProvider)) as IServiceProvider;
            _config = _httpContext.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            _host = _httpContext.HttpContext.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
            CalcUserInfo();
        }

        protected void CalcUserInfo()
        {
            UserId = _userSvc.UserId;
            AllRoleIds = _userSvc.AllRoleIds;
            RoleIds = _userSvc.RoleIds;
            VendorId = _userSvc.VendorId;
        }

        [HttpGet("api/[Controller]")]
        public virtual Task<OdataResult<T>> Get(ODataQueryOptions<T> options)
        {
            var query = GetQuery();
            return ApplyQuery(options, query);
        }

        protected virtual IQueryable<T> GetQuery()
        {
            return ctx.Set<T>().AsNoTracking();
        }

        [AllowAnonymous]
        [HttpGet("api/[Controller]/Public")]
        public virtual async Task<OdataResult<T>> GetPublic(ODataQueryOptions<T> options, string ids)
        {
            if (ids.HasAnyChar())
            {
                var query = GetByIds(ids);
                var data = await ctx.Set<T>().FromSqlRaw(query).AsNoTracking().ToListAsync();
                return new OdataResult<T>
                {
#if DEBUG
                    Query = query,
#endif
                    value = data,
                };
            }
            return await ApplyQuery(options, ctx.Set<T>().AsQueryable());
        }

        [HttpPost("api/[Controller]/ById")]
        public virtual async Task<OdataResult<T>> LoadById([FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config, [FromBody] string ids, [FromQuery] string FieldName, [FromQuery] string DatabaseName)
        {
            if (ids.IsNullOrWhiteSpace())
            {
                return new OdataResult<T>();
            }
            if (!FieldName.IsNullOrWhiteSpace())
            {
                var connectionStr = _config.GetConnectionString("Default");
                using var con = new SqlConnection(connectionStr);
                var sqlCmd = new SqlCommand(GetByIds(ids, FieldName, DatabaseName), con)
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
                return new OdataResult<T>
                {
                    value = tables[0]
                };
            }
            else
            {
                var query = GetByIds(ids);
                var data = await ctx.Set<T>().FromSqlRaw(query).AsNoTracking().ToListAsync();
                return new OdataResult<T>
                {
#if DEBUG
                    Query = query,
#endif
                    value = data,
                };
            }
        }

        private static string GetByIds(string ids, string FieldName = null, string DatabaseName = null)
        {
            var idSql = ids.Split(",").Select(x => $"'{x}'").Combine();
            var query = $"select {FieldName ?? "*"} from {(DatabaseName.IsNullOrWhiteSpace() ? "" : $"{DatabaseName}.dbo.")}[{typeof(T).Name}] where Id in ({idSql})";
            return query;
        }

        [HttpGet("api/[Controller]/Exists")]
        public virtual ActionResult<bool> Exists(ODataQueryOptions<T> options)
        {
            if (options is null)
            {
                return BadRequest("Query parameter is not valid");
            }
            if (options.SelectExpand is not null)
            {
                options.SetReadonlyPropValue(nameof(options.SelectExpand), null);
            }
            var query = GetQuery();
            var limited = options.ApplyTo(query);
            return limited.Any();
        }

        protected async Task<OdataResult<K>> ApplyQuery<K>(IQueryable<K> query)
        {
            var list = await query.ToListAsync();
            var res = new OdataResult<K>
            {
                odata = new Odata { count = list.Count },
                value = list,
            };
            return res;
        }

        protected async Task<OdataResult<K>> ApplyQuery<K>(ODataQueryOptions<K> options, IQueryable<K> query, bool noTracking = true, string sql = null) where K : class
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            query = query.AsNoTracking();
            var shouldCount = options.Count != null;
            var (skip, top) = (options.Skip, options.Top);
            if (options.Skip != null)
            {
                options.SetReadonlyPropValue(nameof(options.Skip), null);
            }
            if (options.Top != null)
            {
                options.SetReadonlyPropValue(nameof(options.Top), null);
            }
            var totalResult = options.ApplyTo(query);
            if (skip != null && skip.Value != 0)
            {
                options.SetReadonlyPropValue(nameof(options.Skip), skip);
            }
            if (top != null && top.Value != 0)
            {
                options.SetReadonlyPropValue(nameof(options.Top), top);
            }
            var limitResult = options.ApplyTo(query);
            OdataResult<K> result;
            if (options.SelectExpand is null)
            {
                var limitedQuery = limitResult as IQueryable<K>;
                result = new OdataResult<K>
                {
                    odata = new Odata { count = shouldCount ? (await (totalResult as IQueryable<K>).CountAsync()) : null },
                    Sql = sql,
                    value = options.Top == null && !shouldCount || top != null && top.Value > 0 ? await limitedQuery.ToListAsync() : null,
                };
                return result;
            }
            result = new OdataResult<K>
            {
                odata = new Odata { count = shouldCount ? totalResult.Count() : 0 },
                Sql = sql,
                value = options.Top == null && !shouldCount || top != null && top.Value > 0 ? await limitResult.ToDynamicArrayAsync() : null
            };
            return result;
        }

        [HttpPatch("api/[Controller]", Order = 1)]
        public virtual async Task<ActionResult<T>> PatchAsync([FromQuery] ODataQueryOptions<T> options, [FromBody] PatchVM patch, [FromQuery] bool disableTrigger = false)
        {
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            using SqlConnection connection = new(_config.GetConnectionString("Default"));
            connection.Open();
            SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                using SqlCommand command = new();
                command.Transaction = transaction;
                command.Connection = connection;
                var updates = patch.Changes.Where(x => x.Field != IdField).ToList();
                var update = updates.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
                if (disableTrigger)
                {
                    command.CommandText += $" DISABLE TRIGGER ALL ON [{typeof(T).Name}];";
                }
                else
                {
                    command.CommandText += $" ENABLE TRIGGER ALL ON [{typeof(T).Name}];";
                }
                command.CommandText += $" UPDATE [{typeof(T).Name}] SET {update.Combine()} WHERE Id = '{id}';";
                if (disableTrigger)
                {
                    command.CommandText += $" ENABLE TRIGGER ALL ON [{typeof(T).Name}];";
                }
                foreach (var item in updates)
                {
                    command.Parameters.AddWithValue($"@{item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                }
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                var entity = await ctx.Set<T>().FindAsync(id);
                if (!disableTrigger)
                {
                    await ctx.Entry(entity).ReloadAsync();
                }
                return entity;
            }
            catch
            {
                await transaction.RollbackAsync();
                var entity = await ctx.Set<T>().FindAsync(id);
                return StatusCode(409, entity);
            }
        }

        protected async Task<T> GetEntityByOdataOptions(ODataQueryOptions<T> options)
        {
            options.SetReadonlyPropValue(nameof(options.Top), null);
            var odataQuery = options.ApplyTo(ctx.Set<T>()) as IQueryable<T>;
            var entity = await odataQuery.FirstOrDefaultAsync();
            if (entity is null)
            {
                return Activator.CreateInstance(typeof(T)) as T;
            }
            return entity;
        }

        [HttpPost("api/[Controller]", Order = 1)]
        public virtual async Task<ActionResult<T>> CreateAsync([FromBody] T entity)
        {
            if (entity == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            SetAuditInfo(entity);
            ctx.Set<T>().Add(entity);
            await ctx.SaveChangesAsync();
            await ctx.Entry(entity).ReloadAsync();
            return entity;
        }

        [HttpPut("api/[Controller]", Order = 1)]
        public virtual async Task<ActionResult<T>> UpdateAsync([FromBody] T entity, string reasonOfChange = "")
        {
            if (entity == null || !ModelState.IsValid)
            {
                return base.BadRequest(ModelState);
            }
            SetAuditInfo(entity);
            ctx.Set<T>().Update(entity);
            await ctx.SaveChangesAsync();
            return entity;
        }

        protected void SetAuditInfo<K>(K entity) where K : class => _userSvc.SetAuditInfo(entity);

        [HttpPut("api/[Controller]/BulkUpdate")]
        public virtual async Task<List<T>> BulkUpdateAsync([FromBody] List<T> entities, string reasonOfChange)
        {
            foreach (var x in entities)
            {
                var updating = (int)x.GetPropValue(IdField) > 0;
                if (updating)
                {
                    await UpdateAsync(x, reasonOfChange);
                }
                else
                {
                    await CreateAsync(x);
                }
            }
            await ctx.SaveChangesAsync();
            return entities;
        }

        protected async Task<List<T>> AddOrUpdate(List<T> entities)
        {
            entities.ForEach(entity =>
            {
                var id = entity.GetPropValue(nameof(Feature.Id)) as int?;
                if (id != null && id <= 0)
                {
                    ctx.Set<T>().Add(entity);
                }
                else if (id != null)
                {
                    ctx.Set<T>().Update(entity);
                }
            });
            await ctx.SaveChangesAsync();
            return entities;
        }

        [HttpDelete("api/[Controller]/Delete", Order = 1)]
        public virtual async Task<ActionResult<bool>> DeactivateAsync([FromBody] List<string> ids)
        {
            var updateCommand = string.Format("Update [{0}] set Active = 0 where Id in ({1})", typeof(T).Name, string.Join(",", ids));
            await ctx.Database.ExecuteSqlRawAsync(updateCommand);
            return true;
        }

        protected string GetUploadExcelPath(string fileName, string webRootPath)
        {
            return Path.Combine(webRootPath, "excel", _userSvc.TenantCode, $"U{UserId}", fileName);
        }

        public static bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        protected static Dictionary<string, object> Read(IDataRecord reader)
        {
            var row = new Dictionary<string, object>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var val = reader[i];
                row[reader.GetName(i)] = val == DBNull.Value ? null : val;
            }
            return row;
        }

        [HttpPost("api/[Controller]/ViewSumary")]
        public virtual async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ViewSumary(
            [FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config, [FromBody] string sum,
            [FromQuery] string group, [FromQuery] string tablename, [FromQuery] string refname,
            [FromQuery] string formatsumary, [FromQuery] string orderby, [FromQuery] string sql, [FromQuery] string where, [FromQuery] string join)
        {
            var connectionStr = _config.GetConnectionString("Default");
            using var con = new SqlConnection(connectionStr);
            var reportQuery = string.Empty;
            group = group.Contains(".") ? $"{group}" : $"[{tablename}].{group}";
            if (sql.IsNullOrWhiteSpace())
            {
                reportQuery = $@"select {group} as '{group.Replace($"[{tablename}].", "")}',{formatsumary} as TotalRecord,{sum}
                                 from [{tablename}]
                                 {join}
                                 where [{tablename}].Active = 1 {(where.IsNullOrWhiteSpace() ? $"" : $"and {where}")}
                                 group by {group}
                                 order by {formatsumary} {orderby}";
            }
            else
            {
                reportQuery = $@"select {group}  as '{group.Replace($"[{tablename}].", "")}',{formatsumary} as TotalRecord,{sum}
                                 from ({sql})  as [{tablename}]
                                 {join}
                                 where [{tablename}].Active = 1 {(where.IsNullOrWhiteSpace() ? $"" : $"and {where}")}
                                 group by {group}
                                 order by {formatsumary} {orderby}";
            }
            if (!refname.IsNullOrEmpty())
            {
                reportQuery += $@" select *
                                 from [{refname}]
                                 where Id in (select {group}
                                              from [{tablename}]
                                              {join}
                                              where [{tablename}].Active = 1 {(where.IsNullOrWhiteSpace() ? $"" : $"and {where} ")}
                                              group by {group})";
            }
            var sqlCmd = new SqlCommand(reportQuery, con)
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
            return tables;
        }

        [HttpPost("api/[Controller]/SubTotal")]
        public virtual async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> SubTotal(
            [FromServices] IServiceProvider serviceProvider,
            [FromServices] IConfiguration config,
            [FromBody] string sum,
            [FromQuery] string group,
            [FromQuery] string tablename,
            [FromQuery] string refname,
            [FromQuery] string formatsumary,
            [FromQuery] string orderby,
            [FromQuery] string sql,
            [FromQuery] bool showNull,
            [FromQuery] string datetimeField,
            [FromQuery] string where,
            [FromQuery] string join)
        {
            var connectionStr = _config.GetConnectionString("Default");
            using var con = new SqlConnection(connectionStr);
            var reportQuery = string.Empty;
            var showNullString = showNull ? $"" : string.Empty;
            if (sql.IsNullOrWhiteSpace())
            {
                reportQuery = $@"select {sum}
                                 from [{tablename}] as {tablename}
                                 {join}
                                 where [{tablename}].Active = 1 {(where.IsNullOrWhiteSpace() ? $"" : $"and {where}")}";
            }
            else
            {
                reportQuery = $@"select {sum}
                                 from ({sql})  as [{tablename}]
                                 {join}
                                 where [{tablename}].Active = 1 {(where.IsNullOrWhiteSpace() ? $"" : $"and {where}")}";
            }
            var sqlCmd = new SqlCommand(reportQuery, con)
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
            return tables;
        }
    }
}
