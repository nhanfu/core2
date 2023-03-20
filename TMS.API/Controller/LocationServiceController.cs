using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class LocationServiceController : TMSController<LocationService>
    {
        public LocationServiceController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
