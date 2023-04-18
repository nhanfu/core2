using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TMS.API.Models;
using HttpStatusCode = Core.Enums.HttpStatusCode;

namespace TMS.API.Services
{
    public class UserService
    {
        private const int MAX_LOGIN = 10;
        public readonly IHttpContextAccessor Context;
        private readonly TMSContext db;
        private readonly IConfiguration _configuration;
        public int UserId { get; set; }
        public int BranchId { get; set; }
        public List<int> CenterIds { get; set; }
        public bool IsSelfTenant { get; set; }
        public bool IsInternalCoor { get; set; }
        public bool IsInternalSale { get; set; }
        public int VendorId { get; set; }
        public string TenantCode { get; set; }
        public List<int> AllRoleIds { get; set; }
        public List<int> RoleIds { get; set; }

        public UserService(IHttpContextAccessor httpContextAccessor, TMSContext db, IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            Context = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            if (Context?.HttpContext is null)
            {
                UserId = Utils.SystemId;
                VendorId = Utils.SelfVendorId;
                return;
            }
            var claims = Context.HttpContext.User.Claims;
            IsSelfTenant = claims.FirstOrDefault(x => x.Type == nameof(IsSelfTenant))?.Value?.TryParseBool() ?? false;
            BranchId = claims.FirstOrDefault(x => x.Type == nameof(BranchId))?.Value?.TryParseInt() ?? 0;
            UserId = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value?.TryParseInt() ?? 0;
            AllRoleIds = claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value.TryParseInt()).Where(x => x != null).Cast<int>().ToList();
            CenterIds = claims.Where(x => x.Type == nameof(CenterIds)).Select(x => x.Value.TryParseInt()).Where(x => x != null).Cast<int>().ToList();
            RoleIds = claims.Where(x => x.Type == ClaimTypes.Actor).Select(x => x.Value.TryParseInt()).Where(x => x != null).Cast<int>().ToList();
            VendorId = claims.FirstOrDefault(x => x.Type == ClaimTypes.GroupSid)?.Value?.TryParseInt() ?? 0;
            IsInternalCoor = claims.FirstOrDefault(x => x.Type == "Internal Coordinator")?.Value?.TryParseBool() ?? false;
            IsInternalSale = claims.FirstOrDefault(x => x.Type == "Internal Sale")?.Value?.TryParseBool() ?? false;
            TenantCode = claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimaryGroupSid)?.Value.ToUpper();
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
        public void SetAuditInfo<K>(K entity, int? userId = null) where K : class
        {
            ReflectionExt.ProcessObjectRecursive(entity, (obj) =>
            {
                long? id = null;
                switch (obj.GetPropValue(IdField))
                {
                    case int intId:
                        id = intId;
                        break;
                    case long longId:
                        id = longId;
                        break;
                }
                if (id is null)
                {
                    return;
                }
                if (id <= 0)
                {
                    obj.SetPropValue(IdField, 0);
                    obj.SetPropValue(nameof(User.InsertedBy), userId ?? UserId);
                    obj.SetPropValue(nameof(User.InsertedDate), DateTime.Now);
                    obj.SetPropValue(nameof(User.UpdatedBy), userId ?? UserId);
                    obj.SetPropValue(nameof(User.UpdatedDate), DateTime.Now);
                    obj.SetPropValue(nameof(User.Active), true);
                }
                else
                {
                    obj.SetPropValue(nameof(User.UpdatedBy), userId ?? UserId);
                    obj.SetPropValue(nameof(User.UpdatedDate), DateTime.Now);
                }
            });
        }

