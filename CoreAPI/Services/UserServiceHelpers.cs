using Core.Extensions;
using Core.Models;
using System.Buffers;
using System.Data;
using System.Text.RegularExpressions;

internal static class UserServiceHelpers
{

    public const string IdField = "Id";
    public const string NotFoundFile = "wwwRoot/404.html";
    public const string Href = "href";
    public const string Src = "src";
    public const int MAX_LOGIN = 5;
    public const string ConnKeyClaim = "ConnKey";
    public const string EnvClaim = "Environment";
    public const string TenantClaim = "TenantCode";
    public const string RoleNameClaim = "Role";
    public const string PassPhrase = "d7a9220a-6949-44c8-a702-789587e536cb";
    public const string BranchIdClaim = "BranchId";
    public const string ForwardedIP = "X-Forwarded-For";
    public const string APIClusterKey = "API_clusters";

    public static readonly Regex[] FobiddenTerms =
    [
        new(@"delete\s"), new(@"create\s"), new(@"insert\s"),
        new(@"update\s"), new(@"select\s"), new(@"from\s"),new(@"where\s"),
        new(@"group by\s"), new(@"having\s"), new(@"order by\s")
    ];
    public static readonly string[] SystemFields = new string[]
    {
        IdField, nameof(User.TenantCode), nameof(User.InsertedBy),
        nameof(User.InsertedDate), nameof(User.UpdatedBy), nameof(User.UpdatedDate)
    }.Select(x => x.ToLower()).ToArray();
    public static int Port;

    public static int ParsePort(HttpRequest request)
    {
        if (Port != 0) return Port;
        var parsePort = request.Headers.TryGetValue("X-Forwarded-To-Port", out var strPort);
        if (parsePort)
        {
            return strPort.ToString().TryParse<int>();
        }
        return 0;
    }

    public static string GetUri(string host, int? port, string scheme, string path)
    {
        var urlPort = "";
        if (port.HasValue
            && !(port.Value == 443 && "https".Equals(scheme, StringComparison.InvariantCultureIgnoreCase))
            && !(port.Value == 80 && "http".Equals(scheme, StringComparison.InvariantCultureIgnoreCase))
            )
        {
            urlPort = ":" + port.Value;
        }
        return $"{scheme}://{host}{urlPort}{path}";
    }
}