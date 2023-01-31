using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using static Retyped.dom.Literals.Types;
using Event = Bridge.Html5.Event;

namespace TMS.UI.Business.Manage
{
    public class CommodityValueBL : TabEditor
    {
        GridView gridView;
        private HTMLInputElement _uploader;
        public CommodityValueBL() : base(nameof(CommodityValue))
        {
            Name = "CommodityValue List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcel(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
        }

        public async Task EditCommodityValue(CommodityValue entity)
        {
            await this.OpenPopup(
                featureName: "CommodityValue Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.CommodityValueEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa giá trị hàng hóa";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddCommodityValue()
        {
            await this.OpenPopup(
                featureName: "CommodityValue Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.CommodityValueEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới giá trị hàng hóa";
                    instance.Entity = new CommodityValue();
                    return instance;
                });
        }

        private async Task SelectedExcel(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploader.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<CommodityValue>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportExcel",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportExcel()
        {
            _uploader.Click();
        }

        public async Task CheckCommodityValueTerm()
        {
            var commodityValueDB = await new Client(nameof(CommodityValue)).GetRawList<CommodityValue>($"?$fiter=Active eq true");
            var commodityValues = commodityValueDB.Where(x => x.EndDate <= DateTime.Now.Date).ToList();
            var startDate1 = new DateTime(DateTime.Now.Year, 1, 1);
            var startDate2 = new DateTime(DateTime.Now.Year, 6, 30);
            var endDate1 = new DateTime(DateTime.Now.Year, 7, 1);
            var endDate2 = new DateTime(DateTime.Now.Year, 12, 31);
            var updated = commodityValues.ForEach<CommodityValue>(x => x.Active = false).ToList();
            await new Client(nameof(CommodityValue)).BulkUpdateAsync<CommodityValue>(updated);
            foreach (var item in commodityValues)
            {
                var newCommodityValue = new CommodityValue()
                {
                    BossId = item.BossId,
                    CommodityId = item.CommodityId,
                    ContainerId = item.ContainerId,
                    TotalPrice = item.TotalPrice,
                    IsWet = item.IsWet,
                    Active = true,
                    InsertedBy = 1,
                    InsertedDate = DateTime.Now.Date
                };
                if (DateTime.Now.Date >= startDate1 && DateTime.Now.Date <= startDate2)
                {
                    newCommodityValue.StartDate = startDate1;
                }
                else if (DateTime.Now.Date >= endDate1 && DateTime.Now.Date <= endDate2)
                {
                    newCommodityValue.StartDate = endDate1;
                }
                if (DateTime.Now.Date >= startDate1 && DateTime.Now.Date <= startDate2)
                {
                    newCommodityValue.EndDate = startDate2;
                }
                else if (DateTime.Now.Date >= endDate1 && DateTime.Now.Date <= endDate2)
                {
                    newCommodityValue.EndDate = endDate2;
                }
                await new Client(nameof(CommodityValue)).CreateAsync(newCommodityValue);
            }
        }

        private int commodityAwaiter;

        public void UpdateCommodityValue()
        {
            Window.ClearTimeout(commodityAwaiter);
            commodityAwaiter = Window.SetTimeout(async () =>
            {
                await UpdateCommodityAsync();
            }, 500);
        }

        public async Task UpdateCommodityAsync()
        {
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault();
            var commodityValueEntity = gridView.GetSelectedRows().Cast<CommodityValue>().ToList().FirstOrDefault();
            if (commodityValueEntity != null && commodityValueEntity.BossId > 0 && commodityValueEntity.CommodityId > 0 && commodityValueEntity.ContainerId > 0)
            {
                var commodityValue = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {commodityValueEntity.BossId} and CommodityId eq {commodityValueEntity.CommodityId} and ContainerId eq {commodityValueEntity.ContainerId}");
                if (commodityValue.TotalPrice != commodityValueEntity.TotalPrice)
                {
                    var confirm = new ConfirmDialog
                    {
                        Content = "Bạn có muốn cập nhật GTHH này từ " + commodityValue.TotalPrice.ToString("N0") + " thành " + commodityValueEntity.TotalPrice.ToString("N0") + " không?"
                    };
                    var selectedCommodityValue = gridView.RowData.Data.Cast<CommodityValue>().ToList().Where(x => x.BossId == commodityValueEntity.BossId && x.CommodityId == commodityValueEntity.CommodityId && x.ContainerId == commodityValueEntity.ContainerId && x.Active == true).FirstOrDefault();
                    confirm.Render();
                    confirm.YesConfirmed += async () =>
                    {
                        gridView.RemoveRow(selectedCommodityValue);
                        commodityValue.EndDate = DateTime.Now.Date;
                        commodityValue.Active = false;
                        await new Client(nameof(CommodityValue)).PatchAsync<object>(GetPatchEntity(commodityValue));
                        var newCommodityValue = CreateCommodityValue(commodityValueEntity);
                        await new Client(nameof(CommodityValue)).CreateAsync<CommodityValue>(newCommodityValue);
                        await gridView.AddRow(newCommodityValue);
                        Toast.Success("Đã cập nhật thành công");
                        UpdateView();
                        var expenseContainerType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7565 and Id eq {commodityValue.ContainerId}");
                        var containerTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7565 and contains(Description, '{expenseContainerType.Description}')");
                        var containerTypeCodes = containerTypes.Select(x => x.Id).ToList();
                        var startDate = newCommodityValue.StartDate.Value.Date.ToString("yyyy-MM-dd");
                        var expenseTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and (contains(Name, 'BH SOC') or contains(Name, 'Bảo hiểm'))");
                        var expenseTypeCodes = expenseTypes.Select(x => x.Id).ToList();
                        var expenses = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and BossId eq {commodityValueEntity.BossId} and CommodityId eq {commodityValueEntity.CommodityId} and ContainerTypeId in ({containerTypeCodes.Combine()}) and (StartShip ge {startDate} or StartShip eq null) and IsPurchasedInsurance eq false and ExpenseTypeId in ({expenseTypeCodes.Combine()}) and RequestChangeId eq null");
                        foreach (var item in expenses)
                        {
                            item.CommodityValue = newCommodityValue.TotalPrice;
                            var checkIsSOC = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and Id eq {item.ExpenseTypeId}");
                            if (checkIsSOC.Name.Contains("BH SOC"))
                            {
                                await CalcInsuranceFees(item, true);
                            }
                            else
                            {
                                await CalcInsuranceFees(item, false);
                            }
                        }
                        var rs = await new Client(nameof(Expense)).BulkUpdateAsync(expenses);
                        if (rs != null)
                        {
                            Toast.Success("Đã áp dụng thành công GTHH");
                        }
                        else
                        {
                            Toast.Warning("Đã áp dụng thất bại GTHH");
                        }
                    };
                    confirm.NoConfirmed += async () =>
                    {
                        commodityValueEntity.TotalPrice = commodityValue.TotalPrice;
                        gridView.UpdateRow(selectedCommodityValue);
                        var listViewItem = gridView.GetListViewItems(selectedCommodityValue).FirstOrDefault();
                        var updated = listViewItem.FilterChildren<Number>(x => x.GuiInfo.FieldName == nameof(CommodityValue.TotalPrice)).ToList();
                        updated.ForEach(x => x.Dirty = true);
                        await new Client(nameof(CommodityValue)).PatchAsync<object>(GetPatchEntity(commodityValueEntity));
                    };
                }
            }
        }

        private CommodityValue CreateCommodityValue(CommodityValue commodityValue)
        {
            var startDate1 = new DateTime(DateTime.Now.Year, 1, 1);
            var endDate1 = new DateTime(DateTime.Now.Year, 6, 30);
            var startDate2 = new DateTime(DateTime.Now.Year, 7, 1);
            var endDate2 = new DateTime(DateTime.Now.Year, 12, 31);
            var newCommodityValue = new CommodityValue();
            newCommodityValue.CopyPropFrom(commodityValue);
            newCommodityValue.Id = 0;
            newCommodityValue.StartDate = DateTime.Now.Date;
            newCommodityValue.Active = true;
            newCommodityValue.InsertedDate = DateTime.Now.Date;
            if (DateTime.Now.Date >= startDate1 && DateTime.Now.Date <= endDate1)
            {
                newCommodityValue.EndDate = endDate1;
            }
            if (DateTime.Now.Date >= startDate2 && DateTime.Now.Date <= endDate2)
            {
                newCommodityValue.EndDate = endDate2;
            }
            return newCommodityValue;
        }

        private async Task CalcInsuranceFees(Expense expense, bool isSOC)
        {
            bool isSubRatio = false;
            if (((expense.IsWet || expense.SteamingTerms || expense.BreakTerms) && expense.IsBought == false) || (expense.IsBought && expense.IsWet))
            {
                isSubRatio = true;
            }
            var journeyId = expense.JourneyId is null ? "" : "and JourneyId eq " + expense.JourneyId.ToString();
            var insuranceFeesRateDB = await new Client(nameof(InsuranceFeesRate)).FirstOrDefaultAsync<InsuranceFeesRate>($"?$filter=Active eq true and TransportationTypeId eq {expense.TransportationTypeId} and JourneyId eq {expense.JourneyId} and IsBought eq {expense.IsBought.ToString().ToLower()} and IsSubRatio eq {isSubRatio.ToString().ToLower()} and IsSOC eq {isSOC.ToLower()}");
            if (insuranceFeesRateDB != null)
            {
                var getContainerType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq {expense.ContainerTypeId}");
                if (getContainerType != null && getContainerType.Description.Contains("Lạnh") && insuranceFeesRateDB.TransportationTypeId == 11673 && insuranceFeesRateDB.JourneyId == 12114)
                {
                    var insuranceFeesRateColdDB = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq 25391");
                    expense.InsuranceFeeRate = insuranceFeesRateColdDB != null ? decimal.Parse(insuranceFeesRateColdDB.Name) : 0;
                }
                else
                {
                    expense.InsuranceFeeRate = insuranceFeesRateDB.Rate;
                }
                if (insuranceFeesRateDB.IsSubRatio && expense.IsBought == false)
                {
                    var extraInsuranceFeesRateDB = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 25374");
                    extraInsuranceFeesRateDB.ForEach(x =>
                    {
                        var prop = expense.GetType().GetProperties().Where(y => y.Name == x.Name && bool.Parse(y.GetValue(expense, null).ToString())).FirstOrDefault();
                        if (prop != null)
                        {
                            expense.InsuranceFeeRate += decimal.Parse(x.Code);
                        }
                    });
                }
            }
            else
            {
                expense.InsuranceFeeRate = 0;
                expense.TotalPriceBeforeTax = 0;
                expense.TotalPriceAfterTax = 0;
            }
            if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == true)
            {
                CalcInsuranceFeeNoVAT(expense);
            }
            else if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == false)
            {
                CalcInsuranceFee(expense);
            }
        }

        private void CalcInsuranceFee(Expense expense)
        {
            expense.TotalPriceBeforeTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
            expense.TotalPriceAfterTax = expense.TotalPriceBeforeTax + Math.Round(expense.TotalPriceBeforeTax * expense.Vat / 100, 0);
        }

        private void CalcInsuranceFeeNoVAT(Expense expense)
        {
            expense.TotalPriceAfterTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
            expense.TotalPriceBeforeTax = Math.Round(expense.TotalPriceAfterTax / (decimal)1.1, 0);
        }

        public PatchUpdate GetPatchEntity(CommodityValue commodityValue)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = commodityValue.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(CommodityValue.Active), Value = commodityValue.Active.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(CommodityValue.EndDate), Value = commodityValue.EndDate.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(CommodityValue.TotalPrice), Value = commodityValue.TotalPrice.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}