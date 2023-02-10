using Bridge;
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
using static Retyped.dom.Literals.Types;

namespace TMS.UI.Business.Accountant
{
    public class ImportRevenueSimultaneousBL : PopupEditor
    {
        public Revenue revenueEntity => Entity as Revenue;
        public ImportRevenueSimultaneousBL() : base(nameof(Revenue))
        {
            Name = "Import Revenue Simultaneous";
        }

        public async Task CreateRevenueSimultaneous()
        {
            var gridView = Parent.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
            if (gridView is null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({ids.Combine()})");
            var listViewItems = transportations.Where(x => x.IsLocked == false && x.IsSubmit == false).ToList();
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Không có DSVC nào có thể nhập");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn nhập doanh thu cho " + listViewItems.Count + " DSVC ?",
            };
            confirm.Render();
            var revenues = new List<Revenue>();
            confirm.YesConfirmed += async () =>
            {
                Spinner.AppendTo(this.Element, true);
                foreach (var item in listViewItems)
                {
                    var newRevenue = new Revenue()
                    {
                        LotNo = revenueEntity.LotNo,
                        LotDate = revenueEntity.LotDate,
                        InvoinceNo = revenueEntity.InvoinceNo,
                        InvoinceDate = revenueEntity.InvoinceDate,
                        UnitPriceBeforeTax = revenueEntity.UnitPriceBeforeTax,
                        UnitPriceAfterTax = revenueEntity.UnitPriceAfterTax,
                        ReceivedPrice = revenueEntity.ReceivedPrice,
                        CollectOnBehaftPrice = revenueEntity.CollectOnBehaftPrice,
                        Vat = revenueEntity.Vat,
                        TotalPriceBeforTax = revenueEntity.TotalPriceBeforTax,
                        VatPrice = revenueEntity.VatPrice,
                        TotalPrice = revenueEntity.TotalPrice,
                        NotePayment = revenueEntity.NotePayment,
                        VendorVatId = revenueEntity.VendorVatId,
                        BossId= item.BossId,
                        ContainerNo= item.ContainerNo,
                        SealNo= item.SealNo,
                        ContainerTypeId= item.ContainerTypeId,
                        ClosingDate= item.ClosingDate,
                        TransportationId = item.Id,
                        Id = 0,
                        Active = true,
                        InsertedDate = DateTime.Now.Date,
                        InsertedBy = Client.Token.UserId
                    };
                    revenues.Add(newRevenue);
                }
                var res = await new Client(nameof(Revenue)).BulkUpdateAsync<Revenue>(revenues);
                if (res != null)
                {
                    await gridView.ApplyFilter(true);
                    Dispose();
                    Toast.Success("Đã nhập thành công");
                }
                else
                {
                    Toast.Success("Đã có lỗi trong quá trình xử lý");
                }
            };
        }

