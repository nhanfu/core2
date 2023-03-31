using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ConvertationController : TMSController<Convertation>
    {
        public ConvertationController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
