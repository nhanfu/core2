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

namespace TMS.UI.Business.Accountant
{
    public class MakeUpFeeBL : PopupEditor
    {
        public Ledger ledgerEntity => Entity as Ledger;
        public MakeUpFeeBL() : base(nameof(Ledger))
        {
            Name = "MakeUp Fee";
        }

        public async Task CreateMakeUpFee()
        {
            var getVendor = await new Client(nameof(Ledger)).FirstOrDefaultAsync<Ledger>($"?$filter=Active eq true and VendorId eq {ledgerEntity.VendorId} and TypeId eq 2 and InvoiceFormId eq 16047");
            Vendor getObject = null;
            if (getVendor != null)
            {
                getObject = await new Client(nameof(Vendor)).FirstOrDefaultAsync<Vendor>($"?$filter=Active eq true and TypeId eq 23741 and Id eq {getVendor.ObjectId}");
            }
            var newLedger = new Ledger();
            CreateLedger(newLedger, null, getVendor, getObject);
            CalcLedger(newLedger);
            newLedger.Note = "Make Up";
            await new Client(nameof(Ledger)).CreateAsync<Ledger>(newLedger);
            CalcSumLedgerParent(ledgerEntity, newLedger);
            CalcLedgerParent(ledgerEntity, newLedger);
            await Save(ledgerEntity);
            Parent.UpdateView(true);
            this.Dispose();
        }

        public void CreateLedger(Ledger ledger, int? route, Ledger getVendor, Vendor getObject)
        {
            ledger.ParentId = ledgerEntity.Id;
            ledger.RouteId = route;
            ledger.IsMakeUp = true;
            ledger.Vat = ledgerEntity.VatMakeUp;
            ledger.OriginPriceAfterTax = ledgerEntity.OriginMakeUpPrice;
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

        public void CalcLedgerParent(Ledger ledgerParent, Ledger makeUp)
        {
            ledgerParent.OriginRealTotalPrice += Math.Round((decimal)makeUp.OriginPriceAfterTax * (decimal)ledgerParent.Vat / 100);
            ledgerParent.OriginTotalPrice = ledgerParent.OriginPriceAfterTax + Math.Round((decimal)ledgerParent.OriginPriceAfterTax * (decimal)ledgerParent.Vat / 100);
            ledgerParent.OriginReturnTotalPrice = ledgerParent.OriginTotalPrice - ledgerParent.OriginRealTotalPrice;
            ledgerParent.OriginRealTotalPrice *= ledgerParent.ExchangeRate;
            ledgerParent.OriginTotalPrice *= ledgerParent.ExchangeRate;
            ledgerParent.OriginReturnTotalPrice *= ledgerParent.ExchangeRate;
            ledgerParent.OriginDebit = ledgerParent.OriginTotalPrice;
        }
    }
}
