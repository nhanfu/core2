using Microsoft.AspNetCore.Http;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class QuotationExpenseRouteController : TMSController<QuotationExpenseRoute>
    {
        public QuotationExpenseRouteController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {

        }
    }
}
