using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Controllers
{
    public class ComponentController : TMSController<Component>
    {
        public ComponentController(CoreContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
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

        [HttpPost("api/[Controller]", Order = 0)]
        public async Task<ActionResult<Component>> CreateAsync([FromBody] Component entity, [FromQuery] string env = "test")
        {
            entity.Signed = await _userSvc.EncryptQuery(entity.Query, env, _userSvc.System, _userSvc.TenantCode, connKey: entity.ConnKey);
            return await base.CreateAsync(entity);
        }

        [HttpPut("api/[Controller]", Order = 0)]
        public async Task<ActionResult<Component>> UpdateAsync([FromBody] Component entity
            , [FromQuery]string reasonOfChange = "", [FromQuery] string env = "test")
        {
            entity.Signed = await _userSvc.EncryptQuery(entity.Query, env, _userSvc.System, _userSvc.TenantCode, connKey: entity.ConnKey);
            return await base.UpdateAsync(entity, reasonOfChange);
        }

        [HttpPost("api/[Controller]/Reader")]
        public Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> Reader(
            [FromBody] SqlViewModel model)
        {
            return _userSvc.ReadDataSet(model);
        }
    }
}
