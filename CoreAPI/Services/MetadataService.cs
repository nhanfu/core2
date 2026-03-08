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

    public Task<Dictionary<string, object>[]> GetMenu()
    {
        try
        {
            // Read menu from YAML/JSON files in tenant's features folder
            string basePath = GetFeatureFolderPath(TenantCode);

            _logger.LogInformation("GetMenu called - TenantCode: {TenantCode}, BasePath: {BasePath}",
                TenantCode, basePath);

            if (!Directory.Exists(basePath))
            {
                _logger.LogWarning("Directory does not exist: {Path}", basePath);
                return Task.FromResult(Array.Empty<Dictionary<string, object>>());
            }

            var menuItems = new List<Dictionary<string, object>>();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            // Get all yaml and json files
            var yamlFiles = Directory.GetFiles(basePath, "*.yaml");
            var jsonFiles = Directory.GetFiles(basePath, "*.json");

            var allFiles = yamlFiles.Concat(jsonFiles).ToList();

            foreach (var file in allFiles)
            {
                try
                {
                    Feature feature = null;

                    if (file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                    {
                        string yaml = File.ReadAllText(file);
                        feature = deserializer.Deserialize<Feature>(yaml);
                    }
                    else if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        string json = File.ReadAllText(file);
                        feature = JsonConvert.DeserializeObject<Feature>(json);
                    }

                    // Only include features with IsMenu = true and has permission
                    if (feature != null && feature.IsMenu && HasMenuPermission(feature))
                    {
                        menuItems.Add(ConvertFeatureToDictionary(feature));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading feature file: {File}", file);
                }
            }

            var result = menuItems.OrderBy(x => x.ContainsKey("Order") ? x["Order"] : 999).ToArray();
            _logger.LogInformation("Returning {Count} menu items", result.Length);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMenu");
            return Task.FromResult(Array.Empty<Dictionary<string, object>>());
        }
    }

    private bool HasMenuPermission(Feature feature)
    {
        // Admin has access to all menus
        if (RoleIds.Contains("ADMIN"))
        {
            return true;
        }

        // Check FeaturePolicies for role permissions
        if (feature.FeaturePolicies == null || feature.FeaturePolicies.Count == 0)
        {
            return false;
        }

        foreach (var policy in feature.FeaturePolicies)
        {
            if (RoleIds.Contains(policy.RoleId) && policy.CanRead == true)
            {
                return true;
            }
        }

        return false;
    }

    private Dictionary<string, object> ConvertFeatureToDictionary(Feature feature)
    {
        var dict = new Dictionary<string, object>
        {
            { "Id", feature.Id },
            { "Name", feature.Name },
            { "Label", feature.Label ?? feature.Name },
            { "Order", feature.Order ?? 999 },
            { "Icon", feature.Icon ?? "" },
            { "IsMenu", feature.IsMenu },
            { "ParentId", feature.ParentId ?? "" },
            { "ClassName", feature.ClassName ?? "" },
            { "Style", feature.Style ?? "" },
            { "Script", feature.Script ?? "" },
            { "Events", feature.Events ?? "" },
            { "EntityId", feature.EntityId ?? "" },
            { "Active", feature.Active ?? true },
            { "IsLock", feature.IsLock },
            { "IgnoreEncode", feature.IgnoreEncode }
        };

        return dict;
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
        string directoryPath = GetFeatureFolderPath(t ?? TenantCode, createIfMissing: true);

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
        string basePath = GetFeatureFolderPath(t);
        string jsonPath = Path.Combine(basePath, featureName + ".json");
        string yamlPath = Path.Combine(basePath, featureName + ".yaml");

        Feature feature = null;

        if (File.Exists(yamlPath))
        {
            string yaml = File.ReadAllText(yamlPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            feature = deserializer.Deserialize<Feature>(yaml);
        }
        else if (File.Exists(jsonPath))
        {
            string json = File.ReadAllText(jsonPath);
            feature = JsonConvert.DeserializeObject<Feature>(json);
        }

        if (feature == null)
        {
            return null;
        }

        // Remove properties starting with _ (underscore)
        return RemoveUnderscoreProperties(feature);
    }

    private static string GetFeatureFolderPath(string tenantCode, bool createIfMissing = false)
    {
        string uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "upload");
        string tenantFolder = ResolveTenantFolder(uploadRoot, tenantCode, createIfMissing);
        return Path.Combine(tenantFolder, "features");
    }

    private static string ResolveTenantFolder(string uploadRoot, string tenantCode, bool createIfMissing)
    {
        const string defaultTenant = "crm";
        string normalizedTenant = string.IsNullOrWhiteSpace(tenantCode) ? defaultTenant : tenantCode.Trim();

        var candidateNames = new[] { normalizedTenant, normalizedTenant.ToLowerInvariant(), defaultTenant }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidateNames)
        {
            string candidatePath = Path.Combine(uploadRoot, candidate);
            if (Directory.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        if (Directory.Exists(uploadRoot))
        {
            var matchedFolder = Directory
                .GetDirectories(uploadRoot)
                .FirstOrDefault(path => string.Equals(Path.GetFileName(path), normalizedTenant, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(matchedFolder))
            {
                return matchedFolder;
            }
        }

        if (createIfMissing)
        {
            return Path.Combine(uploadRoot, normalizedTenant.ToLowerInvariant());
        }

        return Path.Combine(uploadRoot, defaultTenant);
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
