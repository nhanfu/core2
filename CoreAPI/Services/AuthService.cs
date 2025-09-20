using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using CoreAPI.Services.Sql;
using Microsoft.IdentityModel.Tokens;

namespace CoreAPI.Services;

public class AuthService
{
    public readonly IHttpContextAccessor _ctx;

    private readonly UserService _userService;
    private bool _debug;
    public readonly ISqlProvider _sql;
    private readonly IConfiguration _cfg;

    public AuthService(IHttpContextAccessor ctx, UserService userService,
        ISqlProvider sql, IConfiguration cfg)
    {
        _ctx = ctx;
        _sql = sql;
        _cfg = cfg;
#if DEBUG
        _debug = true;
#else
        _debug = false;
#endif
        _userService = userService;
    }

    public async Task<Token> SignInAsync(LoginVM login)
    {
        if (login.TanentCode.HasAnyChar())
        {
            login.TanentCode = login.TanentCode.Trim();
        }
        var matchedUser = await GetUserByLogin(login) ?? throw new ApiException($"Sai mật khẩu hoặc tên đăng nhập.<br /> Vui lòng đăng nhập lại!")
        {
            StatusCode = HttpStatusCode.BadRequest
        };
        var hashedPassword = GetHash(Utils.SHA256, login.Password + matchedUser.Salt);
        var matchPassword = matchedUser.Password == hashedPassword;
        List<PatchDetail> changes = [new PatchDetail { Field = UserServiceHelpers.IdField, OldVal = matchedUser.Id }];
        if (!matchPassword && !_debug)
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
            if (_debug)
            {
                matchPassword = true;
                changes.Add(new PatchDetail { Field = nameof(User.Password), Value = hashedPassword });
            }
        }
        await _userService.SavePatch(new PatchVM
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
        return await GetUserToken(matchedUser, login);
    }
    public async Task<Token> RefreshAsync(RefreshVM token)
    {
        var principal = Utils.GetPrincipalFromAccessToken(token.AccessToken, _cfg);
        var userId = principal.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        var userName = principal.Claims.FirstOrDefault(x => x.Type == "UserName")?.Value;
        var tenant = principal.Claims.FirstOrDefault(x => x.Type == UserServiceHelpers.TenantClaim)?.Value;
        EnsureTokenParam(userId, userName, tenant);
        var query =
            @$"select * from UserLogin 
            where UserId = '{userId}' and RefreshToken = '{token.RefreshToken}'
            and RefreshTokenExp > '{DateTime.Now}' and Active = 1 order by InsertedDate desc";
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
        var updatedUser = await GetUserByLogin(login);
        return await GetUserToken(updatedUser, login, token.RefreshToken);
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

    public async Task<User> GetUserByLogin(LoginVM login)
    {
        var query = @$"
        declare @username varchar(100) = '{login.UserName}';
        select u.* from [User] u 
        where u.Active = 1 and u.Username = @username;
        select top 1 p.* from [Partner] p 
        left join [User] u on p.Id = u.CompanyId
        where u.Active = 1 and u.Username = @username;";
        var ds = await _sql.ReadDataSet(query);
        var userDb = ds.Length > 0 && ds[0].Length > 0 ? ds[0][0].MapTo<User>() : null;
        userDb.Company = ds.Length > 1 && ds[1].Length > 0 ? ds[1][0].MapTo<Partner>() : null;
        return userDb;
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
        await _userService.SavePatch(new PatchVM
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
        await _userService.SavePatch(new PatchVM
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

    public async Task SendMail(EmailVM email, string connStr = null, string webRoot = null)
    {
        var query = $"select top 1 * from [User] m where Id = '{_userService.UserId}'";
        var user = await _sql.ReadDsAs<User>(query, connStr);
        var fromName = user.FullName;
        var fromAddress = user.Email;
        var password = user.PassEmail;
        var server = "smtp.gmail.com";
        await email.SendMailAsync(fromName, fromAddress, password, server, 587, false, webRoot);
    }

    public async Task<Partner> CreateUser(Partner entity)
    {
        var ramdomPass = GenerateRandomToken(8);
        var randomSalt = GenerateRandomToken(8);
        var old = $"select * from [User] where Id = '{entity.Id}'";
        var oldUser = await _sql.ReadDsAs<User>(old);
        var id = "-" + entity.Id;
        if (oldUser != null)
        {
            id = entity.Id;
        }
        var user = new User()
        {
            Id = id,
            UserName = entity.Email,
            FullName = entity.CompanyName,
            Email = entity.Email,
            Password = GetHash(Utils.SHA256, ramdomPass + randomSalt),
            Salt = randomSalt,
            Active = true,
            InsertedBy = entity.InsertedBy,
            InsertedDate = DateTime.Now,
            RoleIds = "CUSTOMER",
            RoleIdsText = "CUSTOMER",
            CompanyId = entity.Id,
            TypeId = 2,
            Avatar = ""
        };
        var save = user.MapToPatch();
        await _userService.SavePatch2(save);
        var email = new EmailVM
        {
            ToAddresses = [user.Email],
            Subject = "Email recovery",
            Body = $"<p>Dear {user.FullName},</p><p>Your account has been created successfully. Please use the following information to login:</p><p>Username: {user.UserName}</p><p>Password: {ramdomPass}</p><p>Thank you!</p>"
        };
        await SendMail(email);
        return entity;
    }

    public async Task<bool> UpdatePassword(UpdatePasswordVM vm)
    {
        var user = await _sql.ReadDsAs<User>($"select * from [User] where Id in ('{_userService.UserId}')");
        var hashedPassword = GetHash(Utils.SHA256, vm.Password + user.Salt);
        var matchPassword = user.Password == hashedPassword;
        if (!matchPassword)
        {
            return false;
        }
        user.Salt = GenerateRandomToken();
        user.Password = GetHash(Utils.SHA256, vm.NewPassword + user.Salt);
        List<PatchDetail> changes =
        [
            new PatchDetail { Field = nameof(User.Id), OldVal = user.Id },
            new PatchDetail { Field = nameof(User.Salt), Value = user.Salt },
            new PatchDetail { Field = nameof(User.Password), Value = user.Password },
        ];
        await _userService.SavePatch(new PatchVM
        {
            Table = nameof(User),
            Changes = changes,
        });
        await _sql.RunSqlCmd(null, $"update [UserLogin] set Active = 0 where UserId in ('{_userService.UserId}')");
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
        await _userService.SavePatch(new PatchVM
        {
            CachedDataConn = vm.CachedDataConn,
            CachedMetaConn = vm.CachedMetaConn,
            Table = nameof(User),
            Changes = changes,
        });
        return randomPassword;
    }

    protected async Task<Token> GetUserToken(User user, LoginVM login, string refreshToken = null)
    {
        if (user is null)
        {
            return null;
        }
        var roleIds = user.RoleIds.Split(",").ToList();
        var roleNames = user.RoleIdsText.Split(",").ToList();
        var signinDate = DateTime.Now;
        var jit = Uuid7.Guid().ToString();
        List<Claim> claims =
        [
            new("PartnerId", user.PartnerId is null ? string.Empty : user.PartnerId),
            new ("UserId", user.Id),
            new ("Avatar", user.Avatar ?? "/icons/default-avatar.jpg"),
            new ("TeamId", user.TeamId ?? string.Empty),
            new ("DepartmentId", user.DepartmentId ?? string.Empty),
            new ("UserName", user.UserName),
            new ("FullName", user.FullName),
            new ("CName", user.Company.CompanyName ?? string.Empty),
            new ("CLogo", user.Company.Logo ?? string.Empty),
            new ("CIcon", user.Company.Icon ?? string.Empty),
            new ("CAddress", user.Company.Address ?? string.Empty),
            new ("CPhoneNumber", user.Company.PhoneNumber ?? string.Empty),
            new ("CEmail",user.Company.Email ?? string.Empty),
            new (UserServiceHelpers.TenantClaim,login.TanentCode),
            new ("Email", user.Email ?? string.Empty),
            new ("Dob", user.Dob?.ToString() ?? string.Empty),
        ];
        claims.AddRange(roleIds.Select(x => new Claim("RoleIds", x.ToString())));
        claims.AddRange(roleNames.Select(x => new Claim(UserServiceHelpers.RoleNameClaim, x.ToString())));
        var newLogin = refreshToken is null;
        refreshToken ??= GenerateRandomToken();
        var (token, exp) = AccessToken(claims);
        var res = JsonToken(user, login.TanentCode, roleIds, roleNames, refreshToken, token, exp, signinDate);
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
            Active = true
        };
        var patch = userLogin.MapToPatch();
        patch.TenantCode = login.TanentCode;
        await _userService.SavePatch(patch);
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

    private static Token JsonToken(User user, string tanent, List<string> rolesIds, List<string> rolesNames, string refreshToken,
        JwtSecurityToken token, DateTime exp, DateTime signinDate)
    {
        return new Token
        {
            UserId = user.Id,
            FullName = user.FullName,
            DepartmentId = user.DepartmentId,
            Code = user.Code,
            PositionId = user.PositionId,
            TeamId = user.TeamId,
            UserName = user.UserName,
            Address = user.Address,
            Avatar = user.Avatar,
            PhoneNumber = user.PhoneNumber,
            Ssn = user.Ssn,
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            AccessTokenExp = exp,
            RefreshTokenExp = DateTime.Now.AddYears(1),
            RefreshToken = refreshToken,
            RoleIds = rolesIds,
            RoleNames = rolesNames,
            Vendor = user.Company,
            TenantCode = tanent,
            SigninDate = signinDate,
        };
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

    public string GetRemoteIpAddress(HttpContext context)
    {
        return context.Request.Headers.TryGetValue(UserServiceHelpers.ForwardedIP, out var value)
            ? value.ToString().Split(',')[0].Trim()
            : context.Connection.RemoteIpAddress.ToString();
    }
}
