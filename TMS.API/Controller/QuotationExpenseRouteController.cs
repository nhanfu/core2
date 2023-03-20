using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class QuotationExpenseRouteController : TMSController<QuotationExpenseRoute>
    {
        public QuotationExpenseRouteController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
