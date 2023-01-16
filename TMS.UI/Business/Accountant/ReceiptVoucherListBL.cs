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
    public class ReceiptVoucherListBL : TabEditor
    {
        public GridView gridView;
        public Ledger selectedLedger { get; set; }
        public Ledger ledgerEntity => Entity as Ledger;
        public ReceiptVoucherListBL() : base(nameof(Ledger))
        {
            Name = "ReceiptVoucher List";
        }

        public async Task AddReceiptVoucher()
        {
            await this.OpenPopup(
                featureName: "ReceiptVoucher Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.ReceiptVoucherEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới phiếu thu";
                    instance.Entity = new Ledger()
                    {
                        ExchangeRate = 1,
                        CurrencyId = 16083,
                        InvoiceFormId = 16050,
                        BillDate = DateTime.Now,
                        InvoiceDate = DateTime.Now
                    };
                    return instance;
                });
        }

        public async Task EditReceiptVoucher(Ledger ledger)
        {
            await this.OpenPopup(
                featureName: "ReceiptVoucher Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.ReceiptVoucherEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa phiếu thu";
                    instance.Entity = ledger;
                    return instance;
                });
        }

        public void SelectedLedger(Ledger ledger)
        {
            selectedLedger = ledger;
        }

        public async Task SetInvoiceNoWhenDel()
        {
            var ledgers = await new Client(nameof(Ledger)).GetRawList<Ledger>($"?$filter=Active eq true and InvoiceFormId eq 16050 and ParentId eq null and Id ge {selectedLedger} and TypeId eq null");
            var no = int.Parse(ledgers.FirstOrDefault().InvoiceNo.Substring(10));
            ledgers.ForEach(x =>
            {
                x.InvoiceNo = no < 10000 ? "PT" + x.InvoiceDate.Value.ToString("yy") + "/" + x.InvoiceDate.Value.ToString("MM") + "-" + $"{no:0000}" : no.ToString();
                no++;
            });
        }
    }
}
