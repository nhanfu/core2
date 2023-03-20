using Core.Exceptions;
using Core.Enums;
using Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class AllotmentController : TMSController<Allotment>
    {
        public AllotmentController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
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
                var deleteCommand = $"delete from [{typeof(Expense).Name}] where {nameof(Expense.AllotmentId)} in ({string.Join(",", ids)}) ; delete from [{typeof(Allotment).Name}] where Id in ({string.Join(",", ids)})";
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
