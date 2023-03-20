using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ComponentGroupController : TMSController<ComponentGroup>
    {
        public ComponentGroupController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [AllowAnonymous]
        public override Task<OdataResult<ComponentGroup>> Get(ODataQueryOptions<ComponentGroup> options)
        {
            return ApplyQuery(options, db.ComponentGroup);
        }
    }
}
