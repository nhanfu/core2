using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ApprovalConfigController : TMSController<ApprovalConfig>
    {
        public ApprovalConfigController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
