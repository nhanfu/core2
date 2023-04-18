using Core.Extensions;
using Microsoft.AspNetCore.Http;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class TransportationRequestDetailsController : GenericController<TransportationRequestDetails>
    {
        public TransportationRequestDetailsController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}