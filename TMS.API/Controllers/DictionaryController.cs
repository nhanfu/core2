using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class DictionaryController : TMSController<Dictionary>
    {
        public DictionaryController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {

        }

        [AllowAnonymous]
        public override Task<OdataResult<Dictionary>> Get(ODataQueryOptions<Dictionary> options)
        {
            return base.Get(options);
        }
    }
}
