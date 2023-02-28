using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class ReturnPlanListBL : TransportationListBL
    {
        public ReturnPlanListBL()
        {
            Name = "ReturnPlan List";
        }

        public async Task CheckQuotationExpense(Transportation transportation, MasterData masterData)
        {
            if (transportation.ContainerTypeId is null || transportation.BrandShipId is null)
            {
                return;
            }
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.Name == nameof(Transportation));
            var listViewItem = gridView.GetListViewItems(transportation).FirstOrDefault();
            if (masterData is null)
            {
                var confirm = new ConfirmDialog
                {
                    Content = "Bạn có muốn xóa phí không?"
                };
                confirm.Render();
                confirm.YesConfirmed += () =>
                {
                    transportation.ReturnVs = 0;
                    listViewItem.UpdateView();
                    var updated = listViewItem.FilterChildren<Number>(x => x.GuiInfo.FieldName == nameof(Transportation.ReturnVs)).ToList();
                    updated.ForEach(x => x.Dirty = true);
                };
            }
            else
            {
                var quotationExpense = await new Client(nameof(QuotationExpense)).FirstOrDefaultAsync<QuotationExpense>($"?$filter=Active eq true and BrandShipId eq {transportation.BrandShipId} and ExpenseTypeId eq {masterData.Id} and BranchId eq {transportation.ExportListId}");
                if (quotationExpense is null)
                {
                    transportation.ReturnVs = 0;
                }
                else
                {
                    if (transportation.Cont20 > 0)
                    {
                        transportation.ReturnVs = quotationExpense.VS20UnitPrice;
                    }
                    else if (transportation.Cont40 > 0)
                    {
                        transportation.ReturnVs = quotationExpense.VS40UnitPrice;
                    }
                }
                listViewItem.UpdateView();
                var updated = listViewItem.FilterChildren<Number>(x => x.GuiInfo.FieldName == nameof(Transportation.ReturnVs)).ToList();
                updated.ForEach(x => x.Dirty = true);
            }
        }

        public override void UpdateQuotation(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));

            Task.Run(async () =>
            {
                var selected = await gridView.GetRealTimeSelectedRows();
                if (selected.Nothing())
                {
                    Toast.Warning("Vui lòng chọn cont cần cập nhật giá!");
                    return;
                }
                var coords = selected.Cast<Transportation>().ToList().LastOrDefault();
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7593 and BossId eq {coords.BossId} and ContainerTypeId eq {coords.ContainerTypeId} and LocationId eq {coords.ReturnId} and StartDate le {coords.ReturnDate.Value.ToOdataFormat()} and PackingId eq {coords.ReturnVendorId}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7593,
                        BossId = coords.BossId,
                        ContainerTypeId = coords.ContainerTypeId,
                        LocationId = coords.ReturnId,
                        StartDate = coords.ReturnDate,
                        PackingId = coords.ReturnVendorId
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá trả hàng";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public void CheckStatusQuotationReturn()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            gridView.BodyContextMenuShow += () =>
            {
                ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                {
                        new ContextMenuItem { Icon = "fas fa-pen", Text = "Cập nhật giá", Click = UpdateQuotation },
                        new ContextMenuItem { Icon = "fal fa-street-view", Text = "Xem kế hoạch", Click = ViewTransportationPlan },
                };
            };
            var listViewItems = gridView.RowData.Data.Cast<Transportation>().ToList();
            ChangeBackgroudColorReturn(listViewItems);
        }

        public void ChangeBackgroudColorReturn(List<Transportation> listViewItems)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            listViewItems.ForEach(x =>
            {
                var listViewItem = gridView.AllListViewItem.FirstOrDefault(y => y.Entity[IdField].ToString() == x.Id.ToString());
                if (listViewItem is null)
                {
                    return;
                }
                listViewItem.Element.RemoveClass("bg-red1");
                if (x.DemDate != null && x.ReturnDate != null && Convert.ToDateTime(x.ReturnDate.Value).Date > Convert.ToDateTime(x.DemDate.Value).Date)
                {
                    listViewItem.Element.AddClass("bg-red1");
                }
            });
        }

        public void AfterPatchUpdateTransportationReturn(Transportation transportation, PatchUpdate patchUpdate, ListViewItem listViewItem)
        {
            if (listViewItem is null)
            {
                return;
            }
            listViewItem.Element.RemoveClass("bg-red1");
            listViewItem.Element.RemoveClass("bg-red");
            if (transportation.DemDate != null && transportation.ReturnDate != null && Convert.ToDateTime(transportation.ReturnDate.Value).Date > Convert.ToDateTime(transportation.DemDate.Value).Date)
            {
                listViewItem.Element.AddClass("bg-red1");
            }
            if (!transportation.IsQuotationReturn)
            {
                listViewItem.Element.AddClass("bg-red");
            }
        }

        public override async Task Allotment()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            var selected = (await gridView.GetRealTimeSelectedRows()).Cast<Transportation>().Where(x => x.Id > 0).ToList();
            if (selected.Nothing())
            {
                Toast.Warning("Vui lòng chọn cont cần phân bổ");
                return;
            }
            var fees = selected.Select(x => new Expense
            {
                ExpenseTypeId = null,
                UnitPrice = 0,
                Quantity = 1,
                IsReturn = true,
                TotalPriceAfterTax = 0,
                TotalPriceBeforeTax = 0,
                Vat = 0,
                ContainerNo = x.ContainerNo,
                SealNo = x.SealNo,
                BossId = x.BossId,
                CommodityId = x.CommodityId,
                ClosingDate = x.ClosingDate,
                ReturnDate = x.ReturnDate,
                TransportationId = x.Id
            }).ToList();
            await this.OpenPopup(
                featureName: "Allotment Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.AllotmentEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Phân bổ chi phí trả hàng";
                    instance.Entity = new Allotment
                    {
                        Expense = fees
                    };
                    return instance;
                });
        }

        public override void BeforeCreatedExpense(Expense expense)
        {
            if (selected is null)
            {
                Toast.Warning("Vui lòng chọn cont cần nhập");
                return;
            }
            expense.ContainerNo = selected.ContainerNo;
            expense.SealNo = selected.SealNo;
            expense.BossId = selected.BossId;
            expense.CommodityId = selected.CommodityId;
            expense.ContainerTypeId = selected.ContainerTypeId;
            expense.RouteId = selected.RouteId;
            expense.YearText = selected.YearText;
            expense.MonthText = selected.MonthText;
            expense.Id = 0;
            expense.TransportationId = selected.Id;
            expense.Quantity = 1;
            expense.IsReturn = true;
        }

        public override async Task ReloadExpense(Transportation transportation)
        {
            selected = transportation;
            gridViewExpense = this.FindComponentByName<GridView>(nameof(Expense));
            gridViewExpense.DataSourceFilter = $"?$filter=Active eq true and TransportationId eq {transportation.Id} and IsReturn eq true and ((ExpenseTypeId in (15981, 15939) eq false)  or IsPurchasedInsurance eq true)";
            await gridViewExpense.ApplyFilter(true);
        }

        public override void CheckReturnDate(Transportation Transportation)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var listViewItem = gridView.GetListViewItems(Transportation).FirstOrDefault();
            if (Transportation.ReturnDate != null && Transportation.ReturnDate.Value.Date < Transportation.ShipDate.Value.Date)
            {
                var confirmDialog = new ConfirmDialog
                {
                    Content = "Ngày đóng hàng nhỏ hơn ngày tàu cập?"
                };
                confirmDialog.NoConfirmed += () =>
                {
                    Transportation.ReturnDate = null;
                    listViewItem.FilterChildren<EditableComponent>(x => x.GuiInfo.FieldName == nameof(Transportation.ReturnDate)).ForEach(x => x.Dirty = true);
                };
                AddChild(confirmDialog);
            }
        }

        public async Task RequestUnClosing(Transportation transportation, PatchUpdate patch)
        {
            var tran = new TransportationListAccountantBL();
            await tran.RequestUnClosing(transportation, patch);
        }
    }
}