using Bridge.Html5;using Core.Clients;
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
using static Retyped.googlemaps.google.maps.DirectionsService;

namespace TMS.UI.Business.Accountant
{
    public class FilterDataBL : PopupEditor
    {
        public Ledger ledgerEntity => Entity as Ledger;
        public FilterDataBL() : base(nameof(Ledger))
        {
            Name = "Filter Data";
        }

        public async Task CreateLedger()
        {
            if (ledgerEntity.StartDate == null || ledgerEntity.EndDate == null || ledgerEntity.VendorId == null)
            {
                Toast.Warning("Chưa chọn đủ dữ liệu");
                return;
            }
            await Save(ledgerEntity);
            var startDate = ledgerEntity.StartDate.Value.Date.ToString("yyyy-MM-dd");
            var endDate = ledgerEntity.EndDate.Value.Date.ToString("yyyy-MM-dd");
            var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and ClosingDate ge {startDate} and ReturnDate le {endDate} and (ReturnVendorId eq {ledgerEntity.VendorId} or ClosingId eq {ledgerEntity.VendorId})");
            var routes = transportations.Select(x => x.RouteId).Distinct().ToList();
            var getVendor = await new Client(nameof(Ledger)).FirstOrDefaultAsync<Ledger>($"?$filter=Active eq true and VendorId eq {ledgerEntity.VendorId} and TypeId eq 2 and InvoiceFormId eq 16047");
            Vendor getObject = null;
            if (getVendor != null)
            {
                getObject = await new Client(nameof(Vendor)).FirstOrDefaultAsync<Vendor>($"?$filter=Active eq true and TypeId eq 23741 and Id eq {getVendor.ObjectId}");
            }
            decimal realTotalPrice = 0;
            if (routes.Count > 0)
            {
                foreach (var route in routes)
                {
                    var transportationsGroup = transportations.Where(x => x.RouteId == route).ToList();
                    var newLedgerCVC = new Ledger();
                    CreateLedger(newLedgerCVC, (int)route, getVendor, getObject, 10);
                    var newLedgerLiftingLowering = new Ledger();
                    CreateLedger(newLedgerLiftingLowering, (int)route, getVendor, getObject, 8);
                    var newLedgerPayOnBehalf = new Ledger();
                    CreateLedger(newLedgerPayOnBehalf, (int)route, getVendor, getObject, 8);
                    //var newLedgerOrther = new Ledger();
                    //CreateLedger(newLedgerOrther, (int)route, getVendor, getObject, 8);
                    foreach (var transportation in transportationsGroup)
                    {
                        newLedgerCVC.OriginPriceAfterTax += transportation.ClosingUnitPrice is null ? 0 : transportation.ClosingUnitPrice;
                        newLedgerCVC.OriginPriceAfterTax += transportation.ReturnUnitPrice is null ? 0 : transportation.ReturnUnitPrice;
                        newLedgerCVC.OriginPriceAfterTax += transportation.ClosingCombinationUnitPrice is null ? 0 : transportation.ClosingCombinationUnitPrice;
                        newLedgerCVC.OriginPriceAfterTax *= newLedgerCVC.ExchangeRate;

                        newLedgerLiftingLowering.OriginPriceAfterTax += transportation.LiftFee is null ? 0 : transportation.LiftFee;
                        newLedgerLiftingLowering.OriginPriceAfterTax += transportation.LandingFee is null ? 0 : transportation.LandingFee;
                        newLedgerLiftingLowering.OriginPriceAfterTax += transportation.ReturnLiftFee is null ? 0 : transportation.ReturnLiftFee;
                        newLedgerLiftingLowering.OriginPriceAfterTax += transportation.ReturnClosingFee is null ? 0 : transportation.ReturnClosingFee;
                        newLedgerLiftingLowering.OriginPriceAfterTax *= newLedgerCVC.ExchangeRate;

                        newLedgerPayOnBehalf.OriginPriceAfterTax += transportation.CollectOnBehaftInvoinceNoFee is null ? 0 : transportation.CollectOnBehaftInvoinceNoFee;
                        newLedgerPayOnBehalf.OriginPriceAfterTax += transportation.CollectOnBehaftFee is null ? 0 : transportation.CollectOnBehaftFee;
                        newLedgerPayOnBehalf.OriginPriceAfterTax *= newLedgerCVC.ExchangeRate;

                        //newLedgerOrther.OriginPriceAfterTax += transportation.OrtherFeeInvoinceNo is null ? 0 : transportation.OrtherFeeInvoinceNo;
                        //newLedgerOrther.OriginPriceAfterTax += transportation.OrtherFee is null ? 0 : transportation.OrtherFee;
                        //newLedgerOrther.OriginPriceAfterTax *= newLedgerCVC.ExchangeRate;
                    }
                    if (newLedgerCVC.OriginPriceAfterTax > 0)
                    {
                        CalcLedger(newLedgerCVC);
                        newLedgerCVC.Note = "CVC";
                        CalcSumLedgerParent(ledgerEntity, newLedgerCVC);
                        await new Client(nameof(Ledger)).CreateAsync<Ledger>(newLedgerCVC);
                    }
                    if (newLedgerLiftingLowering.OriginPriceAfterTax > 0)
                    {
                        CalcLedger(newLedgerLiftingLowering);
                        newLedgerLiftingLowering.Note = "Nâng hạ";
                        CalcSumLedgerParent(ledgerEntity, newLedgerLiftingLowering);
                        await new Client(nameof(Ledger)).CreateAsync<Ledger>(newLedgerLiftingLowering);
                    }
                    if (newLedgerPayOnBehalf.OriginPriceAfterTax > 0)
                    {
                        CalcLedger(newLedgerPayOnBehalf);
                        newLedgerPayOnBehalf.Note = "Chi hộ";
                        CalcSumLedgerParent(ledgerEntity, newLedgerPayOnBehalf);
                        await new Client(nameof(Ledger)).CreateAsync<Ledger>(newLedgerPayOnBehalf);
                    }
                    realTotalPrice += (decimal)newLedgerCVC.OriginPriceAfterTax + (decimal)newLedgerLiftingLowering.OriginPriceAfterTax + (decimal)newLedgerPayOnBehalf.OriginPriceAfterTax;
                    //if (newLedgerOrther.OriginPriceAfterTax > 0)
                    //{
                    //    CalcLedger(newLedgerOrther);
                    //    newLedgerOrther.Note = "CP khác";
                    //    await new Client(nameof(Ledger)).CreateAsync<Ledger>(newLedgerOrther);
                    //    CalcLedgerParent(ledgerEntity, newLedgerOrther);
                    //}
                }
            }
            ledgerEntity.ObjectId = getVendor == null ? null : getVendor.ObjectId;
            ledgerEntity.Taxcode = getObject == null ? null : getObject.TaxCode;
            ledgerEntity.DebitAccId = getVendor == null ? null : getVendor.DebitAccId;
            ledgerEntity.CreditAccId = getVendor == null ? null : getVendor.CreditAccId;
            ledgerEntity.DebitAccVatId = getVendor == null ? null : getVendor.DebitAccVatId;
            ledgerEntity.CreditAccVatId = getVendor == null ? null : getVendor.CreditAccVatId;
            ledgerEntity.ItemsId = getVendor == null ? null : getVendor.ItemsId;
            var date = "Từ " + ledgerEntity.StartDate.Value.Date.ToString("dd/MM/yyyy") + " đến " + ledgerEntity.EndDate.Value.Date.ToString("dd/MM/yyyy");
            ledgerEntity.Note = date;
            CalcLedgerParent(ledgerEntity, realTotalPrice);
            await this.Save(ledgerEntity);
            Parent.UpdateView(true);
            this.Dispose();
        }

