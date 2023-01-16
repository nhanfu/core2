using Bridge.Html5;
using Core.API.ViewModels;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;
using TMS.UI.Extensions;

namespace TMS.UI.Business.Freight
{
    public class DriverContainerDetailBL : BaseDriverDetailBL
    {
        public DriverContainerDetailBL() : base()
        {
            Name = "CoordinationTransit Container";
            Title = "Xem chi tiết vận chuyển";
            ShouldUpdateParentForm = true;
            DOMContentLoaded += () =>
            {
                this.SetShow(false, "LabelCut", "LabelNeo");
                this.SetDisabled(CoorDEntity.EmptyContFromId != null, nameof(CoorDEntity.EmptyContFromId));
                var trailerId = this.FindComponentByName<SearchEntry>(nameof(CoordinationDetail.TrailerId));
                var containerNo = this.FindComponentByName<SearchEntry>(nameof(CoordinationDetail.ContainerNo));
                var statusId = this.FindComponentByName<SearchEntry>(nameof(SurchargePayable.StatusText));
                var emptyContFromId = this.FindComponentByName<SearchEntry>("EmptyContFromId");
                _attachments = this.FindComponentByName<ImageUploader>(nameof(CoordinationDetail.Attachments));
                Truck = CoorDEntity.Truck;
                if (CoorDEntity.ContainerMovingTypeId == (int)ContainerMovingTypeEnum.Export)
                {
                    this.FindComponentByName<CellText>("EmptyContText").Element.TextContent = "LẤY RỖNG -";
                }
                DisableSection();
                ChangeButtonStatus(true);
            };
            CurrentGps = JsonConvert.DeserializeObject<List<GPS>>(Window.LocalStorage.GetItem("coordinations")?.ToString())?.LastOrDefault();
        }

        public void ActionSaveStatus()
        {
            _attachments?.OpenFileDialog(Window.Instance["event"].As<Event>());
        }

        private void UpdateViewDriver()
        {
            this.FindComponentByName<Section>("Info")?.UpdateView();
            ParentForm?.UpdateView();
        }

        public async Task Neo()
        {
            await this.OpenTab(EntityEnum.TruckStopHistory.ToString() + base.GetHashCode(), "TruckStopHistory Form", (Func<TabEditor>)(() =>
            {
                var type = Type.GetType("TMS.UI.Business.Freight.TruckStopHistoryFromBL");
                var instance = Activator.CreateInstance(type) as TabEditor;
                instance.Entity = new TruckStopHistory()
                {
                    CoordinationDetailId = CoorDEntity.Id,
                    TruckId = CoorDEntity.TruckId,
                    UserCoordinationId = CoorDEntity.AccountableUserId,
                    StatusId = (int)ApprovalStatusEnum.New,
                    RoleId = CoorDEntity.RoleId,
                };
                instance.Title = "Yêu cầu neo";
                return instance;
            }));
        }

        public async Task AddSurchargePayable()
        {
            await this.OpenTab("SurchargePayable Driver" + base.GetHashCode(), "SurchargePayable Driver", (Func<TabEditor>)(() =>
            {
                var form = Activator.CreateInstance(Type.GetType("TMS.UI.Business.Sale.SurchargePayableDriverBL")) as TabEditor;
                form.Entity = new SurchargePayable()
                {
                    ExchangeRate = 1,
                    CurrencyId = (int)CurrencyEnum.VND,
                    UserId = CoorDEntity.DriverId,
                    Vat = 0,
                    OrderDetailId = CoorDEntity.OrderDetailId,
                    Quantity = 1,
                    CoordinationDetailId = CoorDEntity.Id,
                    InvoiceDate = DateTime.Now,
                    StatusId = (int)ApprovalStatusEnum.New,
                    EntityId = (int)EntityEnum.PaybackPayment,
                    TruckId = CoorDEntity.TruckId,
                };
                form.Title = "Thêm mới phí chi";
                return form;
            }));
        }

        public async Task EditSurchargePayable(SurchargePayable surchargePayable)
        {
            await this.OpenTab("SurchargePayable Driver" + surchargePayable.Id, "SurchargePayable Driver", () =>
            {
                var form = Activator.CreateInstance(Type.GetType("TMS.UI.Business.Sale.SurchargePayableDriverBL")) as TabEditor;
                form.Entity = surchargePayable;
                form.Title = "Xem chi phí";
                return form;
            });
        }

