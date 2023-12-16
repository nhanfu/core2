using ClosedXML.Excel;
using Core.Exceptions;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Newtonsoft.Json;
using PuppeteerSharp;
using System.Buffers;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Tenray.Topaz;
using Tenray.Topaz.API;
using HttpStatusCode = Core.Enums.HttpStatusCode;

namespace Core.Services;

public class UserService
{
    private const string ContentType = "Content-Type";
    private const string NotFoundFile = "wwwRoot/404.html";
    private const string href = "href";
    private const string src = "src";
    private const int MAX_LOGIN = 5;
    public readonly IHttpContextAccessor _ctx;
    private readonly IConfiguration _cfg;
    private readonly IDistributedCache _cache;
    private readonly IWebHostEnvironment _host;

    public string UserId { get; set; }
    public string UserName { get; set; }
    public string ConnKey { get; set; }
    public string BranchId { get; set; }
    public List<string> CenterIds { get; set; }
    public string VendorId { get; set; }
    public string Env { get; set; }
    public string TenantCode { get; set; }
    public List<string> RoleIds { get; set; }

    static readonly Regex[] _fobiddenTerm =
    [
        new(@"delete\s"), new(@"create\s"), new(@"insert\s"),
        new(@"update\s"), new(@"select\s"), new(@"from\s"),new(@"where\s"),
        new(@"group by\s"), new(@"having\s"), new(@"order by\s")
    ];
    static readonly string[] _systemFields = new string[]
    {
        IdField, nameof(TenantCode), nameof(User.InsertedBy),
        nameof(User.InsertedDate), nameof(User.UpdatedBy), nameof(User.UpdatedDate)
    }.Select(x => x.ToLower()).ToArray();
    private const string ConnKeyClaim = "ConnKey";
    private const string EnvClaim = "Environment";
    private const string TenantClaim = "TenantCode";

