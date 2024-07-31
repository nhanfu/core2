using ClosedXML.Excel;
using Core.Exceptions;
using Core.Extensions;
using Core.Middlewares;
using Core.Models;
using Core.ViewModels;
using CoreAPI.Services.Sql;
using DocumentFormat.OpenXml.Vml.Office;
using Elsa.Common.Entities;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using PuppeteerSharp;
using System.Buffers;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Net.WebSockets;
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
    public readonly IHttpContextAccessor _ctx;
    private readonly HttpRequest _request;
    private readonly IConfiguration _cfg;
    private readonly IDistributedCache _cache;
    private readonly IWebHostEnvironment _host;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WebSocketService _taskSocketSvc;
    private readonly IConfiguration _configuration;
    public readonly ISqlProvider _sql;

    public string UserId { get; set; }
    public string UserName { get; set; }
    public string ConnKey { get; set; }
    public string BranchId { get; set; }
    public List<string> CenterIds { get; set; }
    public string VendorId { get; set; }
    public string Env { get; set; }
    public string TenantCode { get; set; }
    public List<string> RoleIds { get; set; }
    public List<string> RoleNames { get; set; }

    public UserService(IHttpContextAccessor ctx, IConfiguration conf, IDistributedCache cache, IWebHostEnvironment host,
        IHttpClientFactory httpClientFactory, WebSocketService taskSocket, ISqlProvider sql, IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cfg = conf ?? throw new ArgumentNullException(nameof(conf));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache)); ;
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _taskSocketSvc = taskSocket ?? throw new ArgumentNullException(nameof(taskSocket));
        _request = _ctx.HttpContext.Request;
        ExtractMeta();
        _sql = sql;
        SetMetaToSqlProvider(_sql);
    }

    public void SetMetaToSqlProvider(ISqlProvider _sql)
    {
        _sql.TenantCode = TenantCode;
        _sql.Env = Env;
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
        ConnKey = claims.FirstOrDefault(x => x.Type == UserServiceHelpers.ConnKeyClaim)?.Value;
        UserName = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
        CenterIds = claims.Where(x => x.Type == nameof(CenterIds)).Select(x => x.Value).Where(x => x != null).ToList();
        RoleIds = claims.Where(x => x.Type == "RoleIds").Select(x => x.Value).Where(x => x != null).ToList();
        RoleNames = claims.Where(x => x.Type == UserServiceHelpers.RoleNameClaim).Select(x => x.Value).Where(x => x != null).ToList();
        VendorId = claims.FirstOrDefault(x => x.Type == ClaimTypes.GroupSid)?.Value;
        TenantCode = claims.FirstOrDefault(x => x.Type == UserServiceHelpers.TenantClaim)?.Value.ToUpper();
        Env = claims.FirstOrDefault(x => x.Type == UserServiceHelpers.EnvClaim)?.Value.ToUpper();
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

    public static string GetRemoteIpAddress(HttpContext context)
    {
        return context.Request.Headers.TryGetValue(UserServiceHelpers.ForwardedIP, out var value)
            ? value.ToString().Split(',')[0].Trim()
            : context.Connection.RemoteIpAddress.ToString();
    }

    private string DefaultConnStr() => _cfg.GetConnectionString(Utils.ConnKey);

    public async Task<Token> SignInAsync(LoginVM login)
    {
        if (login.TanentCode.HasAnyChar())
        {
            login.TanentCode = login.TanentCode.Trim();
        }
        var (matchedUser, roles) = await GetUserByLogin(login);
        if (matchedUser is null)
        {
            throw new ApiException($"Sai mật khẩu hoặc tên đăng nhập.<br /> Vui lòng đăng nhập lại!")
            {
                StatusCode = HttpStatusCode.BadRequest
            };
        }
        if (matchedUser.LoginFailedCount >= UserServiceHelpers.MAX_LOGIN && matchedUser.LastFailedLogin < DateTime.Now.AddMinutes(5))
        {
            throw new ApiException($"Tài khoản {login.UserName} đã bị khóa trong 5 phút!")
            {
                StatusCode = HttpStatusCode.Conflict
            };
        }
        var hashedPassword = GetHash(Utils.SHA256, login.Password + matchedUser.Salt);
        var matchPassword = matchedUser.Password == hashedPassword;
        List<PatchDetail> changes = [new PatchDetail { Field = UserServiceHelpers.IdField, OldVal = matchedUser.Id }];
        if (!matchPassword)
        {
            var loginFailedCount = matchedUser.LoginFailedCount.HasValue ? matchedUser.LoginFailedCount + 1 : 1;
            changes.Add(new PatchDetail { Field = nameof(User.LastFailedLogin), Value = DateTime.Now.ToISOFormat() });
            changes.Add(new PatchDetail { Field = nameof(User.LoginFailedCount), Value = loginFailedCount.ToString() });
        }
        else
        {
            matchedUser.LastLogin = DateTime.Now;
            matchedUser.LoginFailedCount = 0;
            changes.Add(new PatchDetail { Field = nameof(User.LastLogin), Value = DateTime.Now.ToISOFormat() });
            changes.Add(new PatchDetail { Field = nameof(User.LoginFailedCount), Value = 0.ToString() });
        }
        await SavePatch(new PatchVM
        {
            Table = nameof(User),
            TenantCode = login.TanentCode,
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
        select u.* from [User] u 
        where u.Active = 1 and u.Username = @username;
        select r.* from [User] u 
        left join [UserRole] ur on u.Id = ur.UserId
        left join [Role] r on ur.RoleId = r.Id
        where u.Active = 1 and u.Username = @username";
        var ds = await _sql.ReadDataSet(query);
        var userDb = ds.Length > 0 && ds[0].Length > 0 ? ds[0][0].MapTo<User>() : null;
        var roles = ds.Length > 1 && ds[1].Length > 0 ? ds[1].Select(x => x.MapTo<Role>()).ToArray() : null;
        return (userDb, roles);
    }

    public async Task<Dictionary<string, object>[]> GetDictionary()
    {
        var query = @$"select * from [Dictionary]";
        var ds = await _sql.ReadDataSet(query, _configuration.GetConnectionString("Default"));
        return ds[0];
    }

    public async Task<SqlResult> Go(SqlViewModel sqlViewModel)
    {
        var query = @$"select * from [{sqlViewModel.Table}] where Id in ({sqlViewModel.Id.CombineStrings()})";
        var ds = await _sql.ReadDataSet(query, _configuration.GetConnectionString("Default"));
        return new SqlResult()
        {
            data = ds[0],
            status = 200,
            message = "Select successful"
        };
    }

    public async Task<Dictionary<string, object>[]> GetMenu()
    {
        var query = @$"select * from [Feature] where IsMenu = 1";
        var ds = await _sql.ReadDataSet(query, _configuration.GetConnectionString("Default"));
        return ds[0];
    }


    public async Task<Feature> GetFeature(string name)
    {
        var query = @$"select * from [Feature] where Name = N'{name}'";
        var feature = await _sql.ReadDsAs<Feature>(query, _configuration.GetConnectionString("Default"));
        var query2 = @$"select * from [Component] where FeatureId = '{feature.Id}'
        select * from [FeaturePolicy] where FeatureId = '{feature.Id}'";
        var childs = await _sql.ReadDataSet(query2, _configuration.GetConnectionString("Default"));
        var components = childs.Length > 0 && childs[0].Length > 0 ? childs[0].Select(x => x.MapTo<Component>()).ToList() : null;
        var policys = childs.Length > 1 && childs[1].Length > 0 ? childs[1].Select(x => x.MapTo<FeaturePolicy>()).ToList() : null;
        feature.Components = components;
        var filteredComponentGroups = feature.Components
            .Where(component => component.ComponentType == "Section")
            .ToList();
        filteredComponentGroups.ForEach(group =>
        {
            group.Components = feature.Components.Where(c => c.ComponentGroupId == group.Id).ToList();
        });
        feature.ComponentGroup = filteredComponentGroups;
        feature.FeaturePolicies = policys;
        feature.GridPolicies = feature.Components.Where(component => component.ComponentGroupId == null && component.EntityId != null).ToList();
        return feature;
    }

    protected async Task<Token> GetUserToken(User user, Role[] roles, LoginVM login, string refreshToken = null)
    {
        if (user is null)
        {
            return null;
        }
        var roleIds = roles.Select(x => x.Id).Distinct().ToList();
        var roleNames = roles.Select(x => x.Name).Distinct().ToList();
        var signinDate = DateTime.Now;
        var jit = Uuid7.Guid().ToString();
        List<Claim> claims =
        [
            new(ClaimTypes.GroupSid, user.PartnerId.ToString()),
            new ("UserId", user.Id.ToString()),
            new (ClaimTypes.Name, user.UserName),
            new (JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new (JwtRegisteredClaimNames.Birthdate, user.Dob?.ToString() ?? string.Empty),
        ];
        List<Claim> claim2 = [
            new (JwtRegisteredClaimNames.FamilyName, user.FullName?? string.Empty),
            new (JwtRegisteredClaimNames.Iat, signinDate.ToString()),
            new (JwtRegisteredClaimNames.Jti, jit),
            new (UserServiceHelpers.TenantClaim, login.TanentCode),
        ];
        claims.AddRange(claim2);
        claims.AddRange(roleIds.Select(x => new Claim("RoleIds", x.ToString())));
        claims.AddRange(roleNames.Select(x => new Claim(UserServiceHelpers.RoleNameClaim, x.ToString())));
        var newLogin = refreshToken is null;
        refreshToken ??= GenerateRandomToken();
        var (token, exp) = AccessToken(claims);
        var res = JsonToken(user, roles, login.TanentCode, refreshToken, token, exp, signinDate);
        if (!newLogin || !login.AutoSignIn)
        {
            return res;
        }
        var userLogin = new UserLogin
        {
            Id = jit,
            UserId = user.Id,
            IpAddress = GetRemoteIpAddress(_ctx.HttpContext),
            RefreshToken = refreshToken,
            RefreshTokenExp = res.RefreshTokenExp,
            InsertedDate = signinDate,
        };
        var patch = userLogin.MapToPatch();
        patch.TenantCode = login.TanentCode;
        await SavePatch(patch);
        return res;
    }

    public (JwtSecurityToken, DateTime) AccessToken(IEnumerable<Claim> claims, DateTime? expire = null)
    {
        var exp = expire ?? DateTime.Now.AddDays(1);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Tokens:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _cfg["Tokens:Issuer"],
            _cfg["Tokens:Issuer"],
            claims,
            expires: exp,
            signingCredentials: creds);
        return (token, exp);
    }

    private static Token JsonToken(User user, Role[] roles, string tanent, string refreshToken,
        JwtSecurityToken token, DateTime exp, DateTime signinDate)
    {
        var vendor = new Partner();
        return new Token
        {
            UserId = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Address = user.Address,
            Avatar = user.Avatar,
            PhoneNumber = user.PhoneNumber,
            Ssn = user.Ssn,
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            AccessTokenExp = exp,
            RefreshTokenExp = DateTime.Now.AddYears(1),
            RefreshToken = refreshToken,
            RoleIds = roles.Select(x => x.Id).ToList(),
            RoleNames = roles.Select(x => x.Name).ToList(),
            Vendor = vendor,
            TenantCode = tanent,
            SigninDate = signinDate,
        };
    }

    private static void EnsureTokenParam(params string[] claims)
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
        var principal = Utils.GetPrincipalFromAccessToken(token.AccessToken, _cfg);
        var userId = principal.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        var userName = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
        var tenant = principal.Claims.FirstOrDefault(x => x.Type == UserServiceHelpers.TenantClaim)?.Value;
        EnsureTokenParam(userId, userName, tenant);
        var query =
            @$"select * from UserLogin 
            where UserId = '{userId}' and RefreshToken = '{token.RefreshToken}'
            and RefreshTokenExp > '{DateTime.Now}' order by InsertedDate desc";
        var userLogin = await _sql.ReadDsAs<UserLogin>(query);

        if (userLogin == null)
        {
            return null;
        }
        var login = new LoginVM
        {
            TanentCode = tenant,
            UserName = userName,
        };
        var (updatedUser, roles) = await GetUserByLogin(login);
        return await GetUserToken(updatedUser, roles, login, token.RefreshToken);
    }

    public async Task<SqlQueryResult> RunJs(SqlViewModel vm)
    {
        SqlQueryResult result = new();
        var engine = new TopazEngine();
        engine.SetValue("JSON", new JSONObject());
        engine.AddType<HttpClient>("HttpClient");
        engine.AddNamespace("System");
        engine.AddNamespace("Core.ViewModels");
        engine.AddNamespace("Core.Models");
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
        if (res is SqlQueryResult final) return final;
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

    public async Task<bool> HardDelete(PatchVM vm)
    {
        var sql = vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})");
        try
        {
            await _sql.RunSqlCmd(null, sql.Combine(";"));
        }
        catch
        {
            return false;
        }
        return true;
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

    public async Task<int> SavePatch(PatchVM vm)
    {
        vm.CachedDataConn ??= await _sql.GetConnStrFromKey(vm.DataConn, vm.TenantCode, vm.Env);
        vm.CachedMetaConn ??= await _sql.GetConnStrFromKey(vm.MetaConn, vm.TenantCode, vm.Env);
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

    public async Task<SqlResult> SendEntity(PatchVM vm)
    {
        var name = vm.Name ?? vm.Table;
        var rs = await SavePatch2(vm);
        var id = vm.Changes.FirstOrDefault(x => x.Field == "Id").Value;
        var query2 = @$"SELECT * FROM ApprovalConfig where TableName = '{name}' order by Level asc";
        var approvalConfig = await _sql.ReadDsAsArr<ApprovalConfig>(query2);
        if (approvalConfig.Nothing())
        {
            return new SqlResult()
            {
                status = 500
            };
        }
        var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
        if (matchApprovalConfig is null)
        {
            return new SqlResult()
            {
                status = 500,
            };
        }
        var sqlUser = matchApprovalConfig.SqlApprovedUser;
        var user = await _sql.ReadDsAsArr<User>(Utils.FormatEntity(sqlUser, rs.updatedItem[0]));
        var task = user.Select(x => new TaskNotification()
        {
            Id = Uuid7.Guid().ToString(),
            EntityId = name,
            Title = Utils.FormatEntity(matchApprovalConfig.TitleSend ?? "You have request approved", rs.updatedItem[0]),
            Description = Utils.FormatEntity(matchApprovalConfig.DescriptionSend ?? "You have request approved", rs.updatedItem[0]),
            InsertedBy = UserId,
            RecordId = id,
            InsertedDate = new DateTime(),
            AssignedId = x.Id
        }).ToList();
        foreach (var item in task)
        {
            var patch = item.MapToPatch();
            await SavePatch(patch);
        }
        await NotifyDevices(task, "RequestApprove");
        return new SqlResult()
        {
            status = 200,
            updatedItem = rs.updatedItem
        };
    }

    public async Task<SqlResult> ApprovedEntity(PatchVM vm)
    {
        var now = DateTime.Now;
        var name = vm.Name ?? vm.Table;
        var rs = await SavePatch2(vm);
        var id = vm.Changes.FirstOrDefault(x => x.Field == "Id").Value;
        var query2 = @$"SELECT * FROM ApprovalConfig where TableName = '{name}' order by Level asc";
        var approvalConfig = await _sql.ReadDsAsArr<ApprovalConfig>(query2);
        if (approvalConfig.Nothing())
        {
            return new SqlResult()
            {
                status = 500
            };
        }
        var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
        if (matchApprovalConfig is null)
        {
            return new SqlResult()
            {
                status = 500
            };
        }
        var maxLevel = approvalConfig.Max(x => x.Level);
        var queryApprovement = @$"SELECT * FROM Approvement where Name = '{name}' and RecordId = '{id}' order by CurrentLevel asc";
        var approvements = await _sql.ReadDsAsArr<Approvement>(queryApprovement);
        if (approvements.Any(x => x.CurrentLevel == maxLevel))
        {
            var config = approvalConfig.FirstOrDefault(x => x.Level == maxLevel);
            var sqlEndApprovedUser = Utils.FormatEntity(config.SqlSendUser, rs.updatedItem[0]);
            var userEndApproved = await _sql.ReadDsAsArr<User>(sqlEndApprovedUser);
            vm.Changes.FirstOrDefault(x => x.Field == "StatusId").Value = "3";
            rs = await SavePatch2(vm);
            var task = userEndApproved.Select(x => new TaskNotification()
            {
                Id = Uuid7.Guid().ToString(),
                EntityId = vm.Table,
                Title = Utils.FormatEntity(matchApprovalConfig.TitleApproved ?? "Request is approved", rs.updatedItem[0]),
                Description = Utils.FormatEntity(matchApprovalConfig.DescriptionApproved ?? "Request is approved", rs.updatedItem[0]),
                InsertedBy = UserId,
                RecordId = id,
                InsertedDate = new DateTime(),
                AssignedId = x.Id
            }).ToList();
            foreach (var item in task)
            {
                var patch = item.MapToPatch();
                await SavePatch(patch);
            }
            return new SqlResult()
            {
                status = 200,
                message = "Your data has been approved.",
                updatedItem = rs.updatedItem
            };
        }
        var currentLevel = approvements.FirstOrDefault()?.CurrentLevel ?? 1;
        var currentConfig = approvalConfig.FirstOrDefault(x => x.Level == currentLevel + 1);
        var sqlUser = Utils.FormatEntity(matchApprovalConfig.SqlApprovedUser, rs.updatedItem[0]);
        var user = await _sql.ReadDsAsArr<User>(sqlUser);
        var ids = user.Select(x => x.Id).ToList();
        if (!ids.Contains(UserId))
        {
            return new SqlResult()
            {
                status = 500,
                message = "You do not have permission to browse the data"
            };
        }
        var approval = new Approvement
        {
            Id = Uuid7.Guid().ToString(),
            Approved = true,
            CurrentLevel = currentConfig.Level,
            NextLevel = currentConfig.Level + 1,
            Name = name,
            RecordId = id,
            StatusId = 3,
            UserApproveId = UserId,
            ApprovedBy = UserId,
            ApprovedDate = now,
            InsertedBy = UserId,
            InsertedDate = now
        };
        var patchQpproval = approval.MapToPatch();
        await SavePatch(patchQpproval);
        var sqlSendUser = Utils.FormatEntity(matchApprovalConfig.SqlSendUser, rs.updatedItem[0]);
        var userApproved = await _sql.ReadDsAsArr<User>(sqlSendUser);
        if (approvalConfig.Where(x => x.Level == currentLevel + 1).Nothing())
        {
            vm.Changes.FirstOrDefault(x => x.Field == "StatusId").Value = "3";
            rs = await SavePatch2(vm);
            var task = userApproved.Select(x => new TaskNotification()
            {
                Id = Uuid7.Guid().ToString(),
                EntityId = vm.Table,
                Title = Utils.FormatEntity(matchApprovalConfig.TitleApproved ?? "Request is approved", rs.updatedItem[0]),
                Description = Utils.FormatEntity(matchApprovalConfig.DescriptionApproved ?? "Request is approved", rs.updatedItem[0]),
                RecordId = id,
                InsertedBy = UserId,
                InsertedDate = new DateTime(),
                AssignedId = x.Id
            }).ToList();
            foreach (var item in task)
            {
                var patch = item.MapToPatch();
                await SavePatch(patch);
            }
            await NotifyDevices(task, "Approved");
        }
        else
        {
            var task = user.Select(x => new TaskNotification()
            {
                Id = Uuid7.Guid().ToString(),
                EntityId = vm.Table,
                Title = Utils.FormatEntity(matchApprovalConfig.TitleSend ?? "You have request approved", rs.updatedItem[0]),
                Description = Utils.FormatEntity(matchApprovalConfig.DescriptionSend ?? "You have request approved", rs.updatedItem[0]),
                RecordId = id,
                InsertedBy = UserId,
                InsertedDate = new DateTime(),
                AssignedId = x.Id
            }).ToList();
            foreach (var item in task)
            {
                var patch = item.MapToPatch();
                await SavePatch(patch);
            }
            await NotifyDevices(task, "Approved");
        }
        return new SqlResult()
        {
            status = 200,
            updatedItem = rs.updatedItem
        };
    }

    public async Task<SqlResult> DeclineEntity(PatchVM vm)
    {
        vm.Changes.FirstOrDefault(x => x.Field == "StatusId").Value = "4";
        var now = DateTime.Now;
        var name = vm.Name ?? vm.Table;
        var rs = await SavePatch2(vm);
        var id = vm.Changes.FirstOrDefault(x => x.Field == "Id").Value;
        var query2 = @$"SELECT * FROM ApprovalConfig where TableName = '{name}' order by Level asc";
        var approvalConfig = await _sql.ReadDsAsArr<ApprovalConfig>(query2);
        if (approvalConfig.Nothing())
        {
            return new SqlResult()
            {
                status = 500,
                message = "Please config approved"
            };
        }
        var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
        if (matchApprovalConfig is null)
        {
            return new SqlResult()
            {
                status = 500,
                message = "Please config approved"
            };
        }
        var maxLevel = approvalConfig.Max(x => x.Level);
        var queryApprovement = @$"SELECT * FROM Approvement where Name = '{name}' and RecordId = '{id}' order by CurrentLevel asc";
        var approvements = await _sql.ReadDsAsArr<Approvement>(queryApprovement);
        var currentLevel = approvements.FirstOrDefault()?.CurrentLevel ?? 1;
        var approval = new Approvement
        {
            Id = Uuid7.Guid().ToString(),
            Approved = true,
            CurrentLevel = currentLevel,
            NextLevel = 1,
            Name = name,
            RecordId = id,
            StatusId = 4,
            UserApproveId = UserId,
            ApprovedBy = UserId,
            ApprovedDate = now,
            InsertedBy = UserId,
            InsertedDate = now
        };
        var patchQpproval = approval.MapToPatch();
        await SavePatch(patchQpproval);
        var sqlSendUser = Utils.FormatEntity(matchApprovalConfig.SqlSendUser, rs.updatedItem[0]);
        var userSend = await _sql.ReadDsAsArr<User>(sqlSendUser);
        var task = userSend.Select(x => new TaskNotification()
        {
            Id = Uuid7.Guid().ToString(),
            EntityId = vm.Table,
            Title = Utils.FormatEntity(matchApprovalConfig.TitleDecline ?? "Request is decline", rs.updatedItem[0]),
            Description = Utils.FormatEntity(matchApprovalConfig.DescriptionDecline ?? "Request is decline", rs.updatedItem[0]),
            InsertedBy = UserId,
            RecordId = id,
            InsertedDate = new DateTime(),
            AssignedId = x.Id
        }).ToList();
        foreach (var item in task)
        {
            var patch = item.MapToPatch();
            await SavePatch(patch);
        }
        await NotifyDevices(task, "Decline");
        return new SqlResult()
        {
            status = 200,
            updatedItem = rs.updatedItem
        };
    }

    public async Task<SqlResult> SavePatch2(PatchVM vm)
    {
        var id = vm.Changes.FirstOrDefault(x => x.Field == "Id").Value;
        var tableColumns = (await GetTableColumns(vm.Table))[0];
        var filteredChanges = vm.Changes.Where(change => tableColumns.SelectMany(x => x.Values).Contains(change.Field)).ToList();
        var selectIds = new List<DetailData>();
        if (id.StartsWith("-"))
        {
            id = id.Substring(1);
            AddDefaultFields(filteredChanges, new List<PatchDetail>()
            {
                new PatchDetail { Field = "InsertedDate", Value = DateTime.Now.ToISOFormat() },
                new PatchDetail { Field = "InsertedBy", Value = UserId },
                new PatchDetail { Field = "UpdatedDate", Value = null },
                new PatchDetail { Field = "UpdatedBy", Value = null },
                new PatchDetail { Field = "Active", Value = "1" }
            });
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
            {
                await connection.OpenAsync();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Transaction = transaction;
                        command.Connection = connection;
                        var update = filteredChanges.Select(x => $"@{id.Replace("-", "") + x.Field.ToLower()}");
                        var cells = filteredChanges.Select(x => x.Field).ToList();
                        if (!vm.Delete.Nothing())
                        {
                            command.CommandText += vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})").Combine(";");
                        }
                        command.CommandText += $"INSERT into [{vm.Table}]([{cells.Combine("],[")}]) values({update.Combine()})";
                        foreach (var item in filteredChanges)
                        {
                            if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                            {
                                item.Value = item.Value.Substring(1);
                            }
                            command.Parameters.AddWithValue($"@{id.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                        }
                        int index = 1;
                        if (!vm.Detail.Nothing())
                        {
                            foreach (var detailArray in vm.Detail)
                            {
                                foreach (var detail in detailArray)
                                {
                                    var tableDetailColumns = (await GetTableColumns(detail.Table))[0];
                                    var idDetail = detail.Changes.FirstOrDefault(x => x.Field == "Id").Value;
                                    var filteredDetailChanges = detail.Changes.Where(change => tableDetailColumns.SelectMany(x => x.Values).Contains(change.Field)).ToList();
                                    if (idDetail.StartsWith("-"))
                                    {
                                        var updateDetail = filteredDetailChanges.Select(x => $"@{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                        var cellsDetails = filteredDetailChanges.Select(x => x.Field).ToList();
                                        command.CommandText += $";INSERT into [{detail.Table}]([{cellsDetails.Combine("],[")}]) values({updateDetail.Combine()})";
                                        foreach (var item in filteredDetailChanges)
                                        {
                                            if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                            {
                                                item.Value = item.Value.Substring(1);
                                            }
                                            command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                        }
                                    }
                                    else
                                    {
                                        filteredDetailChanges = filteredDetailChanges.Where(x => x.Field != "Id").ToList();
                                        var updateDetail = filteredDetailChanges.Select(x => $"[{x.Field}] = @{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                        command.CommandText += $";UPDATE [{detail.Table}] SET {updateDetail.Combine()} WHERE Id = '{idDetail}';";
                                        foreach (var item in filteredDetailChanges)
                                        {
                                            if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                            {
                                                item.Value = item.Value.Substring(1);
                                            }
                                            command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                        }
                                    }
                                }
                                selectIds.Add(new DetailData()
                                {
                                    Index = index,
                                    Table = detailArray[0].Table,
                                    ComId = detailArray[0].ComId,
                                    Ids = detailArray.SelectMany(x => x.Changes).Where(x => x.Field == "Id").Select(x => { return x.Value.StartsWith("-") ? x.Value.Substring(1) : x.Value; }).ToList(),
                                });
                                index++;
                            }
                        }
                        await command.ExecuteNonQueryAsync();
                        transaction.Commit();
                        var childs = new List<string>();
                        var sql = $"SELECT * FROM [{vm.Table}] where Id = '{id}'";
                        foreach (var item in selectIds)
                        {
                            sql += $";SELECT * FROM [{item.Table}] where Id in ({item.Ids.CombineStrings()})";
                        }
                        var entity = await _sql.ReadDataSet(sql);
                        selectIds.ForEach(x =>
                        {
                            x.Data = entity[x.Index];
                        });
                        return new SqlResult()
                        {
                            updatedItem = entity[0],
                            Detail = selectIds,
                            status = 200,
                            message = "create successfull"
                        };
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var entity = await _sql.ReadDataSet($"SELECT * FROM [{vm.Table}] where Id = '{id}'");
                    return new SqlResult()
                    {
                        updatedItem = entity[0],
                        status = 500,
                        message = ex.Message
                    };
                }
            }
        }
        else
        {
            AddDefaultFields(filteredChanges, new List<PatchDetail>()
            {
                new PatchDetail { Field = "UpdatedDate", Value = DateTime.Now.ToISOFormat()},
                new PatchDetail { Field = "UpdatedBy", Value = UserId },
            });
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
            {
                await connection.OpenAsync();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Transaction = transaction;
                        command.Connection = connection;
                        var updates = filteredChanges.Where(x => x.Field != "Id").ToList();
                        var update = updates.Select(x => $"[{x.Field}] = @{id.Replace("-", "") + x.Field.ToLower()}");
                        if (!vm.Delete.Nothing())
                        {
                            command.CommandText += vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})").Combine(";");
                        }
                        command.CommandText += $" UPDATE [{vm.Table}] SET {update.Combine()} WHERE Id = '{id}';";
                        foreach (var item in updates)
                        {
                            if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                            {
                                item.Value = item.Value.Substring(1);
                            }
                            command.Parameters.AddWithValue($"@{id.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                        }
                        var changes = updates.Where(x => !x.HistoryValue.IsNullOrWhiteSpace());
                        if (!changes.Nothing())
                        {
                            var history = changes.Select(x => x.HistoryValue).Combine("\n").Replace("'", "''");
                            if (!history.IsNullOrWhiteSpace())
                            {
                                command.CommandText += $" INSERT INTO [History](Id,TextContent,RecordId,TableName,Active,InsertedDate,InsertedBy) values('{Uuid7.Guid()}',N'{history}','{id}','{vm.Table}',1,'{DateTime.Now.ToISOFormat()}','{UserId}');";
                            }
                        }
                        int index = 1;
                        if (!vm.Detail.Nothing())
                        {
                            foreach (var detailArray in vm.Detail)
                            {
                                foreach (var detail in detailArray)
                                {
                                    var tableDetailColumns = (await GetTableColumns(detail.Table))[0];
                                    var idDetail = detail.Changes.FirstOrDefault(x => x.Field == "Id").Value;
                                    var filteredDetailChanges = detail.Changes.Where(change => tableDetailColumns.SelectMany(x => x.Values).Contains(change.Field)).ToList();
                                    if (idDetail.StartsWith("-"))
                                    {
                                        var updateDetail = filteredDetailChanges.Select(x => $"@{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                        var insertDetail = filteredDetailChanges.Select(x => $"[{x.Field}]").ToList();
                                        command.CommandText += $";INSERT into [{detail.Table}]({insertDetail.Combine()}) values({updateDetail.Combine()})";
                                        foreach (var item in filteredDetailChanges)
                                        {
                                            if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                            {
                                                item.Value = item.Value.Substring(1);
                                            }
                                            command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                        }
                                    }
                                    else
                                    {
                                        filteredDetailChanges = filteredDetailChanges.Where(x => x.Field != "Id").ToList();
                                        var updateDetail = filteredDetailChanges.Select(x => $"[{x.Field}] = @{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                        command.CommandText += $";UPDATE [{detail.Table}] SET {updateDetail.Combine()} WHERE Id = '{idDetail}';";
                                        foreach (var item in filteredDetailChanges)
                                        {
                                            if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                            {
                                                item.Value = item.Value.Substring(1);
                                            }
                                            command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                        }
                                    }
                                }
                                selectIds.Add(new DetailData()
                                {
                                    Index = index,
                                    Table = detailArray[0].Table,
                                    ComId = detailArray[0].ComId,
                                    Ids = detailArray.SelectMany(x => x.Changes).Where(x => x.Field == "Id").Select(x => { return x.Value.StartsWith("-") ? x.Value.Substring(1) : x.Value; }).ToList(),
                                });
                                index++;
                            }
                        }
                        await command.ExecuteNonQueryAsync();
                        transaction.Commit();
                        var sql = $"SELECT * FROM [{vm.Table}] where Id = '{id}'";
                        foreach (var item in selectIds)
                        {
                            sql += $";SELECT * FROM [{item.Table}] where Id in ({item.Ids.CombineStrings()})";
                        }
                        var entity = await _sql.ReadDataSet(sql);
                        selectIds.ForEach(x =>
                        {
                            x.Data = entity[x.Index];
                        });
                        return new SqlResult()
                        {
                            updatedItem = entity[0],
                            Detail = selectIds,
                            status = 200,
                            message = "update successfull"
                        };
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var entity = await _sql.ReadDataSet($"SELECT * FROM [{vm.Table}] where Id = '{id}'");
                    return new SqlResult()
                    {
                        updatedItem = entity[0],
                        status = 500,
                        message = ex.Message
                    };
                }
            }
        }
    }

    public async Task<Dictionary<string, object>[][]> GetTableColumns(string tableName)
    {
        string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
        return await _sql.ReadDataSet(query);
    }

    private async Task AfterActionSvc(PatchVM vm, string action)
    {
        var sql = new SqlViewModel
        {
            CachedDataConn = vm.CachedDataConn,
            CachedMetaConn = vm.CachedMetaConn,
            QueueName = vm.QueueName,
            ComId = vm.Table,
            Action = action,
        };
        var sv = await GetService(sql);
        if (sv is not null)
        {
            sql.JsScript = sv.Content;
            try
            {
                await RunJs(sql);
            }
            catch
            {

            }
        }
    }

    public async Task TryNotifyChanges(string action, string[] keys, params PatchVM[] patches)
    {
        if (patches.Nothing()) return;
        keys ??= patches.Select(vm =>
        {
            if (vm.CacheName.IsNullOrWhiteSpace())
            {
                var id = vm.Id.OldVal ?? vm.Id.Value;
                vm.CacheName = $"{vm.Table}{id}";
            }
            return vm.CacheName;
        }).ToArray();
        await TryInvalidCacheInternal(keys);
        await patches.ForEachAsync(vm =>
            vm.QueueName.IsNullOrWhiteSpace() ? null : TryNotifyDeviceInternal(new MQEvent
            {
                Id = Uuid7.Guid().ToString(),
                Action = action,
                Message = vm.ToJson(),
                QueueName = vm.QueueName
            }));
    }

    public async Task TryNotifyDeviceInternal(MQEvent e)
    {
        if (e is null || e.QueueName.IsNullOrWhiteSpace()) return;
        await NotifyDevice(e);
        var clusters = await GetClusters(role: "API");
        try
        {
            await NotifyOtherClusters(clusters, nameof(NotifyDevice), e.ToJson());
        }
        catch
        {
        }
    }

    private Task<HttpResponseMessage[]> NotifyOtherClusters(Cluster[] clusters, string path, string json)
    {
        var request = _ctx.HttpContext.Request;
        var client = _httpClientFactory.CreateClient();
        var otherClusters = GetOtherClusters(clusters, _request.Host.Value, UserServiceHelpers.Port);
        var tasks = otherClusters.Select(x =>
        {
            var uri = UserServiceHelpers.GetUri(x.Host, x.Port, x.Scheme, "/" + path);
            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(json, Encoding.UTF8, Utils.ApplicationJson)
            };
            CopyHeaders(request, HeaderNames.ContentLength, HeaderNames.ContentType);
            client.Timeout = TimeSpan.FromSeconds(5);
            return client.SendAsync(request);
        });
        return Task.WhenAll(tasks);
    }

    public void CopyHeaders(HttpRequestMessage request, params string[] excepts)
    {
        foreach (var headerKey in _ctx.HttpContext.Request.Headers.Keys.Except(excepts))
        {
            var headerValue = _ctx.HttpContext.Request.Headers[headerKey].ToArray();
            if (!request.Headers.TryAddWithoutValidation(headerKey, headerValue) && request.Content != null)
            {
                request.Content?.Headers.TryAddWithoutValidation(headerKey, headerValue);
            }
        }
    }

    private async Task<Cluster[]> GetClusters(string role)
    {
        Cluster[] clusters = null;
        var cachedCluster = await GetStringAsync(UserServiceHelpers.APIClusterKey);
        if (cachedCluster is not null)
        {
            clusters = cachedCluster.TryParse<Cluster[]>();
        }
        if (clusters is not null) return clusters;
        var clusterQuery = $"select * from [Cluster] where ClusterRole = '{role}'";
        clusters = await _sql.ReadDsAsArr<Cluster>(clusterQuery, DefaultConnStr());
        await SetStringAsync(UserServiceHelpers.APIClusterKey, clusters.ToJson(), Utils.CacheTTL);
        return clusters;
    }

    public async Task TryInvalidCacheInternal(params string[] keys)
    {
        if (keys.Nothing()) return;
        await Task.WhenAll(keys.Select(key => _cache.RemoveAsync(key)));
        var clusters = await GetClusters(role: "API");
        try
        {
            var mqEvent = new MQEvent { Action = "ClearCache", Message = keys }.ToJson();
            await NotifyOtherClusters(clusters, "api/cluster/action", mqEvent);
        }
        catch
        {
        }
    }

    private static Cluster[] GetOtherClusters(Cluster[] clusters, string host, int port)
    {
        var res = new List<Cluster>();
        for (int i = 0; i < clusters.Length; i++)
        {
            if (clusters[i].Host != host || clusters[i].Port != port) res.Add(clusters[i]);
        }
        return [.. res];
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
            var origin = @$"select t.* from [{vm.Table}] as t where t.Id = '{oldId}'";
            var ds = await _sql.ReadDataSet(origin, vm.CachedDataConn);
            var originRow = ds.Length > 0 && ds[0].Length > 0 ? ds[0][0] : null;
            var isOwner = Utils.IsOwner(originRow, UserId, RoleIds);
            writePerm = isOwner || allRights.Any(x => x.CanWriteAll);
        }
        return writePerm;
    }

    private async Task<string> UnauthorizedDeleteRecords(PatchVM vm)
    {
        if (vm.ByPassPerm) return null;
        var allRights = vm.ByPassPerm ? [] : await GetEntityPerm(vm.Table, recordId: null, vm.CachedMetaConn);
        var idField = vm.Delete;
        var origin = @$"select t.* from [{vm.Table}] as t where t.Id in ()";
        var ds = await _sql.ReadDataSet(origin, vm.CachedDataConn);
        var originRows = ds.Length > 0 && ds[0].Length > 0 ? ds[0] : null;
        return originRows.WhereNot(x =>
        {
            var isOwner = Utils.IsOwner(x, UserId, RoleIds);
            return isOwner || allRights.Any(x => x.CanDeleteAll);
        }).Select(x => x.GetValueOrDefault(Utils.IdField)).Combine();
    }

    public async Task<int> SavePatches(PatchVM[] patches)
    {
        if (patches.Nothing()) throw new ArgumentException($"{nameof(patches)} is null or empty");
        patches = patches.Where(x => x.Id is not null).ToArray();
        patches[0].CachedDataConn ??= await _sql.GetConnStrFromKey(patches[0].DataConn);
        patches[0].CachedMetaConn ??= await _sql.GetConnStrFromKey(patches[0].MetaConn);
        var tables = patches.Select(x => x.Table);
        string rightQuery = @$"select * from [FeaturePolicy] 
            where Active = 1 and (CanWrite = 1 or CanWriteAll = 1) and EntityName in ({tables.CombineStrings()}) and RoleId in ({RoleIds.CombineStrings()})";
        var permissions = await _sql.ReadDsAsArr<FeaturePolicy>(rightQuery, patches[0].CachedMetaConn);
        permissions = permissions.DistinctBy(x => x.TableName).ToArray();
        var lackPerTables = patches.Select(x => x.Table).Except(permissions.Select(x => x.TableName)).ToArray();
        if (lackPerTables.Length > 0)
        {
            throw new ApiException($"All table must have write permission {lackPerTables.CombineStrings()}")
            {
                StatusCode = HttpStatusCode.Unauthorized
            };
        }
        var sql = patches.Select(_sql.GetCreateOrUpdateCmd).Where(x => x is not null).Combine(";\n");
        var result = await _sql.RunSqlCmd(patches[0].CachedDataConn, sql);
        await TryNotifyChanges("Patch", null, patches);
        await patches.ForEachAsync(vm =>
        {
            vm.CachedDataConn ??= patches[0].CachedDataConn;
            vm.CachedMetaConn ??= patches[0].CachedMetaConn;
            return AfterActionSvc(vm, "AfterPatch");
        });
        return result;
    }

    public async Task<string[]> DeactivateAsync(SqlViewModel vm)
    {
        vm.CachedDataConn ??= await _sql.GetConnStrFromKey(vm.DataConn ?? "default");
        var allRights = await GetEntityPerm(vm.Table, null, vm.CachedDataConn);
        var canDeactivateAll = allRights.Any(x => x.CanDeactivateAll);
        var canDeactivateSelf = allRights.Any(x => x.CanDeactivate);
        var query = $"select * from [{vm.Table}] where Id in ({vm.Id.CombineStrings()})";
        var ds = await _sql.ReadDataSet(query, vm.CachedDataConn);
        var rows = ds.Length > 0 ? ds[0] : null;
        if (rows.Nothing()) return null;
        var canDeactivateRows = rows.Where(x =>
        {
            return canDeactivateAll || canDeactivateSelf && Utils.IsOwner(x, UserId, RoleIds);
        }).Select(x => x.GetValueOrDefault(Utils.IdField)?.ToString()).ToArray();
        if (canDeactivateRows.Nothing()) return null;
        var deactivateCmd = $"update {vm.Table} set Active = 0 where Id in ({canDeactivateRows.CombineStrings()})";
        await _sql.RunSqlCmd(vm.CachedDataConn, deactivateCmd);
        return canDeactivateRows;
    }

    public async Task<SqlComResult> ComQuery(SqlViewModel vm)
    {
        var com = await GetComponent(vm) ?? throw new ApiException("Component not found or not public to the current user")
        {
            StatusCode = HttpStatusCode.NotFound
        };
        var anyInvalid = UserServiceHelpers.FobiddenTerms.Any(term =>
        {
            return vm.Select != null && term.IsMatch(vm.Select.ToLower())
            || vm.Table != null && term.IsMatch(vm.Table.ToLower())
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
        return await RunjsWrap(vm);
    }

    private static string CalcFinalQuery(SqlViewModel vm)
    {
        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(vm.Params);
        var data = JsonConvert.DeserializeObject<SqlQuery>(vm.JsScript);
        data.total = Utils.FormatEntity(data.total, dictionary);
        data.sql = Utils.FormatEntity(data.sql, dictionary);
        var sqlSelect = data.sql;
        var sqlTotal = data.total;
        if (!vm.Where.IsNullOrWhiteSpace())
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
        if (!vm.OrderBy.IsNullOrWhiteSpace())
        {
            sqlSelect += $" ORDER BY {vm.OrderBy}";
        }
        if (vm.Skip != null)
        {
            sqlSelect += $" OFFSET {vm.Skip} ROWS";
        }
        if (vm.Top != null)
        {
            sqlSelect += $" FETCH NEXT {vm.Top} ROWS ONLY";
        }
        if (vm.Count)
        {
            sqlSelect += $"; {sqlTotal}";
        }
        return sqlSelect;
    }

    private async Task<Component> GetComponent(SqlViewModel vm)
    {
        Component com = null;
        var comKey = nameof(Component) + vm.ComId;
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
        if (com is null)
        {
            var query = @$"select top 1 * from [Component] where Id = '{vm.ComId}'";
            com = await _sql.ReadDsAs<Component>(query, vm.CachedMetaConn);
            if (com is null) return null;
            await SetStringAsync(comKey, JsonConvert.SerializeObject(com), Utils.CacheTTL);
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
            var q = @$"select * from [FeaturePolicy] 
            where Active = 1 and EntityName = '{entityName}'
            and (RecordId = '{recordId}' or '{recordId}' = '') and RoleId in ({RoleIds.CombineStrings()})";
            if (pre != null) q += $" and {permissionName} = 1";
            permissions = await _sql.ReadDsAsArr<FeaturePolicy>(q, connStr);
            await SetStringAsync(key, JsonConvert.SerializeObject(permissions), Utils.CacheTTL);
        }

        return permissions;
    }

    public async Task<SqlComResult> RunUserSvc(SqlViewModel vm)
    {
        vm.CachedDataConn ??= await _sql.GetConnStrFromKey(vm.DataConn, vm.AnnonymousTenant, vm.AnnonymousEnv);
        vm.CachedMetaConn ??= await _sql.GetConnStrFromKey(vm.MetaConn, vm.AnnonymousTenant, vm.AnnonymousEnv);
        var sv = await GetService(vm)
            ?? throw new ApiException($"Service Id - \"{vm.SvcId}\", ComId \"{vm.ComId}\" - Action \"{vm.Action}\" NOT found")
            {
                StatusCode = HttpStatusCode.NotFound
            };
        vm.JsScript = sv.Content;
        return await RunjsWrap(vm);
    }

    private async Task<SqlComResult> RunjsWrap(SqlViewModel vm)
    {
        var actQuery = CalcFinalQuery(vm);
        var ds = await _sql.ReadDataSet(actQuery);
        return new SqlComResult()
        {
            count = ds.Length > 1 && ds[1].Length > 0 ? Convert.ToInt32(ds[1][0]["total"]) : null,
            value = ds[0]
        };
    }

    private async Task<Models.Services> GetService(SqlViewModel vm)
    {
        if (vm is null || vm.SvcId.IsNullOrWhiteSpace() && (vm.Action.IsNullOrWhiteSpace() || vm.ComId.IsNullOrWhiteSpace()))
            throw new ApiException("Service can NOT be identified due to lack of Id or action")
            {
                StatusCode = HttpStatusCode.BadRequest
            };
        var tenant = TenantCode ?? vm.AnnonymousTenant;
        var key = $"{nameof(Services)}{vm.ComId}_{vm.Action}_{tenant}";
        var cacheSv = await GetStringAsync(key);
        if (cacheSv != null)
        {
            var res = cacheSv.TryParse<Models.Services>();
            if (res is not null)
            {
                EnsureSvPermission(res);
                return res;
            }
        }
        var query = @$"select * from [Services]
                where Active = 1 and (ComId = '{vm.ComId}' and Action = '{vm.Action}' or Id = '{vm.SvcId}') 
                and (TenantCode = '{TenantCode}' or Annonymous = 1 and TenantCode = '{vm.AnnonymousTenant}')";
        var sv = await _sql.ReadDsAs<Models.Services>(query, vm.CachedMetaConn);
        if (sv is null) return null;
        await SetStringAsync(key, sv.ToJson(), Utils.CacheTTL);
        EnsureSvPermission(sv);
        return sv;
    }

    private void EnsureSvPermission(Models.Services sv)
    {
        if (TenantCode is null && !sv.Annonymous)
        {
            throw new UnauthorizedAccessException("The service is required login");
        }
        var isValidRole = sv.IsPublicInTenant ||
                    (from svRole in sv.RoleIds.Split(',')
                     join usrRole in RoleIds on svRole equals usrRole
                     select svRole).Any();
        if (!isValidRole) throw new UnauthorizedAccessException("The service is not accessible by your roles");
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
                var field = header.FieldName;
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
        var url = $"{refName}{DateTime.Now:ddMMyyyyhhmm}.xlsx";
        worksheet.Columns().AdjustToContents();
        workbook.SaveAs($"wwwroot\\excel\\Download\\{url}");
        return url;
    }

    public async Task<string> PostImageAsync(IWebHostEnvironment host,
            string name = "Captured", bool reup = false)
    {
        var image = await Utils.ReadRequestBody(_ctx.HttpContext.Request, leaveOpen: false);
        var fileName = $"{Path.GetFileNameWithoutExtension(name)}{Path.GetExtension(name)}";
        var path = GetUploadPath(fileName, host.WebRootPath);
        EnsureDirectoryExist(path);
        path = reup ? IncreaseFileName(path) : path;
        await File.WriteAllBytesAsync(path, Convert.FromBase64String(image));
        return GetHttpPath(path, host.WebRootPath);
    }

    public async Task<string> PostFileAsync(IFormFile file, bool reup = false)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}-{Uuid7.Guid()}{Path.GetExtension(file.FileName)}";
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
            DataConn = connKey
        });
        var connStr = await _sql.GetConnStrFromKey(_configuration.GetConnectionString("Default"));
        var tableRights = await GetEntityPerm(table, recordId: null, connStr);
        if (!tableRights.Any(x => x.CanWriteAll))
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
        catch (Exception)
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
        var valueFields = vm.Changes.Where(x => !_sql.SystemFields.Contains(x.Field.ToLower())).ToList();
        var update = valueFields.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
        var now = DateTime.Now.ToString(DateTimeExt.DateFormat);

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
        using var browserFetcher = new BrowserFetcher() { Browser = SupportedBrowser.Chrome };
        await browserFetcher.DownloadAsync();
        var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await email.PdfText.ForEachAsync(async pdf =>
        {
            await page.SetContentAsync(pdf);
            var path = Path.Combine(host.WebRootPath, "download", UserId, Uuid7.Guid().ToString() + ".pdf");
            EnsureDirectoryExist(path);
            await page.PdfAsync(path);
            paths.Add(path);
        });
        return absolute ? paths : paths.Select(path => path.Replace(host.WebRootPath, string.Empty));
    }

    public async Task<bool> EmailAttached(EmailVM email, IWebHostEnvironment host)
    {
        var connStr = await _sql.GetConnStrFromKey(email.ConnKey);
        var paths = await GeneratePdf(email, host, absolute: true);
        paths.SelectForEach(email.ServerAttachements.Add);
        await SendMail(email, connStr, host.WebRootPath);
        return true;
    }

    public async Task SendMail(EmailVM email, string connStr = null, string webRoot = null)
    {
        var query = $"select * from [MasterData] m join [MasterData] p on m.ParentId = p.Id where p.Name = 'ConfigEmail'";
        var config = await _sql.ReadDsAsArr<MasterData>(query, connStr);
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
        var principal = Utils.GetPrincipalFromAccessToken(token.AccessToken, _cfg);
        var sessionId = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
        var ipAddress = GetRemoteIpAddress(_ctx.HttpContext);
        var query = $"select * from [UserLogin] where Id = '{sessionId}'";
        var connStr = await _sql.GetConnStrFromKey(token.ConnKey);
        var userLogin = await _sql.ReadDsAs<UserLogin>(query, connStr);
        if (userLogin is null) return true;
        await SavePatch(new PatchVM
        {
            Table = nameof(UserLogin),
            Changes =
            [
                new PatchDetail { Field = nameof(UserLogin.Id), OldVal = userLogin.Id },
                new PatchDetail { Field = nameof(UserLogin.AccessTokenExp), Value = DateTime.Now.ToISOFormat() },
            ]
        });
        return true;
    }

    public async Task<bool> ForgotPassword(LoginVM login)
    {
        var user = await _sql.ReadDsAs<User>($"select * from [User] where UserName = '{login.UserName}'");
        var span = DateTime.Now - (user.UpdatedDate ?? DateTime.Now);
        if (user.LoginFailedCount >= UserServiceHelpers.MAX_LOGIN && span.TotalMinutes < 5)
        {
            throw new ApiException($"The account {login.UserName} has been locked for a while! Please contact your administrator to unlock.");
        }
        // Send mail
        var emailTemplate = await _sql.ReadDsAs<MasterData>($"select * from [MasterData] where Name = 'ForgotPassEmail'")
            ?? throw new InvalidOperationException("Cannot find recovery email template!");
        var oneClickLink = GenerateRandomToken();
        user.Recover = oneClickLink;
        await SavePatch(new PatchVM
        {
            Table = nameof(User),
            Changes = [new PatchDetail { Field = nameof(User.Recover), Value = oneClickLink }],
        });
        var email = new EmailVM
        {
            ToAddresses = [user.Email],
            Subject = "Email recovery",
        };
        await SendMail(email);
        return true;
    }

    public async Task<string> ResendUser(SqlViewModel vm)
    {
        vm.CachedMetaConn ??= await _sql.GetConnStrFromKey(vm.MetaConn);
        vm.CachedDataConn ??= await _sql.GetConnStrFromKey(vm.DataConn);
        var user = await _sql.ReadDsAs<User>($"select * from [User] where Id in ({vm.Id.CombineStrings()})", vm.CachedMetaConn);
        user.Salt = GenerateRandomToken();
        var randomPassword = GenerateRandomToken(10);
        user.Password = GetHash(Utils.SHA256, randomPassword + user.Salt);
        List<PatchDetail> changes =
        [
            new PatchDetail { Field = nameof(User.Id), OldVal = user.Id },
            new PatchDetail { Field = nameof(User.Salt), Value = user.Salt },
            new PatchDetail { Field = nameof(User.Password), Value = user.Password },
        ];
        await SavePatch(new PatchVM
        {
            CachedDataConn = vm.CachedDataConn,
            CachedMetaConn = vm.CachedMetaConn,
            Table = nameof(User),
            Changes = changes,
        });
        return randomPassword;
    }

    private static async Task WriteTemplateAsync(HttpResponse reponse, Tenant page, string env, string tenant)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(page.Template);

        var links = htmlDoc.DocumentNode.SelectNodes("//link | //script")
            .SelectForEach((HtmlNode x, int i) =>
            {
                ShouldAddVersion(x, UserServiceHelpers.Href);
                ShouldAddVersion(x, UserServiceHelpers.Src);
            });
        var meta = new HtmlNode(HtmlNodeType.Element, htmlDoc, 1)
        {
            Name = "meta"
        };
        meta.SetAttributeValue("name", "startupSvc");
        meta.SetAttributeValue("content", page.SvcId);
        htmlDoc.DocumentNode.SelectSingleNode("//head")?.AppendChild(meta);
        reponse.Headers.TryAdd(HeaderNames.ContentType, Utils.GetMimeType("html"));
        reponse.StatusCode = (int)HttpStatusCode.OK;
        await reponse.WriteAsync(htmlDoc.DocumentNode.OuterHtml);
    }

    private static void ShouldAddVersion(HtmlNode x, string attr)
    {
        var shouldAdd = x.Attributes.Contains(attr)
            && x.Attributes[attr].Value.IndexOf("?v=") < 0;
        if (shouldAdd)
        {
            x.Attributes[attr].Value += "?v=" + Uuid7.Guid().ToString().ToString();
        }
    }

    private async Task WriteDefaultFile(string file, string contentType
        , HttpStatusCode code = HttpStatusCode.OK)
    {
        var response = _ctx.HttpContext.Response;
        if (!response.HasStarted)
        {
            response.Headers.TryAdd(HeaderNames.ContentType, contentType);
            response.Headers.TryAdd(HeaderNames.ContentEncoding, "gzip");
            response.StatusCode = (int)code;
        }
        var html = await File.ReadAllTextAsync(file, encoding: Encoding.UTF8);
        await response.WriteAsync(html);
    }

    public async Task Launch(string tenant, string area, string env, string path)
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
            await GetResource(tenant, path);
            return;
        }
        var key = $"{tenant}_{env}_{area}";
        var cache = await GetStringAsync(key);
        if (cache != null && cache != "null")
        {
            var pageCached = JsonConvert.DeserializeObject<Tenant>(cache);
            await WriteTemplateAsync(response, pageCached, env, tenant);
            return;
        }
        var pageQuery = @$"select * from [Tenant] where TenantCode = '{tenant}' and Env = '{env}' and Area = '{area}'";
        var connStr = DefaultConnStr();
        var page = await _sql.ReadDsAs<Tenant>(pageQuery, connStr)
            ?? throw new ApiException("Page not found") { StatusCode = HttpStatusCode.NotFound };
        await SetStringAsync(key, JsonConvert.SerializeObject(page), Utils.CacheTTL);
        await WriteTemplateAsync(response, page, env: env, tenant: tenant);
    }

    public Task<Dictionary<string, object>[][]> ReadDs
        (string query, string connStr, bool shouldMapToConnStr = false)
        => _sql.ReadDataSet(query, connStr, shouldMapToConnStr);

    public Task NotifyDevices(IEnumerable<TaskNotification> tasks, string queueName)
    {
        return tasks
            .Where(x => x.AssignedId.HasAnyChar())
            .Select(x => new MQEvent
            {
                QueueName = queueName,
                Id = Uuid7.Guid().ToString(),
                Message = x,
                AssignedId = x.AssignedId
            })
        .ForEachAsync(SendMessageToUser);
    }

    private async Task SendMessageToUser(MQEvent task)
    {
        var tenantCode = TenantCode;
        var env = Env;
        var fcm = new FCMWrapper
        {
            To = $"/topics/{tenantCode}/{env}/U{task.AssignedId:0000000}",
            Data = new FCMData
            {
                Title = task.Message.Title,
                Body = task.Message.Description,
            },
            Notification = new FCMNotification
            {
                Title = task.Message.Title,
                Body = task.Message.Description,
                ClickAction = "com.softek.tms.push.background.MESSAGING_EVENT"
            },
        };
        await _taskSocketSvc.SendMessageToUsersAsync([task.Message.AssignedId], task.ToJson(), fcm.ToJson());
    }

    public async Task SendMessageSocket(string socket, TaskNotification task, string queueName)
    {
        var entity = new MQEvent
        {
            QueueName = queueName,
            Id = Uuid7.Guid().ToString(),
            Message = task
        };
        await _taskSocketSvc.SendMessageToSocketAsync(socket, entity.ToJson());
    }

    public async Task NotifyDevice(MQEvent e)
    {
        if (e is null || e.QueueName is null) return;
        await _taskSocketSvc.SendMessageToSubscribers(e.ToJson(), e.QueueName);
    }

    private static async Task<Chat> GetChatGPTResponse(Chat entity)
    {
        var languageRules = new[]
        {
            new { Language = "javascript", Regex = @"```javascript([\s\S]+?)```", Replacement = "<pre><code class=\"language-javascript\">$1</code></pre>" },
            new { Language = "html", Regex = @"```html([\s\S]+?)```", Replacement = "<pre><code class=\"language-html\">$1</code></pre>" },
            new { Language = "csharp", Regex = @"```csharp([\s\S]+?)```", Replacement = "<pre><code class=\"language-csharp\">$1</code></pre>" },
            new { Language = "code", Regex = @"```([\s\S]+?)```", Replacement = "<pre><code>$1</code></pre>" }
        };

        var apiKey = "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";
        var endpoint = "https://api.openai.com/v1/chat/completions";
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        var requestData = new ChatGptVM
        {
            model = "gpt-3.5-turbo",
            messages =
            [
                new ChatGptMessVM
                {
                    role = "user",
                    content = entity.Context,
                    name = entity.FromId.ToString(),
                }
            ]
        };
        var jsonRequestData = JsonConvert.SerializeObject(requestData);
        var response = await httpClient.PostAsync(endpoint, new StringContent(jsonRequestData, Encoding.UTF8, "application/json"));
        var jsonResponseData = await response.Content.ReadAsStringAsync();
        var rs = JsonConvert.DeserializeObject<RsChatGpt>(jsonResponseData);
        var text = rs.choices.FirstOrDefault().message.content;
        foreach (var rule in languageRules)
        {
            var language = rule.Language;
            var regex = new Regex(rule.Regex);
            var replacement = rule.Replacement;
            text = regex.Replace(text, replacement);
        }

        return new Chat()
        {
            FromId = entity.ToId,
            ToId = entity.FromId,
            Context = text,
            ConversationId = entity.ConversationId,
            IsSeft = true,
        };
    }

    internal async Task<Chat> Chat(Chat entity)
    {
        var patchMV = entity.MapToPatch();
        await SavePatch(patchMV);
        if (entity.ToId == 552.ToString())
        {
            var rs1 = await GetChatGPTResponse(entity);
            await SavePatch(rs1.MapToPatch());
            var chat = new MQEvent
            {
                QueueName = entity.QueueName,
                Message = rs1,
                Id = Uuid7.Guid().ToString(),
            };
            await SendMessageToUser(chat);
        }
        else
        {
            var chat = new MQEvent
            {
                QueueName = entity.QueueName,
                Message = entity,
                Id = Uuid7.Guid().ToString(),
            };
            await SendMessageToUser(chat);
        }
        return entity;
    }

    internal IEnumerable<User> GetUserActive()
    {
        var online = _taskSocketSvc.GetAll().Keys;
        var us = online.Select(x =>
        {
            var split = x.Split("/");
            return new User
            {
                Id = split[0],
                Recover = split[3],
                Email = x
            };
        }).OrderBy(x => x.Id);
        return us;
    }

    private static bool _hasOpenClusterSocket = false;
    private readonly ConnectionManager _conn;
    public async Task OpenAPIClustersSocket(string role = "API")
    {
        if (_hasOpenClusterSocket) return;
        _hasOpenClusterSocket = true;
        var clusterQuery = $"select * from [Cluster] where [ClusterRole] = '{role}'";
        var clusters = await _sql.ReadDsAsArr<Cluster>(clusterQuery, _cfg.GetConnectionString(Utils.ConnKey));
        if (clusters.Nothing()) return;
        var tasks = clusters.Select(Connect);
        await Task.WhenAll(tasks);
    }

    private async Task Connect(Cluster cluster)
    {
        var ws = new ClientWebSocket();
        ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);
        var wsScheme = string.Equals(cluster.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
        string url = UserServiceHelpers.GetUri(cluster.Host, cluster.Port, wsScheme, "/clusters");
        try
        {
            await ws.ConnectAsync(new Uri(url), CancellationToken.None);
            //await Listen(ws);
            _conn.AddClusterSocket(ws, $"Balancer/{Uuid7.Guid().ToString()}");
        }
        catch
        {
        }
    }

    internal async Task AddCluster(Node node)
    {
        EnsureSystemRole();
        var delCmd = @$"insert into Cluster (Id, TenantCode, Host, Env, Port, Scheme, ClusterRole, Active, InsertedDate, InsertedBy) values
            ('{node.Id}', '{TenantCode}', '{node.Host}', '{Env}', '{node.Port}', '{node.Scheme}', '{node.Role}', 1, '{DateTime.UtcNow}', 1)";
        await _sql.RunSqlCmd(DefaultConnStr(), delCmd);
        Clusters.Data.Nodes.Add(node);
    }

    private void EnsureSystemRole()
    {
        if (!RoleNames.Any(x => x.Equals("System", StringComparison.OrdinalIgnoreCase)))
            throw new ApiException("Unauthorize access") { StatusCode = Enums.HttpStatusCode.Unauthorized };
    }

    internal async Task RemoveCluster(Node node)
    {
        EnsureSystemRole();
        var delCmd = $"delete from [Cluster] where Id = '{node.Id}'";
        await _sql.RunSqlCmd(DefaultConnStr(), delCmd);
        var node2Remove = Clusters.Data.Nodes.FirstOrDefault(x => x.Host == node.Host && x.Port == node.Port && x.Scheme == node.Scheme);
        Clusters.Data.Nodes.Remove(node2Remove);
    }

    public Task<string> GetConnStrFromKey(string key, string tenantCode = null, string env = null)
        => _sql.GetConnStrFromKey(key, tenantCode, env);

    public Task<string> GetStringAsync(string key) => _cache.GetStringAsync(key?.ToUpper());
    public Task SetStringAsync(string key, string val, DistributedCacheEntryOptions options) => _cache.SetStringAsync(key?.ToUpper(), val, options);

    public async Task GetResource(string tenant, string path)
    {
        var query = @$"select [Content], [ContentType] from [Resource] where [Path] = '{path}' and (Active = 1 and TenantCode = '{tenant}' or Annonymous = 1)";
        var rs = await _sql.ReadDsAs<Resource>(query, DefaultConnStr());
        var response = _ctx.HttpContext.Response;
        if (rs is null)
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            await response.WriteAsync("File not found");
            return;
        }
        response.Headers.TryAdd(HeaderNames.ContentType, rs.ContentType);
        response.StatusCode = (int)HttpStatusCode.OK;
        await response.WriteAsync(rs.Content);
    }

    public string CommandOutput(Cmd cmd)
    {
        try
        {
            ProcessStartInfo procStartInfo = new(cmd.cmd, cmd.args);

            procStartInfo.RedirectStandardError = procStartInfo.RedirectStandardInput = procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            procStartInfo.WorkingDirectory = _host.WebRootPath;

            Process proc = new()
            {
                StartInfo = procStartInfo
            };

            StringBuilder sb = new();
            proc.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
            {
                if (e is not null) sb.Append(e.Data);
            };
            proc.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
            {
                if (e is not null) sb.Append(e.Data);
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            return sb.ToString();
        }
        catch (Exception objException)
        {
            return $"Error in command: {cmd.cmd}, {objException.Message}";
        }
    }

    internal async Task<object> LoadComponent(SqlViewModel vm)
    {
        var query = $"select c.* from Component c " +
            $"join Feature f on c.FeatureId = f.Id " +
            $"where f.TenantCode = '{vm.AnnonymousTenant}' and f.Env = '{vm.AnnonymousEnv}' and f.Name = '{vm.Action}' and f.IsPublic = 1";
        return await ReadDs(query, DefaultConnStr());
    }
}