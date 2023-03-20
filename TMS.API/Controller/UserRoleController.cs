using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class UserRoleController : TMSController<UserRole>
    {
        public UserRoleController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
