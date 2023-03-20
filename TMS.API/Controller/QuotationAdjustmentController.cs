using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class QuotationAdjustmentController : TMSController<Quotation>
    {
        public QuotationAdjustmentController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }
    }
}
