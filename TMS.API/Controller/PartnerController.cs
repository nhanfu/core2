using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class PartnerController : GenericController<Partner>
    {
        private readonly DBAccountantContext db;
        public PartnerController(DBAccountantContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
            db = context;
        }

        public override Task<OdataResult<Partner>> Get(ODataQueryOptions<Partner> options)
        {
            var query = db.Vendor.AsQueryable();
            return ApplyQuery(options, query);
        }

        public override Task<ActionResult<Partner>> UpdateAsync([FromBody] Partner entity, string reasonOfChange = "")
        {
            return base.UpdateAsync(entity, reasonOfChange);
        }
    }
}