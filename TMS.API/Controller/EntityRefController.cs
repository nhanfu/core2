using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class EntityRefController : TMSController<EntityRef>
    {
        public EntityRefController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