        public async Task<Token> SignInAsync(LoginVM login, bool skipHash = false)
        {
            if (login.CompanyName.HasAnyChar())
            {
                login.CompanyName = login.CompanyName.Trim();
            }
            var matchedUser = await GetUserByLogin(login);
            if (matchedUser is null)
            {
                throw new ApiException($"Sai mật khẩu hoặc tên đăng nhập.<br /> Vui lòng đăng nhập lại!") { StatusCode = HttpStatusCode.BadRequest };
            }
            if (matchedUser.LoginFailedCount >= MAX_LOGIN)
            {
                throw new ApiException($"Tài khoản {login.UserName} đã bị khóa!<br /> Vui lòng liên hệ nhà quản trị để cấp lại quyền truy cập!") { StatusCode = HttpStatusCode.Conflict };
            }
            var hashedPassword = GetHash(UserUtils.sHA256, login.Password + matchedUser.Salt);
            var matchPassword = skipHash ? matchedUser.Password == login.Password : matchedUser.Password == hashedPassword;
            if (!matchPassword)
            {
                if (!login.RecoveryToken.IsNullOrWhiteSpace() && login.RecoveryToken == matchedUser.Recover)
                {
                    matchedUser.Password = hashedPassword;
                }
                else
                {
                    matchedUser.LastFailedLogin = Date.Now;
                    matchedUser.LoginFailedCount = matchedUser.LoginFailedCount.HasValue ? matchedUser.LoginFailedCount + 1 : 1;
                }
            }
            else
            {
                matchedUser.LastLogin = DateTime.Now;
                matchedUser.LoginFailedCount = 0;
            }
            if (!skipHash)
            {
                await db.SaveChangesAsync();
            }
            if (!matchPassword)
            {
                throw new ApiException($"Wrong username or password. Please try again!") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await GetUserToken(matchedUser, login.CompanyName, null, login.AutoSignIn);
        }

        private async Task<User> GetUserByLogin(LoginVM login)
        {
            var matchedUser =
                from user in db.User.Include(user => user.Vendor).Include(user => user.UserRole).ThenInclude(userRole => userRole.Role)
                where user.UserName == login.UserName && user.Active
                select user;
            return await matchedUser.FirstOrDefaultAsync();
        }

        protected virtual async Task<Token> GetUserToken(User user, string tanent, string refreshToken = null, bool autoSigin = false)
        {
            if (user is null)
            {
                return null;
            }
            var roleIds = user.UserRole.Select(x => x.RoleId).Distinct().ToList();
            var allRoles = await GetDecendantPath<Role>(roleIds, true);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.GroupSid, user.VendorId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(nameof(User.BranchId), user.BranchId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Birthdate, user.DoB.ToString()),
                new Claim(JwtRegisteredClaimNames.FamilyName, user.FullName?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.PrimaryGroupSid, tanent),
            };
            claims.AddRange(allRoles.Select(x => new Claim(ClaimTypes.Role, x.ToString())));
            claims.AddRange(roleIds.Select(x => new Claim(ClaimTypes.Actor, x.ToString())));
            var newLogin = refreshToken is null;
            refreshToken ??= GenerateRandomToken();
            var (token, exp) = AccessToken(claims);
            var res = JsonToken(user, user.UserRole.ToList(), tanent, allRoles.ToList(), refreshToken, token, exp);
            if (!newLogin || !autoSigin)
            {
                return res;
            }
            var userLogin = new UserLogin
            {
                UserId = user.Id,
                IpAddress = Context.HttpContext.Connection.RemoteIpAddress.ToString(),
                RefreshToken = refreshToken,
                ExpiredDate = res.RefreshTokenExp,
                SignInDate = DateTime.Now,
            };
            db.Add(userLogin);
            await db.SaveChangesAsync();
            return res;
        }

        private Task<List<int>> GetDecendantPath<T>(int rootId, bool includeRoot) where T : class => GetDecendantPath<T>(new List<int> { rootId }, includeRoot);
        public async Task<List<int>> GetDecendantPath<T>(List<int> rootIds, bool includeRoot) where T : class
        {
            var str_roleIds = string.Join(",", rootIds);
            var tableName = typeof(T).Name;
            var decendants = rootIds.Nothing() ? new List<T>() : await db.Set<T>().FromSqlRaw(
                $"select * from [{tableName}] d " +
                $"cross apply (select [data] from dbo.SplitStringToTable(d.Path, '\\') where [data] in ({str_roleIds})) as decendants"
            ).ToListAsync();
            List<int> result = null;
            if (includeRoot)
            {
                result = decendants.Select(x => (int)x.GetPropValue(IdField)).Union(rootIds).ToList();
            }
            else
            {
                result = decendants.Select(x => (int)x.GetPropValue(IdField)).Except(rootIds).ToList();
            }
            return result;
        }

