using Core.Extensions;
using Core.Models;

namespace Core.Controllers
{
    public class UserRoleController : TMSController<UserRole>
    {
        public UserRoleController(CoreContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
