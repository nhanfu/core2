using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ReturnPlanController : TMSController<ReturnPlan>
    {
        public ReturnPlanController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
