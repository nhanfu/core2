using Microsoft.AspNetCore.Http;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class InsuranceFeesController : TMSController<Transportation>
    {
        public InsuranceFeesController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }
    }
}
