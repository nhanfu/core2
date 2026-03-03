using Core.Exceptions;
using Core.Models;
using CoreAPI.BgService;
using CoreAPI.Services.Interfaces;
using CoreAPI.Services.Sql;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CoreAPI.Services;

public class MetadataService : IMetadataService
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
        string basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "upload", t ?? "system", "features");
        string jsonPath = Path.Combine(basePath, featureName + ".json");
        string yamlPath = Path.Combine(basePath, featureName + ".yaml");

        Feature feature = null;

        // Try JSON first
        if (File.Exists(jsonPath))
        {
            string json = File.ReadAllText(jsonPath);
            feature = JsonConvert.DeserializeObject<Feature>(json);
        }
        // Then try YAML
        else if (File.Exists(yamlPath))
        {
            string yaml = File.ReadAllText(yamlPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            feature = deserializer.Deserialize<Feature>(yaml);
        }

        if (feature == null)
        {
            return null;
        }

        // Remove properties starting with _ (underscore)
        return RemoveUnderscoreProperties(feature);
    }

    private static Feature RemoveUnderscoreProperties(Feature feature)
    {
        // Use reflection to remove properties starting with _
        var json = JsonConvert.SerializeObject(feature);
        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        RemoveUnderscorePropertiesRecursive(dict);
        return JsonConvert.DeserializeObject<Feature>(JsonConvert.SerializeObject(dict));
    }

    private static void RemoveUnderscorePropertiesRecursive(Dictionary<string, object> dict)
    {
        var keysToRemove = dict.Keys.Where(k => k.StartsWith("_")).ToList();
        foreach (var key in keysToRemove)
        {
            dict.Remove(key);
        }

        foreach (var value in dict.Values)
        {
            if (value is Dictionary<string, object> nestedDict)
            {
                RemoveUnderscorePropertiesRecursive(nestedDict);
            }
            else if (value is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> listItemDict)
                    {
                        RemoveUnderscorePropertiesRecursive(listItemDict);
                    }
                }
            }
        }
    }
}
