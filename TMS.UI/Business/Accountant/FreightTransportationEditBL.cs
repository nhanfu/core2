using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;
using TMS.UI.Business.Manage;

namespace TMS.UI.Business.Accountant
{
    public class FreightTransportationEditBL : PopupEditor
    {
        GridView gridView;
        public Ledger ledgerEntity => Entity as Ledger;
        public FreightTransportationEditBL() : base(nameof(Ledger))
        {
            Name = "Freight Transportation Editor";
        }

        protected override async void ToggleApprovalBtn(object entity = null)
        {
            if (!Client.Token.AllRoleIds.Contains(31) && !Client.Token.AllRoleIds.Contains(8))
            {
                this.SetShow(false, "btnMakeUp", "wrapper-makeup");
                this.SetDisabled(true, "Vat", "CurrencyId", "ExchangeRate");
            }
            var ledgers = await new Client(nameof(Ledger)).GetRawList<Ledger>($"?$filter=Active eq true and ParentId eq {ledgerEntity.Id} and InvoiceFormId eq {ledgerEntity.InvoiceFormId} and TypeId eq null");
            if (ledgers.Count > 0)
            {
                this.SetShow(false, "btnFilter");
                if (ledgers.Where(x => x.IsMakeUp).Any())
                {
                    this.SetShow(false, "btnMakeUp");
                }
            }
            base.ToggleApprovalBtn(entity);
        }

