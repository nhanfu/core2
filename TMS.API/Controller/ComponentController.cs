using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tenray.Topaz;
using Tenray.Topaz.API;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.API.Controllers
{
    public class ComponentController : TMSController<Component>
    {
        public ComponentController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
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
            SetAuditInfo(entity);
            var text = JsonConvert.SerializeObject(entity);
            var hash = _userSvc.GetHash(UserUtils.sHA256, text);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Hash, hash),
                new Claim(ClaimTypes.System, _config.GetConnectionString("Default")),
            };
            var accessToken = _userSvc.AccessToken(claims, DateTimeOffset.Now.AddYears(1)).Item1;
            entity.Signed = new JwtSecurityTokenHandler().WriteToken(accessToken);
            return base.CreateAsync(entity);
        }

        [HttpPost("api/[Controller]/SqlReader")]
        public async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ExecSql(
            [FromBody] SqlViewModel model)
        {
            var entity = model.Component;
            string token = entity.Signed;
            entity.Signed = null;
            var text = JsonConvert.SerializeObject(entity);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var hash = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Hash)?.Value;
            if (hash != _userSvc.GetHash(UserUtils.sHA256, text))
            {
                throw new ApiException("Permission denied!") { StatusCode = Core.Enums.HttpStatusCode.Unauthorized };
            }
            var connStr = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.System)?.Value;
            var engine = new TopazEngine();
            engine.SetValue("JSON", new JSONObject());
            engine.AddType<HttpClient>("HttpClient");
            engine.AddNamespace("System");
            engine.SetValue("args", model.Entity);

            await engine.ExecuteScriptAsync(entity.Query);
            var res = engine.GetValue("result") as string;
            var sqlQuery = JsonConvert.DeserializeObject<SqlQueryResult>(res);
            return await ReportDataSet(sqlQuery.Query, null, connStr);

        }
    }
}
