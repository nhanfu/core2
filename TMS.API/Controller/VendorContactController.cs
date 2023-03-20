using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class VendorContactController : TMSController<VendorContact>
    {
        public VendorContactController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
