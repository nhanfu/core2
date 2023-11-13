using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Core.Models;

namespace Core.Controllers
{
    public class DictionaryController : TMSController<Dictionary>
    {
        public DictionaryController(CoreContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }

        protected override IQueryable<Dictionary> GetQuery()
        {
            return db.Dictionary.Where(x => x.TenantCode == _userSvc.TenantCode);
        }

        [AllowAnonymous]
        public override Task<OdataResult<Dictionary>> Get(ODataQueryOptions<Dictionary> options)
        {
            return ApplyQuery(options, GetQuery());
        }
    }
}
