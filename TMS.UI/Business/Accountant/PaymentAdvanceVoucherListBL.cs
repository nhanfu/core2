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
    public class PaymentAdvanceVoucherListBL : TabEditor
    {
        public GridView gridView;
        public Ledger selectedLedger { get; set; }
        public Ledger ledgerEntity => Entity as Ledger;
        public PaymentAdvanceVoucherListBL() : base(nameof(Ledger))
        {
            Name = "PaymentAdvanceVoucher List";
        }

        public async Task AddPaymentAdvanceVoucher()
        {
            await this.OpenPopup(
                featureName: "PaymentAdvanceVoucher Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.PaymentAdvanceVoucherEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới phiếu chi tạm ứng";
                    instance.Entity = new Ledger()
                    {
                        InvoiceFormId = 16051,
                        BillDate = DateTime.Now,
                        InvoiceDate = DateTime.Now
                    };
                    return instance;
                });
        }

        public async Task EditPaymentAdvanceVoucher(Ledger ledger)
        {
            await this.OpenPopup(
                featureName: "PaymentAdvanceVoucher Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.PaymentAdvanceVoucherEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa phiếu chi tạm ứng";
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
            var ledgers = await new Client(nameof(Ledger)).GetRawList<Ledger>($"?$filter=Active eq true and InvoiceFormId eq 16051 and ParentId eq null and Id ge {selectedLedger} and TypeId eq null");
            var no = int.Parse(ledgers.FirstOrDefault().InvoiceNo.Substring(10));
            ledgers.ForEach(x =>
            {
                x.InvoiceNo = no < 10000 ? "PCTU" + x.InvoiceDate.Value.ToString("yy") + "/" + x.InvoiceDate.Value.ToString("MM") + "-" + $"{no:0000}" : no.ToString();
                no++;
            });
        }
    }
}
