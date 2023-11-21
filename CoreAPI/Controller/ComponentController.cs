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

        [HttpPost("api/[Controller]/Reader")]
        public Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> Reader(
            [FromBody] SqlViewModel model)
        {
            return _userSvc.ReadDataSetWrapper(model);
        }
    }
}
