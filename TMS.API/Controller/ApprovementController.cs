using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ApprovementController : TMSController<Approvement>
    {
        public ApprovementController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
