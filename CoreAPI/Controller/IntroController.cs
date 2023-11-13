using Core.Extensions;
using Core.Models;

namespace Core.Controllers
{
    public class IntroController : TMSController<Intro>
    {
        public IntroController(CoreContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
