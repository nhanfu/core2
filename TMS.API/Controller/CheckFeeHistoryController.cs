using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class CheckFeeHistoryController : TMSController<CheckFeeHistory>
    {
        public CheckFeeHistoryController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
