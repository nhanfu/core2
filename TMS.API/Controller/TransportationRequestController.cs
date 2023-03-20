using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class TransportationRequestController : TMSController<TransportationRequest>
    {
        public TransportationRequestController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
