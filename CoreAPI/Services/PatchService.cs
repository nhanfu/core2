using Core.Exceptions;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using CoreAPI.Services.Interfaces;
using CoreAPI.Services.Sql;
using CoreAPI.BgService;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace CoreAPI.Services;

public class PatchService : IPatchService
{
    private readonly ISqlProvider _sql;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PatchService> _logger;

    // User context - these should be set from the current request
    public string UserId { get; set; }
    public string TenantCode { get; set; }
    public string Env { get; set; }
    public List<string> RoleIds { get; set; } = new();
    public List<string> CenterIds { get; set; } = new();

    public PatchService(
        ISqlProvider sql,
        IDistributedCache cache,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<PatchService> logger)
    {
        _sql = sql;
        _cache = cache;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _sql.TenantCode = TenantCode;
        _sql.Env = Env;
    }

    public void SetUserContext(string userId, string tenantCode, string env, List<string> roleIds)
    {
        UserId = userId;
        TenantCode = tenantCode;
        Env = env;
        RoleIds = roleIds;

        _sql.TenantCode = tenantCode;
        _sql.Env = env;
        _sql.UserId = userId;
        _sql.SystemFields = new List<string> { "id", "insertedby", "inserteddate", "updatedby", "updateddate" };
        if (tenantCode?.ToLower() != "system") _sql.SystemFields.Add("tenantcode");
    }

    public async Task<int> SavePatch(PatchVM vm)
    {
        vm.CachedDataConn ??= _sql.GetConnStrFromKey(vm.DataConn, vm.TenantCode, vm.Env);
        vm.CachedMetaConn ??= _sql.GetConnStrFromKey(vm.MetaConn, vm.TenantCode, vm.Env);
        var canWrite = await HasWritePermission(vm);
        if (!canWrite) throw new ApiException($"Unauthorized to write on \"{vm.Table}\"")
        {
            StatusCode = Core.Enums.HttpStatusCode.Unauthorized
        };
        var cmd = _sql.GetCreateOrUpdateCmd(vm);
        if (string.IsNullOrWhiteSpace(cmd)) return 0;
        var result = await _sql.RunSqlCmd(vm.CachedDataConn, cmd);
        return result;
    }

    public async Task<int> UpdatePatch(PatchVM vm)
    {
        vm.CachedDataConn ??= _sql.GetConnStrFromKey(vm.DataConn, vm.TenantCode, vm.Env);
        vm.CachedMetaConn ??= _sql.GetConnStrFromKey(vm.MetaConn, vm.TenantCode, vm.Env);
        var canWrite = await HasWritePermission(vm);
        if (!canWrite) throw new ApiException($"Unauthorized to write on \"{vm.Table}\"")
        {
            StatusCode = Core.Enums.HttpStatusCode.Unauthorized
        };
        var cmd = _sql.GetUpdateCmd(vm);
        if (string.IsNullOrWhiteSpace(cmd)) return 0;
        var result = await _sql.RunSqlCmd(vm.CachedDataConn, cmd);
        return result;
    }

