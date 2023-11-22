using Core.Exceptions;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IdentityModel.Tokens.Jwt;

namespace Core.Controllers
{
    public class UserController : TMSController<User>
    {
        private readonly IConfiguration _configuration;

        public UserController(CoreContext context, IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor, EntityService entityService) : base(context, entityService, httpContextAccessor)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
                var hashPassword = base._userSvc.GetHash(UserUtils.sHA256, profile.OldPassword + user.Salt);
                if (hashPassword != user.Password)
                {
                    throw new InvalidOperationException("The old password is not matched!");
                }
                if (profile.NewPassword != profile.ConfirmedPassword)
                {
                    throw new InvalidOperationException("The password is not matched confirmed password!");
                }
                profile.Salt = base._userSvc.GenerateRandomToken();
                profile.Password = base._userSvc.GetHash(UserUtils.sHA256, profile.NewPassword + profile.Salt);
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
            user.Salt = base._userSvc.GenerateRandomToken();
            var randomPassword = "123";
            user.Password = base._userSvc.GetHash(UserUtils.sHA256, randomPassword + user.Salt);
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
            return await _userSvc.SignInAsync(login);
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
            var ipAddress = base._userSvc.GetRemoteIpAddress(_httpContext.HttpContext);
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
            return await _userSvc.RefreshAsync(token);
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
            var oneClickLink = base._userSvc.GenerateRandomToken();
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
            user.Salt = base._userSvc.GenerateRandomToken();
            var randomPassword = base._userSvc.GenerateRandomToken(10);
            user.Password = base._userSvc.GetHash(UserUtils.sHA256, randomPassword + user.Salt);
            SetAuditInfo(user);
            await db.SaveChangesAsync();
            return randomPassword;
        }

        [AllowAnonymous]
        [HttpPost("api/[Controller]/svc")]
        public Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ExecUserSvc([FromBody] SqlViewModel vm)
        {
            return _userSvc.ExecUserSvc(vm);
        }

        [HttpPost("api/[Controller]/excel")]
        public Task<string> ExportExcel([FromBody] SqlViewModel vm)
        {
            return _userSvc.ExportExcel(vm);
        }

        [HttpPatch("api/v2/[Controller]", Order = 0)]
        public Task<bool> PatchAsync([FromBody] PatchUpdate patch)
        {
            return _userSvc.Patch(patch);
        }
    }
}
