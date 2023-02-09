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

namespace TMS.UI.Business.Accountant
{
    public class ImportRevenueSimultaneousBL : PopupEditor
    {
        public Transportation transportationEntity => Entity as Transportation;
        public ImportRevenueSimultaneousBL() : base(nameof(Transportation))
        {
            Name = "Import Revenue Simultaneous";
        }

        public async Task ImportRevenueSimultaneous()
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
            confirm.YesConfirmed += async () =>
            {
                Spinner.AppendTo(this.Element, true);
                foreach (var item in listViewItems)
                {
                    var newRevenue = new Revenue()
                    {
                        LotNo = transportationEntity.LotNo,
                        LotDate = transportationEntity.LotDate,
                        InvoinceNo = transportationEntity.InvoinceNo,
                        InvoinceDate = transportationEntity.InvoinceDate,
                        UnitPriceBeforeTax = transportationEntity.UnitPriceBeforeTax,
                        UnitPriceAfterTax = transportationEntity.UnitPriceAfterTax,
                        ReceivedPrice = transportationEntity.ReceivedPrice,
                        CollectOnBehaftPrice = transportationEntity.CollectOnBehaftPrice,
                        Vat = transportationEntity.Vat,
                        TotalPriceBeforTax = transportationEntity.TotalPriceBeforTax,
                        VatPrice = transportationEntity.VatPrice,
                        TotalPrice = transportationEntity.TotalPrice,
                        NotePayment = transportationEntity.NotePayment,
                        VendorVatId = transportationEntity.VendorVatId,
                        BossId= item.BossId,
                        TransportationId = item.Id,
                        Active = true,
                        InsertedDate = DateTime.Now.Date,
                        InsertedBy = Client.Token.UserId
                    };
                    await new Client(nameof(Revenue)).CreateAsync<Revenue>(newRevenue);
                }
                Dispose();
                Toast.Success("Đã nhập thành công");
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

        public void CalcRevenue(Transportation revenue)
        {
            revenue.Vat = revenue.Vat == null ? 10 : revenue.Vat;
            revenue.TotalPriceBeforTax = Math.Round(revenue.TotalPrice == null ? 0 : (decimal)revenue.TotalPrice / (1 + ((decimal)revenue.Vat / 100)));
            revenue.VatPrice = Math.Round(revenue.TotalPriceBeforTax == null ? 0 : (decimal)revenue.TotalPriceBeforTax * (decimal)revenue.Vat / 100);
            this.UpdateView(false, nameof(Transportation.Vat), nameof(Transportation.TotalPriceBeforTax), nameof(Transportation.VatPrice));
        }

        public PatchUpdate GetPatchEntity(Transportation transportation)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportation.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.LotNo), Value = transportation.LotNo?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.LotDate), Value = transportation.LotDate?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.InvoinceNo), Value = transportation.InvoinceNo?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.InvoinceDate), Value = transportation.InvoinceDate?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.UnitPriceBeforeTax), Value = transportation.UnitPriceBeforeTax?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.UnitPriceAfterTax), Value = transportation.UnitPriceAfterTax?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.ReceivedPrice), Value = transportation.ReceivedPrice?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.CollectOnBehaftPrice), Value = transportation.CollectOnBehaftPrice?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.Vat), Value = transportation.Vat?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.TotalPriceBeforTax), Value = transportation.TotalPriceBeforTax?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.VatPrice), Value = transportation.VatPrice?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.TotalPrice), Value = transportation.TotalPrice?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.Cp1), Value = transportation.Cp1.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.Cp2), Value = transportation.Cp2.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.NotePayment), Value = transportation.NotePayment?.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.VendorVatId), Value = transportation.VendorVatId?.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}
