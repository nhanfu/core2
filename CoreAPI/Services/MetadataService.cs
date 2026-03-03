using Core.Exceptions;
using Core.Models;
using CoreAPI.BgService;
using CoreAPI.Services.Sql;
using Newtonsoft.Json;

namespace CoreAPI.Services;

public class MetadataService
{
    private readonly ISqlProvider _sql;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetadataService> _logger;

    // User context
    public string TenantCode { get; set; }
    public List<string> RoleIds { get; set; } = new();

    public MetadataService(
        ISqlProvider sql,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<MetadataService> logger)
    {
        _sql = sql;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void SetUserContext(string tenantCode, List<string> roleIds)
    {
        TenantCode = tenantCode;
        RoleIds = roleIds;

        _sql.TenantCode = tenantCode;
    }

    public async Task<Dictionary<string, object>[]> GetMenu()
    {
        var roleIdsStr = RoleIds.Count > 0 ? string.Join(",", RoleIds.Select(x => $"'{x}'")) : "";
        var query = $@"select * from [Feature] f where IsMenu = 1 and (exists (select Id from FeaturePolicy where FeatureId = f.Id and RoleId in ({roleIdsStr}) and CanRead = 1) or 'ADMIN' in ({roleIdsStr}))";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(_serviceProvider, _configuration, "logistics"));
        return ds[0];
    }

    public Feature GetFeature(string name)
    {
        var feature = GetFeatureFromJson(name, TenantCode) ?? throw new ApiException("Feature not found")
        {
            StatusCode = Core.Enums.HttpStatusCode.NotFound
        };
        return feature;
    }

    public async Task SaveFeatureToJson(Feature feature, string t)
    {
        _logger.LogInformation("Begin save feature");
        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "upload", t ?? TenantCode, "features");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string filePath = Path.Combine(directoryPath, feature.Name + ".json");
        string json = JsonConvert.SerializeObject(feature, Formatting.Indented, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        });

        await File.WriteAllTextAsync(filePath, json);
    }

    public static Feature GetFeatureFromJson(string featureName, string t)
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "upload", t ?? "system", "features", featureName + ".json");

        if (!File.Exists(filePath))
        {
            return null;
        }

        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<Feature>(json);
    }
}
