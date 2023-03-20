using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class DictionaryController : TMSController<Dictionary>
    {
        public DictionaryController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }

        [AllowAnonymous]
        public override Task<OdataResult<Dictionary>> Get(ODataQueryOptions<Dictionary> options)
        {
            return base.Get(options);
        }
    }
}
