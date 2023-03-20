using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class BankAccountController : TMSController<BankAccount>
    {
        public BankAccountController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
