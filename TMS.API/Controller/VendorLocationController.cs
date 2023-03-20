using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class VendorLocationController : TMSController<VendorLocation>
    {
        public VendorLocationController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        protected override IQueryable<VendorLocation> GetQuery()
        {
            var query = base.GetQuery();
            query = query.Where(x => x.ExportListId == VendorId);
            return query;
        }
    }
}
