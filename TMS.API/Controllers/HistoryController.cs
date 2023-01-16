using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class HistoryController : GenericController<History>
    {
        private readonly HistoryContext db;
        public HistoryController(HistoryContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
            db = context;
        }

        public override Task<OdataResult<History>> Get(ODataQueryOptions<History> options)
        {
            var query = db.ECOMMERCE_History.AsQueryable();
            return ApplyQuery(options, query);
        }

        public override Task<ActionResult<History>> CreateAsync([FromBody] History entity)
        {
            entity.TanentCode = _userSvc.VendorId.ToString();
            return base.CreateAsync(entity);
        }
    }
}