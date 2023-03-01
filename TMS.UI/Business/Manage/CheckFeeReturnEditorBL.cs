using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class CheckFeeReturnEditorBL : TabEditor
    {
        public CheckFeeHistory CheckFeeHistoryEntity => Entity as CheckFeeHistory;
        public CheckFeeReturnEditorBL() : base(nameof(CheckFeeHistory))
        {
            Name = "CheckFee Return Editor";
        }

        public async Task ExportCheckFee()
        {
            var path = await new Client(nameof(Transportation)).PostAsync<string>(Entity["TransportationList"].As<List<Transportation>>(), "ExportCheckFee");
            Client.Download($"/excel/Download/{path}");
            Toast.Success("Xuất file thành công");
        }

        public virtual async Task Allotment()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.RefName == nameof(Transportation));
            var selected = (await gridView.GetRealTimeSelectedRows()).Cast<Transportation>().Where(x => x.Id > 0).ToList();
            if (selected.Nothing())
            {
                Toast.Warning("Vui lòng chọn cont cần phân bổ");
                return;
            }
            var fees = selected.Select(x => new Expense
            {
                ExpenseTypeId = null,
                IsReturn = true,
                UnitPrice = 0,
                Quantity = 1,
                TotalPriceAfterTax = 0,
                TotalPriceBeforeTax = 0,
                Vat = 0,
                ContainerNo = x.ContainerNo,
                SealNo = x.SealNo,
                BossId = x.BossId,
                CommodityId = x.CommodityId,
                ClosingDate = x.ClosingDate,
                ReturnDate = x.ReturnDate,
                TransportationId = x.Id
            }).ToList();
            await this.OpenPopup(
                featureName: "Allotment Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.AllotmentEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Phân bổ chi phí trả hàng";
                    instance.Entity = new Allotment
                    {
                        UnitPrice = 0,
                        Expense = fees,
                    };
                    return instance;
                });
        }

        public async Task ChangeTransportationList(Transportation transportation, ListViewItem listViewItem)
        {
            var pathModel = listViewItem.GetPathEntity();
            if (pathModel.Changes.Any(x => x.Field == nameof(Transportation.ContainerNoReturnCheck) || x.Field == nameof(Transportation.ClosingDateReturnCheck)) && (pathModel.Changes.FirstOrDefault(x => x.Field == IdField).Value.IsNullOrWhiteSpace() || transportation.Id <= 0))
            {
                PatchUpdate patchUpdate = new PatchUpdate();
                var tran = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$filter={nameof(Transportation.ReturnId)} eq {transportation.ReturnVendorId} and {nameof(Transportation.ContainerNo)} eq '{transportation.ContainerNoReturnCheck}' and (cast({nameof(Transportation.ReturnDate)},Edm.DateTimeOffset) eq cast({transportation.ClosingDateReturnCheck.Value.Date.ToISOFormat()},Edm.DateTimeOffset))");
                var changes = new List<PatchUpdateDetail>();
                var gridView = listViewItem.FindClosest<GridView>();
                if (tran != null)
                {
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Id),
                        Value = tran.Id.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ContainerNoReturnCheck),
                        Value = transportation.ContainerNoReturnCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ContainerNoUpload),
                        Value = transportation.ContainerNoReturnUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ReceivedReturnCheck),
                        Value = transportation.ReceivedReturnCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ReceivedCheckReturnUpload),
                        Value = transportation.ReceivedCheckReturnUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ClosingDateReturnCheck),
                        Value = transportation.ClosingDateReturnCheck.Value.ToString("yyyy-MM-dd"),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ClosingDateReturnUpload),
                        Value = transportation.ClosingDateReturnUpload.Value.ToString("yyyy-MM-dd"),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.SealReturnCheck),
                        Value = transportation.SealReturnCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.SealCheckReturnUpload),
                        Value = transportation.SealCheckReturnUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.BossReturnCheck),
                        Value = transportation.BossReturnCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.BossCheckReturnUpload),
                        Value = transportation.BossCheckReturnUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Cont20ReturnCheck),
                        Value = transportation.Cont20ReturnCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Cont40ReturnCheck),
                        Value = transportation.Cont40ReturnCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.PickupEmptyReturnCheck),
                        Value = transportation.PickupEmptyReturnCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.PickupEmptyReturnUpload),
                        Value = transportation.PickupEmptyReturnUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.PortLoadingReturnCheck),
                        Value = transportation.PortLoadingReturnCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.PortLoadingReturnUpload),
                        Value = transportation.PortLoadingReturnUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.LiftFeeCheckReturnUpload),
                        Value = transportation.LiftFeeCheckReturnUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.LiftFeeReturnCheck),
                        Value = transportation.LiftFeeReturnCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.LandingFeeReturnCheck),
                        Value = transportation.LandingFeeReturnCheck is null ? "0" : transportation.LandingFeeReturnCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.LandingFeeReturnUpload),
                        Value = transportation.LandingFeeReturnUpload is null ? "0" : transportation.LandingFeeReturnUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnBehaftInvoinceNoFeeReturnCheck),
                        Value = transportation.CollectOnBehaftInvoinceNoFeeReturnCheck is null ? "0" : transportation.CollectOnBehaftInvoinceNoFeeReturnCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnBehaftInvoinceNoFeeReturnUpload),
                        Value = transportation.CollectOnBehaftInvoinceNoFeeReturnUpload is null ? "0" : transportation.CollectOnBehaftInvoinceNoFeeReturnUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnBehaftFeeReturnUpload),
                        Value = transportation.CollectOnBehaftFeeReturnUpload is null ? "0" : transportation.CollectOnBehaftFeeReturnUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnBehaftFeeReturnCheck),
                        Value = transportation.CollectOnBehaftFeeReturnCheck is null ? "0" : transportation.CollectOnBehaftFeeReturnCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnSupPriceReturnCheck),
                        Value = transportation.CollectOnSupPriceReturnCheck is null ? "0" : transportation.CollectOnSupPriceReturnCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnSupPriceReturnUpload),
                        Value = transportation.CollectOnSupPriceReturnUpload is null ? "0" : transportation.CollectOnSupPriceReturnUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.TotalPriceAfterTaxReturnCheck),
                        Value = transportation.TotalPriceAfterTaxReturnCheck is null ? "0" : transportation.TotalPriceAfterTaxReturnCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.TotalPriceAfterTaxReturnUpload),
                        Value = transportation.TotalPriceAfterTaxReturnUpload is null ? "0" : transportation.TotalPriceAfterTaxReturnUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVatReturn),
                        Value = transportation.FeeVatReturn is null ? "0" : transportation.FeeVatReturn.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVatReturn2),
                        Value = transportation.FeeVatReturn2 is null ? "0" : transportation.FeeVatReturn2.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVatReturn3),
                        Value = transportation.FeeVatReturn3 is null ? "0" : transportation.FeeVatReturn3.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVat1UploadReturn),
                        Value = transportation.FeeVat1UploadReturn is null ? "0" : transportation.FeeVat1UploadReturn.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVat2UploadReturn),
                        Value = transportation.FeeVat2UploadReturn is null ? "0" : transportation.FeeVat2UploadReturn.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVat3UploadReturn),
                        Value = transportation.FeeVat3UploadReturn is null ? "0" : transportation.FeeVat3UploadReturn.ToString(),
                    });
                    //
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeReturn1),
                        Value = transportation.FeeReturn1 is null ? "0" : transportation.FeeReturn1.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeReturn2),
                        Value = transportation.FeeReturn2 is null ? "0" : transportation.FeeReturn2.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeReturn3),
                        Value = transportation.FeeReturn3 is null ? "0" : transportation.FeeReturn3.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeReturn4),
                        Value = transportation.FeeReturn4 is null ? "0" : transportation.FeeReturn4.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeReturn5),
                        Value = transportation.FeeReturn5 is null ? "0" : transportation.FeeReturn5.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeReturn6),
                        Value = transportation.FeeReturn6 is null ? "0" : transportation.FeeReturn6.ToString(),
                    });
                    //
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee1UploadReturn),
                        Value = transportation.Fee1UploadReturn is null ? "0" : transportation.Fee1UploadReturn.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee2UploadReturn),
                        Value = transportation.Fee2UploadReturn is null ? "0" : transportation.Fee2UploadReturn.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee3UploadReturn),
                        Value = transportation.Fee3UploadReturn is null ? "0" : transportation.Fee3UploadReturn.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee4UploadReturn),
                        Value = transportation.Fee4UploadReturn is null ? "0" : transportation.Fee4UploadReturn.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee5UploadReturn),
                        Value = transportation.Fee5UploadReturn is null ? "0" : transportation.Fee5UploadReturn.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee6UploadReturn),
                        Value = transportation.Fee6UploadReturn is null ? "0" : transportation.Fee6UploadReturn.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CheckFeeHistoryReturnId),
                        Value = CheckFeeHistoryEntity.Id.ToString(),
                    });
                    patchUpdate.Changes = changes;
                    var rs = await new Client(nameof(Transportation)).PatchAsync<Transportation>(patchUpdate);
                    listViewItem.Entity.CopyPropFrom(rs);
                    await gridView.LoadMasterData(new object[] { rs });
                    listViewItem.EmptyRow = false;
                    listViewItem.UpdateView(true);
                    Toast.Success("Đã tìm thấy cont!");
                }
                else
                {
                    tran = new Transportation();
                    tran.ReturnVendorId = tran.ReturnVendorId;
                    tran.OrderExcelReturn = tran.OrderExcelReturn;
                    tran.CheckFeeHistoryReturnId = tran.CheckFeeHistoryReturnId;
                    tran.ReceivedReturnCheck = tran.ReceivedReturnCheck;
                    tran.ClosingDateReturnCheck = tran.ClosingDateReturnCheck;
                    tran.SealReturnCheck = tran.SealReturnCheck;
                    tran.ContainerNoReturnCheck = tran.ContainerNoReturnCheck;
                    tran.BossReturnCheck = tran.BossReturnCheck;
                    tran.Cont20ReturnCheck = tran.Cont20ReturnCheck;
                    tran.Cont40ReturnCheck = tran.Cont40ReturnCheck;
                    tran.ClosingPercentReturnCheck = tran.ClosingPercentReturnCheck;
                    tran.PickupEmptyReturnCheck = tran.PickupEmptyReturnCheck;
                    tran.PortLoadingReturnCheck = tran.PortLoadingReturnCheck;
                    tran.LiftFeeReturnCheck = tran.LiftFeeReturnCheck;
                    tran.LandingFeeReturnCheck = tran.LandingFeeReturnCheck;
                    tran.CollectOnBehaftInvoinceNoFeeReturnCheck = tran.CollectOnBehaftInvoinceNoFeeReturnCheck;
                    tran.FeeVatReturn = tran.FeeVatReturn;
                    tran.FeeVatReturn2 = tran.FeeVatReturn2;
                    tran.FeeVatReturn3 = tran.FeeVatReturn3;
                    tran.FeeVat1UploadReturn = tran.FeeVat1UploadReturn;
                    tran.FeeVat2UploadReturn = tran.FeeVat2UploadReturn;
                    tran.FeeVat3UploadReturn = tran.FeeVat3UploadReturn;
                    tran.FeeReturn1 = tran.FeeReturn1;
                    tran.FeeReturn2 = tran.FeeReturn2;
                    tran.FeeReturn3 = tran.FeeReturn3;
                    tran.FeeReturn4 = tran.FeeReturn4;
                    tran.FeeReturn5 = tran.FeeReturn5;
                    tran.FeeReturn6 = tran.FeeReturn6;
                    tran.Fee1UploadReturn = tran.Fee1UploadReturn;
                    tran.Fee2UploadReturn = tran.Fee2UploadReturn;
                    tran.Fee3UploadReturn = tran.Fee3UploadReturn;
                    tran.Fee4UploadReturn = tran.Fee4UploadReturn;
                    tran.Fee5UploadReturn = tran.Fee5UploadReturn;
                    tran.Fee6UploadReturn = tran.Fee6UploadReturn;
                    tran.CollectOnBehaftFeeReturnCheck = tran.CollectOnBehaftFeeReturnCheck;
                    tran.CollectOnSupPriceReturnCheck = tran.CollectOnSupPriceReturnCheck;
                    tran.TotalPriceAfterTaxReturnCheck = tran.TotalPriceAfterTaxReturnCheck;
                    tran.ReceivedCheckReturnUpload = tran.ReceivedCheckReturnUpload;
                    tran.ClosingDateReturnUpload = tran.ClosingDateReturnUpload;
                    tran.SealCheckReturnUpload = tran.SealCheckReturnUpload;
                    tran.ContainerNoReturnUpload = tran.ContainerNoReturnUpload;
                    tran.Cont20CheckReturnUpload = tran.Cont20CheckReturnUpload;
                    tran.Cont40CheckReturnUpload = tran.Cont40CheckReturnUpload;
                    tran.ClosingPercentReturnUpload = tran.ClosingPercentReturnUpload;
                    tran.PickupEmptyReturnUpload = tran.PickupEmptyReturnUpload;
                    tran.PortLoadingReturnUpload = tran.PortLoadingReturnUpload;
                    tran.LiftFeeCheckReturnUpload = tran.LiftFeeCheckReturnUpload;
                    tran.LandingFeeReturnUpload = tran.LandingFeeReturnUpload;
                    tran.CollectOnBehaftInvoinceNoFeeReturnUpload = tran.CollectOnBehaftInvoinceNoFeeReturnUpload;
                    tran.CollectOnBehaftFeeReturnUpload = tran.CollectOnBehaftFeeReturnUpload;
                    tran.CollectOnSupPriceReturnUpload = tran.CollectOnSupPriceReturnUpload;
                    tran.TotalPriceAfterTaxReturnUpload = tran.TotalPriceAfterTaxReturnUpload;
                    listViewItem.Entity.CopyPropFrom(tran);
                    await gridView.LoadMasterData(new List<Transportation>() { tran });
                    listViewItem.EmptyRow = true;
                    listViewItem.UpdateView(true);
                    Toast.Warning("Không tìm thấy cont!");
                }
            }
            else
            {
                await listViewItem.PatchUpdate();
            }
        }

        public void CheckStatusQuotationReturn()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.RefName == nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            gridView.BodyContextMenuShow += () =>
            {
                ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                {
                        new ContextMenuItem { Icon = "fas fa-pen", Text = "Cập nhật giá", Click = UpdateQuotation },
                };
            };
        }

        public void UpdateQuotation(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.RefName == nameof(Transportation));

            Task.Run(async () =>
            {
                var selected = gridView.LastListViewItem;
                if (selected is null)
                {
                    Toast.Warning("Vui lòng chọn cont cần cập nhật giá!");
                    return;
                }
                var coords = selected.Entity.As<Transportation>();
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7593 and BossId eq {coords.BossId} and ContainerTypeId eq {coords.ContainerTypeId} and LocationId eq {coords.ReturnId} and StartDate le {coords.ReturnDate.Value.ToOdataFormat()} and PackingId eq {coords.ReturnVendorId}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7593,
                        BossId = coords.BossId,
                        ContainerTypeId = coords.ContainerTypeId,
                        LocationId = coords.ReturnId,
                        StartDate = coords.ReturnDate,
                        PackingId = coords.ReturnVendorId
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá trả hàng";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }
    }
}
