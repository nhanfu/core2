using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Tenray.Topaz;
using Tenray.Topaz.API;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.API.Controllers
{
    public class ComponentController : TMSController<Component>
    {
        public ComponentController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [AllowAnonymous]
        public override Task<OdataResult<Component>> Get(ODataQueryOptions<Component> options)
        {
            var query = db.Component
                .Where(x => _userSvc.TenantCode != null && x.TenantCode == _userSvc.TenantCode
                || _userSvc.TenantCode == null && !x.IsPrivate);
            return ApplyQuery(options, query);
        }

        public override Task<ActionResult<Component>> CreateAsync([FromBody] Component entity)
        {
            entity.Signed = GetSignedQuery(entity.Query);
            return base.CreateAsync(entity);
        }

        private string GetSignedQuery(string query)
        {
            var hash = _userSvc.GetHash(UserUtils.sHA256, query);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Hash, hash),
                new Claim(ClaimTypes.System, _config.GetConnectionString("Default")),
            };
            var accessToken = _userSvc.AccessToken(claims, DateTimeOffset.Now.AddYears(1)).Item1;
            return new JwtSecurityTokenHandler().WriteToken(accessToken);
        }

        public override Task<ActionResult<Component>> UpdateAsync([FromBody] Component entity, string reasonOfChange = "")
        {
            entity.Signed = GetSignedQuery(entity.Query);
            return base.UpdateAsync(entity, reasonOfChange);
        }

        [HttpPost("api/[Controller]/SqlReader")]
        public async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> SqlReader(
            [FromBody] SqlViewModel model)
        {
            var entity = model.Component;
            var token = UserUtils.GetPrincipalFromAccessToken(entity.Signed, _config);
            var hash = token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Hash)?.Value;
            var originalHash = _userSvc.GetHash(UserUtils.sHA256, entity.Query);
            if (hash != originalHash)
            {
                throw new ApiException("Permission denied!") { StatusCode = Core.Enums.HttpStatusCode.Unauthorized };
            }
            var connStr = token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.System)?.Value;
            var engine = new TopazEngine();
            engine.SetValue("JSON", new JSONObject());
            engine.AddType<HttpClient>("HttpClient");
            engine.AddNamespace("System");
            engine.SetValue("args", model.Entity);

            try
            {
                await engine.ExecuteScriptAsync(entity.Query);
                var res = engine.GetValue("result") as string;
                return await ReportDataSet(res, null, connStr);
            }
            catch
            {
                throw;
            }
        }
    }
}
