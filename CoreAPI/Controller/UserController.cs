using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using Core.Exceptions;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using Tenray.Topaz;
using Tenray.Topaz.API;
using DocumentFormat.OpenXml.Vml.Office;

namespace Core.Controllers
{
    public class UserController : TMSController<User>
    {
        private readonly IConfiguration _configuration;
        private readonly UserService _userSerivce;

        public UserController(CoreContext context, IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor, EntityService entityService, UserService userSerivce, IServiceProvider serviceProvider) : base(context, entityService, httpContextAccessor)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userSerivce = userSerivce;
        }

        [AllowAnonymous]
        [HttpGet("api/User/GetDriver")]
        public Task<OdataResult<User>> GetDriver(ODataQueryOptions<User> options)
        {
            var query =
                from u in db.User
                join ur in db.UserRole on u.Id equals ur.UserId
                join r in db.Role on ur.RoleId equals r.Id
                where r.RoleName.Contains("driver")
                select u;
            return ApplyQuery(options, query.Distinct());
        }

        [HttpGet("api/User/GetSaleUser")]
        public async Task<OdataResult<User>> GetSaleUser(ODataQueryOptions<User> options, string vendorId)
        {
            if (vendorId is null)
            {
                return new OdataResult<User>();
            }
            var query =
                from user in db.User
                from shared in db.FeaturePolicy.Where(x => x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id
                    && x.RecordId == vendorId && x.UserId == user.Id)
                select user;
            return await ApplyQuery(options, query);
        }

        [HttpGet("api/User/Limited")]
        public Task<OdataResult<User>> GetLimitedUser(ODataQueryOptions<User> options)
        {
            var customerEnum = _entitySvc.GetEntity(nameof(Vendor)).Id;
            var query =
                from user in db.User
                join policyLeft in db.FeaturePolicy on user.Id equals policyLeft.RecordId into policyLeftJoin
                from policy in policyLeftJoin.DefaultIfEmpty()
                where user.InsertedBy == UserId || policy != null && policy.CanRead && policy.EntityId == customerEnum
                        && (policy.UserId == UserId || AllRoleIds.Contains(policy.RoleId))
                select user;

            return ApplyQuery(options, query);
        }

        private async Task<bool> CanUpdateUser(User user)
        {
            return user.InsertedBy != UserId && !AllRoleIds.Contains(user.CreatedRoleId) && !await HasSystemRole();
        }

        [HttpGet("api/User/UserRole/{roleName}")]
        public Task<OdataResult<User>> GetUserByRole(string roleName, ODataQueryOptions<User> options)
        {
            var query =
                from user in db.User
                join userRole in db.UserRole on user.Id equals userRole.UserId
                join role in db.Role on userRole.RoleId equals role.Id
                where role.RoleName == roleName
                select user;

            return ApplyQuery(options, query);
        }

        public override async Task<ActionResult<User>> UpdateAsync([FromBody] User user, string reasonOfChange = "")
        {
            //await EnsureEditUserPermission();
            SetAuditInfo(user);
            db.Entry(user).Property(p => p.Salt).IsModified = false;
            db.Entry(user).Property(p => p.Password).IsModified = false;
            db.Entry(user).Property(p => p.InsertedBy).IsModified = false;
            db.Entry(user).Property(p => p.InsertedDate).IsModified = false;
            db.Update(user);
            await db.SaveChangesAsync();
            return user;
        }

        private async Task EnsureEditUserPermission()
        {
            var canWriteUser = await db.FeaturePolicy
                .AnyAsync(x => x.Feature.Name == "User Detail" && AllRoleIds.Contains(x.RoleId) && x.CanWrite);
            if (!canWriteUser)
            {
                throw new UnauthorizedAccessException("No permission to update");
            }
        }

        [HttpPut("api/[Controller]/UpdateProfile")]
        public async Task<ActionResult<bool>> UpdateProfileAsync([FromBody] UserProfileVM profile)
        {
            var user = await db.User.FindAsync(UserId);
            if (profile.OldPassword.HasAnyChar())
            {
                var hashPassword = _userSvc.GetHash(UserUtils.sHA256, profile.OldPassword + user.Salt);
                if (hashPassword != user.Password)
                {
                    throw new InvalidOperationException("The old password is not matched!");
                }
                if (profile.NewPassword != profile.ConfirmedPassword)
                {
                    throw new InvalidOperationException("The password is not matched confirmed password!");
                }
                profile.Salt = _userSvc.GenerateRandomToken();
                profile.Password = _userSvc.GetHash(UserUtils.sHA256, profile.NewPassword + profile.Salt);
            }
            user.Salt = profile.Salt;
            user.Password = profile.Password;
            if (!profile.OldPassword.HasAnyChar())
            {
                db.Entry(user).Property(p => p.Salt).IsModified = false;
                db.Entry(user).Property(p => p.Password).IsModified = false;
                db.Entry(user).Property(p => p.InsertedBy).IsModified = false;
                db.Entry(user).Property(p => p.InsertedDate).IsModified = false;
            }
            SetAuditInfo(user);
            await db.SaveChangesAsync();
            return true;
        }

