using Microsoft.AspNetCore.Http;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ApprovalConfigController : TMSController<ApprovalConfig>
    {
        public ApprovalConfigController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {

        }
    }
}
