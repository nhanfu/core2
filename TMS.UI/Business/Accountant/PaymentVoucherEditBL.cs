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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;
using TMS.UI.Business.Manage;

namespace TMS.UI.Business.Accountant
{
    public class PaymentVoucherEditBL : PopupEditor
    {
        public GridView gridView;
        public Ledger ledgerEntity => Entity as Ledger;
        public List<Ledger> ledgerParentList = new List<Ledger>();
        public PaymentVoucherEditBL() : base(nameof(Ledger))
        {
            Name = "PaymentVoucher Editor";
        }

        protected override void ToggleApprovalBtn(object entity = null)
        {
            if (ledgerEntity.Id > 0)
            {
                this.SetShow(false, "btnCreateInvoice");
                this.SetShow(true, "btnSave");
                this.SetShow(true, "btnPayment");
            }
            else
            {
                this.SetShow(true, "btnCreateInvoice");
                this.SetShow(false, "btnSave");
                this.SetShow(false, "btnPayment");
            }
            base.ToggleApprovalBtn(entity);
        }

        public async Task LoadInvoice()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.Name.Contains(nameof(Ledger)));
            if (gridView.RowData.Data.Count <= 0)
            {
                var gridViewSelected = Parent.FindActiveComponent<GridView>().FirstOrDefault(x => x.Name.Contains(nameof(Ledger)));
                ledgerParentList = gridViewSelected.GetSelectedRows().Cast<Ledger>().ToList();
                var ledger = ledgerParentList.FirstOrDefault();
                var newLedger = new Ledger()
                {
                    InvoiceFormId = ledgerEntity.InvoiceFormId,
                    VendorId = ledger.VendorId,
                    ObjectId = ledger.ObjectId,
                    Taxcode = ledger.Taxcode
                };
                await gridView.AddRow(newLedger);
                gridView.UpdateView(true);
            }
        }

        public override async Task<bool> Save(object entity = null)
        {
            if (ledgerEntity.Id <= 0)
            {
                Toast.Warning("Chưa tạo phiếu");
                return false;
            }
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var ledger = gridView.RowData.Data.FirstOrDefault().Cast<Ledger>();
            var rs = await new Client(nameof(Ledger)).UpdateAsync<Ledger>(ledger);
            if (rs == null)
            {
                Toast.Warning("Đã có lỗi xảy ra");
                return false;
            }
            return await base.Save(entity);
        }

        public async Task CreateInvoice()
        {
            var rs = await new Client(nameof(Ledger)).CreateAsync<Ledger>(ledgerEntity);
            if (rs != null)
            {
                ledgerEntity.CopyPropFrom(rs);
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
                var ledger = gridView.RowData.Data.FirstOrDefault().Cast<Ledger>();
                ledger.ParentId = rs.Id;
                var ledgerServices = new List<LedgerService>();
                foreach (var item in ledgerParentList)
                {
                    var ledgerService = new LedgerService()
                    {
                        InvoiceId = item.Id,
                        TargetInvoiceId = rs.Id
                    };
                    ledgerServices.Add(ledgerService);
                }
                var res = await new Client(nameof(Ledger)).CreateAsync<Ledger>(ledger);
                var resService = await new Client(nameof(LedgerService)).BulkUpdateAsync<LedgerService>(ledgerServices);
                if (res != null && resService != null)
                {
                    if (res.OriginPriceAfterTax > 0)
                    {
                        var ids = resService.Select(x => x.InvoiceId);
                        var freightTrans = await new Client(nameof(Ledger)).GetRawList<Ledger>($"?$filter=Active eq true and Id in ({ids.Combine()})");
                        var credit = res.OriginPriceAfterTax;
                        foreach (var item in freightTrans.OrderBy(x => x.InsertedDate))
                        {
                            if (credit <= 0)
                            {
                                break;
                            }
                            if (item.OriginDebit == 0)
                            {
                                continue;
                            }
                            if (credit >= item.OriginDebit)
                            {
                                credit -= item.OriginDebit;
                                item.OriginCredit = item.OriginTotalPrice;
                                item.OriginDebit = 0;
                                item.IsAllPaid = true;
                            }
                            else
                            {
                                if (item.OriginCredit == null)
                                {
                                    item.OriginCredit = 0;
                                }
                                item.OriginCredit += credit;
                                item.OriginDebit -= credit;
                                credit = 0;
                            }
                        }
                        var confirm = new ConfirmDialog
                        {
                            Content = $"Bạn có chắc chắn muốn chi {ledger.OriginPriceAfterTax:N0} cho {freightTrans.Count} phiếu cước vận chuyện?",
                        };
                        confirm.Render();
                        confirm.YesConfirmed += async () =>
                        {
                            var resupdate = await new Client(nameof(Ledger)).BulkUpdateAsync<Ledger>(freightTrans);
                            if (resupdate != null)
                            {
                                Toast.Success("Cập nhật thành công");
                            }
                            else
                            {
                                Toast.Warning("Đã có lỗi xảy ra");
                            }
                        };
                        confirm.NoConfirmed += async () =>
                        {
                            res.OriginPriceAfterTax = 0;
                            await new Client(nameof(Ledger)).UpdateAsync<Ledger>(res);
                            gridView.ClearRowData();
                            gridView.UpdateView(true);
                        };
                    }
                    Toast.Success("Tạo phiếu thành công");
                    this.SetShow(false, "btnCreateInvoice");
                    this.SetShow(true, "btnSave");
                    gridView.ClearRowData();
                    this.UpdateView(true);
                }
                else
                {
                    Toast.Warning("Đã có lỗi xảy ra");
                }
            }
            else
            {
                Toast.Warning("Đã có lỗi xảy ra");
            }
        }

        private int payment;

        public void PaymentFreightTransportation(Ledger ledger)
        {
            Window.ClearTimeout(payment);
            payment = Window.SetTimeout(async () =>
            {
                await PaymentFreightTransportationAsync(ledger);
            }, 500);
        }

        public async Task PaymentFreightTransportationAsync(Ledger ledger)
        {
            if (ledgerEntity.Id <= 0)
            {
                return;
            }
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var ledgerDB = await new Client(nameof(Ledger)).FirstOrDefaultAsync<Ledger>($"?$filter=Active eq true and Id eq {ledger.Id}");
            if (ledger.OriginPriceAfterTax == ledgerDB.OriginPriceAfterTax)
            {
                return;
            }
            var freightTransportations = await new Client(nameof(LedgerService)).GetRawList<LedgerService>($"?$filter=Active eq true and TargetInvoiceId eq {ledgerEntity.Id}");
            if (freightTransportations == null)
            {
                Toast.Warning("Không có phiếu để chi");
                return;
            }
            var ids = freightTransportations.Select(x => x.InvoiceId);
            var freightTrans = await new Client(nameof(Ledger)).GetRawList<Ledger>($"?$filter=Active eq true and Id in ({ids.Combine()})");
            if (ledger.OriginPriceAfterTax > ledgerDB.OriginPriceAfterTax)
            {
                var credit = ledger.OriginPriceAfterTax - ledgerDB.OriginPriceAfterTax;
                foreach (var item in freightTrans.OrderBy(x => x.InsertedDate))
                {
                    if (credit <= 0)
                    {
                        break;
                    }
                    if (item.OriginDebit == 0)
                    {
                        continue;
                    }
                    if (credit >= item.OriginDebit)
                    {
                        credit -= item.OriginDebit;
                        item.OriginCredit = item.OriginTotalPrice;
                        item.OriginDebit = 0;
                        item.IsAllPaid = true;
                    }
                    else
                    {
                        if (item.OriginCredit == null)
                        {
                            item.OriginCredit = 0;
                        }
                        item.OriginCredit += credit;
                        item.OriginDebit -= credit;
                        credit = 0;
                    }
                }
            }
            else
            {
                var debit = ledgerDB.OriginPriceAfterTax - ledger.OriginPriceAfterTax;
                foreach (var item in freightTrans.OrderByDescending(x => x.InsertedDate))
                {
                    if (debit <= 0)
                    {
                        break;
                    }
                    if (item.OriginCredit == 0)
                    {
                        continue;
                    }
                    if (debit >= item.OriginCredit)
                    {
                        debit -= item.OriginCredit;
                        item.OriginCredit = 0;
                        item.OriginDebit = item.OriginTotalPrice;
                        item.IsAllPaid = false;
                    }
                    else
                    {
                        item.OriginCredit -= debit;
                        item.OriginDebit += debit;
                        debit = 0;
                        item.IsAllPaid = false;
                    }
                }
            }
            var confirm = new ConfirmDialog
            {
                Content = $"Bạn có chắc chắn muốn chi {ledger.OriginPriceAfterTax:N0} cho {freightTrans.Count} phiếu cước vận chuyện?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var res = await new Client(nameof(Ledger)).BulkUpdateAsync<Ledger>(freightTrans);
                if (res != null)
                {
                    await new Client(nameof(Ledger)).UpdateAsync<Ledger>(ledger);
                    Toast.Success("Cập nhật thành công");
                }
                else
                {
                    Toast.Warning("Đã có lỗi xảy ra");
                }
            };
            confirm.NoConfirmed += () =>
            {
                gridView.UpdateView(true);
            };
        }

        private int calcTotalPrice;

        public void CalcTotalPrice(Ledger ledger)
        {
            Window.ClearTimeout(calcTotalPrice);
            calcTotalPrice = Window.SetTimeout(async () =>
            {
                await CalcTotalPriceAsync(ledger);
            }, 500);
        }

        public async Task CalcTotalPriceAsync(Ledger ledger)
        {
            if (ledger.Quantity != null && ledger.Quantity >= 0 && ledger.OriginUnitPrice != null && ledger.OriginUnitPrice >= 0)
            {
                ledger.OriginPriceBeforeTax = ledger.Quantity * ledger.OriginUnitPrice;
            }
            else
            {
                ledger.OriginPriceBeforeTax = 0;
            }
            if (ledger.OriginPriceBeforeTax != null && ledger.OriginPriceBeforeTax >= 0 && ledger.Vat != null && ledger.Vat >= 0)
            {
                ledger.OriginVatAmount = Math.Round((decimal)ledger.OriginPriceBeforeTax * (decimal)ledger.Vat / 100);
            }
            else
            {
                ledger.OriginVatAmount = 0;
            }
            if (ledger.OriginPriceBeforeTax != null && ledger.OriginVatAmount != null)
            {
                ledger.OriginPriceAfterTax = ledger.OriginPriceBeforeTax + ledger.OriginVatAmount;
            }
            else
            {
                ledger.OriginPriceAfterTax = 0;
            }
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var listViewItem = gridView.GetListViewItems(ledger).FirstOrDefault();
            listViewItem.UpdateView(false, nameof(Ledger.OriginPriceBeforeTax), nameof(Ledger.OriginVatAmount), nameof(Ledger.OriginPriceAfterTax));
            await PaymentFreightTransportationAsync(ledger);
        }

        public PatchUpdate GetPatchLedger(Ledger ledger)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = ledger.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Ledger.OriginCredit), Value = ledger.OriginCredit.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Ledger.OriginDebit), Value = ledger.OriginDebit.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Ledger.IsAllPaid), Value = ledger.IsAllPaid.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}
