using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class IntroController : TMSController<Intro>
    {
        public IntroController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
