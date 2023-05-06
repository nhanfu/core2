using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using TMS.API.Models;
using TMS.API.Services;

namespace TMS.API.Controllers
{
    public class TransportationRequestController : TMSController<TransportationRequest>
    {
        public TransportationRequestController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [HttpGet("api/[Controller]/GetByRole")]
        public Task<OdataResult<TransportationRequest>> UserClick(ODataQueryOptions<TransportationRequest> options)
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(TransportationRequest).Name}]
                    where 1 = 1 and (Active = 1 or Active = 0) and InsertedBy = {UserId}";
            var qr = db.TransportationRequest.FromSqlRaw(sql);
            return ApplyQuery(options, qr, sql: sql);
        }
    }
}