        public void SetGridView()
        {
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault(x => x.Name == nameof(Ledger));
            var listViewItems = gridView.RowData.Data.Cast<Ledger>().ToList();
            var makeup = listViewItems.Where(x => x.IsMakeUp).FirstOrDefault();
            listViewItems.ForEach(x =>
            {
                var listViewItem = gridView.GetListViewItems(x).FirstOrDefault();
                if (listViewItem is null)
                {
                    return;
                }
                if (x.IsMakeUp == false)
                {
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "OriginPriceAfterTax").ForEach(y => y.Disabled = true);
                    if (!Client.Token.AllRoleIds.Contains(31) && !Client.Token.AllRoleIds.Contains(8))
                    {
                        listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "OriginVatAmount").ForEach(y => y.Disabled = true);
                        listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "Vat").ForEach(y => y.Disabled = true);
                        listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "OriginPriceBeforeTax").ForEach(y => y.Disabled = true);
                    }
                }
                else
                {
                    if (!Client.Token.AllRoleIds.Contains(31) && !Client.Token.AllRoleIds.Contains(8))
                    {
                        gridView.RemoveRowById(x.Id);
                        gridView.UpdateView();
                    }
                }
            });
            if (makeup != null)
            {
                var ledger = listViewItems.Where(x => x.Vat == makeup.Vat).FirstOrDefault();
                var listviewitem = gridView.GetListViewItems(ledger).FirstOrDefault();
                if (ledger != null && makeup != null && !Client.Token.AllRoleIds.Contains(31) && !Client.Token.AllRoleIds.Contains(8))
                {
                    ledger.OriginPriceAfterTax += makeup.OriginPriceAfterTax;
                    ledger.OriginPriceBeforeTax += makeup.OriginPriceBeforeTax;
                    ledger.OriginVatAmount += makeup.OriginVatAmount;
                    listviewitem.UpdateView();
                }
            }
        }

        public async Task SetExchangeRate()
        {
            var masterData = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq {ledgerEntity.CurrencyId}");
            ledgerEntity.ExchangeRate = masterData.Enum;
            UpdateView(false, nameof(Ledger.ExchangeRate));
            if (ledgerEntity.Id <= 0)
            {
                return;
            }
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault(x => x.Name == nameof(Ledger));
            var ledgers = gridView.RowData.Data.Cast<Ledger>().ToList();
            ledgers.ForEach(async x =>
            {
                await CostCalcLedgerAsync(x);
            });
        }

        public async Task CostCalcLedgerAsync(Ledger ledger)
        {
            if (ledger.Id <= 0)
            {
                return;
            }
            if (ledger.Vat < 0 || ledger.Vat > 100 || ledger.Vat == null)
            {
                Toast.Warning("Thuế chỉ từ 0% -> 100%");
                return;
            }
            ledger.OriginPriceAfterTax = ledger.ExchangeRate > 1 ? Math.Round((decimal)ledger.OriginPriceAfterTax / (decimal)ledger.ExchangeRate) : ledger.OriginPriceAfterTax;
            if (ledger.IsMakeUp)
            {
                ledgerEntity.OriginMakeUpPrice = ledger.OriginPriceAfterTax;
                ledgerEntity.VatMakeUp = ledger.Vat;
            }
            ledger.CurrencyId = ledgerEntity.CurrencyId;
            ledger.ExchangeRate = ledgerEntity.ExchangeRate;
            ledger.OriginPriceAfterTax *= (ledger.ExchangeRate is null ? 0 : (decimal)ledger.ExchangeRate);
            if (ledger.Vat == 0)
            {
                ledger.OriginVatAmount = 0;
                ledger.OriginPriceBeforeTax = ledger.OriginPriceAfterTax;
            }
            else
            {
                ledger.OriginVatAmount = Math.Round((ledger.OriginPriceAfterTax is null ? 0 : (decimal)ledger.OriginPriceAfterTax) * (ledger.Vat is null ? 0 : (decimal)ledger.Vat) / 100);
                ledger.OriginPriceBeforeTax = (ledger.OriginPriceAfterTax is null ? 0 : ledger.OriginPriceAfterTax) - (ledger.OriginVatAmount is null ? 0 : ledger.OriginVatAmount);
            }
            await new Client(nameof(Ledger)).UpdateAsync<Ledger>(ledger);
            await CostCalcLedgerParentAsync(null);
        }

        private int costCalcLedger;
        private int costCalcLedgerPriceBeforeTax;
        private int costCalcLedgerChangeVatAmount;
        private int costCalcLedgerParent;

        public void CostCalcLedger(Ledger ledger)
        {
            Window.ClearTimeout(costCalcLedger);
            costCalcLedger = Window.SetTimeout(async () =>
            {
                await CostCalcLedgerAsync(ledger);
            }, 500);
        }

        public void CostCalcLedgerPriceBeforeTax(Ledger ledger)
        {
            Window.ClearTimeout(costCalcLedgerPriceBeforeTax);
            costCalcLedgerPriceBeforeTax = Window.SetTimeout(async () =>
            {
                await CostCalcLedgerPriceBeforeTaxAsync(ledger);
            }, 500);
        }

        public void CostCalcLedgerChangeVatAmount(Ledger ledger)
        {
            Window.ClearTimeout(costCalcLedgerChangeVatAmount);
            costCalcLedgerChangeVatAmount = Window.SetTimeout(async () =>
            {
                await CostCalcLedgerChangeVatAmountAsync(ledger);
            }, 500);
        }

        public void CostCalcLedgerParent()
        {
            Window.ClearTimeout(costCalcLedgerParent);
            costCalcLedgerParent = Window.SetTimeout(async () =>
            {
                await CostCalcLedgerParentAsync(null);
            }, 100);
        }

        public async Task CostCalcLedgerPriceBeforeTaxAsync(Ledger ledger)
        {
            if (ledger.OriginPriceAfterTax < 0)
            {
                Toast.Warning("Số tiền không thể âm");
            }
            if (ledger.OriginPriceBeforeTax + ledger.OriginVatAmount > ledger.OriginPriceAfterTax)
            {
                ledger.OriginPriceAfterTax = ledger.OriginPriceAfterTax + (ledger.OriginPriceBeforeTax + ledger.OriginVatAmount - ledger.OriginPriceAfterTax);
            }
            else
            {
                ledger.OriginVatAmount = Math.Round((ledger.OriginPriceAfterTax is null ? 0 : (decimal)ledger.OriginPriceAfterTax) - (ledger.OriginPriceBeforeTax is null ? 0 : (decimal)ledger.OriginPriceBeforeTax));
            }
            await new Client(nameof(Ledger)).PatchAsync<Ledger>(GetPatchLedger(ledger));
            await CostCalcLedgerParentAsync(null);
        }

        public async Task CostCalcLedgerChangeVatAmountAsync(Ledger ledger)
        {
            if (ledger.OriginVatAmount < 0)
            {
                Toast.Warning("Thuế VAT không thể âm");
                return;
            }
            if (ledger.OriginVatAmount > ledger.OriginPriceAfterTax)
            {
                Toast.Warning("Thuế VAT không thể lớn hơn tổng tiền");
                return;
            }
            ledger.OriginPriceBeforeTax = Math.Round((ledger.OriginPriceAfterTax is null ? 0 : (decimal)ledger.OriginPriceAfterTax) - (ledger.OriginVatAmount is null ? 0 : (decimal)ledger.OriginVatAmount));
            await new Client(nameof(Ledger)).PatchAsync<Ledger>(GetPatchLedger(ledger));
            await CostCalcLedgerParentAsync(null);
        }

        private decimal CalcVAT(decimal? PriceAfterTax, decimal? VatAmount)
        {
           return Math.Round((VatAmount is null ? 0 : (decimal)VatAmount) / 100 * (PriceAfterTax is null ? 0 : (decimal)PriceAfterTax), 5);
        }

        public async Task CostCalcLedgerParentAsync(List<Ledger> ledgers)
        {
            if (ledgerEntity.Id <= 0)
            {
                return;
            }
            if (ledgers == null)
            {
                gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault(x => x.Name == nameof(Ledger));
                ledgers = gridView.RowData.Data.Cast<Ledger>().ToList();
            }
            ledgerEntity.OriginPriceBeforeTax = 0;
            ledgerEntity.OriginVatAmount = 0;
            ledgerEntity.OriginPriceAfterTax = 0;
            ledgerEntity.OriginRealTotalPrice = 0;
            ledgerEntity.OriginTotalPrice = 0;
            decimal makeUp = 0;
            ledgers.ForEach(x =>
            {
                ledgerEntity.OriginPriceBeforeTax += x.OriginPriceBeforeTax;
                ledgerEntity.OriginVatAmount += x.OriginVatAmount;
                ledgerEntity.OriginPriceAfterTax += x.OriginPriceAfterTax;
                ledgerEntity.OriginRealTotalPrice += x.IsMakeUp ? 0 : x.OriginPriceAfterTax;
                if (x.IsMakeUp) { makeUp = (decimal)x.OriginPriceAfterTax; }
            });
            ledgerEntity.OriginRealTotalPrice += Math.Round((decimal)ledgerEntity.OriginRealTotalPrice * (decimal)ledgerEntity.Vat / 100) + Math.Round(makeUp * (decimal)ledgerEntity.Vat / 100);
            ledgerEntity.OriginTotalPrice = ledgerEntity.OriginPriceAfterTax + Math.Round((decimal)ledgerEntity.OriginPriceAfterTax * (decimal)ledgerEntity.Vat / 100);
            ledgerEntity.OriginReturnTotalPrice = ledgerEntity.OriginTotalPrice - ledgerEntity.OriginRealTotalPrice;
            ledgerEntity.OriginRealTotalPrice *= ledgerEntity.ExchangeRate;
            ledgerEntity.OriginTotalPrice *= ledgerEntity.ExchangeRate;
            ledgerEntity.OriginReturnTotalPrice *= ledgerEntity.ExchangeRate;
            ledgerEntity.OriginDebit = ledgerEntity.OriginTotalPrice - (ledgerEntity.OriginCredit == null ? 0 : ledgerEntity.OriginCredit);
            await Save(ledgerEntity);
            this.UpdateView();
        }

        public async Task FilterData(Ledger ledger)
        {
            if (ledger.Vat > 100)
            {
                Toast.Warning("Thuế phải nhỏ hơn 100%");
                return;
            }
            if (ledger.InvoiceDate is null)
            {
                Toast.Warning("Ngày is required");
                return;
            }
            this.SetShow(false, "btnFilter");
            await this.OpenPopup(
                featureName: "Filter Data",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.FilterDataBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Tạo dữ liệu";
                    instance.Entity = ledger;
                    return instance;
                });
        }

        public async Task ImportMakeUpFee(Ledger ledger)
        {
            this.SetShow(false, "btnMakeUp");
            await this.OpenPopup(
                featureName: "MakeUp Fee",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.MakeUpFeeBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Nhập chi phí";
                    instance.Entity = ledger;
                    return instance;
                });
        }

        public void SetObjectLedgers()
        {
            if (ledgerEntity.Id <= 0)
            {
                return;
            }
            if (ledgerEntity.VendorId == null)
            {
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn đổi đối tượng công nợ",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault(x => x.Name == nameof(Ledger));
                var ledgers = gridView.RowData.Data.Cast<Ledger>().ToList();
                var getVendor = await new Client(nameof(Ledger)).FirstOrDefaultAsync<Ledger>($"?$filter=Active eq true and VendorId eq {ledgerEntity.VendorId} and TypeId eq 2 and InvoiceFormId eq 16047");
                Vendor getObject = null;
                if (getVendor != null)
                {
                    getObject = await new Client(nameof(Vendor)).FirstOrDefaultAsync<Vendor>($"?$filter=Active eq true and TypeId eq 23741 and Id eq {getVendor.ObjectId}");
                }
                if (ledgers.Count > 0)
                {
                    ledgers.ForEach(x =>
                    {
                        x.ObjectId = getVendor == null ? null : getVendor.ObjectId;
                        x.Taxcode = getObject == null ? null : getObject.TaxCode;
                        x.DebitAccId = getVendor == null ? null : getVendor.DebitAccId;
                        x.CreditAccId = getVendor == null ? null : getVendor.CreditAccId;
                        x.DebitAccVatId = getVendor == null ? null : getVendor.DebitAccVatId;
                        x.CreditAccVatId = getVendor == null ? null : getVendor.CreditAccVatId;
                        x.ItemsId = getVendor == null ? null : getVendor.ItemsId;
                    });
                    await new Client(nameof(Ledger)).BulkUpdateAsync<Ledger>(ledgers);
                }
                ledgerEntity.Address = getObject.Address;
                await new Client(nameof(Ledger)).PatchAsync<Ledger>(GetPatchObjectLedger(ledgerEntity));
            };
        }

        public PatchUpdate GetPatchLedger(Ledger ledger)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = ledger.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Ledger.OriginVatAmount), Value = ledger.OriginVatAmount.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Ledger.OriginPriceBeforeTax), Value = ledger.OriginPriceBeforeTax.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Ledger.OriginPriceAfterTax), Value = ledger.OriginPriceAfterTax.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchObjectLedger(Ledger ledger)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = ledger.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Ledger.ObjectId), Value = ledger.ObjectId.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Ledger.Address), Value = ledger.Address.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}
