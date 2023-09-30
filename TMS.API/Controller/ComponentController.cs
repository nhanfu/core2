using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using TMS.API.Models;

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
            return base.Get(options);
        }
    }
}
