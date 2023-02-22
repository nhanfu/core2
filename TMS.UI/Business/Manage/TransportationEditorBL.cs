using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using static Retyped.dom.Literals.Types;
using MasterData = Core.Models.MasterData;

namespace TMS.UI.Business.Manage
{
    public class TransportationEditorBL : PopupEditor
    {
        public GridView gridView;
        public Transportation TransportationEntity => Entity as Transportation;

        public TransportationEditorBL() : base(nameof(Transportation))
        {
            Name = "Transportation Editor";
        }

        public async Task CalcTax(Expense expense)
        {
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault();
            MasterData expenseType = null;
            if (expense.ExpenseTypeId != null)
            {
                expenseType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and Id eq {expense.ExpenseTypeId}");
            }
            if (expenseType != null && expenseType.Name.Contains("Bảo hiểm") && expenseType.Name.Contains("BH SOC"))
            {
                return;
            }
            else
            {
                expense.TotalPriceBeforeTax = expense.UnitPrice * expense.Quantity;
                expense.TotalPriceAfterTax = expense.TotalPriceBeforeTax + expense.TotalPriceBeforeTax * expense.Vat / 100;
            }
            await gridView.AddOrUpdateRow(expense);
            var expenseTypeIds = TransportationEntity.Expense.Where(x => x.ExpenseTypeId != null).Select(x => x.ExpenseTypeId.Value).Distinct().ToList();
            var expenseTypes = await new Client(nameof(MasterData)).GetRawListById<MasterData>(expenseTypeIds);
            foreach (var item in expenseTypes)
            {
                var totalThisValue = TransportationEntity.Expense.Where(x => x.ExpenseTypeId == item.Id).Sum(x => x.TotalPriceAfterTax);
                TransportationEntity.SetComplexPropValue(item.Description, totalThisValue);
            }

            foreach (var item in expenseTypes.Select(x => x.Additional).Distinct().ToList())
            {
                var expenseTypeThisIds = expenseTypes.Where(x => x.Additional == item).Select(x => x.Id).Distinct().ToList();
                var totalThisValue = TransportationEntity.Expense.Where(x => expenseTypeThisIds.Contains(x.ExpenseTypeId.Value)).Sum(x => x.TotalPriceAfterTax);
                TransportationEntity.SetComplexPropValue(item, totalThisValue);
            }
        }

        private int commodityAwaiter;

        public async void UpdateCommodityValue(Expense expense)
        {
            Window.ClearTimeout(commodityAwaiter);
            commodityAwaiter = Window.SetTimeout(async () =>
            {
                await UpdateCommodityAsync(expense);
            }, 500);
            await CalcTax(expense);
        }

        private async Task UpdateCommodityAsync(Expense expense)
        {
            var expenseType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and Id eq {expense.ExpenseTypeId}");
            if (expenseType.Name.Contains("Bảo hiểm") == false && expenseType.Name.Contains("BH SOC") == false)
            {
                return;
            }
            if (expense.BossId != null && expense.BossId > 0 && expense.CommodityId != null && expense.CommodityId > 0 && expense.ContainerTypeId != null && expense.ContainerTypeId > 0)
            {
                var containerId = await CheckContainerType(expense);
                var commodityValueDB = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {expense.BossId} and CommodityId eq {expense.CommodityId} and ContainerId eq {containerId}");
                if (commodityValueDB is null)
                {
                    var newCommodityValue = CreateCommodityValue(expense);
                    newCommodityValue.StartDate = DateTime.Now.Date;
                    await new Client(nameof(CommodityValue)).CreateAsync(newCommodityValue);
                }
                else
                {
                    if (expense.CommodityValue != commodityValueDB.TotalPrice)
                    {
                        var confirm = new ConfirmDialog
                        {
                            Content = "Bạn có muốn lưu giá trị này vào bảng GTHH không?",
                        };
                        confirm.Render();
                        confirm.YesConfirmed += async () =>
                        {
                            commodityValueDB.EndDate = DateTime.Now.Date;
                            commodityValueDB.Active = false;
                            await new Client(nameof(CommodityValue)).PatchAsync<object>(GetPatchEntity(commodityValueDB));
                            var newCommodityValue = CreateCommodityValue(expense);
                            newCommodityValue.TotalPrice = (decimal)expense.CommodityValue;
                            newCommodityValue.StartDate = DateTime.Now.Date;
                            await new Client(nameof(CommodityValue)).CreateAsync(newCommodityValue);
                        };
                    }
                    GridView gridView = this.FindComponentByName<GridView>(nameof(Expense));
                    var masterDataDB = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq 11685");
                    var vat = decimal.Parse(masterDataDB.Name);
                    if (expenseType.Name.Contains("Bảo hiểm"))
                    {
                        await CalcInsuranceFees(expense, false);
                    }
                    else if (expenseType.Name.Contains("BH SOC"))
                    {
                        await CalcInsuranceFees(expense, true);
                    }
                    await new Client(nameof(Expense)).PatchAsync<object>(GetPatchEntity(expense));
                    var listViewItem = gridView.GetListViewItems(expense).FirstOrDefault();
                    listViewItem.UpdateView();
                    var updated = listViewItem.FilterChildren<Number>(x => x.GuiInfo.FieldName == nameof(Expense.CommodityValue) || x.GuiInfo.FieldName == nameof(Expense.TotalPriceBeforeTax) || x.GuiInfo.FieldName == nameof(Expense.TotalPriceAfterTax)).ToList();
                    updated.ForEach(x => x.Dirty = true);
                    await listViewItem.PatchUpdate();
                }
            }
        }