    public async Task<SqlResult> SavePatch2(PatchVM vm)
    {
        // Simplified implementation - the full one is complex
        var id = vm.Changes.FirstOrDefault(x => x.Field == "Id")?.Value;
        if (string.IsNullOrEmpty(id))
        {
            return new SqlResult { status = 400, message = "Id is required" };
        }

        vm.CachedDataConn ??= _sql.GetConnStrFromKey(vm.DataConn, vm.TenantCode, vm.Env);

        if (id.StartsWith("-"))
        {
            // Insert new
            var (dup, mess, currentEntity) = await CheckDuplicate(vm);
            if (dup)
            {
                return new SqlResult
                {
                    updatedItem = null,
                    status = 409,
                    message = FormatEntityMessage(mess, currentEntity)
                };
            }
            id = id.Substring(1);
            AddDefaultFields(vm.Changes, new List<PatchDetail>
            {
                new PatchDetail { Field = "InsertedDate", Value = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") },
                new PatchDetail { Field = "InsertedBy", Value = UserId },
                new PatchDetail { Field = "UpdatedDate", Value = null },
                new PatchDetail { Field = "UpdatedBy", Value = null },
                new PatchDetail { Field = "Active", Value = "1" }
            });

            var cells = vm.Changes.Select(x => x.Field).ToList();
            var update = vm.Changes.Select(x => $"@{id + x.Field.ToLower()}");
            var cmd = $"INSERT into \"{vm.Table}\"(\"{cells.Combine("\",\"")}\") values({update.Combine()})";

            var result = await _sql.RunSqlCmd(vm.CachedDataConn, cmd);
            if (result > 0)
            {
                var entity = await _sql.ReadDataSet($"SELECT * FROM \"{vm.Table}\" WHERE \"Id\" = '{id}'");
                return new SqlResult
                {
                    updatedItem = entity[0],
                    status = 200,
                    message = "create successful"
                };
            }
        }
        else
        {
            // Update existing
            var (dup, mess, currentEntity) = await CheckDuplicate(vm, true);
            if (dup)
            {
                var entity = await _sql.ReadDataSet($"SELECT * FROM \"{vm.Table}\" WHERE \"Id\" = '{id}'");
                return new SqlResult
                {
                    updatedItem = entity[0],
                    status = 409,
                    message = FormatEntityMessage(mess, currentEntity)
                };
            }

            AddDefaultFields(vm.Changes, new List<PatchDetail>
            {
                new PatchDetail { Field = "UpdatedDate", Value = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") },
                new PatchDetail { Field = "UpdatedBy", Value = UserId }
            });

            var updates = vm.Changes.Where(x => x.Field != "Id").ToList();
            var update = updates.Select(x => $"\"{x.Field}\" = @{id + x.Field.ToLower()}");
            var cmd = $"UPDATE \"{vm.Table}\" SET {update.Combine()} WHERE \"Id\" = '{id}'";

            var result = await _sql.RunSqlCmd(vm.CachedDataConn, cmd);
            if (result > 0)
            {
                var entity = await _sql.ReadDataSet($"SELECT * FROM \"{vm.Table}\" WHERE \"Id\" = '{id}'");
                return new SqlResult
                {
                    updatedItem = entity[0],
                    status = 200,
                    message = "update successful"
                };
            }
        }

        return new SqlResult { status = 500, message = "Operation failed" };
    }

    public async Task<SqlResult> SavePatchs2(List<PatchVM> vms)
    {
        var results = new List<SqlResult>();
        foreach (var vm in vms)
        {
            var result = await SavePatch2(vm);
            results.Add(result);
            if (result.status != 200) break;
        }

        var successCount = results.Count(r => r.status == 200);
        return new SqlResult
        {
            status = successCount == vms.Count ? 200 : 207,
            message = $"{successCount}/{vms.Count} operations successful"
        };
    }

    public async Task<int> SavePatches(PatchVM[] patches)
    {
        if (patches == null || patches.Length == 0)
            throw new ArgumentException("patches is null or empty");

        patches = patches.Where(x => x.Id != null).ToArray();
        if (patches.Length == 0) return 0;

        patches[0].CachedDataConn ??= _sql.GetConnStrFromKey(patches[0].DataConn);
        patches[0].CachedMetaConn ??= _sql.GetConnStrFromKey(patches[0].MetaConn);

        var tables = patches.Select(x => x.Table);
        string rightQuery = $"select * from \"FeaturePolicy\" " +
            $"where \"Active\" = true and (\"CanWrite\" = true or \"CanWriteAll\" = true) and \"EntityName\" in ({tables.CombineStrings()}) and \"RoleId\" in ({RoleIds.CombineStrings()})";

        var permissions = await _sql.ReadDsAsArr<Core.Models.FeaturePolicy>(rightQuery, patches[0].CachedMetaConn);
        permissions = permissions.DistinctBy(x => x.TableName).ToArray();

        var lackPerTables = patches.Select(x => x.Table).Except(permissions.Select(x => x.TableName)).ToArray();
        if (lackPerTables.Length > 0)
        {
            throw new ApiException($"All table must have write permission {lackPerTables.CombineStrings()}")
            {
                StatusCode = Core.Enums.HttpStatusCode.Unauthorized
            };
        }

        var sql = patches.Select(_sql.GetCreateOrUpdateCmd).Where(x => x != null).Combine(";\n");
        var result = await _sql.RunSqlCmd(patches[0].CachedDataConn, sql);
        return result;
    }

