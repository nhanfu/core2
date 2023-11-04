using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public override async Task<ActionResult<Component>> CreateAsync([FromBody] Component entity)
        {
            entity.Signed = await _userSvc.EncryptQuery(entity.Query);
            return await base.CreateAsync(entity);
        }

        public override async Task<ActionResult<Component>> UpdateAsync([FromBody] Component entity, string reasonOfChange = "")
        {
            entity.Signed = await _userSvc.EncryptQuery(entity.Query);
            return await base.UpdateAsync(entity, reasonOfChange);
        }

        [HttpPost("api/[Controller]/SqlReader")]
        public async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> SqlReader(
            [FromBody] SqlViewModel model)
        {
            var entity = model.Component;
            var connStr = _userSvc.DecryptQuery(entity.Query, entity.Signed);

            var res = await _userSvc.ExecJs(model.Entity, entity.Query);
            return await ReportDataSet(res, connStr);
        }
    }
}
