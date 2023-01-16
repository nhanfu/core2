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
    public class FreightShipListBL : TabEditor
    {
        public Ledger selectedLedger { get; set; }
        public FreightShipListBL() : base(nameof(Ledger))
        {
            Name = "FreightShip List";
        }

        public void CheckAllPaid()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Ledger));
            if (gridView is null)
            {
                return;
            }
            gridView.BodyContextMenuShow += () =>
            {
                ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                {
                        new ContextMenuItem { Icon = "fas fa-money-check-alt mr-1", Text = "Phiếu chi", Click = OpenPaymentVoucher },
                        new ContextMenuItem { Icon = "fas fa-money-check-alt mr-1", Text = "Phiếu UNC", Click = OpenCreditNote },
                };
            };
        }

        public async Task AddFreightShip()
        {
            await this.OpenPopup(
                featureName: "FreightShip Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.FreightShipEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới phiếu cước tàu";
                    instance.Entity = new Ledger()
                    {
                        InvoiceFormId = 16048,
                        BillDate = DateTime.Now,
                        InvoiceDate = DateTime.Now
                    };
                    return instance;
                });
        }

        public async Task EditFreightShip(Ledger ledger)
        {
            await this.OpenPopup(
                featureName: "FreightShip Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.FreightShipEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa phiếu cước tàu";
                    instance.Entity = ledger;
                    return instance;
                });
        }

        public void OpenCreditNote(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var ledgers = gridView.GetSelectedRows().Cast<Ledger>().ToList();
            if (ledgers.Count <= 0)
            {
                Toast.Warning("Vui lòng chọn phiếu muốn chi");
                return;
            }
            if (ledgers.Where(x => x.ObjectId == null).Count() > 0)
            {
                Toast.Warning("Vui lòng chỉ chọn những phiếu có đối tượng công nợ");
                return;
            }
            if (ledgers.Select(x => x.ObjectId).Distinct().Count() > 1)
            {
                Toast.Warning("Vui lòng chỉ chọn những phiếu có cùng đối tượng công nợ");
                return;
            }
            if (ledgers.Where(x => x.OriginDebit <= 0).Count() > 0)
            {
                Toast.Warning("Vui lòng chỉ chọn những phiếu còn nợ");
                return;
            }
            Task.Run(async () =>
            {
                await this.OpenPopup(
                featureName: "CreditNote Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.CreditNoteEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    var ledger = ledgers.FirstOrDefault();
                    instance.Title = "Phiếu UNC";
                    instance.Entity = new Ledger()
                    {
                        InvoiceFormId = 16054,
                        InvoiceDate = DateTime.Now,
                        VendorId = ledger.VendorId,
                        ObjectId = ledger.ObjectId,
                        Taxcode = ledger.Taxcode
                    };
                    return instance;
                });
            });
        }

        public void OpenPaymentVoucher(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var ledgers = gridView.GetSelectedRows().Cast<Ledger>().ToList();
            if (ledgers.Count <= 0)
            {
                Toast.Warning("Vui lòng chọn phiếu muốn chi");
                return;
            }
            if (ledgers.Where(x => x.ObjectId == null).Count() > 0)
            {
                Toast.Warning("Vui lòng chỉ chọn những phiếu có đối tượng công nợ");
                return;
            }
            if (ledgers.Select(x => x.ObjectId).Distinct().Count() > 1)
            {
                Toast.Warning("Vui lòng chỉ chọn những phiếu có cùng đối tượng công nợ");
                return;
            }
            if (ledgers.Where(x => x.OriginDebit <= 0).Count() > 0)
            {
                Toast.Warning("Vui lòng chỉ chọn những phiếu còn nợ");
                return;
            }
            Task.Run(async () =>
            {
                await this.OpenPopup(
                featureName: "PaymentVoucher Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.PaymentVoucherEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    var ledger = ledgers.FirstOrDefault();
                    instance.Title = "Phiếu chi";
                    instance.Entity = new Ledger()
                    {
                        InvoiceFormId = 16049,
                        InvoiceDate = DateTime.Now,
                        VendorId = ledger.VendorId,
                        ObjectId = ledger.ObjectId,
                        Taxcode = ledger.Taxcode
                    };
                    return instance;
                });
            });
        }

        public void SelectedLedger(Ledger ledger)
        {
            selectedLedger = ledger;
        }

        public async Task SetInvoiceNoWhenDel()
        {
            var ledgers = await new Client(nameof(Ledger)).GetRawList<Ledger>($"?$filter=Active eq true and InvoiceFormId eq 16048 and ParentId eq null and Id ge {selectedLedger} and TypeId eq null");
            var no = int.Parse(ledgers.FirstOrDefault().InvoiceNo.Substring(10));
            ledgers.ForEach(x =>
            {
                x.InvoiceNo = no < 10000 ? "CT" + x.InvoiceDate.Value.ToString("yy") + "/" + x.InvoiceDate.Value.ToString("MM") + "-" + $"{no:0000}" : no.ToString();
                no++;
            });
        }
    }
}