        public void CreateLedger(Ledger ledger, int route, Ledger getVendor, Vendor getObject, decimal vat)
        {
            ledger.ParentId = ledgerEntity.Id;
            ledger.RouteId = route;
            ledger.Vat = vat;
            ledger.OriginPriceAfterTax = 0;
            ledger.InvoiceFormId = ledgerEntity.InvoiceFormId;
            ledger.ExchangeRate = ledgerEntity.ExchangeRate;
            ledger.CurrencyId = ledgerEntity.CurrencyId;
            ledger.VendorId = ledgerEntity.VendorId;
            ledger.ObjectId = getVendor == null ? null : getVendor.ObjectId;
            ledger.Taxcode = getObject == null ? null : getObject.TaxCode;
            ledger.DebitAccId = getVendor == null ? null : getVendor.DebitAccId;
            ledger.CreditAccId = getVendor == null ? null : getVendor.CreditAccId;
            ledger.DebitAccVatId = getVendor == null ? null : getVendor.DebitAccVatId;
            ledger.CreditAccVatId = getVendor == null ? null : getVendor.CreditAccVatId;
            ledger.ItemsId = getVendor == null ? null : getVendor.ItemsId;
        }

        public void CalcLedger(Ledger ledger)
        {
            ledger.OriginVatAmount = Math.Round((ledger.OriginPriceAfterTax is null ? 0 : (decimal)ledger.OriginPriceAfterTax) * (ledger.Vat is null ? 0 : (decimal)ledger.Vat) / 100);
            ledger.OriginPriceBeforeTax = (ledger.OriginPriceAfterTax is null ? 0 : ledger.OriginPriceAfterTax) - (ledger.OriginVatAmount is null ? 0 : ledger.OriginVatAmount);
        }

        public void CalcSumLedgerParent(Ledger ledgerParent, Ledger ledger)
        {
            ledgerParent.OriginPriceBeforeTax += ledger.OriginPriceBeforeTax;
            ledgerParent.OriginVatAmount += ledger.OriginVatAmount;
            ledgerParent.OriginPriceAfterTax += ledger.OriginPriceAfterTax;
        }

        public void CalcLedgerParent(Ledger ledgerParent, decimal realTotalPrice)
        {
            if (ledgerParent.OriginRealTotalPrice == 0)
            {
                ledgerParent.OriginRealTotalPrice = ledgerParent.OriginPriceAfterTax + Math.Round((decimal)ledgerParent.OriginPriceAfterTax * (decimal)ledgerParent.Vat / 100);
            }
            else
            {
                ledgerParent.OriginRealTotalPrice += realTotalPrice + Math.Round(realTotalPrice * (decimal)ledgerParent.Vat / 100);
            }
            ledgerParent.OriginTotalPrice = ledgerParent.OriginPriceAfterTax + Math.Round((decimal)ledgerParent.OriginPriceAfterTax * (decimal)ledgerParent.Vat / 100);
            ledgerParent.OriginReturnTotalPrice = ledgerParent.OriginTotalPrice - ledgerParent.OriginRealTotalPrice;
            ledgerParent.OriginRealTotalPrice *= ledgerParent.ExchangeRate;
            ledgerParent.OriginTotalPrice *= ledgerParent.ExchangeRate;
            ledgerParent.OriginReturnTotalPrice *= ledgerParent.ExchangeRate;
            ledgerParent.OriginDebit = ledgerParent.OriginTotalPrice;
        }
    }
}
