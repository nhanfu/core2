using Bridge.Html5;
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
    public class TransportationReturnEditorBL : PopupEditor
    {
        public GridView gridView;
        public Transportation TransportationEntity => Entity as Transportation;

        public TransportationReturnEditorBL() : base(nameof(Transportation))
        {
            Name = "Transportation Return Editor";
        }

        public virtual void BeforeCreatedExpense(Expense expense)
        {
            if (TransportationEntity is null)
            {
                Toast.Warning("Vui lòng chọn cont cần nhập");
                return;
            }
            expense.ContainerNo = TransportationEntity.ContainerNo;
            expense.SealNo = TransportationEntity.SealNo;
            expense.BossId = TransportationEntity.BossId;
            expense.CommodityId = TransportationEntity.CommodityId;
            expense.ContainerTypeId = TransportationEntity.ContainerTypeId;
            expense.RouteId = TransportationEntity.RouteId;
            expense.YearText = TransportationEntity.YearText;
            expense.MonthText = TransportationEntity.MonthText;
            expense.TransportationId = TransportationEntity.Id;
            expense.Id = 0;
            expense.Quantity = 1;
            expense.IsReturn = true;
        }

        public async Task AfterCreatedExpense(Expense expense, PatchUpdate patchUpdate, ListViewItem listViewItem1)
        {
            var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$expand=Expense&$filter=Active eq true and Id in ({string.Join(",", TransportationEntity.Id)})");
            var expenseTypeIds = transportations.SelectMany(x => x.Expense).Where(x => x.ExpenseTypeId != null).Select(x => x.ExpenseTypeId.Value).Distinct().ToList();
            var expenseTypes = await new Client(nameof(MasterData)).GetRawListById<MasterData>(expenseTypeIds);
            var notTotal = expenseTypes.Where(x => x.Additional.IsNullOrWhiteSpace()).Select(x => x.Id).ToList();
            foreach (var item in transportations)
            {
                var details = new List<PatchUpdateDetail>()
                {
                    new PatchUpdateDetail { Field = Utils.IdField, Value = item.Id.ToString() }
                };
                var expenses = item.Expense;
                foreach (var itemDetail in expenseTypes.Select(x => x.Additional).Distinct().ToList())


                {
                    var expenseTypeThisIds = expenseTypes.Where(x => x.Additional == itemDetail).Select(x => x.Id).Distinct().ToList();
                    var totalThisValue = expenses.Where(x => expenseTypeThisIds.Contains(x.ExpenseTypeId.Value)).Sum(x => x.TotalPriceAfterTax);
                    details.Add(new PatchUpdateDetail { Field = itemDetail, Value = totalThisValue.ToString() });
                }
                var path = new PatchUpdate { Changes = details.Where(x => x.Field != null && x.Field != "null" && x.Field != "").DistinctBy(x => x.Field).ToList() };
                await new Client(nameof(Transportation)).PatchAsync<Transportation>(path, ig: $"&disableTrigger=true");
            }
        }

        public void CalcTax(Expense expense)
        {
            var grid = this.FindComponentByName<GridView>(nameof(Expense));
            var listViewItem = grid.GetListViewItems(expense).FirstOrDefault();
            expense.TotalPriceBeforeTax = expense.UnitPrice * expense.Quantity;
            expense.TotalPriceAfterTax = expense.TotalPriceBeforeTax + expense.TotalPriceBeforeTax * expense.Vat / 100;
            if (listViewItem != null)
            {
                listViewItem.UpdateView();
                var updated = listViewItem.FilterChildren(x => x.GuiInfo.FieldName == nameof(Expense.TotalPriceBeforeTax) || x.GuiInfo.FieldName == nameof(Expense.TotalPriceAfterTax));
                updated.ForEach(x => x.Dirty = true);
            }
        }

        public override void Dispose()
        {
            var parent = TabEditor as ReturnPlanListBL;
            parent._expensePopup = null;
            base.Dispose();
        }
    }
}