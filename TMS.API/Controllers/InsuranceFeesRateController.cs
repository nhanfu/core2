using Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using TMS.API.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Core.Enums;

namespace TMS.API.Controllers
{
    public class InsuranceFeesRateController : TMSController<InsuranceFeesRate>
    {
        public InsuranceFeesRateController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<InsuranceFeesRate>> CreateAsync([FromBody] InsuranceFeesRate entity)
        {
            var check = await db.InsuranceFeesRate.Where(x => x.JourneyId == entity.JourneyId
            && x.TransportationTypeId == entity.TransportationTypeId
            && x.IsBought == entity.IsBought
            && x.IsWet == entity.IsWet
            && x.IsSOC == entity.IsSOC
            && x.IsVAT == entity.IsVAT).FirstOrDefaultAsync();
            if (check != null)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await base.CreateAsync(entity);
        }

        public override async Task<ActionResult<InsuranceFeesRate>> UpdateAsync([FromBody] InsuranceFeesRate entity, string reasonOfChange = "")
        {
            var check = await db.InsuranceFeesRate.Where(x => x.JourneyId == entity.JourneyId
            && x.TransportationTypeId == entity.TransportationTypeId
            && x.IsBought == entity.IsBought
            && x.IsWet == entity.IsWet
            && x.IsSOC == entity.IsSOC
            && x.IsVAT == entity.IsVAT).FirstOrDefaultAsync();
            if (check != null)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await base.UpdateAsync(entity, reasonOfChange);
        }
    }
}
