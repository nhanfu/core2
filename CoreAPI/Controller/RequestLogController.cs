using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Core.Models;

namespace Core.Controllers
{
    public class RequestLogController : GenericController<RequestLog>
    {
        private readonly LOGContext db;
        public RequestLogController(LOGContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
            db = context;
        }

        public override Task<OdataResult<RequestLog>> Get(ODataQueryOptions<RequestLog> options)
        {
            var query = db.RequestLog.AsQueryable();
            return ApplyQuery(options, query);
        }
    }
}