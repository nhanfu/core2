using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ComponentGroupController : TMSController<ComponentGroup>
    {
        public ComponentGroupController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        [AllowAnonymous]
        public override Task<OdataResult<ComponentGroup>> Get(ODataQueryOptions<ComponentGroup> options)
        {
            return ApplyQuery(options, db.ComponentGroup);
        }
    }
}
