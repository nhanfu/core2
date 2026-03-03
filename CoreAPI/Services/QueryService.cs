using Core.Extensions;
using Core.ViewModels;
using CoreAPI.BgService;
using CoreAPI.Services.Interfaces;
using CoreAPI.Services.Sql;
using Jint;
using Newtonsoft.Json;

namespace CoreAPI.Services;

public class QueryService : IQueryService
{
    private readonly ISqlProvider _sql;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueryService> _logger;

    // User context - these should be set from the current request
    public string UserId { get; set; }
    public string TenantCode { get; set; }
    public string Env { get; set; }
    public List<string> RoleIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
    public string VendorId { get; set; }
    public string UserName { get; set; }
    public string GroupId { get; set; }

    public QueryService(
        ISqlProvider sql,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<QueryService> logger)
    {
        _sql = sql;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void SetUserContext(
        string userId,
        string tenantCode,
        string env,
        List<string> roleIds,
        List<string> roleNames,
        string vendorId,
        string userName,
        string groupId)
    {
        UserId = userId;
        TenantCode = tenantCode;
        Env = env;
        RoleIds = roleIds;
        RoleNames = roleNames;
        VendorId = vendorId;
        UserName = userName;
        GroupId = groupId;

        _sql.TenantCode = tenantCode;
        _sql.Env = env;
        _sql.UserId = userId;
        _sql.SystemFields = new List<string> { "id", "insertedby", "inserteddate", "updatedby", "updateddate" };
        if (tenantCode?.ToLower() != "system") _sql.SystemFields.Add("tenantcode");
    }

    public async Task<SqlResult> Go(SqlViewModel sqlViewModel)
    {
        var query = $"select * from [{sqlViewModel.Table}] where Id in ({sqlViewModel.Id.CombineStrings()})";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(_serviceProvider, _configuration, "logistics"));
        return new SqlResult()
        {
            data = ds[0],
            status = 200,
            message = "Select successful"
        };
    }

