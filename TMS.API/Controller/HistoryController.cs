using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class HistoryController : GenericController<History>
    {
        private readonly HistoryContext db;
        public HistoryController(HistoryContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
            db = context;
        }

        public override Task<OdataResult<History>> Get(ODataQueryOptions<History> options)
        {
            var query = db.Core_History.AsQueryable();
            return ApplyQuery(options, query);
        }

        public override Task<ActionResult<History>> CreateAsync([FromBody] History entity)
        {
            entity.TanentCode = _userSvc.VendorId.ToString();
            return base.CreateAsync(entity);
        }
    }
}