        protected override async Task StatusChanged()
        {
            await UnanchorTrailer();
            await base.StatusChanged();
        }

        protected override void ChangeButtonStatus(bool checkAnchored)
        {
            base.ChangeButtonStatus(checkAnchored);
            if (checkAnchored)
            {
                Task.Run(CheckCutTrailer);
            }
        }

        private void ContainerReturnConfirmed()
        {
            CoorDEntity.Long12 = CurrentGps?.longitude;
            CoorDEntity.Lat12 = CurrentGps?.latitude;
            CoorDEntity.ActualContainerReturnDate = DateTime.Now;
        }

        private void DeliveredConfirmed()
        {
            CoorDEntity.Long11 = CurrentGps?.longitude;
            CoorDEntity.Lat11 = CurrentGps?.latitude;
            CoorDEntity.ActualDeliveredDate = DateTime.Now;
        }

        private void DeliveryPlaceConfirmed()
        {
            CoorDEntity.Long10 = CurrentGps?.longitude;
            CoorDEntity.Lat10 = CurrentGps?.latitude;
            CoorDEntity.ActualDeliveryPlaceDate = DateTime.Now;
        }

        private void GoodsTakenConfirmed()
        {
            CoorDEntity.Long9 = CurrentGps?.longitude;
            CoorDEntity.Lat9 = CurrentGps?.latitude;
            CoorDEntity.ActualGoodsTakenDate = DateTime.Now;
        }

        private void GoodPlaceConfirmed()
        {
            CoorDEntity.Long8 = CurrentGps?.longitude;
            CoorDEntity.Lat8 = CurrentGps?.latitude;
            CoorDEntity.ActualGoodsPlaceDate = DateTime.Now;
        }

        private void ContainerTakenConfirmed()
        {
            CoorDEntity.Long7 = CurrentGps?.longitude;
            CoorDEntity.Lat7 = CurrentGps?.latitude;
            CoorDEntity.ActualContainerTakenDate = DateTime.Now;
        }

        private void ContainerPlaceConfirmed()
        {
            CoorDEntity.Long6 = CurrentGps?.longitude;
            CoorDEntity.Lat6 = CurrentGps?.latitude;
            CoorDEntity.ActualContainerPlaceDate = DateTime.Now;
        }

        private void ProcessStart()
        {
            if (CoorDEntity.ContainerMovingTypeId == (int)ContainerMovingTypeEnum.Import)
            {
                StartImportConfirmed();
            }
            else if (CoorDEntity.ContainerMovingTypeId == (int)ContainerMovingTypeEnum.Export)
            {
                StartExportConfirmed();
            }
            else
            {
                OtherStartConfirmed();
            }
            CoorDEntity.ActualStartDate = DateTime.Now;
        }

        private async Task ConfirmPosition(Terminal place, Action positionAction)
        {
            if (place is null)
            {
                Toast.Warning("Chưa xác định địa điểm");
                return;
            }
            var isValidGPS = await IsValidGps(place);
            if (isValidGPS)
            {
                positionAction?.Invoke();
                return;
            }
            _confirmPosition = new ConfirmDialog
            {
                Content = "Vị trí giao nhận chưa đúng quy định. Bạn vẫn muốn tiếp tục?"
            };
            _confirmPosition.Render();
            _confirmPosition.YesConfirmed = positionAction;
        }

        private void OtherStartConfirmed()
        {
            CoorDEntity.Long8 = CurrentGps?.longitude;
            CoorDEntity.Lat8 = CurrentGps?.latitude;
        }

        private void StartExportConfirmed()
        {
            CoorDEntity.Long6 = CurrentGps?.longitude;
            CoorDEntity.Lat6 = CurrentGps?.latitude;
        }

        private void StartImportConfirmed()
        {
            CoorDEntity.Long8 = CurrentGps?.longitude;
            CoorDEntity.Lat8 = CurrentGps?.latitude;
        }

