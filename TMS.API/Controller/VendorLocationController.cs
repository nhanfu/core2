using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class VendorLocationController : TMSController<VendorLocation>
    {
        public VendorLocationController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        protected override IQueryable<VendorLocation> GetQuery()
        {
            var query = base.GetQuery();
            if (RoleIds.Contains(23) || RoleIds.Contains(24))
            {
                query = query.Where(x => x.ExportListId == VendorId);
            }
            return query;
        }
    }
}