        private int containerId = 0;

        public async Task<int> CheckContainerType(Expense expense)
        {
            var containerTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7565");
            var containerTypeCodes = containerTypes.ToDictionary(x => x.Id);
            var containerTypeName = containerTypeCodes.GetValueOrDefault((int)expense.ContainerTypeId);
            var containers = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and (contains(Name, '40HC') or contains(Name, '20DC'))");
            if (containerTypeName.Description.Contains("Cont 20"))
            {
                containerId = containers.Find(x => x.Name.Contains("20DC")).Id;
            }
            else if (containerTypeName.Description.Contains("Cont 40"))
            {
                containerId = containers.Find(x => x.Name.Contains("40HC")).Id;
            }
            return containerId;
        }

        private CommodityValue CreateCommodityValue(Expense expense)
        {
            var startDate1 = new DateTime(DateTime.Now.Year, 1, 1);
            var endDate1 = new DateTime(DateTime.Now.Year, 6, 30);
            var startDate2 = new DateTime(DateTime.Now.Year, 7, 1);
            var endDate2 = new DateTime(DateTime.Now.Year, 12, 31);
            var newCommodityValue = new CommodityValue();
            newCommodityValue.CopyPropFrom(expense);
            newCommodityValue.Id = 0;
            newCommodityValue.ContainerId = containerId;
            newCommodityValue.TotalPrice = 0;
            newCommodityValue.Notes = expense.CommodityValueNotes;
            newCommodityValue.Active = true;
            newCommodityValue.InsertedDate = DateTime.Now.Date;
            newCommodityValue.InsertedBy = Client.Token.UserId;
            if (DateTime.Now.Date >= startDate1 && DateTime.Now.Date <= endDate1)
            {
                newCommodityValue.StartDate = startDate1;
                newCommodityValue.EndDate = endDate1;
            }
            if (DateTime.Now.Date >= startDate2 && DateTime.Now.Date <= endDate2)
            {
                newCommodityValue.StartDate = startDate2;
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
            InsuranceFeesRate insuranceFeesRateDB = null;
            if (expense.IsBought)
            {
                insuranceFeesRateDB = await new Client(nameof(InsuranceFeesRate)).FirstOrDefaultAsync<InsuranceFeesRate>($"?$filter=Active eq true and TransportationTypeId eq {expense.TransportationTypeId} and JourneyId eq {expense.JourneyId} and IsBought eq {expense.IsBought.ToString().ToLower()} and IsSOC eq {isSOC.ToString().ToLower()} and IsSubRatio eq {isSubRatio.ToString().ToLower()}");
            }
            else
            {
                insuranceFeesRateDB = await new Client(nameof(InsuranceFeesRate)).FirstOrDefaultAsync<InsuranceFeesRate>($"?$filter=Active eq true and TransportationTypeId eq {expense.TransportationTypeId} and JourneyId eq {expense.JourneyId} and IsBought eq {expense.IsBought.ToString().ToLower()} and IsSOC eq {isSOC.ToString().ToLower()}");
            }
            if (insuranceFeesRateDB != null)
            {
                var getContainerType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq {expense.ContainerTypeId}");
                if (getContainerType != null && getContainerType.Description.ToLower().Contains("lạnh") && insuranceFeesRateDB.TransportationTypeId == 11673 && insuranceFeesRateDB.JourneyId == 12114)
                {
                    var insuranceFeesRateColdDB = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq 25391");
                    expense.InsuranceFeeRate = insuranceFeesRateColdDB != null ? decimal.Parse(insuranceFeesRateColdDB.Name) : 0;
                }
                else
                {
                    expense.InsuranceFeeRate = insuranceFeesRateDB.Rate;
                }
                if (isSubRatio && expense.IsBought == false)
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
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchEntity(Expense expense)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = expense.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.CommodityValue), Value = expense.CommodityValue.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.TotalPriceAfterTax), Value = expense.TotalPriceAfterTax.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.TotalPriceBeforeTax), Value = expense.TotalPriceBeforeTax.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchEntity(Transportation transportation)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportation.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.CustomerReturnFee), Value = transportation.CustomerReturnFee.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.TotalBet), Value = transportation.TotalBet.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}