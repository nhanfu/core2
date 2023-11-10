using Core.Extensions;
using Core.Models;

namespace Core.Controllers
{
    public class UserRoleController : TMSController<UserRole>
    {
        public UserRoleController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
