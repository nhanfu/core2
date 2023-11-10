using Core.Extensions;
using Core.Models;

namespace Core.Controllers
{
    public class EntityRefController : TMSController<EntityRef>
    {
        public EntityRefController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
