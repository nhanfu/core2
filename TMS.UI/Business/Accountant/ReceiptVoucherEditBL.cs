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
    public class ReceiptVoucherEditBL : PopupEditor
    {
        public GridView gridView;
        public Ledger ledgerEntity => Entity as Ledger;
        public List<Ledger> ledgerParentList = new List<Ledger>();
        public ReceiptVoucherEditBL() : base(nameof(Ledger))
        {
            Name = "ReceiptVoucher Editor";
        }

        protected override void ToggleApprovalBtn(object entity = null)
        {
            if (ledgerEntity.Id > 0)
            {
                this.SetShow(false, "btnCreateInvoice");
                this.SetShow(true, "btnSave");
            }
            else
            {
                this.SetShow(true, "btnCreateInvoice");
                this.SetShow(false, "btnSave");
            }
            base.ToggleApprovalBtn(entity);
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

        public async Task SetExchangeRate()
        {
            var masterData = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq {ledgerEntity.CurrencyId}");
            ledgerEntity.ExchangeRate = masterData.Enum;
            this.UpdateView(false, nameof(Ledger.ExchangeRate));
            CostCalcLedgers();
        }

        public void CostCalcLedgers()
        {
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault(x => x.Name == nameof(Ledger));
            var ledgers = gridView.RowData.Data.Cast<Ledger>().ToList();
            ledgers.ForEach(x =>
            {
                var listViewItem = gridView.GetListViewItems(x).FirstOrDefault();
                if (listViewItem == null)
                {
                    return;
                }
                x.OriginPriceAfterTax = x.ExchangeRate > 1 ? Math.Round((decimal)x.OriginPriceAfterTax / (decimal)x.ExchangeRate) : x.OriginPriceAfterTax;
                x.CurrencyId = ledgerEntity.CurrencyId;
                x.ExchangeRate = ledgerEntity.ExchangeRate;
                x.OriginPriceAfterTax *= (x.ExchangeRate is null ? 0 : (decimal)x.ExchangeRate);
                listViewItem.UpdateView(false, nameof(Ledger.OriginPriceAfterTax));
            });
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
    }
}