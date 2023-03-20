using Microsoft.AspNetCore.Http;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class TransportationRequestController : TMSController<TransportationRequest>
    {
        public TransportationRequestController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }
    }
}