    public UserService(IHttpContextAccessor httpContextAccessor, CoreContext db,
        IConfiguration configuration, IDistributedCache cache, IWebHostEnvironment host)
    {
        _cfg = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache)); ;
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _ctx = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        ExtractClaims();
    }

    private void ExtractClaims()
    {
        var claims = _ctx.HttpContext.User.Claims;
        BranchId = claims.FirstOrDefault(x => x.Type == BranchIdClaim)?.Value;
        UserId = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        ConnKey = claims.FirstOrDefault(x => x.Type == ConnKeyClaim)?.Value;
        UserName = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
        CenterIds = claims.Where(x => x.Type == nameof(CenterIds)).Select(x => x.Value).Where(x => x != null).ToList();
        RoleIds = claims.Where(x => x.Type == ClaimTypes.Actor).Select(x => x.Value).Where(x => x != null).ToList();
        VendorId = claims.FirstOrDefault(x => x.Type == ClaimTypes.GroupSid)?.Value;
        TenantCode = claims.FirstOrDefault(x => x.Type == TenantClaim)?.Value.ToUpper();
        Env = claims.FirstOrDefault(x => x.Type == EnvClaim)?.Value.ToUpper();
    }

    public string GenerateRandomToken(int? maxLength = 32)
    {
        var builder = new StringBuilder();
        var random = new Random();
        char ch;
        for (int i = 0; i < maxLength; i++)
        {
            ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
            builder.Append(ch);
        }
        return builder.ToString();
    }

    public string GetHash(HashAlgorithm hashAlgorithm, string input)
    {
        byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sBuilder = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }
        return sBuilder.ToString();
    }

    public const string IdField = "Id";
    private const string PassPhrase = "d7a9220a-6949-44c8-a702-789587e536cb";
    private const string BranchIdClaim = "BranchId";

    public string GetRemoteIpAddress(HttpContext context)
    {
        return context.Request.Headers.TryGetValue("X-Forwarded-For", out var value)
            ? value.ToString().Split(',')[0].Trim()
            : context.Connection.RemoteIpAddress.ToString();
    }

    public async Task<Token> SignInAsync(LoginVM login)
    {
        if (login.CompanyName.HasAnyChar())
        {
            login.CompanyName = login.CompanyName.Trim();
        }
        login.CachedConnStr = await GetConnStrFromKey(login.ConnKey, login.CompanyName, login.Env);
        var (matchedUser, roles) = await GetUserByLogin(login);
        if (matchedUser is null)
        {
            throw new ApiException($"Sai mật khẩu hoặc tên đăng nhập.<br /> Vui lòng đăng nhập lại!")
            {
                StatusCode = HttpStatusCode.BadRequest
            };
        }
        if (matchedUser.LoginFailedCount >= MAX_LOGIN && matchedUser.LastFailedLogin < DateTimeOffset.Now.AddMinutes(5))
        {
            throw new ApiException($"Tài khoản {login.UserName} đã bị khóa trong 5 phút!")
            {
                StatusCode = HttpStatusCode.Conflict
            };
        }
        var hashedPassword = GetHash(UserUtils.sHA256, login.Password + matchedUser.Salt);
        var matchPassword = matchedUser.Password == hashedPassword;
        List<PatchDetail> changes = [new PatchDetail { Field = IdField, OldVal = matchedUser.Id }];
        if (!matchPassword)
        {
            var loginFailedCount = matchedUser.LoginFailedCount.HasValue ? matchedUser.LoginFailedCount + 1 : 1;
            changes.Add(new PatchDetail { Field = nameof(User.LastFailedLogin), Value = DateTimeOffset.Now.ToISOFormat() });
            changes.Add(new PatchDetail { Field = nameof(User.LoginFailedCount), Value = loginFailedCount.ToString() });
        }
        else
        {
            matchedUser.LastLogin = DateTimeOffset.Now;
            matchedUser.LoginFailedCount = 0;
            changes.Add(new PatchDetail { Field = nameof(User.LastLogin), Value = DateTimeOffset.Now.ToISOFormat() });
            changes.Add(new PatchDetail { Field = nameof(User.LoginFailedCount), Value = 0.ToString() });
        }
        await SavePatch(new PatchVM
        {
            Table = nameof(User),
            CachedConnStr = login.CachedConnStr,
            Changes = changes
        });
        if (!matchPassword)
        {
            throw new ApiException($"Wrong username or password. Please try again!")
            {
                StatusCode = HttpStatusCode.BadRequest
            };
        }
        return await GetUserToken(matchedUser, roles, login);
    }

    private async Task<(User, Role[])> GetUserByLogin(LoginVM login)
    {
        var query = @$"
        declare @username varchar(100) = '{login.UserName}';
        declare @tenant varchar(100) = '{login.CompanyName}';
            select u.* from [User] u 
            join Vendor v on u.VendorId = v.Id
            where u.Active = 1 and u.Username = @username and v.Code = @tenant;
        select r.* from [User] u 
            join Vendor v on u.VendorId = v.Id
            left join UserRole ur on u.Id = ur.UserId
            left join [Role] r on ur.RoleId = r.Id
            where u.Active = 1 and u.Username = @username and v.Code = @tenant"
        ;
        var ds = await ReadDataSet(query, login.CachedConnStr);
        var userDb = ds.Length > 0 && ds[0].Length > 0 ? ds[0][0].MapTo<User>() : null;
        var roles = ds.Length > 1 && ds[1].Length > 0 ? ds[1].Select(x => x.MapTo<Role>()).ToArray() : null;
        return (userDb, roles);
    }

    protected async Task<Token> GetUserToken(User user, Role[] roles, LoginVM login, string refreshToken = null)
    {
        if (user is null)
        {
            return null;
        }
        var roleIds = roles.Select(x => x.Id).Distinct().ToList();
        var signinDate = DateTimeOffset.Now;
        var jit = Guid.NewGuid().ToString();
        List<Claim> claims =
        [
            new(ClaimTypes.GroupSid, user.VendorId.ToString()),
            new (ClaimTypes.NameIdentifier, user.Id.ToString()),
            new (ClaimTypes.Name, user.UserName),
            new (BranchIdClaim, user.BranchId?.ToString() ?? string.Empty),
            new (JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new (JwtRegisteredClaimNames.Birthdate, user.DoB?.ToString() ?? string.Empty),
        ];
        List<Claim> claim2 = [
            new (JwtRegisteredClaimNames.FamilyName, user.FullName?? string.Empty),
            new (JwtRegisteredClaimNames.Iat, signinDate.ToString()),
            new (JwtRegisteredClaimNames.Jti, jit),
            new (TenantClaim, login.CompanyName),
            new (EnvClaim, login.Env),
            new (ConnKeyClaim, login.ConnKey ?? Utils.ConnKey),
        ];
        claims.AddRange(claim2);
        claims.AddRange(roleIds.Select(x => new Claim(ClaimTypes.Actor, x.ToString())));
        var newLogin = refreshToken is null;
        refreshToken ??= GenerateRandomToken();
        var (token, exp) = AccessToken(claims);
        var res = JsonToken(user, roles, login.CompanyName, refreshToken, token, exp, signinDate);
        if (!newLogin || !login.AutoSignIn)
        {
            return res;
        }
        var userLogin = new UserLogin
        {
            Id = jit,
            TenantCode = login.CompanyName,
            UserId = user.Id,
            IpAddress = GetRemoteIpAddress(_ctx.HttpContext),
            RefreshToken = refreshToken,
            ExpiredDate = res.RefreshTokenExp,
            SignInDate = signinDate,
        };
        var patch = userLogin.MapToPatch();
        patch.CachedConnStr = login.CachedConnStr;
        await SavePatch(patch);
        return res;
    }

    public (JwtSecurityToken, DateTimeOffset) AccessToken(IEnumerable<Claim> claims, DateTimeOffset? expire = null)
    {
        var exp = expire ?? DateTimeOffset.Now.AddDays(1);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Tokens:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _cfg["Tokens:Issuer"],
            _cfg["Tokens:Issuer"],
            claims,
            expires: exp.DateTime,
            signingCredentials: creds);
        return (token, exp);
    }

    private static Token JsonToken(User user, Role[] roles, string tanent, string refreshToken,
        JwtSecurityToken token, DateTimeOffset exp, DateTimeOffset signinDate)
    {
        var vendor = new Vendor();
        vendor.CopyPropFrom(user.Vendor);
        return new Token
        {
            UserId = user.Id,
            CostCenterId = user.BranchId,
            FullName = user.FullName,
            UserName = user.UserName,
            Address = user.Address,
            Avatar = user.Avatar,
            PhoneNumber = user.PhoneNumber,
            Ssn = user.Ssn,
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            AccessTokenExp = exp,
            RefreshTokenExp = DateTimeOffset.Now.AddYears(1),
            RefreshToken = refreshToken,
            RoleIds = roles.Select(x => x.Id).ToList(),
            RoleNames = roles.Select(x => x.RoleName).ToList(),
            Vendor = vendor,
            TenantCode = tanent,
            SigninDate = signinDate,
        };
    }

    private void EnsureTokenParam(params string[] claims)
    {
        foreach (var claim in claims)
        {
            if (claim.IsNullOrWhiteSpace()) throw new ApiException("Invalid access token")
            {
                StatusCode = HttpStatusCode.BadRequest
            };
        }
    }
    public async Task<Token> RefreshAsync(RefreshVM token)
    {
        var principal = UserUtils.GetPrincipalFromAccessToken(token.AccessToken, _cfg);
        var userId = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        var userName = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
        var tenant = principal.Claims.FirstOrDefault(x => x.Type == TenantClaim)?.Value;
        var env = principal.Claims.FirstOrDefault(x => x.Type == EnvClaim)?.Value;
        var connKey = principal.Claims.FirstOrDefault(x => x.Type == ConnKeyClaim)?.Value;
        EnsureTokenParam(userId, userName, tenant, env, connKey);
        var query =
            @$"select * from UserLogin 
            where UserId = '{userId}' and RefreshToken = '{token.RefreshToken}'
            and ExpiredDate > '{DateTimeOffset.Now}' order by SignInDate desc";
        token.CachedConnStr ??= await GetConnStrFromKey(connKey, tenant, env);
        var userLogin = await ReadDsAs<UserLogin>(query, token.CachedConnStr);

        if (userLogin == null)
        {
            return null;
        }
        var login = new LoginVM
        {
            CompanyName = tenant,
            UserName = userName,
            ConnKey = connKey,
            CachedConnStr = token.CachedConnStr,
        };
        var (updatedUser, roles) = await GetUserByLogin(login);
        return await GetUserToken(updatedUser, roles, login, token.RefreshToken);
    }

    public async Task<string> GetConnStrFromKey(string connKey, string tenantCode = null, string env = null)
    {
        if (connKey.IsNullOrWhiteSpace()) connKey = Utils.ConnKey;
        tenantCode = TenantCode ?? tenantCode;
        env = Env ?? env;
        var key = $"{tenantCode}_{connKey}_{env}";
        var conStr = await _cache.GetStringAsync(key);
        if (conStr != null) return conStr;
        var query = $"select * from TenantEnv where TenantCode = '{tenantCode}' and ConnKey = '{connKey}' and Env = '{env}'";
        var tenantEnv = await ReadDsAs<TenantEnv>(query, _cfg.GetConnectionString(Utils.ConnKey))
            ?? throw new ApiException($"Tenant environment NOT found {key}");
        await _cache.SetStringAsync(key, tenantEnv.ConnStr, Utils.CacheTTL);
        return tenantEnv.ConnStr;
    }

    public async Task<SqlQueryResult> ExecJs(SqlViewModel vm)
    {
        SqlQueryResult result = new();
        var engine = new TopazEngine();
        engine.SetValue("JSON", new JSONObject());
        engine.AddType<HttpClient>("HttpClient");
        engine.AddNamespace("System");
        engine.AddNamespace("Core");
        engine.AddExtensionMethods(typeof(Enumerable));
        engine.AddExtensionMethods(typeof(IEnumerableCore));
        var claims = _ctx.HttpContext.User?.Claims;
        if (claims != null)
        {
            var map = new { UserId, RoleIds, TenantCode, Env, CenterIds, BranchId, VendorId };
            engine.SetValue("claims", JsonConvert.SerializeObject(map));
        }
        engine.SetValue("args", vm.Params);
        engine.SetValue("sv", this);
        engine.SetValue("vm", vm);

        await engine.ExecuteScriptAsync(vm.JsScript);
        var res = engine.GetValue("result");
        if (res is not string strRes)
        {
            result.Result = res;
            return result;
        }
        try
        {

            result = JsonConvert.DeserializeObject<SqlQueryResult>(strRes);
        }
        catch (Exception)
        {
            result.Query = strRes;
        }
        return result;
    }

    public bool HasSqlComment(string sql)
    {
        TSql110Parser parser = new(true);
        var fragments = parser.Parse(new StringReader(sql), out var errors);

        return fragments.ScriptTokenStream
            .Any(x => x.TokenType == TSqlTokenType.MultilineComment || x.TokenType == TSqlTokenType.SingleLineComment);
    }

    static readonly TSqlTokenType[] SideEffectCmd = [
            TSqlTokenType.Insert, TSqlTokenType.Update, TSqlTokenType.Delete,
                TSqlTokenType.Create, TSqlTokenType.Drop, TSqlTokenType.Alter,
                TSqlTokenType.Truncate, TSqlTokenType.MultilineComment, TSqlTokenType.SingleLineComment
        ];
    public bool HasSideEffect(string sql, params TSqlTokenType[] allowCmds)
    {
        var finalCmd = SideEffectCmd.Except(allowCmds).ToArray();
        TSql110Parser parser = new(true);
        var fragments = parser.Parse(new StringReader(sql), out var errors);
        return fragments.ScriptTokenStream.Any(x => finalCmd.Contains(x.TokenType));
    }

    public async Task<T> ReadDsAs<T>(string query, string connInfo, bool shouldGetConnStr = false) where T : class
    {
        var ds = await ReadDataSet(query, connInfo, shouldGetConnStr);
        if (ds.Length == 0 || ds[0].Length == 0) return null;
        return ds[0][0].MapTo<T>();
    }

    public async Task<T[]> ReadDsAsArr<T>(string query, string connInfo, bool shouldGetConnStr = false) where T : class
    {
        var ds = await ReadDataSet(query, connInfo, shouldGetConnStr);
        if (ds.Length == 0 || ds[0].Length == 0) return [];
        return ds[0].Select(x => x.MapTo<T>()).ToArray();
    }

    public async Task<Dictionary<string, object>[][]> ReadDataSet(string query, string connInfo, bool shouldGetConnStr = false)
    {
        var sideEffect = HasSideEffect(query);
        if (sideEffect) throw new ApiException("Side effect of query is NOT allowed");
        var connStr = connInfo;
        if (shouldGetConnStr) connStr = await GetConnStrFromKey(connInfo);
        var con = new SqlConnection(connStr);
        var sqlCmd = new SqlCommand(query, con)
        {
            CommandType = CommandType.Text
        };
        SqlDataReader reader = null;
        var tables = new List<Dictionary<string, object>[]>();
        try
        {
            await con.OpenAsync();
            reader = await sqlCmd.ExecuteReaderAsync();
            while (true)
            {
                var table = new List<Dictionary<string, object>>();
                while (await reader.ReadAsync())
                {
                    table.Add(Read(reader));
                }
                tables.Add([.. table]);
                var next = await reader.NextResultAsync();
                if (!next) break;
            }
            return [.. tables];
        }
        catch (Exception e)
        {
            var message = $"{e.Message} {query} {connStr}";
#if RELEASE
            message = $"{e.Message} {query} ";
#endif
            throw new ApiException(message, e)
            {
                StatusCode = HttpStatusCode.InternalServerError,
            };
        }
        finally
        {
            if (reader is not null) await reader.DisposeAsync();
            await sqlCmd.DisposeAsync();
            await con.DisposeAsync();
        }
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

    public static string RemoveWhiteSpace(string val) => val.Replace(" ", "");

    public async Task<int> SavePatch(PatchVM vm)
    {
        var canWrite = await HasWritePermission(vm);
        if (!canWrite) throw new ApiException($"Unauthorized access on \"{vm.Table}\"")
        {
            StatusCode = HttpStatusCode.Unauthorized
        };
        var cmd = GetCmd(vm);
        var connStr = vm.CachedConnStr ?? await GetConnStrFromKey(vm.ConnKey);
        return await RunSqlCmd(connStr, cmd);
    }

    private async Task<bool> HasWritePermission(PatchVM vm)
    {
        if (vm.ByPassPerm) return true;
        bool writePerm = false;
        var connStr = vm.CachedConnStr ?? await GetConnStrFromKey(vm.ConnKey);
        vm.CachedConnStr = connStr;
        var allRights = vm.ByPassPerm ? [] : await GetEntityPerm(vm.Table, recordId: null, connStr);
        var idField = vm.Changes.FirstOrDefault(x => x.Field == Utils.IdField);
        var oldId = idField?.OldVal;
        if (oldId is null)
        {
            writePerm = allRights.Any(x => x.CanAdd || x.CanWriteAll);
        }
        else
        {
            var origin = @$"select t.* from [{vm.Table}] as t where t.Id = '{oldId}'";
            var ds = await ReadDataSet(origin, connStr);
            var originRow = ds.Length > 0 && ds[0].Length > 0 ? ds[0][0] : null;
            var isOwner = Utils.IsOwner(originRow, UserId, RoleIds);
            writePerm = isOwner || allRights.Any(x => x.CanWriteAll);
        }
        return writePerm;
    }

    public async Task<int> SavePatches(PatchVM[] patches)
    {
        var connStr = patches[0].CachedConnStr ?? await GetConnStrFromKey(patches[0].ConnKey);
        var tables = patches.Select(x => x.Table);
        List<string> rightQuery = [@$"select * from FeaturePolicy 
            where Active = 1 and (CanWrite = 1 or CanWriteAll = 1) and EntityName in ({tables.CombineStrings()}) and RoleId in ({RoleIds.CombineStrings()})"];
        rightQuery.AddRange(patches.Select(x =>
        {
            var idField = GetIdField(x);
            return $"select * from {x.Table} where Id = '{idField.OldVal}'";
        }));
        var ds = await ReadDataSet(rightQuery.Combine(Utils.SemiColon), connStr);
        var permissions = ds.Length > 0 && ds[0].Length > 0 ? ds[0].Select(x => x.MapTo<FeaturePolicy>()).ToArray() : [];
        permissions = permissions.DistinctBy(x => x.EntityName).ToArray();
        var lackPerTables = patches.Select(x => x.Table).Except(permissions.Select(x => x.EntityName)).ToArray();
        if (lackPerTables.Length > 0)
        {
            throw new ApiException($"All table must have write permission {lackPerTables.CombineStrings()}")
            {
                StatusCode = HttpStatusCode.Unauthorized
            };
        }
        var sql = patches.Select(GetCmd);
        return await RunSqlCmd(connStr, sql.Combine(";\n"));
    }

    private static PatchDetail GetIdField(PatchVM x)
    {
        return x.Changes.FirstOrDefault(x => x.Field == IdField);
    }

    public string GetCmd(PatchVM vm)
    {
        if (vm == null || vm.Table.IsNullOrWhiteSpace() || vm.Changes.Nothing())
        {
            throw new ApiException("Table name and change details can NOT be empty") { StatusCode = HttpStatusCode.BadRequest };
        }
        vm.Table = RemoveWhiteSpace(vm.Table);
        vm.Changes = vm.Changes.Where(x =>
        {
            if (x.Field.IsNullOrWhiteSpace()) throw new ApiException($"Field name can NOT be empty") { StatusCode = HttpStatusCode.BadRequest };
            x.Field = RemoveWhiteSpace(x.Field);
            x.Value = x.Value?.Replace("'", "''");
            x.OldVal = x.OldVal?.Replace("'", "''");
            return !x.JustHistory && !_systemFields.Contains(x.Field);
        }).ToList();
        var idField = GetIdField(vm);
        var valueFields = vm.Changes.Where(x => !_systemFields.Contains(x.Field.ToLower())).ToArray();
        var now = DateTimeOffset.Now.ToString(DateTimeExt.DateFormat);
        var oldId = idField?.OldVal;
        if (oldId is not null)
        {
            var update = valueFields.Combine(x => x.Value is null ? $"[{x.Field}] = null" : $"[{x.Field}] = N'{x.Value}'");
            return @$"update [{vm.Table}] set {update}, TenantCode = '{TenantCode ?? vm.TenantCode}', 
                UpdatedBy = '{UserId ?? 1.ToString()}', UpdatedDate = '{now}' where Id = '{oldId}';";
        }
        else
        {
            valueFields = valueFields.Where(x => x.Field != "Active").ToArray();
            var fields = valueFields.Combine(x => $"[{x.Field}]");
            var values = valueFields.Combine(x => x.Value is null ? "null" : $"N'{x.Value}'");
            return @$"insert into [{vm.Table}] ([Id], [TenantCode], [Active], [InsertedBy], [InsertedDate], {fields}) 
                    values ('{idField.Value}', '{TenantCode ?? vm.TenantCode}', 1, '{UserId ?? 1.ToString()}', '{now}', {values});";
        }
    }

    public async Task<int> RunSqlCmd(string connStr, string cmdText)
    {
        SqlConnection connection = new(connStr);
        await connection.OpenAsync();
        var transaction = connection.BeginTransaction();
        var cmd = new SqlCommand
        {
            Transaction = transaction,
            Connection = connection,
            CommandText = cmdText
        };
        var anyComment = HasSqlComment(cmd.CommandText);
        if (anyComment) throw new ApiException("Comment is NOT allowed");
        try
        {
            var affected = await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return affected;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            var message = "Error occurs";
#if DEBUG
            message = $"Error occurs at {connStr} {cmdText}";
#endif
            throw new ApiException(message, e)
            {
                StatusCode = HttpStatusCode.InternalServerError
            };
        }
        finally
        {
            await transaction.DisposeAsync();
            await cmd.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    public async Task<string[]> DeactivateAsync(SqlViewModel vm)
    {
        vm.CachedConnStr ??= await GetConnStrFromKey(vm.ConnKey ?? "default");
        var allRights = await GetEntityPerm(vm.Table, null, vm.CachedConnStr);
        var canDeactivateAll = allRights.Any(x => x.CanDeactivateAll);
        var canDeactivateSelf = allRights.Any(x => x.CanDeactivate);
        var query = $"select * from {vm.Table} where Id in ({vm.Ids.CombineStrings()})";
        var ds = await ReadDataSet(query, vm.CachedConnStr);
        var rows = ds.Length > 0 ? ds[0] : null;
        if (rows.Nothing()) return null;
        var canDeactivateRows = rows.Where(x =>
        {
            return canDeactivateAll || canDeactivateSelf && Utils.IsOwner(x, UserId, RoleIds);
        }).Select(x => x.GetValueOrDefault(Utils.IdField)?.ToString()).ToArray();
        if (canDeactivateRows.Nothing()) return null;
        var deactivateCmd = $"update {vm.Table} set Active = 0 where Id in ({canDeactivateRows.CombineStrings()})";
        await RunSqlCmd(vm.CachedConnStr, deactivateCmd);
        return canDeactivateRows;
    }

    public async Task<object> ComQuery(SqlViewModel vm)
    {
        vm.CachedConnStr ??= await GetConnStrFromKey(vm.ConnKey, vm.AnnonymousTenant, vm.AnnonymousEnv);
        var com = await GetComponent(vm);
        var anyInvalid = _fobiddenTerm.Any(term =>
        {
            return vm.Select != null && term.IsMatch(vm.Select.ToLower())
            || vm.Table != null && term.IsMatch(vm.Table.ToLower())
            || vm.Where != null && term.IsMatch(vm.Where.ToLower())
            || vm.GroupBy != null && term.IsMatch(vm.GroupBy.ToLower())
            || vm.Having != null && term.IsMatch(vm.Having.ToLower())
            || vm.OrderBy != null && term.IsMatch(vm.OrderBy.ToLower())
            || vm.Paging != null && term.IsMatch(vm.Paging.ToLower());
        });
        if (anyInvalid)
        {
            throw new ArgumentException("Parameters must NOT contains sql keywords");
        }
        vm.JsScript = com.Query;
        var jsRes = await ExecJs(vm);
        if (jsRes.Result != null)
        {
            return jsRes.Result;
        }
        return await GetResultFromQuery(vm, vm.CachedConnStr, jsRes);
    }

    private async Task<Dictionary<string, object>[][]> GetResultFromQuery(SqlViewModel vm, string decryptedConnStr, SqlQueryResult jsRes)
    {
        var select = vm.Select.HasAnyChar() ? $"select {vm.Select}" : string.Empty;
        var where = vm.Where.HasAnyChar() ? $"where {vm.Where}" : string.Empty;
        var groupBy = vm.GroupBy.HasAnyChar() ? $"group by {vm.GroupBy}" : string.Empty;
        var having = vm.Having.HasAnyChar() ? $"having {vm.Having}" : string.Empty;
        var orderBy = vm.OrderBy.HasAnyChar() ? $"order by {vm.OrderBy}" : string.Empty;
        var xQuery = vm.SkipXQuery ? string.Empty : jsRes.XQuery;
        var countQuery = vm.Count ?
            $@"select count(*) as total from (
                {jsRes.Query}) as ds 
                {where}
                {groupBy}
                {having};" : string.Empty;
        var finalQuery = @$"{select}
                from ({jsRes.Query}) as ds
                {where}
                {groupBy}
                {having}
                {orderBy}
                {vm.Paging};
                {countQuery}
                {xQuery}";
        return await ReadDataSet(finalQuery, decryptedConnStr);
    }

    private async Task<Component> GetComponent(SqlViewModel vm)
    {
        Component com = null;
        var comKey = "com_" + vm.ComId;
        var cached = _cache.GetString(comKey);
        if (cached != null)
        {
            try
            {
                com = JsonConvert.DeserializeObject<Component>(cached);
            }
            catch
            {

            }
        }
        string connStr = null;
        if (com is null)
        {
            var query = @$"select top 1 * from Component 
            where Id = '{vm.ComId}' and (Annonymous = 1 or IsPrivate = 0 and '{TenantCode}' != '' or TenantCode = '{TenantCode}')";
            connStr = vm.CachedConnStr ?? await GetConnStrFromKey(vm.ConnKey, vm.AnnonymousTenant, vm.AnnonymousEnv);
            vm.CachedConnStr = connStr;
            com = await ReadDsAs<Component>(query, connStr);
            if (com is null) return null;
            await _cache.SetStringAsync(comKey, JsonConvert.SerializeObject(com), Utils.CacheTTL);
        }
        var readPermission = await GetEntityPerm("Component", vm.ComId, connStr, x => x.CanRead);
        var hasPerm = com.Annonymous || !com.IsPrivate && UserId != null || readPermission.Length != 0;
        if (!hasPerm)
            throw new ApiException("Access denied")
            {
                StatusCode = HttpStatusCode.Unauthorized
            };

        return com;
    }

    private async Task<FeaturePolicy[]> GetEntityPerm(string entityName, string recordId, string connStr,
        Expression<Func<FeaturePolicy, bool>> pre = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        if (RoleIds.Nothing()) return [];
        var permissionName = ((pre?.Body as MemberExpression)?.Member as PropertyInfo)?.Name;
        var key = entityName + "_" + (permissionName ?? "AllRights");
        var permissionByComCache = await _cache.GetStringAsync(key);
        FeaturePolicy[] permissions;
        if (!permissionByComCache.IsNullOrWhiteSpace())
        {
            permissions = JsonConvert.DeserializeObject<FeaturePolicy[]>(permissionByComCache);
        }
        else
        {
            var q = @$"select * from FeaturePolicy 
            where Active = 1 and EntityName = '{entityName}'
            and (RecordId = '{recordId}' or '{recordId}' = '') and RoleId in ({RoleIds.CombineStrings()})";
            if (pre != null) q += $" and {permissionName} = 1";
            permissions = await ReadDsAsArr<FeaturePolicy>(q, connStr);
            await _cache.SetStringAsync(key, JsonConvert.SerializeObject(permissions), Utils.CacheTTL);
        }

        return permissions;
    }

    public async Task<object> RunUserSvc(SqlViewModel vm)
    {
        var sv = await GetService(vm)
            ?? throw new ApiException($"Service \"{vm.ComId} - {vm.Action}\" NOT found")
            {
                StatusCode = HttpStatusCode.NotFound
            };
        vm.JsScript = sv.Content;
        var jsRes = await ExecJs(vm);
        if (jsRes.Result is not null)
        {
            return jsRes.Result;
        }
        return await GetResultFromQuery(vm, vm.CachedConnStr, jsRes);
    }

    private async Task<Models.Services> GetService(SqlViewModel vm)
    {
        if (vm is null || vm.SvcId.IsNullOrWhiteSpace() && (vm.Action.IsNullOrWhiteSpace() || vm.ComId.IsNullOrWhiteSpace()))
        {
            throw new ApiException("Service can NOT be identified due to lack of Id or action") { StatusCode = HttpStatusCode.BadRequest };
        }
        Models.Services sv = null;
        if (vm.SvcId is not null)
        {
            var cacheSv = await _cache.GetStringAsync(vm.SvcId);
            if (cacheSv != null)
            {
                sv = JsonConvert.DeserializeObject<Models.Services>(cacheSv);
            }
        }
        if (sv is null)
        {
            var query = @$"select * from Services
                where (ComId = '{vm.ComId}' and Action = '{vm.Action}' or Id = '{vm.SvcId}') 
                and (TenantCode = '{TenantCode}' or Annonymous = 1 and TenantCode = '{vm.AnnonymousTenant}')";
            vm.CachedConnStr = await GetConnStrFromKey(vm.ConnKey, vm.AnnonymousTenant, vm.AnnonymousEnv);
            sv = await ReadDsAs<Models.Services>(query, vm.CachedConnStr);
        }

        if (sv == null)
        {
            return null;
        }
        if (TenantCode is null && !sv.Annonymous)
        {
            throw new UnauthorizedAccessException("The service is required login");
        }
        var isValidRole = sv.IsPublicInTenant ||
            (from svRole in sv.RoleIds.Split(',')
             join usrRole in RoleIds on svRole equals usrRole
             select svRole).Any();
        if (!isValidRole) throw new UnauthorizedAccessException("The service is not accessible by your roles");
        return sv;
    }

    public async Task<string> ExportExcel(SqlViewModel vm)
    {
        var comDs = await ComQuery(vm);
        var dataSets = comDs as Dictionary<string, object>[][];
        var table = dataSets.ElementAt(0);
        var headers = JsonConvert.DeserializeObject<List<Component>>(JsonConvert.SerializeObject(dataSets.ElementAt(1)));
        if (vm.FieldName.HasElement())
        {
            headers = headers.Where(x =>
            {
                var field = x.FieldText.HasNonSpaceChar() ? x.FieldText : x.FieldName;
                return x.Active && x.ShortDesc.HasNonSpaceChar() && vm.FieldName.Contains(x.FieldName);
            }).ToList();
        }
        return ExportExcel(vm.Params ?? "Export data", headers, table);
    }

    public string ConvertHtmlToPlainText(string htmlContent)
    {
        // Remove HTML tags using regular expression
        string plainText = Regex.Replace(htmlContent, @"<[^>]+>|&nbsp;", "").Trim();

        // Decode HTML entities using regular expression
        plainText = Regex.Replace(plainText, @"&(amp|quot|gt|lt|nbsp);", m => DecodeEntity(m.Groups[1].Value));

        return plainText;
    }

    public string DecodeEntity(string entity) =>
        entity switch
        {
            "amp" => "&",
            "quot" => "\"",
            "gt" => ">",
            "lt" => "<",
            "nbsp" => " ",
            _ => entity,
        };

    private string ExportExcel(string refName, List<Component> headers, IEnumerable<Dictionary<string, object>> dataSet)
    {
        headers = headers.Where(x => x.Active && x.ShortDesc.HasNonSpaceChar()).ToList();
        XLWorkbook workbook;
        bool anyGroup = headers.Any(x => !string.IsNullOrEmpty(x.GroupName));
        workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");
        worksheet.Cell("A1").Value = refName;
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 14;
        worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Range(1, 1, headers.Count() + 1, headers.Count + 1).Row(1).Merge();
        worksheet.Style.Font.SetFontName("Times New Roman");
        var i = 2;
        worksheet.Cell(2, 1).SetValue("STT");
        worksheet.Cell(2, 1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
        worksheet.Cell(2, 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        worksheet.Cell(2, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        worksheet.Cell(2, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        if (anyGroup)
        {
            worksheet.Range(2, 1, 3, 1).Merge();
        }
        foreach (var header in headers)
        {
            if (anyGroup && !string.IsNullOrEmpty(header.GroupName))
            {
                var colspan = headers.Count(x => x.GroupName == header.GroupName);
                if (header != headers.FirstOrDefault(x => x.GroupName == header.GroupName))
                {
                    i++;
                    continue;
                }
                worksheet.Cell(2, i).SetValue(ConvertHtmlToPlainText(header.GroupName));
                worksheet.Range(2, i, 2, i + colspan - 1).Merge();
                worksheet.Range(2, i, 2, i + colspan - 1).Style.Font.Bold = true;
                worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                worksheet.Range(2, i, 2, i + colspan - 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Range(2, i, 2, i + colspan - 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                i++;
                continue;
            }
            worksheet.Cell(2, i).SetValue(ConvertHtmlToPlainText(header.ShortDesc));
            worksheet.Cell(2, i).Style.Font.Bold = true;
            worksheet.Cell(2, i).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(2, i).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(2, i).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(2, i).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(2, i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, i).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            if (anyGroup && string.IsNullOrEmpty(header.GroupName))
            {
                worksheet.Range(2, i, 3, i).Merge();
                worksheet.Cell(3, i).Style.Font.Bold = true;
                worksheet.Cell(3, i).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(3, i).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(3, i).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(3, i).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(3, i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(3, i).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            i++;
        }
        var h = 2;
        if (anyGroup)
        {
            foreach (var item in headers)
            {
                if (anyGroup && !string.IsNullOrEmpty(item.GroupName))
                {
                    worksheet.Cell(3, h).SetValue(ConvertHtmlToPlainText(item.ShortDesc));
                    worksheet.Cell(3, h).Style.Font.Bold = true;
                    worksheet.Cell(3, h).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, h).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, h).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, h).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, h).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(3, h).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                h++;
            }
        }
        var x = 3;
        if (anyGroup)
        {
            x++;
        }
        var j = 1;
        foreach (var item in dataSet)
        {
            var y = 2;
            worksheet.Cell(x, 1).SetValue(j);
            worksheet.Cell(x, 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(x, 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(x, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(x, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            foreach (var header in headers)
            {
                var field = header.FieldText.HasNonSpaceChar() ? header.FieldText : header.FieldName;
                var vl = item.GetValueOrDefault(field);
                switch (header.ComponentType)
                {
                    case "Input":
                    case "Textarea":
                    case "Label":
                    case "SearchEntry":
                        worksheet.Cell(x, y).SetValue(vl?.ToString().DecodeSpecialChar());
                        break;
                    case "Datepicker":
                        worksheet.Cell(x, y).SetValue((DateTime?)vl);
                        break;
                    case "Number":
                        if (vl is int v)
                        {
                            worksheet.Cell(x, y).SetValue(vl is null ? default : v);
                        }
                        else
                        {
                            worksheet.Cell(x, y).SetValue(vl is null ? default : (decimal)vl);
                            worksheet.Cell(x, y).Style.NumberFormat.Format = "#,##";
                        }
                        break;
                    case "Checkbox":
                        worksheet.Cell(x, y).SetValue(vl.ToString() == "False" ? default : 1);
                        break;
                    default:
                        break;
                }
                worksheet.Cell(x, y).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(x, y).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(x, y).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(x, y).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                y++;
            }
            j++;
            x++;
        }
        var k = 2;
        var last = dataSet.Count() + 3;
        worksheet.Cell(last, 1).Value = "Total";
        worksheet.Cell(last, 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
        worksheet.Cell(last, 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        worksheet.Cell(last, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        worksheet.Cell(last, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        foreach (var item in headers)
        {
            if (item.ComponentType == "Number")
            {
                var value = dataSet.Select(x => x[item.FieldName]).Where(x => x != null).Sum(x =>
                {
                    if (x is int v)
                    {
                        return x is null ? default : v;
                    }
                    else
                    {
                        return x is null ? default : (decimal)x;
                    }
                });
                worksheet.Cell(last, k).SetValue(value);
                worksheet.Cell(last, k).Style.Font.Bold = true;
                worksheet.Cell(last, k).Style.NumberFormat.Format = "#,##";
            }
            worksheet.Cell(last, k).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(last, k).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(last, k).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(last, k).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            k++;
        }
        var url = $"{refName}{DateTimeOffset.Now:ddMMyyyyhhmm}.xlsx";
        worksheet.Columns().AdjustToContents();
        workbook.SaveAs($"wwwroot\\excel\\Download\\{url}");
        return url;
    }

    public async Task<string> PostImageAsync(IWebHostEnvironment host,
            string name = "Captured", bool reup = false)
    {
        var image = await Utils.ReadRequestBodyAsync(_ctx.HttpContext.Request, leaveOpen: false);
        var fileName = $"{Path.GetFileNameWithoutExtension(name)}{Path.GetExtension(name)}";
        var path = GetUploadPath(fileName, host.WebRootPath);
        EnsureDirectoryExist(path);
        path = reup ? IncreaseFileName(path) : path;
        await File.WriteAllBytesAsync(path, Convert.FromBase64String(image));
        return GetHttpPath(path, host.WebRootPath);
    }

    public async Task<string> PostFileAsync(IFormFile file, bool reup = false)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var path = GetUploadPath(fileName, _host.WebRootPath);
        EnsureDirectoryExist(path);
        path = reup ? IncreaseFileName(path) : path;
        using var stream = File.Create(path);
        await file.CopyToAsync(stream);
        stream.Close();
        return GetHttpPath(path, _host.WebRootPath);
    }

    public string GetHttpPath(string path, string webRootPath)
    {
        return _ctx.HttpContext.Request.Scheme + "://" + _ctx.HttpContext.Request.Host.Value + path.Replace(webRootPath, string.Empty).Replace("\\", "/");
    }

    public async Task<bool> ImportCsv(List<IFormFile> files, string table, string comId, string connKey)
    {
        if (comId.IsNullOrWhiteSpace() || table.IsNullOrWhiteSpace())
        {
            throw new ApiException("ComId or table cannot be null") { StatusCode = HttpStatusCode.BadRequest };
        }
        if (files.Nothing())
        {
            throw new ApiException("No file uploaded") { StatusCode = HttpStatusCode.BadRequest };
        }
        var com = await GetComponent(new SqlViewModel
        {
            ComId = comId,
            ConnKey = connKey
        });
        var connStr = await GetConnStrFromKey(com.ConnKey);
        var tableRights = await GetEntityPerm(table, recordId: null, connStr);
        if (!tableRights.Any(x => x.CanAdd || x.CanWriteAll))
            throw new UnauthorizedAccessException("Cannot import data due to lack of permission");

        var file = files.FirstOrDefault();
        var path = GetUploadPath(file.FileName, _host.WebRootPath);
        EnsureDirectoryExist(path);
        path = IncreaseFileName(path);
        using var stream = File.Create(path);
        await file.CopyToAsync(stream);
        stream.Close();

        var patches = await ParseCsvFile(path, table);

        using SqlConnection connection = new(connStr);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            using SqlCommand command = new();
            command.Transaction = transaction;
            command.Connection = connection;
            patches.SelectForEach((x, index) =>
            {
                ImportItem(x, command, index);
            });
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return true;
    }

    private void ImportItem(PatchVM vm, SqlCommand command, int index)
    {
        var idField = vm.Changes.FirstOrDefault(x => x.Field == Utils.IdField);
        var oldId = idField?.OldVal;
        var valueFields = vm.Changes.Where(x => !_systemFields.Contains(x.Field.ToLower())).ToList();
        var update = valueFields.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
        var now = DateTimeOffset.Now.ToString(DateTimeExt.DateFormat);

        var fields = valueFields.Combine(x => $"[{x.Field}]");
        var fieldParams = valueFields.Combine(x => $"@{x.Field}{index}");
        command.CommandText += @$"insert into [{vm.Table}] ([Id], [TenantCode], [Active], [InsertedBy], [InsertedDate], {fields}) 
                        values ('{idField.Value}', '{TenantCode}', 1, '{UserId}', '{now}', {fieldParams});";
        foreach (var item in valueFields)
        {
            command.Parameters.AddWithValue($"@{item.Field}{index}", item.Value is null ? DBNull.Value : item.Value);
        }
    }

    public string GetUploadPath(string fileName, string webRootPath)
    {
        return Path.Combine(webRootPath, "upload", TenantCode, $"U{UserId}", fileName);
    }

    public static string IncreaseFileName(string path)
    {
        var uploadedPath = path;
        var index = 0;
        while (File.Exists(path))
        {
            var noExtension = Path.GetFileNameWithoutExtension(uploadedPath);
            var dir = Path.GetDirectoryName(uploadedPath);
            index++;
            path = Path.Combine(dir, noExtension + "_" + index + Path.GetExtension(uploadedPath));
        }

        return path;
    }

    public static void EnsureDirectoryExist(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    protected async Task<List<PatchVM>> ParseCsvFile(string path, string table)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        var tempPath = IncreaseFileName(path);
        using var streamReader = new StreamReader(path);
        using var streamWriter = new StreamWriter(tempPath);
        string currentLine;
        var lineCount = 0;
        string[] headers = null;
        var patches = new List<PatchVM>();
        while ((currentLine = await streamReader.ReadLineAsync()) != null)
        {
            if (lineCount == 0 || currentLine.IsNullOrWhiteSpace())
            {
                lineCount++;
                var firstLine = ParseCsvLine(currentLine, lineCount)
                    ?? throw new ApiException("Header must be the first line of csv")
                    {
                        StatusCode = HttpStatusCode.BadRequest
                    };
                headers = firstLine.Select(x => x.Field).ToArray();
                continue;
            }
            var updatedLine = ParseCsvLine(currentLine, lineCount);
            updatedLine.SelectForEach((x, index) => x.Field = headers[index]);
            patches.Add(new PatchVM { Table = table, Changes = updatedLine });
            lineCount++;
        }
        return patches;
    }

    private static List<PatchDetail> ParseCsvLine(string currentLine, int lineCount, string[] headers = null)
    {
        List<PatchDetail> res;
        string propVal = null;
        try
        {
            var parser = new CsvParser(currentLine);
            var values = parser.ToArray();
            res = new List<PatchDetail>(values.Length);
            for (int index = 0; index < values.Length && index < values.Length; index++)
            {
                res.Add(new PatchDetail
                {
                    Field = headers is null ? values[index] : headers[index],
                    Value = values[index]
                });
            }
        }
        catch
        {
            throw new ApiException($"Struture of line {lineCount}, value {propVal} is not valid")
            {
                StatusCode = HttpStatusCode.BadRequest
            };
        }

        return res;
    }

    public async Task<IEnumerable<string>> GeneratePdf(EmailVM email, IWebHostEnvironment host, bool absolute)
    {
        if (email.PdfText.Nothing())
        {
            return Enumerable.Empty<string>();
        }
        List<string> paths = [];
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
        var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await email.PdfText.ForEachAsync(async pdf =>
        {
            await page.SetContentAsync(pdf);
            var path = Path.Combine(host.WebRootPath, "download", UserId, Guid.NewGuid() + ".pdf");
            EnsureDirectoryExist(path);
            await page.PdfAsync(path);
            paths.Add(path);
        });
        return absolute ? paths : paths.Select(path => path.Replace(host.WebRootPath, string.Empty));
    }

    public async Task<bool> EmailAttached(EmailVM email, IWebHostEnvironment host)
    {
        var connStr = await GetConnStrFromKey(email.ConnKey);
        var paths = await GeneratePdf(email, host, absolute: true);
        paths.SelectForEach(email.ServerAttachements.Add);
        await SendMail(email, connStr, host.WebRootPath);
        return true;
    }

    public async Task SendMail(EmailVM email, string connStr, string webRoot = null)
    {
        var query = $"select * from MasterData m join MasterData p on m.ParentId = p.Id where p.Name = 'ConfigEmail'";
        var config = await ReadDsAsArr<MasterData>(query, connStr);
        var fromName = config.FirstOrDefault(x => x.Name == "FromName")?.Description;
        var fromAddress = email.FromAddress ?? config.FirstOrDefault(x => x.Name == "FromAddress")?.Description;
        var password = config.FirstOrDefault(x => x.Name == "Password")?.Description ?? throw new ApiException("Email server is not authorzied") { StatusCode = HttpStatusCode.InternalServerError };
        var server = config.FirstOrDefault(x => x.Name == "Server").Description ?? throw new ApiException("Email server is not authorzied") { StatusCode = HttpStatusCode.InternalServerError };
        var strPort = config.FirstOrDefault(x => x.Name == "Port")?.Description;
        var strSSL = config.FirstOrDefault(x => x.Name == "SSL")?.Description;
        var port = strPort.TryParseInt();
        var ssl = strSSL.TryParseBool();
        await email.SendMailAsync(fromName, fromAddress, password, server, port ?? 587, ssl ?? false, webRoot);
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

    public async Task<bool> SignOutAsync(Token token)
    {
        if (token is null)
        {
            throw new ApiException("Token is required");
        }
        var principal = UserUtils.GetPrincipalFromAccessToken(token.AccessToken, _cfg);
        var sessionId = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
        var ipAddress = GetRemoteIpAddress(_ctx.HttpContext);
        var query = $"select * from UserLogin where Id = '{sessionId}'";
        var connStr = await GetConnStrFromKey(token.ConnKey);
        var userLogin = await ReadDsAs<UserLogin>(query, connStr);
        if (userLogin is null) return true;
        await SavePatch(new PatchVM
        {
            Table = nameof(UserLogin),
            Changes =
            [
                new PatchDetail { Field = nameof(UserLogin.Id), OldVal = userLogin.Id },
                new PatchDetail { Field = nameof(UserLogin.ExpiredDate), Value = DateTimeOffset.Now.ToISOFormat() },
            ]
        });
        return true;
    }

    public async Task<bool> ForgotPassword(LoginVM login)
    {
        login.CachedConnStr ??= await GetConnStrFromKey(login.ConnKey, login.CompanyName, login.Env);
        var user = await ReadDsAs<User>($"select * from [User] where UserName = '{login.UserName}'", login.CachedConnStr);
        var span = DateTimeOffset.Now - (user.UpdatedDate ?? DateTimeOffset.Now);
        if (user.LoginFailedCount >= MAX_LOGIN && span.TotalMinutes < 5)
        {
            throw new ApiException($"The account {login.UserName} has been locked for a while! Please contact your administrator to unlock.");
        }
        // Send mail
        var emailTemplate = await ReadDsAs<MasterData>($"select * from MasterData where Name = 'ForgotPassEmail'", login.CachedConnStr)
            ?? throw new InvalidOperationException("Cannot find recovery email template!");
        var oneClickLink = GenerateRandomToken();
        user.Recover = oneClickLink;
        await SavePatch(new PatchVM
        {
            CachedConnStr = login.CachedConnStr,
            Table = nameof(User),
            Changes = [new PatchDetail { Field = nameof(User.Recover), Value = oneClickLink }],
        });
        var email = new EmailVM
        {
            ToAddresses = [user.Email],
            Subject = "Email recovery",
            Body = Utils.FormatEntity(emailTemplate.Description, user)
        };
        await SendMail(email, login.CachedConnStr);
        return true;
    }

    public async Task<string> ResendUser(SqlViewModel vm)
    {
        vm.CachedConnStr ??= await GetConnStrFromKey(vm.ConnKey);
        var user = await ReadDsAs<User>($"select * from [User] where Id in ({vm.Ids.CombineStrings()})", vm.CachedConnStr);
        user.Salt = GenerateRandomToken();
        var randomPassword = GenerateRandomToken(10);
        user.Password = GetHash(UserUtils.sHA256, randomPassword + user.Salt);
        List<PatchDetail> changes =
        [
            new PatchDetail { Field = nameof(User.Id), OldVal = user.Id },
            new PatchDetail { Field = nameof(User.Salt), Value = user.Salt },
            new PatchDetail { Field = nameof(User.Password), Value = user.Password },
        ];
        await SavePatch(new PatchVM
        {
            CachedConnStr = vm.CachedConnStr,
            Table = nameof(User),
            Changes = changes,
        });
        return randomPassword;
    }

    private static async Task WriteTemplateAsync(HttpResponse reponse, TenantPage page, string env, string tenant)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(page.Template);

        var links = htmlDoc.DocumentNode.SelectNodes("//link | //script")
            .SelectForEach((HtmlNode x, int i) =>
            {
                ShouldAddVersion(x, href);
                ShouldAddVersion(x, src);
            });
        var meta = new HtmlNode(HtmlNodeType.Element, htmlDoc, 1)
        {
            Name = "meta"
        };
        meta.SetAttributeValue("name", "startupSvc");
        meta.SetAttributeValue("content", page.SvcId);
        htmlDoc.DocumentNode.SelectSingleNode("//head")?.AppendChild(meta);
        reponse.Headers.TryAdd(ContentType, Utils.GetMimeType("html"));
        reponse.StatusCode = (int)HttpStatusCode.OK;
        await reponse.WriteAsync(htmlDoc.DocumentNode.OuterHtml);
    }

    private static void ShouldAddVersion(HtmlNode x, string attr)
    {
        var shouldAdd = x.Attributes.Contains(attr)
            && x.Attributes[attr].Value.IndexOf("?v=") < 0;
        if (shouldAdd)
        {
            x.Attributes[attr].Value += "?v=" + Guid.NewGuid().ToString();
        }
    }

    private async Task WriteDefaultFile(string file, string contentType
        , HttpStatusCode code = HttpStatusCode.OK)
    {
        var response = _ctx.HttpContext.Response;
        if (!response.HasStarted)
        {
            response.Headers.TryAdd(ContentType, contentType);
            response.Headers.TryAdd("Content-Encoding", "gzip");
            response.StatusCode = (int)code;
        }
        var html = await File.ReadAllTextAsync(file, encoding: Encoding.UTF8);
        await response.WriteAsync(html);
    }

    public async Task Launch(string tenant, string area, string env)
    {
        if (TenantCode != null && TenantCode != tenant)
        {
            throw new UnauthorizedAccessException($"Page not found for the tanent {tenant} due to the current user was signed in with the tenant {TenantCode}.");
        }
        var request = _ctx.HttpContext.Request;
        var response = _ctx.HttpContext.Response;
        var ext = Path.GetExtension(request.Path);
        if (!ext.IsNullOrWhiteSpace())
        {
            await response.WriteAsync("File not found");
            return;
        }
        var htmlMimeType = Utils.GetMimeType("html");
        var key = $"{tenant}_{env}_{area}";
        var cache = await _cache.GetStringAsync(key);
        if (cache != null)
        {
            var pageCached = JsonConvert.DeserializeObject<TenantPage>(cache);
            await WriteTemplateAsync(response, pageCached, env, tenant);
            return;
        }
        var envQuery = $"select * from TenantEnv where TenantCode = '{tenant}' and Env = '{env}'";
        var connStr = _cfg.GetConnectionString(Utils.ConnKey);
        var tnEnv = await ReadDsAs<TenantEnv>(envQuery, _cfg.GetConnectionString(Utils.ConnKey));
        if (tnEnv is null)
        {
            await WriteDefaultFile(NotFoundFile, htmlMimeType, HttpStatusCode.NotFound);
            return;
        }
        var pageQuery = $"select * from TenantPage where TenantEnvId = '{tnEnv.Id}' and Area = '{area}'";
        var page = await ReadDsAs<TenantPage>(pageQuery, connStr);
        await _cache.SetStringAsync(key, JsonConvert.SerializeObject(page), Utils.CacheTTL);
        await WriteTemplateAsync(response, page, env: env, tenant: tenant);
    }

    public async Task<bool> CloneFeature(SqlViewModel vm)
    {
        if (vm.Ids.Nothing())
        {
            return false;
        }
        var connStr = vm.CachedConnStr ?? await GetConnStrFromKey(vm.ConnKey);
        var id = vm.Ids.Combine();
        var query = @$"select * from Feature where Id = '{id}';
            select * from FeaturePolicy where FeatureId = '{id}';
            select * from ComponentGroup where FeatureId = '{id}';
            select * from Component c left join ComponentGroup g on c.ComponentGroupId = g.Id
            where g.FeatureId = '{id}' or c.FeatureId = '{id}'";
        var ds = await ReadDataSet(query, connStr);
        if (ds.Length == 0 || ds[0].Length == 0) return false;
        var feature = ds[0][0].MapTo<Feature>();
        var policies = ds.Length > 1 ? ds[1].Select(x => x.MapTo<FeaturePolicy>()).ToList() : [];
        var groups = ds.Length > 2 ? ds[2].Select(x => x.MapTo<ComponentGroup>()).ToList() : [];
        var components = ds.Length > 3 ? ds[3].Select(x => x.MapTo<Component>()).ToList() : [];

        feature.Id = Id.NewGuid().ToString();
        var featurePatch = feature.MapToPatch();
        var policyPatches = policies.Select(x =>
        {
            x.Id = Id.NewGuid().ToString();
            x.FeatureId = feature.Id;
            return x.MapToPatch();
        }).ToArray();
        var groupMap = groups.DistinctBy(x => x.Id).ToDictionary(x => x.Id);
        var visited = new HashSet<ComponentGroup>();
        var comVisited = new HashSet<Component>();
        var groupPatches = groups.Select(group =>
        {
            var notVisited = visited.Add(group);
            var parentGroup = group.ParentId is null ? null : groupMap.GetValueOrDefault(group.ParentId);
            if (parentGroup is not null && visited.Contains(parentGroup))
            {
                group.ParentId = parentGroup.Id;
            }
            else if (parentGroup is not null)
            {
                CloneComponentToGroup(parentGroup, components);
                visited.Add(parentGroup);
            }

            if (notVisited) CloneComponentToGroup(group, components);
            return group.MapToPatch();
        }).ToArray();
        var standAloneCom = components.Except(groups.SelectMany(x => x.Component)).SelectForEach(x =>
        {
            x.Id = Id.NewGuid();
        });
        var comPatches = components.Select(x => x.MapToPatch()).ToArray();
        PatchVM[] patches = [
            featurePatch, ..policyPatches, ..groupPatches, ..comPatches
        ];
        patches.SelectForEach(x => x.CachedConnStr = connStr);
        await SavePatches(patches);

        return true;
    }

    private static void CloneComponentToGroup(ComponentGroup group, List<Component> components)
    {
        var com = components.Where(c => c.ComponentGroupId == group.Id).ToList();
        group.Id = Id.NewGuid().ToString();
        group.Component = com;
        com.ForEach(c =>
        {
            c.Id = Id.NewGuid().ToString();
            c.ComponentGroupId = group.Id;
        });
    }
}