        private Task<bool> IsValidGps(Terminal terminal)
        {
            var tcs = new TaskCompletionSource<bool>();
            /*@
            * if (typeof(cordova) == 'undefined') {
            *     tcs.setResult(true);
            *     return tcs.task;
            * }
            */
            Task.Run(async () =>
            {
                var settingGps = await new Client(nameof(MasterData))
                    .FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Name eq 'IsCheckGps'");
                if (settingGps is null || settingGps.Description.TryParseInt() == 0)
                {
                    tcs.SetResult(true);
                    return;
                }
                var gps = JsonConvert.DeserializeObject<List<GPS>>(Window.LocalStorage.GetItem("coordinations").ToString());
                if (gps is null)
                {
                    Toast.Warning("Vui lòng chấp nhận vị trí!");
                    tcs.SetResult(false);
                    return;
                }
                var currentGps = gps.LastOrDefault();
                var distance = TrackingTruck.GetDistanceFromLatLonInKm(currentGps.latitude, currentGps.longitude, terminal.Long, terminal.Lat);
                if (distance * 1000 < terminal.Radius)
                {
                    tcs.SetResult(true);
                    return;
                }
                tcs.SetResult(false);
            });
            return tcs.Task;
        }

        public void ContainerNo(CoordinationDetail coordinationDetail)
        {
            coordinationDetail.ContainerNo = coordinationDetail.ContainerNo?.ToUpper();
            coordinationDetail.SealNumbersCont = coordinationDetail.SealNumbersCont?.ToUpper();
            this.FindComponentByName<Textbox>(nameof(coordinationDetail.ContainerNo))?.UpdateView();
            this.FindComponentByName<Textbox>(nameof(coordinationDetail.SealNumbersCont))?.UpdateView();
        }

        public async Task Parking(CoordinationDetail coordinationDetail)
        {
            await this.OpenTab(EntityEnum.CoordinationDetail.ToString() + GetHashCode(), "TruckStopHistory Parking", () =>
            {
                var type = Type.GetType("TMS.UI.Business.Freight.TruckStopHistoryParkingBL");
                var instance = Activator.CreateInstance(type) as TabEditor;
                instance.Entity = coordinationDetail;
                return instance;
            });
        }

        private async Task CheckCutTrailer()
        {
            this.SetShow(CoorDEntity.TrailerId != null, "btnCut");
            if (CoorDEntity.TrailerId is null)
            {
                this.SetShow(false, "LabelCut");
                return;
            }
            var checkTrailerCut = await new Client(nameof(TruckStopHistory)).FirstOrDefaultAsync<TruckStopHistory>(
                $"?$filter=Active eq true and TruckId eq {CoorDEntity.TrailerId} and StopTypeId eq {(int)StopType.CutTrailer}");
            var isTrailerCut = checkTrailerCut != null;
            this.SetShow(isTrailerCut, "LabelCut");
            this.SetShow(!isTrailerCut, "btnCut");
        }

        protected async Task UnanchorTrailer()
        {
            if (CoorDEntity.TrailerId is null)
            {
                return;
            }
            var truckStopHistoryOdata = await new Client(nameof(TruckStopHistory)).GetList<TruckStopHistory>(
                $"?$filter=Active eq true and TruckId eq {CoorDEntity.TrailerId} and StopTypeId eq {(int)StopType.CutTrailer} " +
                $"and StatusId eq {(int)ApprovalStatusEnum.Approved} and IsContinues eq false");
            var truckStopHistorys = truckStopHistoryOdata.Value.ToList();
            if(truckStopHistorys.Nothing())
            {
                return;
            }    
            truckStopHistorys.ForEach(x =>
            {
                x.IsContinues = true;
                truckStopHistorys.ClearReferences();
            });
            await new Client(nameof(TruckStopHistory)).BulkUpdateAsync(truckStopHistorys);
        }

        public async Task CutMooc()
        {
            var truckStopHistory = new TruckStopHistory()
            {
                CoordinationDetailId = CoorDEntity.Id,
                TruckId = CoorDEntity.TruckId,
                UserCoordinationId = CoorDEntity.AccountableUserId,
                StopTypeId = (int)StopType.CutTrailer,
                StatusId = (int)ApprovalStatusEnum.New,
                RoleId = CoorDEntity.RoleId,
            };
            var entity = await new Client(nameof(TruckStopHistory)).CreateAsync<TruckStopHistory>(truckStopHistory);
            var res = await new Client(nameof(TruckStopHistory)).PostAsync<object>(entity, "RequestApprove");
            ProcessEnumMessage(res);
        }
    }
}
