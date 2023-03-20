using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.Services;

namespace TMS.API.Controllers
{
    public class VendorServiceController : TMSController<VendorService>
    {
        public VendorServiceController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }
    }
}