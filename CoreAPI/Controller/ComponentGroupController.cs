using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace Core.Controllers
{
    public class ComponentGroupController : TMSController<ComponentGroup>
    {
        public ComponentGroupController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [AllowAnonymous]
        public override Task<OdataResult<ComponentGroup>> Get(ODataQueryOptions<ComponentGroup> options)
        {
            var query = db.ComponentGroup
                .Where(x => _userSvc.TenantCode != null && x.TenantCode == _userSvc.TenantCode
                || _userSvc.TenantCode == null && !x.IsPrivate);
            return ApplyQuery(options, query);
        }

        public override async Task<ActionResult<ComponentGroup>> CreateAsync([FromBody] ComponentGroup entity)
        {
            var feature = await db.Feature.FirstOrDefaultAsync(x => x.Id == entity.FeatureId);
            if (feature != null && feature.IsPublic)
            {
                entity.IsPrivate = false;
            }
            return await base.CreateAsync(entity);
        }
    }
}
