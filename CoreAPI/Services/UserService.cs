using Core.Exceptions;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using CoreAPI.BgService;
using CoreAPI.Services;
using CoreAPI.Services.Sql;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Buffers;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using HttpStatusCode = Core.Enums.HttpStatusCode;

namespace Core.Services;

public class UserService
{
    public readonly IHttpContextAccessor _ctx;
    private readonly HttpRequest _request;
    private readonly IConfiguration _cfg;
    private readonly IDistributedCache _cache;
    private readonly IWebHostEnvironment _host;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider iServiceProvider;
    private readonly SendMailService _sendMailService;
    private readonly IConfiguration _configuration;
    public readonly ISqlProvider _sql;
    public readonly ILogger<UserService> _logger;
    public string GroupId { get; set; }
    public string DepartmentId { get; set; }
    public string UserId { get; set; }
    public string FullName { get; set; }
    public string UserName { get; set; }
    public string Avatar { get; set; }
    public string CLogo { get; set; }
    public string CCompanyName { get; set; }
    public string CAddress { get; set; }
    public string CPhoneNumber { get; set; }
    public string CEmail { get; set; }
    public string ConnKey { get; set; }
    public string BranchId { get; set; }
    public List<string> CenterIds { get; set; }
    public string VendorId { get; set; }
    public string Env { get; set; }
    public string TenantCode { get; set; }
    public List<string> RoleIds { get; set; }
    public List<string> RoleNames { get; set; }
    public UserService(IHttpContextAccessor ctx, IConfiguration conf, IDistributedCache cache, IWebHostEnvironment host,
        IHttpClientFactory httpClientFactory, ISqlProvider sql, IConfiguration configuration,
        SendMailService sendMailService, IServiceProvider serviceProvider, ILogger<UserService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cfg = conf ?? throw new ArgumentNullException(nameof(conf));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache)); ;
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _request = _ctx.HttpContext.Request;
        _sendMailService = sendMailService;
        iServiceProvider = serviceProvider;
        _logger = logger;
        ExtractMeta();
        _sql = sql;
        SetMetaToSqlProvider(_sql);
    }

    public void SetMetaToSqlProvider(ISqlProvider _sql)
    {
        _sql.TenantCode = TenantCode;
        _sql.Env = Env;
        _sql.UserId = UserId;
        _sql.UserId = UserId;
        _sql.SystemFields = new List<string>
        {
            UserServiceHelpers.IdField, nameof(User.InsertedBy), nameof(User.InsertedDate), nameof(User.UpdatedBy), nameof(User.UpdatedDate)
        }.Select(x => x.ToLower()).ToList();
        if (TenantCode?.ToLower() != "system") _sql.SystemFields.Add("TenantCode");
    }

    private void ExtractMeta()
    {
        var claims = _ctx.HttpContext?.User?.Claims;
        if (claims is null) return;
        BranchId = claims.FirstOrDefault(x => x.Type == UserServiceHelpers.BranchIdClaim)?.Value;
        UserId = claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        FullName = claims.FirstOrDefault(x => x.Type == "FullName")?.Value;
        Avatar = claims.FirstOrDefault(x => x.Type == "Avatar")?.Value;
        GroupId = claims.FirstOrDefault(x => x.Type == "TeamId")?.Value;
        DepartmentId = claims.FirstOrDefault(x => x.Type == "DepartmentId")?.Value;
        UserName = claims.FirstOrDefault(x => x.Type == "UserName")?.Value;
        CenterIds = claims.Where(x => x.Type == nameof(CenterIds)).Select(x => x.Value).Where(x => x != null).ToList();
        RoleIds = claims.Where(x => x.Type == "RoleIds").Select(x => x.Value).Where(x => x != null).ToList();
        RoleNames = claims.Where(x => x.Type == UserServiceHelpers.RoleNameClaim).Select(x => x.Value).Where(x => x != null).ToList();
        VendorId = claims.FirstOrDefault(x => x.Type == "PartnerId")?.Value;
        TenantCode = claims.FirstOrDefault(x => x.Type == UserServiceHelpers.TenantClaim)?.Value.ToUpper();
        CLogo = claims.FirstOrDefault(x => x.Type == "CLogo")?.Value;
        CCompanyName = claims.FirstOrDefault(x => x.Type == "CCompanyName")?.Value;
        CAddress = claims.FirstOrDefault(x => x.Type == "CAddress")?.Value;
        CPhoneNumber = claims.FirstOrDefault(x => x.Type == "CPhoneNumber")?.Value;
        CEmail = claims.FirstOrDefault(x => x.Type == "CEmail")?.Value;
    }

    public async Task<Dictionary<string, object>[]> GetDictionary()
    {
        var query = @$"select * from ""Dictionary""";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return ds[0];
    }

    public async Task<Dictionary<string, object>[]> MyNotification()
    {
        var query = @$"select * from ""TaskNotification"" where ""AssignedId"" = '{UserId}' order by ""InsertedDate"" desc";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return ds[0];
    }

    public async Task<Dictionary<string, object>[]> WebConfig()
    {
        var query = @$"select * from ""WebConfig""";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return ds[0];
    }

    public async Task<bool> PostUserSetting(UserSetting userSetting)
    {
        var query = @$"select * from ""UserSetting"" where ""UserId"" = '{UserId}' and ""ComponentId"" = '{userSetting.ComponentId}' and ""FeatureId"" = '{userSetting.FeatureId}'";
        var setting = await _sql.ReadDsAs<UserSetting>(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        if (setting != null)
        {
            setting.Value = userSetting.Value;
            setting.UpdatedBy = UserId;
            setting.UpdatedDate = DateTime.Now;
            var patch = setting.MapToPatch();
            await UpdatePatch(patch);
        }
        else
        {
            setting = new UserSetting();
            setting.Id = Uuid7.Guid().ToString();
            setting.ComponentId = userSetting.ComponentId;
            setting.FeatureId = userSetting.FeatureId;
            setting.UserId = UserId;
            setting.Active = true;
            setting.Value = userSetting.Value;
            setting.InsertedBy = UserId;
            setting.InsertedDate = DateTime.Now;
            var patch = setting.MapToPatch();
            await SavePatch(patch);
        }
        return true;
    }

    public async Task<int> SavePatch(PatchVM vm)
    {
        vm.CachedDataConn ??= _sql.GetConnStrFromKey(vm.DataConn, vm.TenantCode, vm.Env);
        vm.CachedMetaConn ??= _sql.GetConnStrFromKey(vm.MetaConn, vm.TenantCode, vm.Env);
        var canWrite = await HasWritePermission(vm);
        if (!canWrite) throw new ApiException($"Unauthorized to write on \"{vm.Table}\"")
        {
            StatusCode = HttpStatusCode.Unauthorized
        };
        var cmd = _sql.GetCreateOrUpdateCmd(vm);
        if (cmd.IsNullOrWhiteSpace()) return 0;
        var result = await _sql.RunSqlCmd(vm.CachedDataConn, cmd);
        if (result == 0) return result;
        return result;
    }

    public async Task<int> UpdatePatch(PatchVM vm)
    {
        vm.CachedDataConn ??= _sql.GetConnStrFromKey(vm.DataConn, vm.TenantCode, vm.Env);
        vm.CachedMetaConn ??= _sql.GetConnStrFromKey(vm.MetaConn, vm.TenantCode, vm.Env);
        var canWrite = await HasWritePermission(vm);
        if (!canWrite) throw new ApiException($"Unauthorized to write on \"{vm.Table}\"")
        {
            StatusCode = HttpStatusCode.Unauthorized
        };
        var cmd = _sql.GetUpdateCmd(vm);
        if (cmd.IsNullOrWhiteSpace()) return 0;
        var result = await _sql.RunSqlCmd(vm.CachedDataConn, cmd);
        if (result == 0) return result;
        return result;
    }

    private async Task<bool> HasWritePermission(PatchVM vm)
    {
        if (vm.ByPassPerm) return true;
        bool writePerm = false;
        var allRights = vm.ByPassPerm ? [] : await GetEntityPerm(vm.Table, recordId: null, vm.CachedMetaConn);
        var idField = vm.Changes.FirstOrDefault(x => x.Field == Utils.IdField);
        var oldId = idField?.OldVal;
        if (oldId is null)
        {
            writePerm = allRights.Any(x => x.CanWriteAll);
        }
        else
        {
            var origin = @$"select t.* from ""{vm.Table}"" as t where t.""Id"" = '{oldId}'";
            var ds = await _sql.ReadDataSet(origin, vm.CachedDataConn);
            var originRow = ds.Length > 0 && ds[0].Length > 0 ? ds[0][0] : null;
            var isOwner = Utils.IsOwner(originRow, UserId, RoleIds);
            writePerm = isOwner || allRights.Any(x => x.CanWriteAll);
        }
        return writePerm;
    }

    public async Task<CheckDeleteResult> CheckDelete(CheckDeleteItem item)
    {
        var query = @$"select * from ""Component"" where ""Id"" = '{item.ComId}' limit 1";
        var com = await _sql.ReadDsAs<Component>(query);
        var data = JsonConvert.DeserializeObject<SqlQuery>(com.Query);
        Dictionary<string, object> dictionary = item.Params.IsNullOrWhiteSpace() ? new Dictionary<string, object>() : JsonConvert.DeserializeObject<Dictionary<string, object>>(item.Params);
        dictionary["EntityIds"] = item.EntityIds.CombineStrings();
        var qr = Utils.FormatEntity(data.delete, dictionary);
        var exists = await _sql.ReadDataSet(qr);
        return new CheckDeleteResult()
        {
            status = (exists[0] != null && exists[0].Length > 0) ? 500 : 200,
            message = dictionary["Message"] != null ? dictionary["Message"]?.ToString() : null
        };
    }

    private async Task<Component> FindComponentById(SqlViewModel vm, IEnumerable<Component> components, Feature feature)
    {
        Component com = null;
        foreach (var c in components)
        {
            if (c.Id == vm.ComId)
            {
                // If component is not private or user is an admin, it's a match.
                if (!c.IsPrivate || RoleIds.Contains("ADMIN"))
                {
                    com = c;
                    break;
                }

                var permissions = feature.FeaturePolicies.Where(x => RoleIds.Contains(x.RoleId) && x.CanRead).ToArray();

                if (permissions.Length > 0)
                {
                    com = c;
                }

                break;
            }
            else if (c.Components != null && c.Components.Count > 0)
            {
                com = await FindComponentById(vm, c.Components, feature);
                if (com != null) break;
            }
        }

        return com;
    }

    private async Task<FeaturePolicy[]> GetEntityPerm(string entityName, string recordId, string connStr,
        Expression<Func<FeaturePolicy, bool>> pre = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        if (RoleIds.Nothing()) return [];
        var permissionName = ((pre?.Body as MemberExpression)?.Member as PropertyInfo)?.Name;
        var key = entityName + "_" + (permissionName ?? "AllRights");
        var permissionByComCache = await GetStringAsync(key);
        FeaturePolicy[] permissions;
        if (!permissionByComCache.IsNullOrWhiteSpace())
        {
            permissions = JsonConvert.DeserializeObject<FeaturePolicy[]>(permissionByComCache);
        }
        else
        {
            var q = @$"select * from ""FeaturePolicy""
            where ""Active"" = true and ""EntityName"" = '{entityName}'
            and (""RecordId"" = '{recordId}' or '{recordId}' = '') and ""RoleId"" in ({RoleIds.CombineStrings()})";
            if (pre != null) q += $" and {permissionName} = true";
            permissions = await _sql.ReadDsAsArr<FeaturePolicy>(q, connStr);
            await SetStringAsync(key, JsonConvert.SerializeObject(permissions), Utils.CacheTTL);
        }

        return permissions;
    }

    public async Task<Dictionary<string, object>> GetMessageActive()
    {
        var users = await _sql.ReadDataSet($"SELECT COUNT(\"Id\") as Total FROM \"ConversationRead\" WHERE \"UserId\" = '{UserId}' and \"Read\" = false", BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return users[0][0];
    }

    public ValueTask<bool> DeleteFile(string path)
    {
        var absolutePath = Path.Combine(_host.WebRootPath, path);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
        return new ValueTask<bool>(true);
    }
    public Task<string> GetStringAsync(string key) => _cache.GetStringAsync(key?.ToUpper());
    public Task SetStringAsync(string key, string val, DistributedCacheEntryOptions options) => _cache.SetStringAsync(key?.ToUpper(), val, options);
}