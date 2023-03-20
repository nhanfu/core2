using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class LedgerServiceController : TMSController<LedgerService>
    {
        public LedgerServiceController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