        public override async Task<ActionResult<User>> CreateAsync([FromBody] User user)
        {
            await EnsureEditUserPermission();
            user.UserRole = user.UserRole.Where(x => AllRoleIds.Contains(x.RoleId)).ToList();
            var roles = await db.Role.Where(x => AllRoleIds.Contains(x.Id)).Select(x => new { x.Id, x.Level }).ToListAsync();
            user.CreatedRoleId = roles.FirstOrDefault(x => x.Level == roles.Min(r => r.Level))?.Id;
            user.Salt = _userSvc.GenerateRandomToken();
            var randomPassword = "123";
            user.Password = _userSvc.GetHash(UserUtils.sHA256, randomPassword + user.Salt);
            var res = await base.CreateAsync(user);
            user.Password = randomPassword;
            var accountCreatedEmailTemplate = await db.MasterData.FirstOrDefaultAsync(x => x.Name == "AccountCreated");
            if (user.Email.HasAnyChar())
            {
                var email = new EmailVM
                {
                    Body = Utils.FormatEntity(accountCreatedEmailTemplate.Description, user),
                    Subject = $"[{_config["SysName"]}] Tài khoản của bạn vừa được khởi tạo",
                    ToAddresses = new List<string> { user.Email }
                };
                await SendMail(email, db);
            }
            user.Salt = null;
            user.Password = null;
            return res;
        }

        [AllowAnonymous]
        [HttpPost("api/{system}/{tenant}/[Controller]/SignIn")]
        public async Task<ActionResult<Token>> SignInAsync([FromBody] LoginVM login, [FromRoute] string system, [FromRoute] string tenant)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            login.System ??= system;
            login.CompanyName ??= tenant;
            return await _userSerivce.SignInAsync(login);
        }

        [HttpPost("api/[Controller]/SignOut")]
        public async Task<bool> SignOutAsync([FromBody] Token token)
        {
            if (token is null)
            {
                throw new ApiException("Token is required");
            }
            var principal = UserUtils.GetPrincipalFromAccessToken(token.AccessToken, _configuration);
            var sessionId = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            var ipAddress = _userSvc.GetRemoteIpAddress(_httpContext.HttpContext);
            var userLogin = await db.UserLogin.FindAsync(sessionId) ?? throw new ApiException("Login session not found");
            userLogin.ExpiredDate = DateTimeOffset.Now;
            await db.SaveChangesAsync();
            return true;
        }

        [AllowAnonymous]
        [HttpPost("api/[Controller]/Refresh")]
        public async Task<Token> RefreshAsync([FromBody] RefreshVM token)
        {
            if (token is null)
            {
                throw new ApiException("Token is required");
            }
            return await _userSerivce.RefreshAsync(token);
        }

        [AllowAnonymous]
        [HttpPost("api/[Controller]/ForgotPassword")]
        public async Task<bool> ForgotPassword([FromBody] LoginVM login)
        {
            var str_maxLoginFailed = await db.MasterData.FirstOrDefaultAsync(x => x.Name == "10");
            var maxLoginFailed = str_maxLoginFailed.Description.TryParseInt() ?? 5;
            var user = await db.User.FirstOrDefaultAsync(x => x.UserName == login.UserName);
            var span = DateTimeOffset.Now - (user.UpdatedDate ?? DateTimeOffset.Now);
            if (user.LoginFailedCount >= maxLoginFailed && span.TotalMinutes < 5)
            {
                throw new ApiException($"The account {login.UserName} has been locked for a while! Please contact your administrator to unlock.");
            }
            // Send mail
            var emailTemplate = await db.MasterData.FirstOrDefaultAsync(x => x.Name == "") ?? throw new InvalidOperationException("Cannot find recovery email template!");
            var oneClickLink = _userSvc.GenerateRandomToken();
            user.Recover = oneClickLink;
            await db.SaveChangesAsync();
            var email = new EmailVM
            {
                ToAddresses = new List<string>() { user.Email },
                Subject = "Email recovery",
                Body = Utils.FormatEntity(emailTemplate.Description, user)
            };
            await SendMail(email, db);
            return true;
        }

        [HttpGet("api/User/ReSendUser/{userId}")]
        public async Task<string> ReSendUser(string userId)
        {
            var user = await db.User.FirstOrDefaultAsync(x => x.Id == userId);
            if (await CanUpdateUser(user))
            {
                throw new ApiException("Bạn không có quyền đổi mật khẩu của người dùng");
            }
            user.Salt = _userSvc.GenerateRandomToken();
            var randomPassword = _userSvc.GenerateRandomToken(10);
            user.Password = _userSvc.GetHash(UserUtils.sHA256, randomPassword + user.Salt);
            SetAuditInfo(user);
            await db.SaveChangesAsync();
            return randomPassword;
        }

        [HttpPost("api/[Controller]/svc")]
        public async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ExecuteJs([FromBody] SqlViewModel vm)
        {
            var svQuery = db.Services.Where(x =>
                    x.System == _userSerivce.System && x.TenantCode == _userSerivce.TenantCode
                    && (x.ComId == vm.ComId && x.Action == vm.Action || vm.SvcId != null && x.Id == vm.SvcId));
            var sv = await svQuery.FirstOrDefaultAsync();

            if (sv == null)
            {
                return null;
            }
            var isValidRole = sv.IsPublicInTenant ||
                (from svRole in sv.RoleIds.Split(',')
                 join usrRole in _userSerivce.RoleIds on svRole equals usrRole
                 select svRole).Any();
            if (!isValidRole) throw new UnauthorizedAccessException("The service is not accessible by your roles");

            var jsRes = await _userSvc.ExecJs(vm.Entity, sv.Content);
            var conStr = await _userSerivce.GetConnStrFromKey(sv.ConnKey);
            return await ReportDataSet(jsRes.Query, conStr);
        }
    }
}
