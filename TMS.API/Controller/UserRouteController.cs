using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class UserRouteController : TMSController<UserRoute>
    {
        public UserRouteController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
