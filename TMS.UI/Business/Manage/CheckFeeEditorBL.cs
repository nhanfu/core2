using Core.Clients;
using Core.Components.Forms;
using System.Threading.Tasks;
using TMS.API.Models;
using Core.Extensions;
using Core.Components.Extensions;
using Core.Components;
using System.Linq;
using Core.ViewModels;
using System.Collections.Generic;
using System;

namespace TMS.UI.Business.Manage
{
    public class CheckFeeEditorBL : TabEditor
    {
        public CheckFeeHistory CheckFeeHistoryEntity => Entity as CheckFeeHistory;
        public CheckFeeEditorBL() : base(nameof(CheckFeeHistory))
        {
            Name = "CheckFee Editor";
        }

        public async Task ExportCheckFee()
        {
            var path = await new Client(nameof(Transportation)).PostAsync<string>(CheckFeeHistoryEntity, "ExportCheckFee?Type=1");
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
                    instance.Title = "Phân bổ chi phí đóng hàng";
                    instance.Entity = new Allotment
                    {
                        UnitPrice = 0,
                        Expense = fees
                    };
                    return instance;
                });
        }

        public async Task ChangeTransportationList(Transportation transportation, ListViewItem listViewItem)
        {
            var pathModel = listViewItem.GetPathEntity();
            if (pathModel.Changes.Any(x => x.Field == nameof(Transportation.ContainerNoCheck) || x.Field == nameof(Transportation.ClosingDateCheck)) && (pathModel.Changes.FirstOrDefault(x => x.Field == IdField).Value.IsNullOrWhiteSpace() || transportation.Id <= 0))
            {
                PatchUpdate patchUpdate = new PatchUpdate();
                var tran = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$filter={nameof(Transportation.ClosingId)} eq {transportation.ClosingId} and {nameof(Transportation.ContainerNo)} eq '{transportation.ContainerNoCheck}' and (cast({nameof(Transportation.ClosingDate)},Edm.DateTimeOffset) eq cast({transportation.ClosingDateCheck.Value.Date.ToISOFormat()},Edm.DateTimeOffset))");
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
                        Field = nameof(Transportation.ContainerNoCheck),
                        Value = transportation.ContainerNoCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ContainerNoUpload),
                        Value = transportation.ContainerNoUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ReceivedCheck),
                        Value = transportation.ReceivedCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ReceivedCheckUpload),
                        Value = transportation.ReceivedCheckUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ClosingDateCheck),
                        Value = transportation.ClosingDateCheck is null ? null : transportation.ClosingDateCheck.Value.ToString("yyyy-MM-dd"),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ClosingDateUpload),
                        Value = transportation.ClosingDateUpload is null ? null : transportation.ClosingDateUpload.Value.ToString("yyyy-MM-dd"),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.SealCheck),
                        Value = transportation.SealCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.SealCheckUpload),
                        Value = transportation.SealCheckUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.BossCheck),
                        Value = transportation.BossCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.BossCheckUpload),
                        Value = transportation.BossCheckUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Cont20Check),
                        Value = transportation.Cont20Check.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Cont40Check),
                        Value = transportation.Cont40Check.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.PickupEmptyCheck),
                        Value = transportation.PickupEmptyCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.PickupEmptyUpload),
                        Value = transportation.PickupEmptyUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.PortLoadingCheck),
                        Value = transportation.PortLoadingCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.PortLoadingUpload),
                        Value = transportation.PortLoadingUpload,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.LiftFeeCheckUpload),
                        Value = transportation.LiftFeeCheckUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.LiftFeeCheck),
                        Value = transportation.LiftFeeCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.LandingFeeCheck),
                        Value = transportation.LandingFeeCheck is null ? "0" : transportation.LandingFeeCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.LandingFeeUpload),
                        Value = transportation.LandingFeeUpload is null ? "0" : transportation.LandingFeeUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnBehaftInvoinceNoFeeCheck),
                        Value = transportation.CollectOnBehaftInvoinceNoFeeCheck is null ? "0" : transportation.CollectOnBehaftInvoinceNoFeeCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnBehaftInvoinceNoFeeUpload),
                        Value = transportation.CollectOnBehaftInvoinceNoFeeUpload is null ? "0" : transportation.CollectOnBehaftInvoinceNoFeeUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnBehaftFeeUpload),
                        Value = transportation.CollectOnBehaftFeeUpload is null ? "0" : transportation.CollectOnBehaftFeeUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnBehaftFeeCheck),
                        Value = transportation.CollectOnBehaftFeeCheck is null ? "0" : transportation.CollectOnBehaftFeeCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnSupPriceCheck),
                        Value = transportation.CollectOnSupPriceCheck is null ? "0" : transportation.CollectOnSupPriceCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CollectOnSupPriceUpload),
                        Value = transportation.CollectOnSupPriceUpload is null ? "0" : transportation.CollectOnSupPriceUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.TotalPriceAfterTaxCheck),
                        Value = transportation.TotalPriceAfterTaxCheck is null ? "0" : transportation.TotalPriceAfterTaxCheck.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.TotalPriceAfterTaxUpload),
                        Value = transportation.TotalPriceAfterTaxUpload is null ? "0" : transportation.TotalPriceAfterTaxUpload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVat1),
                        Value = transportation.FeeVat1 is null ? "0" : transportation.FeeVat1.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVat2),
                        Value = transportation.FeeVat2 is null ? "0" : transportation.FeeVat2.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVat3),
                        Value = transportation.FeeVat3 is null ? "0" : transportation.FeeVat3.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVat1Upload),
                        Value = transportation.FeeVat1Upload is null ? "0" : transportation.FeeVat1Upload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVat2Upload),
                        Value = transportation.FeeVat2Upload is null ? "0" : transportation.FeeVat2Upload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.FeeVat3Upload),
                        Value = transportation.FeeVat3Upload is null ? "0" : transportation.FeeVat3Upload.ToString(),
                    });
                    //
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee1),
                        Value = transportation.Fee1 is null ? "0" : transportation.Fee1.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee2),
                        Value = transportation.Fee2 is null ? "0" : transportation.Fee2.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee3),
                        Value = transportation.Fee3 is null ? "0" : transportation.Fee3.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee4),
                        Value = transportation.Fee4 is null ? "0" : transportation.Fee4.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee5),
                        Value = transportation.Fee5 is null ? "0" : transportation.Fee5.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee6),
                        Value = transportation.Fee6 is null ? "0" : transportation.Fee6.ToString(),
                    });
                    //
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee1Upload),
                        Value = transportation.Fee1Upload is null ? "0" : transportation.Fee1Upload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee2Upload),
                        Value = transportation.Fee2Upload is null ? "0" : transportation.Fee2Upload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee3Upload),
                        Value = transportation.Fee3Upload is null ? "0" : transportation.Fee3Upload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee4Upload),
                        Value = transportation.Fee4Upload is null ? "0" : transportation.Fee4Upload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee5Upload),
                        Value = transportation.Fee5Upload is null ? "0" : transportation.Fee5Upload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.Fee6Upload),
                        Value = transportation.Fee6Upload is null ? "0" : transportation.Fee6Upload.ToString(),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.CheckFeeHistoryId),
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
                    tran = new Transportation()
                    {
                        ClosingId = transportation.ClosingId,
                        ReceivedCheck = transportation.ReceivedCheck,
                        ClosingDateCheck = transportation.ClosingDateCheck,
                        SealCheck = transportation.SealCheck,
                        BossCheck = transportation.BossCheck,
                        ContainerNoCheck = transportation.ContainerNoCheck,
                        Cont20Check = transportation.Cont20Check,
                        Cont40Check = transportation.Cont40Check,
                        PickupEmptyCheck = transportation.PickupEmptyCheck,
                        PortLoadingCheck = transportation.PortLoadingCheck,
                        ClosingPercentCheck = transportation.ClosingPercentCheck,
                        LiftFeeCheck = transportation.LiftFeeCheck,
                        LandingFeeCheck = transportation.LandingFeeCheck,
                        CollectOnBehaftInvoinceNoFeeCheck = transportation.CollectOnBehaftInvoinceNoFeeCheck,
                        CollectOnBehaftFeeCheck = transportation.CollectOnBehaftFeeCheck,
                        CollectOnSupPriceCheck = transportation.CollectOnSupPriceCheck,
                        TotalPriceAfterTaxCheck = transportation.TotalPriceAfterTaxCheck,
                        FeeVat1 = transportation.FeeVat1,
                        FeeVat2 = transportation.FeeVat2,
                        FeeVat3 = transportation.FeeVat3,
                        FeeVat1Upload = transportation.FeeVat1,
                        FeeVat2Upload = transportation.FeeVat2,
                        FeeVat3Upload = transportation.FeeVat3,
                        Fee1 = transportation.Fee1,
                        Fee2 = transportation.Fee2,
                        Fee3 = transportation.Fee3,
                        Fee4 = transportation.Fee4,
                        Fee5 = transportation.Fee5,
                        Fee6 = transportation.Fee6,
                        Fee1Upload = transportation.Fee1,
                        Fee2Upload = transportation.Fee2,
                        Fee3Upload = transportation.Fee3,
                        Fee4Upload = transportation.Fee4,
                        Fee5Upload = transportation.Fee5,
                        Fee6Upload = transportation.Fee6,
                        CheckFeeHistoryId = CheckFeeHistoryEntity.Id
                    };
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

        public virtual void UpdateQuotation(object arg)
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
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7592 " +
                    $"and BossId eq {coords.BossId} " +
                    $"and ContainerTypeId eq {coords.ContainerTypeId} " +
                    $"and LocationId eq {coords.ReceivedId} " +
                    $"and StartDate le {coords.ClosingDate.Value.ToOdataFormat()} " +
                    $"and PackingId eq {coords.ClosingId}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7592,
                        BossId = coords.BossId,
                        ContainerTypeId = coords.ContainerTypeId,
                        LocationId = coords.ReceivedId,
                        StartDate = coords.ClosingDate,
                        PackingId = coords.ClosingId
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá đóng hàng";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public virtual void CheckQuotationTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.RefName == nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            gridView.BodyContextMenuShow += () =>
            {
                var menus = new List<ContextMenuItem>();
                menus.Clear();
                menus.Add(new ContextMenuItem { Icon = "fas fa-pen", Text = "Cập nhật giá", Click = UpdateQuotation });
                menus.Add(new ContextMenuItem
                {
                    Icon = "fas fa-pen",
                    Text = "Cập nhật phí",
                    MenuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem { Text = "Cập cước tàu", Click = UpdateShipQuotation },
                        new ContextMenuItem { Text = "Cập phí nâng", Click = UpdateLiftQuotation },
                        new ContextMenuItem { Text = "Cập phí hạ", Click = UpdateLadingQuotation },
                    }
                });
                ContextMenu.Instance.MenuItems = menus;
            };
        }

        public void UpdateShipQuotation(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));

            Task.Run(async () =>
            {
                var selected = gridView.LastListViewItem;
                if (selected is null)
                {
                    Toast.Warning("Vui lòng chọn cont cần cập nhật giá!");
                    return;
                }
                var coords = selected.Entity.As<Transportation>();
                if (coords.BrandShipId is null || coords.RouteId is null || coords.ContainerTypeId is null)
                {
                    Toast.Warning("Vui lòng nhập đầy đủ thông tin");
                    return;
                }
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7598 " +
                    $"and RouteId eq {coords.RouteId} " +
                    $"and PackingId eq {coords.BrandShipId} " +
                    $"and ContainerTypeId eq {coords.ContainerTypeId} " +
                    $"and cast(StartDate,Edm.DateTimeOffset) le {coords.ClosingDate.Value.ToOdataFormat()}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7598,
                        RouteId = coords.RouteId,
                        PackingId = coords.BrandShipId,
                        ContainerTypeId = coords.ContainerTypeId,
                        StartDate = coords.ClosingDate,
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá cước tàu";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public void UpdateLiftQuotation(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));

            Task.Run(async () =>
            {
                var selected = gridView.LastListViewItem;
                if (selected is null)
                {
                    Toast.Warning("Vui lòng chọn cont cần cập nhật giá!");
                    return;
                }
                var coords = selected.Entity.As<Transportation>();
                if (coords.PickupEmptyId is null || coords.ContainerTypeId is null)
                {
                    Toast.Warning("Vui lòng nhập đầy đủ thông tin");
                    return;
                }
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7594 " +
                    $"and ContainerTypeId eq {coords.ContainerTypeId} " +
                    $"and LocationId eq {coords.PickupEmptyId} " +
                    $"and StartDate le {coords.ClosingDate.Value.ToOdataFormat()}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7594,
                        LocationId = coords.PickupEmptyId,
                        ContainerTypeId = coords.ContainerTypeId,
                        StartDate = coords.ClosingDate,
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá nâng đóng hàng";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public void UpdateLadingQuotation(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            Task.Run(async () =>
            {
                var selected = gridView.LastListViewItem;
                if (selected is null)
                {
                    Toast.Warning("Vui lòng chọn cont cần cập nhật giá!");
                    return;
                }
                var coords = selected.As<Transportation>();
                if (coords.PortLoadingId is null || coords.ContainerTypeId is null)
                {
                    Toast.Warning("Vui lòng nhập đầy đủ thông tin");
                    return;
                }
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7596 " +
                   $"and ContainerTypeId eq {coords.ContainerTypeId} " +
                   $"and LocationId eq {coords.PortLoadingId} " +
                   $"and StartDate le {coords.ClosingDate.Value.ToOdataFormat()}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7596,
                        LocationId = coords.PortLoadingId,
                        ContainerTypeId = coords.ContainerTypeId,
                        StartDate = coords.ClosingDate,
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá hạ đóng hàng";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }
    }
}
