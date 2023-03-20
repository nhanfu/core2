using Microsoft.AspNetCore.Http;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class EntityRefController : TMSController<EntityRef>
    {
        public EntityRefController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {

        }
    }
}
