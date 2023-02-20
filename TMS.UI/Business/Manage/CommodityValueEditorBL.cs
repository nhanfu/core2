using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.MVVM;
using Core.Notifications;
using Core.ViewModels;
using Retyped;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using static Retyped.dom.Literals.Types;

namespace TMS.UI.Business.Manage
{
    public class CommodityValueEditorBL : PopupEditor
    {
        GridView gridView;
        public CommodityValue commodityValueEntity => Entity as CommodityValue;
        public CommodityValueEditorBL() : base(nameof(CommodityValue))
        {
            Name = "CommodityValue Editor";
        }

        protected override void ToggleApprovalBtn(object entity = null)
        {
            if (commodityValueEntity.Id > 0)
            {
                this.SetDisabled(true, "BossId");
                this.SetDisabled(true, "CommodityId");
            }
            base.ToggleApprovalBtn();
        }

        public async Task SetInfo()
        {
            var vendor = await new Client(nameof(Vendor)).FirstOrDefaultAsync<Vendor>($"?$filter=Active eq true and TypeId eq 7551 and Id eq {commodityValueEntity.BossId}");
            commodityValueEntity.SaleId = vendor is null ? null : vendor.UserId;
            UpdateView(false, nameof(CommodityValue.SaleId));
        }

        public async Task CheckCommodityValue()
        {
            if (commodityValueEntity.StartDate != null && commodityValueEntity.EndDate != null && commodityValueEntity.StartDate > commodityValueEntity.EndDate)
            {
                Toast.Warning("Ngày bắt đầu không được lớn hơn ngày kết thúc");
                return;
            }
            if (commodityValueEntity.BossId > 0 && commodityValueEntity.CommodityId > 0 && commodityValueEntity.ContainerId > 0)
            {
                gridView = gridView ?? Parent.FindActiveComponent<GridView>().FirstOrDefault();
                var commodityValue = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {commodityValueEntity.BossId} and CommodityId eq {commodityValueEntity.CommodityId} and ContainerId eq {commodityValueEntity.ContainerId}");
                if (commodityValue is null)
                {
                    await new Client(nameof(CommodityValue)).CreateAsync<CommodityValue>(commodityValueEntity);
                    Toast.Success("Đã tạo mới thành công");
                }
                else
                {
                    if (commodityValueEntity.Id > 0)
                    {
                        commodityValue.IsBought = commodityValueEntity.IsBought;
                        commodityValue.IsWet = commodityValueEntity.IsWet;
                        commodityValue.CustomerTypeId = commodityValueEntity.CustomerTypeId;
                        commodityValue.ContainerId = commodityValueEntity.ContainerId;
                        commodityValue.StartDate = commodityValueEntity.StartDate;
                        commodityValue.EndDate = commodityValueEntity.EndDate;
                    }
                    if (commodityValue.TotalPrice != commodityValueEntity.TotalPrice)
                    {
                        var confirm = new ConfirmDialog
                        {
                            Content = "Bạn có muốn cập nhật GTHH này từ " + commodityValue.TotalPrice.ToString("N0") + " thành " + commodityValueEntity.TotalPrice.ToString("N0") + " không?"
                        };
                        confirm.Render();
                        confirm.YesConfirmed += async () =>
                        {
                            commodityValue.EndDate = DateTime.Now.Date;
                            commodityValue.Active = false;
                            var newCommodityValue = CreateCommodityValue(commodityValueEntity);
                            await new Client(nameof(CommodityValue)).PatchAsync<object>(GetPatchEntity(commodityValue));
                            await new Client(nameof(CommodityValue)).CreateAsync<CommodityValue>(newCommodityValue);
                            Toast.Success("Đã cập nhật thành công");
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
                                this.Dispose();
                            }
                            else
                            {
                                Toast.Warning("Đã áp dụng thất bại GTHH");
                                this.Dispose();
                            }
                        };
                    }
                    else
                    {
                        await new Client(nameof(CommodityValue)).UpdateAsync<CommodityValue>(commodityValue);
                    }
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
            var insuranceFeesRateDB = await new Client(nameof(InsuranceFeesRate)).FirstOrDefaultAsync<InsuranceFeesRate>($"?$filter=Active eq true and TransportationTypeId eq {expense.TransportationTypeId} and JourneyId eq {expense.JourneyId} and IsBought eq {expense.IsBought.ToString().ToLower()} and IsSOC eq {isSOC.ToString().ToLower()}  and IsSubRatio eq  {isSubRatio.ToString().ToLower()}");
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
    }
}