        public async Task UpdateRevenueSimultaneous()
        {
            var gridView = Parent.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
            if (gridView is null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({ids.Combine()})");
            var listViewItems = transportations.Where(x => x.IsLocked == false && x.IsSubmit == false).ToList();
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Không có DSVC nào có thể nhập");
                return;
            }
            var revenues = await new Client(nameof(Revenue)).GetRawList<Revenue>($"?$filter=Active eq true and TransportationId in ({ids.Combine()})");
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn nhập doanh thu cho " + listViewItems.Count + " DSVC với " + revenues.Count + " dòng doanh thu ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                Spinner.AppendTo(this.Element, true);
                foreach (var item in revenues)
                {
                    item.LotNo = item.LotNo == null || item.LotNo == "" ? revenueEntity.LotNo : item.LotNo;
                    item.LotDate = item.LotDate == null ? revenueEntity.LotDate : item.LotDate;
                    item.InvoinceNo = item.InvoinceNo == null || item.InvoinceNo == "" ? revenueEntity.InvoinceNo : item.InvoinceNo;
                    item.InvoinceDate = item.InvoinceDate == null ? revenueEntity.InvoinceDate : item.InvoinceDate;
                    item.UnitPriceBeforeTax = item.UnitPriceBeforeTax == null || item.UnitPriceBeforeTax == 0 ? revenueEntity.UnitPriceBeforeTax : item.UnitPriceBeforeTax;
                    item.UnitPriceAfterTax = item.UnitPriceAfterTax == null || item.UnitPriceAfterTax == 0 ? revenueEntity.UnitPriceAfterTax : item.UnitPriceAfterTax;
                    item.ReceivedPrice = item.ReceivedPrice == null || item.ReceivedPrice == 0 ? revenueEntity.ReceivedPrice : item.ReceivedPrice;
                    item.CollectOnBehaftPrice = item.CollectOnBehaftPrice == null || item.CollectOnBehaftPrice == 0  ? revenueEntity.CollectOnBehaftPrice : item.CollectOnBehaftPrice;
                    item.Vat = item.Vat == null || item.Vat == 0  ? revenueEntity.Vat : item.Vat;
                    item.TotalPriceBeforTax = item.TotalPriceBeforTax == null || item.TotalPriceBeforTax == 0 ? revenueEntity.TotalPriceBeforTax : item.TotalPriceBeforTax;  
                    item.VatPrice = item.VatPrice == null || item.VatPrice == 0  ? revenueEntity.VatPrice : item.VatPrice;
                    item.TotalPrice = item.TotalPrice == null || item.TotalPrice == 0  ? revenueEntity.TotalPrice : item.TotalPrice;
                    item.NotePayment = item.NotePayment == null || item.NotePayment == "" ? revenueEntity.NotePayment : item.NotePayment;
                    item.VendorVatId = item.VendorVatId == null ? revenueEntity.VendorVatId : item.VendorVatId;
                }
                var res = await new Client(nameof(Revenue)).BulkUpdateAsync<Revenue>(revenues);
                if (res != null)
                {
                    await gridView.ApplyFilter(true);
                    Dispose();
                    Toast.Success("Đã nhập thành công");
                }
                else
                {
                    Toast.Success("Đã có lỗi trong quá trình xử lý");
                }
            };
        }

