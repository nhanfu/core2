using Core.Exceptions;
using Core.Models;
using CoreAPI.Services.Interfaces;
using CoreAPI.Services.Sql;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CoreAPI.Services;

public class MetadataService : IMetadataService
{
    private readonly ISqlProvider _sql;
    private readonly ILogger<MetadataService> _logger;

    public string TenantCode { get; set; }
    public List<string> RoleIds { get; set; } = new();

    private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public MetadataService(ISqlProvider sql, ILogger<MetadataService> logger)
    {
        _sql = sql;
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
            string basePath = GetFeatureFolderPath(TenantCode);
            _logger.LogInformation("GetMenu called - TenantCode: {TenantCode}, BasePath: {BasePath}", TenantCode, basePath);

            if (!Directory.Exists(basePath))
            {
                _logger.LogWarning("Directory does not exist: {Path}", basePath);
                return Task.FromResult(Array.Empty<Dictionary<string, object>>());
            }

            var menuItems = new List<Dictionary<string, object>>();

            foreach (var file in Directory.GetFiles(basePath, "*.yaml").Concat(Directory.GetFiles(basePath, "*.json")))
            {
                try
                {
                    var feature = ReadFeatureFile(file);
                    if (feature?.IsMenu == true && HasMenuPermission(feature))
                    {
                        menuItems.Add(ConvertFeatureToDictionary(feature));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading feature file: {File}", file);
                }
            }

            var result = menuItems.OrderBy(x => x.GetValueOrDefault("Order", 999)).ToArray();
            _logger.LogInformation("Returning {Count} menu items", result.Length);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMenu");
            return Task.FromResult(Array.Empty<Dictionary<string, object>>());
        }
    }

    public Feature LoadFeature(string name)
    {
        var feature = LoadFeatureFromFile(name, TenantCode) ?? throw new ApiException("Feature not found")
        {
            StatusCode = Core.Enums.HttpStatusCode.NotFound
        };
        return feature;
    }

    public async Task SaveFeatureToJson(Feature feature, string tenantCode)
    {
        _logger.LogInformation("Begin save feature");
        string directoryPath = GetFeatureFolderPath(tenantCode ?? TenantCode, createIfMissing: true);

        Directory.CreateDirectory(directoryPath);

        string filePath = Path.Combine(directoryPath, feature.Name + ".json");
        string json = JsonConvert.SerializeObject(feature, Formatting.Indented, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        });

        await File.WriteAllTextAsync(filePath, json);
    }

    private static Feature ReadFeatureFile(string filePath)
    {
        return filePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
            ? _yamlDeserializer.Deserialize<Feature>(File.ReadAllText(filePath))
            : JsonConvert.DeserializeObject<Feature>(File.ReadAllText(filePath));
    }

    private static Feature LoadFeatureFromFile(string featureName, string tenantCode)
    {
        string basePath = GetFeatureFolderPath(tenantCode);
        string yamlPath = Path.Combine(basePath, featureName + ".yaml");

        Feature feature = null!;

        if (File.Exists(yamlPath))
        {
            feature = _yamlDeserializer.Deserialize<Feature>(File.ReadAllText(yamlPath));
        }

        return feature;
    }

    private bool HasMenuPermission(Feature feature)
    {
        if (RoleIds.Contains("ADMIN"))
            return true;

        return feature.FeaturePolicies?.Any(p => RoleIds.Contains(p.RoleId) && p.CanRead == true) ?? false;
    }

    private static Dictionary<string, object> ConvertFeatureToDictionary(Feature feature)
    {
        return new Dictionary<string, object>
        {
            ["Id"] = feature.Id,
            ["Name"] = feature.Name,
            ["Label"] = feature.Label ?? feature.Name,
            ["Order"] = feature.Order ?? 999,
            ["Icon"] = feature.Icon ?? "",
            ["IsMenu"] = feature.IsMenu,
            ["ParentId"] = feature.ParentId ?? "",
            ["ClassName"] = feature.ClassName ?? "",
            ["Style"] = feature.Style ?? "",
            ["Script"] = feature.Script ?? "",
            ["Events"] = feature.Events ?? "",
            ["EntityId"] = feature.EntityId ?? "",
            ["Active"] = feature.Active ?? true,
            ["IsLock"] = feature.IsLock,
            ["IgnoreEncode"] = feature.IgnoreEncode
        };
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
        string normalized = string.IsNullOrWhiteSpace(tenantCode) ? defaultTenant : tenantCode.Trim();

        foreach (var candidate in new[] { normalized, normalized.ToLowerInvariant(), defaultTenant }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string candidatePath = Path.Combine(uploadRoot, candidate);
            if (Directory.Exists(candidatePath))
                return candidatePath;
        }

        if (Directory.Exists(uploadRoot))
        {
            string matchedFolder = Directory.GetDirectories(uploadRoot)
                .FirstOrDefault(path => string.Equals(Path.GetFileName(path), normalized, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(matchedFolder))
                return matchedFolder;
        }

        return createIfMissing
            ? Path.Combine(uploadRoot, normalized.ToLowerInvariant())
            : Path.Combine(uploadRoot, defaultTenant);
    }
}