        private (JwtSecurityToken, DateTime) AccessToken(IEnumerable<Claim> claims)
        {
            var exp = DateTime.Now.AddDays(1);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Tokens:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _configuration["Tokens:Issuer"],
                _configuration["Tokens:Issuer"],
                claims,
                expires: exp,
                signingCredentials: creds);
            return (token, exp);
        }

        private Token JsonToken(User user, List<UserRole> roles, string tanent, List<int> allRoleIds, string refreshToken, JwtSecurityToken token, DateTime exp)
        {
            var vendor = new Core.Models.Vendor();
            vendor.CopyPropFrom(user.Vendor);
            return new Token
            {
                UserId = user.Id,
                CostCenterId = user.BranchId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                UserName = user.UserName,
                Address = user.Address,
                Avatar = user.Avatar,
                PhoneNumber = user.PhoneNumber,
                Ssn = user.Ssn,
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                AccessTokenExp = exp,
                RefreshTokenExp = Date.Now.AddYears(1),
                RefreshToken = refreshToken,
                RoleIds = roles.Select(x => x.RoleId).ToList(),
                AllRoleIds = allRoleIds,
                RoleNames = roles.Select(x => x.Role.RoleName).ToList(),
                Vendor = vendor,
                TenantCode = tanent,
                SysName = _configuration["SysName"],
            };
        }

        public async Task<Token> RefreshAsync(RefreshVM token, string t)
        {
            var principal = UserUtils.GetPrincipalFromAccessToken(token.AccessToken, _configuration);
            var issuedAt = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Iat)?.Value.TryParseDateTime();
            var userIdClaim = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim is null)
            {
                throw new InvalidOperationException($"{nameof(userIdClaim)} is null");
            }
            var ipAddress = Context.HttpContext.Connection.RemoteIpAddress.ToString();
            int.TryParse(userIdClaim.Value, out int userId);
            var userLogin = await db.UserLogin
                .OrderByDescending(x => x.SignInDate)
                .FirstOrDefaultAsync(x => x.UserId == userId
                    && x.RefreshToken == token.RefreshToken
                    && x.ExpiredDate > DateTime.Now);

            if (userLogin == null)
            {
                Console.WriteLine("Refresh token timeout.");
                return null;
            }
            var updatedUser = await db.User.Include(user => user.Vendor)
                .Include(x => x.UserRole).ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId);
            return await GetUserToken(updatedUser, t, token.RefreshToken);
        }

        public async Task<Role> GetRole(string roleName, RoleSelection? selection = RoleSelection.TopFirst)
        {
            var roleQuery = db.Role
                .Where(x => AllRoleIds.Contains(x.Id) && x.RoleName.Contains(roleName));
            if (selection == RoleSelection.TopFirst)
            {
                roleQuery = roleQuery.OrderBy(x => x.Path.Length);
            }
            else
            {
                roleQuery = roleQuery.OrderByDescending(x => x.Path.Length);
            }
            return await roleQuery.FirstOrDefaultAsync();
        }

        public async Task<User> GetUserByRole(string roleName, RoleSelection? selection = RoleSelection.TopFirst)
        {
            var role = await GetRole(roleName, selection);
            var userQuery =
                from user in db.User
                join userRole in db.UserRole on user.Id equals userRole.UserId
                where user.Active && userRole.Active && role.Id == userRole.RoleId
                select user;
            return await userQuery.FirstOrDefaultAsync();
        }
    }
}
