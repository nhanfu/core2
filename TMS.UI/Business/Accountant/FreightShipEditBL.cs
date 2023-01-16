using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.UI.Business.Accountant
{
    public class FreightShipEditBL : PopupEditor
    {
        public Ledger ledgerEntity => Entity as Ledger;
        public FreightShipEditBL() : base(nameof(Ledger))
        {
            Name = "FreightShip Editor";
        }

        public override async Task<bool> Save(object entity = null)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var ledgers = gridView.RowData.Data.Cast<Ledger>().ToList();
            ledgerEntity.OriginTotalPrice = 0;
            ledgers.ForEach(x => { ledgerEntity.OriginTotalPrice += x.OriginPriceAfterTax; });
            ledgerEntity.OriginDebit = ledgerEntity.OriginTotalPrice - (ledgerEntity.OriginCredit == null ? 0 : ledgerEntity.OriginCredit);
            var res = await base.Save(ledgerEntity);
            if (res)
            {
                ledgers.ForEach(x => { x.ParentId = ledgerEntity.Id; x.InvoiceFormId = ledgerEntity.InvoiceFormId; });
                await new Client(nameof(Ledger)).BulkUpdateAsync<Ledger>(ledgers);
            }
            return res;
        }

        private int calcTotalPrice;

        public void CalcTotalPrice(Ledger ledger)
        {
            Window.ClearTimeout(calcTotalPrice);
            calcTotalPrice = Window.SetTimeout(() =>
            {
                CalcTotalPriceAsync(ledger);
            }, 500);
        }

        public void CalcTotalPriceAsync(Ledger ledger)
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
        }
    }
}
