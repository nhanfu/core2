using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class SettingPolicyDetailController : TMSController<SettingPolicyDetail>
    {
        public SettingPolicyDetailController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
