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
        public CheckFeeEditorBL() : base(nameof(CheckFeeHistory))
        {
            Name = "CheckFee Editor";
        }

        public async Task ExportCheckFee()
        {
            var path = await new Client(nameof(Transportation)).PostAsync<string>(Entity["TransportationList"].As<List<Transportation>>(), "ExportCheckFee");
            Client.Download($"/excel/Download/{path}");
            Toast.Success("Xuất file thành công");
        }

        public async void UpdateColor(Transportation transportation)
        {
            var gridView = this.FindActiveComponent<GridView>(x => x.GuiInfo.FieldName == "TransportationList").FirstOrDefault();
            var listViewItem = gridView.GetItemFocus();
            await BeforePatchUpdateTransportationList(transportation, listViewItem, gridView);
        }

        public async Task BeforePatchUpdateTransportationList(Transportation transportation, ListViewItem listViewItem, GridView gridView)
        {
            if (transportation.Id <= 0)
            {
                PatchUpdate patchUpdate = new PatchUpdate();
                var tran = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$filter={nameof(Transportation.ClosingId)} eq {transportation.ClosingId} and {nameof(Transportation.ContainerNo)} eq '{transportation.ContainerNoCheck}' and (cast({nameof(Transportation.ClosingDate)},Edm.DateTimeOffset) eq cast({transportation.ClosingDateCheck.Value.Date.ToISOFormat()},Edm.DateTimeOffset))");
                var changes = new List<PatchUpdateDetail>();
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
                        Field = nameof(Transportation.ReceivedCheck),
                        Value = transportation.ReceivedCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.ClosingDateCheck),
                        Value = transportation.ClosingDateCheck.Value.ToString("yyyy-MM-dd"),
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.SealCheck),
                        Value = transportation.SealCheck,
                    });
                    changes.Add(new PatchUpdateDetail()
                    {
                        Field = nameof(Transportation.BossCheck),
                        Value = transportation.BossCheck,
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
                        Field = nameof(Transportation.PortLoadingCheck),
                        Value = transportation.PortLoadingCheck,
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
                        Field = nameof(Transportation.CollectOnBehaftInvoinceNoFeeCheck),
                        Value = transportation.CollectOnBehaftInvoinceNoFeeCheck is null ? "0" : transportation.CollectOnBehaftInvoinceNoFeeCheck.ToString(),
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
                        Field = nameof(Transportation.TotalPriceAfterTaxCheck),
                        Value = transportation.TotalPriceAfterTaxCheck is null ? "0" : transportation.TotalPriceAfterTaxCheck.ToString(),
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
                    };
                    listViewItem.Entity.CopyPropFrom(tran);
                    await listViewItem.PatchUpdate();
                    Toast.Warning("Không tìm thấy cont!");
                }
                return;
            }
        }

        public async Task SetTransportation(Transportation transportation)
        {
            var gridView = this.FindActiveComponent<GridView>(x => x.GuiInfo.FieldName == "TransportationList").FirstOrDefault();
            var listViewItem = gridView.GetListViewItems(transportation).FirstOrDefault();
            var tran = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$filter={nameof(Transportation.ClosingId)} eq {transportation.ClosingId} and {nameof(Transportation.ContainerNo)} eq '{transportation.ContainerNoCheck}' and (cast({nameof(Transportation.ClosingDate)},Edm.DateTimeOffset) eq cast({transportation.ClosingDateCheck.Value.Date.ToISOFormat()},Edm.DateTimeOffset))");
            if (tran != null)
            {
                tran.CheckFeeHistoryId = Entity[IdField].As<int>();
                tran.ReceivedCheck = transportation.ReceivedCheck;
                tran.ClosingDateCheck = transportation.ClosingDateCheck;
                tran.SealCheck = transportation.SealCheck;
                tran.ContainerNoCheck = transportation.ContainerNoCheck;
                tran.BossCheck = transportation.BossCheck;
                tran.Cont20Check = transportation.Cont20Check;
                tran.Cont40Check = transportation.Cont40Check;
                tran.PickupEmptyCheck = transportation.PickupEmptyCheck;
                tran.PortLoadingCheck = transportation.PortLoadingCheck;
                tran.LiftFeeCheck = transportation.LiftFeeCheck;
                tran.LandingFeeCheck = transportation.LandingFeeCheck;
                tran.CollectOnBehaftInvoinceNoFeeCheck = transportation.CollectOnBehaftInvoinceNoFeeCheck;
                tran.CollectOnBehaftFeeCheck = transportation.CollectOnBehaftFeeCheck;
                tran.CollectOnSupPriceCheck = transportation.CollectOnSupPriceCheck;
                tran.TotalPriceAfterTaxCheck = transportation.TotalPriceAfterTaxCheck;
                if (tran.IsSeftPayment || tran.IsEmptyLift || tran.PickupEmptyCheck.Contains("kết hợp"))
                {
                    tran.LiftFee = 0;
                }
                if (tran.IsSeftPaymentLand || tran.IsLanding)
                {
                    tran.LandingFee = 0;
                }
                listViewItem.Entity.CopyPropFrom(tran);
                await gridView.LoadMasterData(new object[] { tran });
                listViewItem.UpdateView();
                await new Client(nameof(Transportation)).UpdateAsync<Transportation>(tran);
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
                };
                listViewItem.Entity.CopyPropFrom(tran);
                listViewItem.UpdateView(true);
                Toast.Warning("Không tìm thấy cont!");
            }
        }
    }
}