    public async Task<bool> HardDelete(PatchVM vm)
    {
        if (vm.Delete == null || vm.Delete.Count == 0) return true;

        if (string.IsNullOrWhiteSpace(vm.ComId))
        {
            var sql = vm.Delete.Select(x => $"delete from \"{x.Table}\" where \"Id\" in ({x.Ids.CombineStrings()})");
            try
            {
                await _sql.RunSqlCmd(null, sql.Combine(";"));
                return true;
            }
            catch
            {
                return false;
            }
        }
        else
        {
            var query = $"select * from \"Component\" where \"Id\" = '{vm.ComId}' limit 1";
            var com = await _sql.ReadDsAs<Core.Models.Component>(query);
            var data = JsonConvert.DeserializeObject<SqlQuery>(com.Query);

            Dictionary<string, object> dictionary = new Dictionary<string, object>
            {
                { "EntityIds", vm.Delete.SelectMany(x => x.Ids).CombineStrings() },
                { "NewId", vm.NewId }
            };

            if (!string.IsNullOrWhiteSpace(data?.update))
            {
                var qr = FormatEntityMessage(data.update, dictionary);
                var deletequery = qr + ";" + vm.Delete.Select(x => $"delete from \"{x.Table}\" where \"Id\" in ({x.Ids.CombineStrings()})").Combine(";");
                try
                {
                    await _sql.RunSqlCmd(null, deletequery);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                var sql = vm.Delete.Select(x => $"delete from \"{x.Table}\" where \"Id\" in ({x.Ids.CombineStrings()})");
                try
                {
                    await _sql.RunSqlCmd(null, sql.Combine(";"));
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    public async Task<string[]> DeactivateAsync(SqlViewModel vm)
    {
        vm.CachedDataConn ??= _sql.GetConnStrFromKey(vm.DataConn ?? "default");
        var allRights = await GetEntityPerm(vm.Table, null, vm.CachedDataConn);
        var canDeactivateAll = allRights.Any(x => x.CanDeactivateAll);
        var canDeactivateSelf = allRights.Any(x => x.CanDeactivate);

        var query = $"select * from \"{vm.Table}\" where \"Id\" in ({vm.Id.CombineStrings()})";
        var ds = await _sql.ReadDataSet(query, vm.CachedDataConn);
        var rows = ds.Length > 0 ? ds[0] : null;

        if (rows == null) return Array.Empty<string>();

        var unauthorized = new List<string>();
        foreach (var row in rows)
        {
            var isOwner = IsOwner(row, UserId, RoleIds);
            if (!canDeactivateAll && !(canDeactivateSelf && isOwner))
            {
                unauthorized.Add(row.GetValueOrDefault("Id")?.ToString() ?? "");
            }
        }

        if (unauthorized.Count > 0)
        {
            return unauthorized.ToArray();
        }

        var activeField = rows[0].ContainsKey("Active") ? "Active" : "IsActive";
        var updateQuery = $"UPDATE \"{vm.Table}\" SET \"{activeField}\" = false WHERE \"Id\" in ({vm.Id.CombineStrings()})";
        await _sql.RunSqlCmd(vm.CachedDataConn, updateQuery);

        return Array.Empty<string>();
    }

    private async Task<bool> HasWritePermission(PatchVM vm)
    {
        if (vm.ByPassPerm) return true;

        bool writePerm = false;
        var allRights = vm.ByPassPerm ? Array.Empty<Core.Models.FeaturePolicy>() : await GetEntityPerm(vm.Table, recordId: null, vm.CachedMetaConn);
        var idField = vm.Changes.FirstOrDefault(x => x.Field == "Id");
        var oldId = idField?.OldVal;

        if (oldId == null)
        {
            writePerm = allRights.Any(x => x.CanWriteAll);
        }
        else
        {
            var origin = $"select t.* from \"{vm.Table}\" as t where t.\"Id\" = '{oldId}'";
            var ds = await _sql.ReadDataSet(origin, vm.CachedDataConn);
            var originRow = ds.Length > 0 && ds[0].Length > 0 ? ds[0][0] : null;
            var isOwner = IsOwner(originRow, UserId, RoleIds);
            writePerm = isOwner || allRights.Any(x => x.CanWriteAll);
        }
        return writePerm;
    }

    private async Task<Core.Models.FeaturePolicy[]> GetEntityPerm(string entityName, string recordId, string connStr)
    {
        if (string.IsNullOrWhiteSpace(entityName)) return Array.Empty<Core.Models.FeaturePolicy>();
        if (RoleIds.Count == 0) return Array.Empty<Core.Models.FeaturePolicy>();

        var key = entityName + "_AllRights";
        var permissionByComCache = await GetStringAsync(key);

        Core.Models.FeaturePolicy[] permissions;
        if (!string.IsNullOrWhiteSpace(permissionByComCache))
        {
            permissions = JsonConvert.DeserializeObject<Core.Models.FeaturePolicy[]>(permissionByComCache);
        }
        else
        {
            var q = $"select * from \"FeaturePolicy\" " +
                $"where \"Active\" = true and \"EntityName\" = '{entityName}' " +
                $"and (\"RecordId\" = '{recordId}' or '{recordId}' = '') and \"RoleId\" in ({RoleIds.CombineStrings()})";

            permissions = await _sql.ReadDsAsArr<Core.Models.FeaturePolicy>(q, connStr);
            await SetStringAsync(key, JsonConvert.SerializeObject(permissions), TimeSpan.FromMinutes(5));
        }

        return permissions;
    }

    private async Task<(bool, string, Dictionary<string, object>)> CheckDuplicate(PatchVM patch, bool Update = false)
    {
        // Simplified - full implementation would check unique constraints
        return (false, "", new Dictionary<string, object>());
    }

    private string FormatEntityMessage(string template, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(template)) return "";

        var result = template;
        foreach (var kvp in data)
        {
            result = result.Replace("{" + kvp.Key + "}", kvp.Value?.ToString() ?? "");
        }
        return result;
    }

    private bool IsOwner(Dictionary<string, object> row, string userId, List<string> roleIds)
    {
        if (row == null) return false;

        var insertedBy = row.GetValueOrDefault("InsertedBy")?.ToString();
        return insertedBy == userId || roleIds.Contains("ADMIN");
    }

    public void AddDefaultFields(List<PatchDetail> changes, List<PatchDetail> defaultFields)
    {
        foreach (var field in defaultFields)
        {
            var existingField = changes.FirstOrDefault(change => change.Field == field.Field);
            if (existingField != null)
            {
                existingField.Value = field.Value;
            }
            else
            {
                changes.Add(field);
            }
        }
    }

    public async Task<Dictionary<string, object>[][]> GetTableColumns(string tableName)
    {
        var query = $@"
        SELECT c.COLUMN_NAME
        FROM INFORMATION_SCHEMA.COLUMNS c
        JOIN sys.columns sc ON c.COLUMN_NAME = sc.name
        AND OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME) = sc.object_id
        WHERE c.TABLE_NAME = '{tableName}'
        AND sc.is_computed = 0";

        return await _sql.ReadDataSet(query);
    }

    // Cache helpers
    private async Task<string> GetStringAsync(string key)
    {
        return await _cache.GetStringAsync(key);
    }

    private async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        if (expiry.HasValue)
            await _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry });
        else
            await _cache.SetStringAsync(key, value);
    }
}
