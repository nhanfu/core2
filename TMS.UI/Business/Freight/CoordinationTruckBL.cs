using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Freight
{
    public class CoordinationTruckBL : BaseCoordinationBL
    {
        private int timeOut1;
        private int timeOut2;

        public CoordinationTruckBL() : base()
        {
            Name = "CoordinationTruck";
        }

        public void SelectedCoordinationDetailTruck()
        {
            Window.ClearTimeout(timeOut1);
            timeOut1 = Window.SetTimeout(async () =>
            {
                base.ResizeHandler();
                _currentListView.BodyContextMenuShow = () =>
                {
                    ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem { Icon = "fas fa-car-side", Text = "Xếp lịch", Click = OrganizeTruck },
                        new ContextMenuItem { Icon = "fas fa-truck-moving", Text = "Hợp hàng", Click = CombineTruck },
                    };
                };
                await ChangeTextGroup();
            }, 1000);
        }

        public void SelectedCoordinationDetailDelivered()
        {
            Window.ClearTimeout(timeOut2);
            timeOut2 = Window.SetTimeout(async () =>
            {
                _currentListView.BodyContextMenuShow = () =>
                {
                    ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem { Icon = "fas fa-truck-moving", Text = "Gửi tài xế", Click = SendDriver },
                        new ContextMenuItem { Icon = "fas fa-car-side", Text = "Tách hàng", Click = DeAssemblyTruck },
                        new ContextMenuItem { Icon = "fas fa-truck-moving", Text = "Hợp hàng", Click = CombineTruck  },
                    };
                };
                await ChangeTextGroup();
            }, 1000);
        }

        public void DeAssemblyTruck(object arg)
        {
            Task.Run(() =>
            {
                var orderDetailGrid = this.FindActiveComponent<ListView>();
                var selected = orderDetailGrid.FirstOrDefault().GetSelectedRows();
                if (selected.Nothing())
                {
                    Toast.Warning("Vui lòng chọn chuyến xe tách hàng!");
                    return;
                }
                var coords = selected.Cast<CoordinationDetail>().ToList();
                if (coords.Where(x => x.CoordinationId is null).ToList().Any())
                {
                    Toast.Warning("Đảm bảo những chuyến xe đã được hợp hàng!");
                    return;
                }
                if (coords.Where(x => x.TruckId == null || x.DriverId == null).ToList().Any())
                {
                    Toast.Warning("Đảm bảo rằng tất cả chuyến xe đã có xe và tài xế!");
                    return;
                }
                var confirm = new ConfirmDialog
                {
                    Content = "Bạn có chắc muốn tách chuyến xe ?",
                };
                confirm.Render();
                confirm.YesConfirmed += async () =>
                {
                    var coordinationDetailOdata = await new Client(nameof(CoordinationDetail)).GetList<CoordinationDetail>($"?$filter=Active eq true and CoordinationId eq {coords.FirstOrDefault().CoordinationId}");
                    var coordinationDetails = coordinationDetailOdata?.Value.ToList();
                    coordinationDetails.ForEach(async x =>
                    {
                        var coordination = await new Client(nameof(Coordination)).GetAsync<Coordination>(x.CoordinationId ?? 0);
                        coordination.TotalVolume -= x.TotalCbm;
                        coordination.TotalWeight -= x.TotalWeight;
                        await new Client(nameof(Coordination)).UpdateAsync<Coordination>(coordination);
                    });
                    coordinationDetails.ForEach(x =>
                    {
                        x.CoordinationId = null;
                        x.CoordinationTypeId = null;
                        x.CoordinationTypeName = null;
                        x.ClearReferences();
                    });
                    await Client.BulkUpdateAsync(coords);
                    Toast.Success("Tách hàng thành công");
                    await orderDetailGrid.FirstOrDefault().ReloadData();
                };
            });
        }

        protected override TaskNotification DeSendNotification(CoordinationDetail coordinationDetail, int? driver, Terminal from, Terminal to, Vendor customer)
        {
            return new TaskNotification
            {
                Title = $"Thu lệnh kh:{customer.CompanyLocalShortName}",
                Description = $"Từ: {from.ShortName} đến: {to.ShortName}",
                EntityId = (int)EntityEnum.CoordinationDetail,
                RecordId = null,
                RoleId = 2,
                Attachment = "fas fa-redo-alt",
                AssignedId = driver,
                Deadline = DateTime.Now,
                StatusId = 339,
                RemindBefore = 540
            };
        }

        protected override TaskNotification DeSendCancelNotification(CoordinationDetail coordinationDetail, int? driver, Terminal from, Terminal to, Vendor customer)
        {
            return new TaskNotification
            {
                Title = $"Hủy lệnh kh:{customer.CompanyLocalShortName}",
                Description = $"Từ: {from.ShortName} đến: {to.ShortName}",
                EntityId = (int)EntityEnum.CoordinationDetail,
                RecordId = null,
                Attachment = "fas fa-redo-alt",
                AssignedId = driver,
                Deadline = DateTime.Now,
                StatusId = 339,
                RemindBefore = 540
            };
        }

        protected override TaskNotification SendNotification(CoordinationDetail coordinationDetail, int? driver, Terminal from, Terminal to, Vendor customer)
        {
            return new TaskNotification
            {
                Title = $"Lệnh mới kh:{customer.CompanyLocalShortName}",
                Description = $"Từ: {from.ShortName} đến: {to.ShortName}",
                EntityId = (int)EntityEnum.CoordinationDetail,
                RecordId = coordinationDetail.Id,
                Attachment = "fas fa-car-side",
                AssignedId = driver,
                Deadline = DateTime.Now,
                StatusId = 339,
                RemindBefore = 540
            };
        }

        public async Task EditCoordinationDetail(CoordinationDetail entity)
        {
            await OpenCoordinationDetail(entity, "Edit Coordination Truck Detail", "CoordinationTruckDetailBL");
        }

        public async Task TruckChange(CoordinationDetail coordinationDetail, Truck truck)
        {
            if (coordinationDetail is null)
            {
                return;
            }
            if (coordinationDetail.TruckId is null || truck is null)
            {
                coordinationDetail.DriverId = null;
                coordinationDetail.TruckCbm = 0;
                coordinationDetail.TruckWeight = 0;
                coordinationDetail.BoxTypeId = null;
                await _currentListView.AddOrUpdateRow(coordinationDetail);
                return;
            }
            coordinationDetail.DriverId = truck.DriverId;
            coordinationDetail.TruckCbm = truck.MaxCBM;
            coordinationDetail.TruckWeight = truck.MaxWeight;
            coordinationDetail.BoxTypeId = truck.BoxTypeId;
            await _currentListView.AddOrUpdateRow(coordinationDetail);
            UpdateUnitedCoors(coordinationDetail);
        }

        public override void ChangeIn()
        {
            this.SetShow(false, "btnComposite", "btnDeSendDriver", "btnDeAssembly", "btnAssembly", "btnSendDriver", "btnFilter", "btnDeCom", "btnSendVendor", "btnDeSendVendor", "btnDeSendOrder", "btnRetrieval", "btnCancelCoordinaton");
            switch (_currentListView.Name)
            {
                case "CoordinationDetail":
                case "CoordinationDetail_Mobile":
                    this.SetShow(true, "btnComposite", "btnAssembly", "btnDeCom", "btnDeSendOrder", "btnRetrieval", "btnCancelCoordinaton");
                    break;
                case "CoordinationDetailNonSend":
                case "CoordinationDetailNonSend_Mobile":
                    this.SetShow(true, "btnDeSendDriver", "btnSendVendor", "btnDeAssembly", "btnAssembly", "btnSendDriver", "btnDeCom", "btnCancelCoordinaton");
                    break;
                case "CoordinationDetailIsSend":
                case "CoordinationDetailIsSend_Mobile":
                    this.SetShow(true, "btnDeSendDriver", "btnDeSendVendor", "btnCancelCoordinaton");
                    break;
            }
        }

        public void SplitTrip()
        {
            var selected = _currentListView.GetSelectedRows();
            if (selected.Nothing())
            {
                Toast.Warning("Vui lòng chọn chuyến xe cần xế lịch!");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc muốn tách đơn hàng ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var coords = selected.Cast<CoordinationDetail>().ToList();
                await this.OpenPopup(featureName: "SplitTrip",
                    factory: () =>
                    {
                        var type = Type.GetType("TMS.UI.Business.Freight.SplitTripBL");
                        var instance = Activator.CreateInstance(type) as PopupEditor;
                        instance.Entity = coords.FirstOrDefault();
                        return instance;
                    });
            };
        }
    }
}