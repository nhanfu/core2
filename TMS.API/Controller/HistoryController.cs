using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var query = db.Core_History.Where(x => x.TenantCode == _userSvc.TenantCode).AsNoTracking();
            return ApplyQuery(options, query);
        }

        public override Task<ActionResult<History>> CreateAsync([FromBody] History entity)
        {
            entity.TenantCode = _userSvc.TenantCode.ToString();
            return base.CreateAsync(entity);
        }
    }
}