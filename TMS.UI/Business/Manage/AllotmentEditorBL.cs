using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class AllotmentEditorBL : PopupEditor
    {
        public Allotment AEntity => Entity as Allotment;
        public GridView gridView;
        public AllotmentEditorBL() : base(nameof(Allotment))
        {
            Name = "Allotment Editor";
        }

        public async Task UpdateExpenses()
        {
            if (AEntity.UnitPrice == null)
            {
                AEntity.UnitPrice = 0;
            }
            var list = AEntity.Expense.ToList();
            var count = list.Count;
            gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            list.ForEach(x =>
            {
                x.ExpenseTypeId = AEntity.ExpenseTypeId;
                x.IsCollectOnBehaft = AEntity.IsCollectOnBehaft;
                x.IsVat = AEntity.IsVat;
                x.Notes = AEntity.Notes;
                if (AEntity.IsFull)
                {
                    x.UnitPrice = AEntity.UnitPrice;
                }
                else
                {
                    var unitprice = AEntity.UnitPrice / count;
                    x.UnitPrice = unitprice;
                }
                x.TotalPriceBeforeTax = x.UnitPrice * x.Quantity;
                x.TotalPriceAfterTax = x.TotalPriceBeforeTax;
                gridView.UpdateRow(x);
            });
        }

        public override async Task<bool> Save(object entity = null)
        {
            var rs = await base.Save(entity);
            await UpdateTotalFee();
            var grid = ParentForm.FindComponentByName<GridView>(nameof(Expense));
            await grid?.ApplyFilter(true);
            var gridTran = ParentForm.FindComponentByName<GridView>(nameof(Transportation));
            await gridTran?.ApplyFilter(true);
            Dirty = false;
            Dispose();
            return rs;
        }

        private async Task UpdateTotalFee()
        {
            var tranIds = AEntity.Expense.Select(x => x.TransportationId.Value).ToList();
            var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$expand=Expense&$filter=Active eq true and Id in ({string.Join(",", tranIds)})");
            var expenseTypeIds = transportations.SelectMany(x => x.Expense).Where(x => x.ExpenseTypeId != null).Select(x => x.ExpenseTypeId.Value).Distinct().ToList();
            var expenseTypes = await new Client(nameof(MasterData)).GetRawListById<MasterData>(expenseTypeIds);
            var notTotal = expenseTypes.Where(x => x.Additional.IsNullOrWhiteSpace()).Select(x => x.Id).ToList();
            foreach (var item in transportations)
            {
                var details = new List<PatchUpdateDetail>();
                details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = item.Id.ToString() });
                var expenses = item.Expense;
                foreach (var itemDetail in expenseTypes.Select(x => x.Additional).Distinct().ToList())
                {
                    var expenseTypeThisIds = expenseTypes.Where(x => x.Additional == itemDetail).Select(x => x.Id).Distinct().ToList();
                    var totalThisValue = expenses.Where(x => expenseTypeThisIds.Contains(x.ExpenseTypeId.Value)).Sum(x => x.TotalPriceAfterTax);
                    details.Add(new PatchUpdateDetail { Field = itemDetail, Value = totalThisValue.ToString() });
                }
                var path = new PatchUpdate { Changes = details.Where(x => x.Field != null && x.Field != "null").DistinctBy(x => x.Field).ToList() };
                await new Client(nameof(Transportation)).PatchAsync<Transportation>(path);
            }
        }
    }
}