    public async Task<Dictionary<string, object>[][]> Gos(List<Gos> gos)
    {
        var query = gos.Select(x => $"select * from [{x.TableName}] where Id in ({x.Ids.CombineStrings()})").Combine(";");
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(_serviceProvider, _configuration, "logistics"));
        return ds;
    }

    public async Task<SqlResult> GoByName(SqlViewModel sqlViewModel)
    {
        var param = sqlViewModel.Id
            .Select((x, index) => new WhereParamVM()
            {
                FieldName = "@id" + (index + 1),
                Value = x
            }).ToList();
        var query = $"select * from [{sqlViewModel.Table}] where [{sqlViewModel.Format.Replace("{", "").Replace("}", "")}] in ({param.Select(x => x.FieldName).ToList().Combine()})";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(_serviceProvider, _configuration, "logistics"), false, param);
        return new SqlResult()
        {
            data = ds[0],
            status = 200,
            message = "Select successful"
        };
    }

    public async Task<SqlComResult> ComQuery(SqlViewModel vm)
    {
        var actQuery = CalcFinalQuery(vm);
        var dataParam = new List<WhereParamVM>();
        if (!string.IsNullOrWhiteSpace(vm.WhereParams))
        {
            dataParam = JsonConvert.DeserializeObject<List<WhereParamVM>>(vm.WhereParams);
        }
        var ds = await _sql.ReadDataSet(actQuery, null, false, dataParam);
        return new SqlComResult()
        {
            count = ds.Length > 1 && ds[1].Length > 0 ? Convert.ToInt32(ds[1][0]["total"]) : null,
            value = ds[0]
        };
    }

    public async Task<Dictionary<string, object>[][]> Report(SqlViewModel vm)
    {
        var actQuery = CalcFinalQuery(vm);
        var dataParam = new List<WhereParamVM>();
        if (!string.IsNullOrWhiteSpace(vm.WhereParams))
        {
            dataParam = JsonConvert.DeserializeObject<List<WhereParamVM>>(vm.WhereParams);
        }
        return await _sql.ReadDataSet(actQuery, null, false, dataParam);
    }

    public async Task<Dictionary<string, object>[][]> Sql(SqlViewModel vm)
    {
        var actQuery = CalcFinalQuery(vm);
        var dataParam = new List<WhereParamVM>();
        if (!string.IsNullOrWhiteSpace(vm.WhereParams))
        {
            dataParam = JsonConvert.DeserializeObject<List<WhereParamVM>>(vm.WhereParams);
        }
        return await _sql.ReadDataSet(actQuery, null, false, dataParam);
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

    private string CalcFinalQuery(SqlViewModel vm)
    {
        // If JsScript contains a JavaScript function (arrow function syntax), use Jint to execute it
        if (!string.IsNullOrWhiteSpace(vm.JsScript) && vm.JsScript.Contains("=>"))
        {
            return CalcQueryWithJint(vm);
        }

        // Fallback to legacy behavior for backward compatibility
        var dictionary = string.IsNullOrWhiteSpace(vm.Params)
            ? new Dictionary<string, object>()
            : JsonConvert.DeserializeObject<Dictionary<string, object>>(vm.Params);

        if (dictionary.GetValueOrDefault("TokenUserId") != null)
        {
            dictionary["TokenUserId"] = UserId;
        }
        else
        {
            dictionary.Add("TokenUserId", UserId);
        }

        if (dictionary.GetValueOrDefault("TokenRoleNames") != null)
        {
            dictionary["TokenRoleNames"] = RoleNames.Combine() ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenRoleNames", RoleNames.Combine());
        }

        if (dictionary.GetValueOrDefault("TokenPartnerId") != null)
        {
            dictionary["TokenPartnerId"] = VendorId ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenPartnerId", VendorId);
        }

        if (dictionary.GetValueOrDefault("TokenUserName") != null)
        {
            dictionary["TokenUserName"] = UserName;
        }
        else
        {
            dictionary.Add("TokenUserName", UserName);
        }

        if (dictionary.GetValueOrDefault("TokenGroupId") != null)
        {
            dictionary["TokenGroupId"] = GroupId ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenGroupId", GroupId);
        }

        if (vm.JsScript.Contains("ds.InsertedBy = '{TokenUserId}'") && RoleNames.Contains("BOD"))
        {
            vm.JsScript = vm.JsScript.Replace("ds.InsertedBy = '{TokenUserId}'", "ds.InsertedBy = '{TokenUserId}' or '{TokenRoleNames}' like '%BOD%'");
        }

        vm.OrderBy = FormatEntity(vm.OrderBy ?? "", dictionary);
        var data = JsonConvert.DeserializeObject<SqlQuery>(vm.JsScript);
        dictionary["Skip"] = vm.Skip;
        dictionary["Top"] = vm.Top;
        data.total = FormatEntity(data.total ?? "", dictionary);
        data.sql = FormatEntity(data.sql ?? "", dictionary);
        var sqlSelect = data.sql;
        var sqlTotal = data.total;

        if (!string.IsNullOrWhiteSpace(vm.Where))
        {
            if (sqlSelect.ToLower().Contains("where"))
            {
                sqlSelect += $" AND ({vm.Where})";
                sqlTotal += $" AND ({vm.Where})";
            }
            else
            {
                sqlSelect += $" WHERE {vm.Where}";
                sqlTotal += $" WHERE {vm.Where}";
            }
        }

        if (!string.IsNullOrWhiteSpace(vm.OrderBy) && !data.sql.ToLower().Contains("order by"))
        {
            sqlSelect += $" ORDER BY {vm.OrderBy}";
        }

        if (vm.Skip != null && !data.sql.ToLower().Contains("offset"))
        {
            sqlSelect += $" OFFSET {vm.Skip} ROWS";
        }

        if (vm.Top != null && !data.sql.ToLower().Contains("fetch next"))
        {
            sqlSelect += $" FETCH NEXT {vm.Top} ROWS ONLY";
        }

        return sqlSelect;
    }

    /// <summary>
    /// Calculates query using Jint runtime with user-defined JavaScript function.
    /// The function is declared in the JSScript property as an arrow function.
    /// Example: (vm) => `select * from "Users" where "Id" = ${vm.id}`
    /// </summary>
    private string CalcQueryWithJint(SqlViewModel vm)
    {
        var dictionary = string.IsNullOrWhiteSpace(vm.Params)
            ? []
            : JsonConvert.DeserializeObject<Dictionary<string, object>>(vm.Params);

        // Add token values to the context
        dictionary["TokenUserId"] = UserId;
        dictionary["TokenRoleNames"] = RoleNames.Combine() ?? string.Empty;
        dictionary["TokenPartnerId"] = VendorId ?? string.Empty;
        dictionary["TokenUserName"] = UserName ?? string.Empty;
        dictionary["TokenGroupId"] = GroupId ?? string.Empty;
        dictionary["Skip"] = vm.Skip;
        dictionary["Top"] = vm.Top;
        dictionary["TenantCode"] = TenantCode;

        try
        {
            // Create Jint engine with strict timeout
            var engine = new Engine(options => options
                .TimeoutInterval(TimeSpan.FromSeconds(5))
                .LimitRecursion(100));

            // Set context values in JavaScript
            foreach (var kvp in dictionary)
            {
                engine.SetValue(kvp.Key, kvp.Value);
            }

            // Add helper functions
            engine.SetValue("vm", vm);

            // Execute the JavaScript function and get the SQL query
            var result = engine.Evaluate(vm.JsScript);

            if (result != null && result.Type != Jint.Runtime.Types.Undefined)
            {
                return result.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Jint script: {Script}", vm.JsScript);
            throw new InvalidOperationException($"Error executing Jint script: {ex.Message}", ex);
        }

        return string.Empty;
    }

    private string FormatEntity(string template, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(template)) return "";

        var result = template;
        foreach (var kvp in data)
        {
            var value = kvp.Value?.ToString() ?? "null";
            result = result.Replace("{" + kvp.Key + "}", value);
        }
        return result;
    }
}
