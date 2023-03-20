using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class InsuranceFeesController : TMSController<Transportation>
    {
        public InsuranceFeesController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