        public async Task UpdateRevenueSimultaneousByRevenue()
        {
            var gridView = Parent.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "Revenue");
            if (gridView is null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var revenues = await new Client(nameof(Revenue)).GetRawList<Revenue>($"?$filter=Active eq true and Id in ({ids.Combine()})");
            var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({revenues.Select(x => x.TransportationId).Combine()})");
            var listViewItems = transportations.Where(x => x.IsLocked == false && x.IsSubmit == false).ToList();
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Không có doanh thu nào có thể nhập");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn nhập doanh thu cho " + revenues.Count + " dòng doanh thu ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                Spinner.AppendTo(this.Element, true);
                foreach (var item in revenues)
                {
                    item.LotNo = item.LotNo == null || item.LotNo == "" ? revenueEntity.LotNo : item.LotNo;
                    item.LotDate = item.LotDate == null ? revenueEntity.LotDate : item.LotDate;
                    item.InvoinceNo = item.InvoinceNo == null || item.InvoinceNo == "" ? revenueEntity.InvoinceNo : item.InvoinceNo;
                    item.InvoinceDate = item.InvoinceDate == null ? revenueEntity.InvoinceDate : item.InvoinceDate;
                    item.UnitPriceBeforeTax = item.UnitPriceBeforeTax == null || item.UnitPriceBeforeTax == 0 ? revenueEntity.UnitPriceBeforeTax : item.UnitPriceBeforeTax;
                    item.UnitPriceAfterTax = item.UnitPriceAfterTax == null || item.UnitPriceAfterTax == 0 ? revenueEntity.UnitPriceAfterTax : item.UnitPriceAfterTax;
                    item.ReceivedPrice = item.ReceivedPrice == null || item.ReceivedPrice == 0 ? revenueEntity.ReceivedPrice : item.ReceivedPrice;
                    item.CollectOnBehaftPrice = item.CollectOnBehaftPrice == null || item.CollectOnBehaftPrice == 0 ? revenueEntity.CollectOnBehaftPrice : item.CollectOnBehaftPrice;
                    item.Vat = item.Vat == null || item.Vat == 0 ? revenueEntity.Vat : item.Vat;
                    item.TotalPriceBeforTax = item.TotalPriceBeforTax == null || item.TotalPriceBeforTax == 0 ? revenueEntity.TotalPriceBeforTax : item.TotalPriceBeforTax;
                    item.VatPrice = item.VatPrice == null || item.VatPrice == 0 ? revenueEntity.VatPrice : item.VatPrice;
                    item.TotalPrice = item.TotalPrice == null || item.TotalPrice == 0 ? revenueEntity.TotalPrice : item.TotalPrice;
                    item.NotePayment = item.NotePayment == null || item.NotePayment == "" ? revenueEntity.NotePayment : item.NotePayment;
                    item.VendorVatId = item.VendorVatId == null ? revenueEntity.VendorVatId : item.VendorVatId;
                }
                var res = await new Client(nameof(Revenue)).BulkUpdateAsync<Revenue>(revenues);
                if (res != null)
                {
                    await gridView.ApplyFilter(true);
                    Dispose();
                    Toast.Success("Đã nhập thành công");
                }
                else
                {
                    Toast.Success("Đã có lỗi trong quá trình xử lý");
                }
            };
        }

        public override void Cancel()
        {
            this.Dispose();
            base.Cancel();
        }

        public override void CancelWithoutAsk()
        {
            this.Dispose();
            base.CancelWithoutAsk();
        }

        public void CalcRevenue()
        {
            revenueEntity.Vat = revenueEntity.Vat == null ? 10 : revenueEntity.Vat;
            revenueEntity.TotalPriceBeforTax = Math.Round(revenueEntity.TotalPrice == null ? 0 : (decimal)revenueEntity.TotalPrice / (1 + ((decimal)revenueEntity.Vat / 100)));
            revenueEntity.VatPrice = Math.Round(revenueEntity.TotalPriceBeforTax == null ? 0 : (decimal)revenueEntity.TotalPriceBeforTax * (decimal)revenueEntity.Vat / 100);
            this.UpdateView(false, nameof(Transportation.Vat), nameof(Transportation.TotalPriceBeforTax), nameof(Transportation.VatPrice));
        }

        public void CalcRevenueTotalPrice()
        {
            revenueEntity.UnitPriceAfterTax = revenueEntity.UnitPriceAfterTax == null ? 0 : revenueEntity.UnitPriceAfterTax;
            revenueEntity.ReceivedPrice = revenueEntity.ReceivedPrice == null ? 0 : revenueEntity.ReceivedPrice;
            revenueEntity.TotalPrice = revenueEntity.UnitPriceAfterTax + revenueEntity.ReceivedPrice;
            this.UpdateView(false, nameof(Transportation.TotalPrice));
            CalcRevenue();
        }

        public PatchUpdate GetPatchEntity(Revenue revenue)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = revenue.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.LotNo), Value = revenue.LotNo?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.LotDate), Value = revenue.LotDate?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.InvoinceNo), Value = revenue.InvoinceNo?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.InvoinceDate), Value = revenue.InvoinceDate?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.UnitPriceBeforeTax), Value = revenue.UnitPriceBeforeTax?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.UnitPriceAfterTax), Value = revenue.UnitPriceAfterTax?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.ReceivedPrice), Value = revenue.ReceivedPrice?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.CollectOnBehaftPrice), Value = revenue.CollectOnBehaftPrice?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.Vat), Value = revenue.Vat?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.TotalPriceBeforTax), Value = revenue.TotalPriceBeforTax?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.VatPrice), Value = revenue.VatPrice?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.TotalPrice), Value = revenue.TotalPrice?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.NotePayment), Value = revenue.NotePayment?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.VendorVatId), Value = revenue.VendorVatId?.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}
