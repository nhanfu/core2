using Microsoft.AspNetCore.Http;
using System.Linq;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class VendorLocationController : TMSController<VendorLocation>
    {
        public VendorLocationController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
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
