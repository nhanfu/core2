using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class AllotmentController : TMSController<Allotment>
    {
        public AllotmentController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
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
                var exps = await db.Expense.Where(x => x.AllotmentId != null).AsNoTracking().Where(x => ids.Contains(x.AllotmentId.Value)).ToListAsync();
                var deleteCommand = $"delete from [{typeof(Expense).Name}] where {nameof(Expense.AllotmentId)} in ({string.Join(",", ids)}) ; delete from [{typeof(Allotment).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteCommand);
                var tranids = exps.Select(x => x.TransportationId).Where(x => x != null).Distinct().ToList();
                var trans = await db.Transportation.Include(x => x.Expense).Where(x => tranids.Contains(x.Id)).ToListAsync();
                var expenseTypeIds = exps.Select(x => x.ExpenseTypeId).Where(x => x != null).Distinct().ToList();
                var expenseTypes = await db.MasterData.AsNoTracking().Where(x => expenseTypeIds.Contains(x.Id)).ToListAsync();
                foreach (var item in trans)
                {
                    var details = new List<PatchUpdateDetail>();
                    var expenses = item.Expense;
                    foreach (var itemDetail in expenseTypes.Select(x => x.Additional).Distinct().Where(x => !x.IsNullOrWhiteSpace()).ToList())
                    {
                        var expenseTypeThisIds = expenseTypes.Where(x => x.Additional == itemDetail).Select(x => x.Id).Distinct().ToList();
                        var totalThisValue = expenses.Where(x => expenseTypeThisIds.Contains(x.ExpenseTypeId.Value)).Sum(x => x.TotalPriceAfterTax);
                        item.SetPropValue(itemDetail, totalThisValue);
                    }
                }
                await db.SaveChangesAsync();
                return true;
            }
            catch
            {
                throw new ApiException("Không thể xóa dữ liệu!") { StatusCode = HttpStatusCode.BadRequest };
            }
        }
    }
}
