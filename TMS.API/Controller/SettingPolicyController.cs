using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class SettingPolicyController : TMSController<SettingPolicy>
    {
        public SettingPolicyController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }

        public override async Task<ActionResult<bool>> HardDeleteAsync([FromBody] List<int> ids)
        {
            if (ids.Nothing())
            {
                return true;
            }
            ids = ids.Where(x => x > 0).ToList();
            if (ids.Nothing())
            {
                return true;
            }
            try
            {
                var deleteCommand = $"delete from [{typeof(SettingPolicyDetail).Name}] where SettingPolicyId in ({string.Join(",", ids)}) " +
                    $" delete from [{typeof(SettingPolicy).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteCommand);
                return true;
            }
            catch
            {
                throw new ApiException("Không thể xóa dữ liệu!") { StatusCode = HttpStatusCode.BadRequest };
            }
        }
    }
}
