using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class VendorServiceController : TMSController<VendorService>
    {
        public VendorServiceController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}