using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Extensions;
using TMS.API.Models;

namespace TMS.UI.Business.Freight
{
    public class CoordinationContainerBL : BaseCoordinationBL
    {
        private int timeOut1;
        private int timeOut2;

        public CoordinationContainerBL() : base()
        {
            Name = "CoordinationContainer";
        }

        public void SelectedCoordinationDetail()
        {
            Window.ClearTimeout(timeOut1);
            timeOut1 = Window.SetTimeout(async () =>
            {
                base.ResizeHandler();
                if (_currentListView is null)
                {
                    return;
                }
                _currentListView.BodyContextMenuShow += () =>
                {
                    ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem { Icon = "fas fa-car-side", Text = "Xếp lịch", Click = OrganizeTruck },
                        new ContextMenuItem { Icon = "fas fa-truck-moving", Text = "Hợp cont", Click = CombineTruck },
                    };
                };
                await ChangeTextGroup();
            }, 1000);
        }

        public void SelectedCoordinationDetailNonSend()
        {
            Window.ClearTimeout(timeOut2);
            timeOut2 = Window.SetTimeout((Action)(async () =>
            {
                if (_currentListView is null)
                {
                    return;
                }
                _currentListView.BodyContextMenuShow += () =>
                {
                    ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem { Icon = "far fa-envelope", Text = "Gửi tài xế", Click = SendDriver },
                        new ContextMenuItem { Icon = "fas fa-not-equal", Text = "Tách cont", Click = DeAssemblyContainer },
                        new ContextMenuItem { Icon = "fas fa-truck-moving", Text = "Hợp cont", Click = CombineTruck },
                    };
                };
                await ChangeTextGroup();
            }), 1000);
        }

        public async Task EditCoordinationDetail(CoordinationDetail entity)
        {
            await base.OpenCoordinationDetail(entity, "Edit Coordination Detail", "CoordinationDetailBL");
        }

        public async Task CheckRomooc(CoordinationDetail coordinationDetail, Truck trailer)
        {
            if (coordinationDetail.TruckTypeId is null || coordinationDetail.TrailerId is null)
            {
                return;
            }
            var containerType = new Client(nameof(MasterData)).GetAsync<MasterData>(coordinationDetail.TruckTypeId ?? 0);
            var trailerType = new Client(nameof(Truck)).GetAsync<MasterData>(trailer.TruckTypeId ?? 0);
            await Task.WhenAll(containerType, trailerType);
            if (containerType.Result.Name.Contains("40") && trailerType.Result.Name.Contains("20"))
            {
                Toast.Warning("Container vượt quá kích cỡ của rơ-mooc!");
            }
        }

        public void DeAssemblyContainer(object arg)
        {
            var selected = _currentListView.GetSelectedRows();
            if (selected.Nothing())
            {
                Toast.Warning("Vui lòng chọn chuyến xe tách cont!");
                return;
            }
            var coords = selected.Cast<CoordinationDetail>().ToList();
            if (coords.Where(x => x.CoordinationId is null).ToList().Any())
            {
                Toast.Warning("Đảm bảo những chuyến xe đã được hợp cont!");
                return;
            }
            if (coords.Where(x => x.TruckId == null || x.DriverId == null).ToList().Any())
            {
                Toast.Warning("Đảm bảo rằng tất cả chuyến xe đã có xe và tài xế!");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc muốn tách cont ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var coordinationDetailOdata = await new Client(nameof(CoordinationDetail)).GetList<CoordinationDetail>($"?$filter=Active eq true and CoordinationId eq {coords.FirstOrDefault().CoordinationId}");
                var coordinationDetails = coordinationDetailOdata?.Value.ToList();
                coordinationDetails.ForEach(x =>
                {
                    x.CoordinationId = null;
                    x.CoordinationTypeId = null;
                    x.CoordinationTypeName = null;
                    x.ClearReferences();
                });
                await Client.BulkUpdateAsync(coordinationDetails);
                Toast.Success("Tách cont thành công");
                await _currentListView.ReloadData();
            };
        }

        public async Task ChangeRoMooc(CoordinationDetail coordinationDetail, Truck trailer)
        {
            if (trailer is null)
            {
                return;
            }

            await CheckRomooc(coordinationDetail, trailer);
            coordinationDetail.TrailerNo = trailer.TruckPlate;
            await _currentListView.AddOrUpdateRow(coordinationDetail);
        }

        public async Task ContainerNo(CoordinationDetail coordinationDetail)
        {
            coordinationDetail.ContainerNo = coordinationDetail.ContainerNo?.ToUpper();
            coordinationDetail.SealNumbersCont = coordinationDetail.SealNumbersCont?.ToUpper();
            await _currentListView.AddOrUpdateRow(coordinationDetail);
        }

        public void TruckChange(CoordinationDetail coordinationDetail, Truck truck)
        {
            if (coordinationDetail is null)
            {
                return;
            }
            if (coordinationDetail.TruckId is null || truck is null)
            {
                coordinationDetail.TrailerId = null;
                coordinationDetail.DriverId = null;
                UpdateRow(coordinationDetail);
                return;
            }
            if (truck.TrailerId != null)
            {
                coordinationDetail.TrailerId = truck.TrailerId;
            }

            if (truck.DriverId != null)
            {
                coordinationDetail.DriverId = truck.DriverId;
            }
            UpdateRow(coordinationDetail);
            UpdateUnitedCoors(coordinationDetail);
        }

        private void UpdateRow(CoordinationDetail coordinationDetail)
        {
            if (_currentListView is null || coordinationDetail is null)
            {
                return;
            }
            var rowSection = _currentListView.GetListViewItems(coordinationDetail).FirstOrDefault();
            rowSection.UpdateView(nameof(CoordinationDetail.TrailerId), nameof(CoordinationDetail.DriverId));
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

        protected override TaskNotification DeSendNotification(CoordinationDetail coordinationDetail, int? driver, Terminal from, Terminal to, Vendor customer)
        {
            return new TaskNotification
            {
                Title = $"Thu lệnh kh:{customer.CompanyLocalShortName}",
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

        protected override TaskNotification DeSendCancelNotification(CoordinationDetail coordinationDetail, int? driver, Terminal from, Terminal to, Vendor customer)
        {
            return new TaskNotification
            {
                Title = $"Hủy lệnh kh:{customer.CompanyLocalShortName}",
                Description = $"Từ: {from.ShortName} đến: {to.ShortName}",
                EntityId = (int)EntityEnum.CoordinationDetail,
                RecordId = null,
                RoleId = null,
                Attachment = "fas fa-redo-alt",
                AssignedId = driver,
                Deadline = DateTime.Now,
                StatusId = 339,
                RemindBefore = 540
            };
        }

        public void CalcOppositeRouteFee(CoordinationDetail coorDetail)
        {
            var action = coorDetail.IsOppositeRoute ? "thêm mới" : "xóa";
            var message = $"Thao tác này sẽ {action} phụ phí thu hoặc chi phí theo quy định của báo giá.<br />Bạn có chắc chắn muốn tiếp tục?";
            ConfirmDialog.RenderConfirm(message,
            async () =>
            {
                var surchargeTypeTask = new Client(nameof(SurchargeType)).FirstOrDefaultAsync<SurchargeType>($"?$filter=Active eq true and Name eq 'Opposite Route'");
                var quoTask = QuotationExt.LoadQuotation(new List<int?> { coorDetail.QuotationId, coorDetail.VendorQuoId }, loadGlobalSurcharge: false);
                var surchargeTask = new Client(nameof(Surcharge)).GetRawList<Surcharge>($"?$filter=CoordinationDetailId eq {coorDetail.Id}");
                var expenseTask = new Client(nameof(SurchargePayable)).GetRawList<SurchargePayable>($"?$filter=CoordinationDetailId eq {coorDetail.Id}");
                await Task.WhenAll(surchargeTypeTask, quoTask, surchargeTask, expenseTask);
                coorDetail.Quotation = quoTask.Result.FirstOrDefault(x => x.Id == coorDetail.QuotationId);
                coorDetail.VendorQuo = quoTask.Result.FirstOrDefault(x => x.Id == coorDetail.VendorQuoId);
                coorDetail.Surcharge = coorDetail.Surcharge ?? surchargeTask.Result;
                coorDetail.SurchargePayable = coorDetail.SurchargePayable ?? expenseTask.Result;
                var surchargeType = surchargeTypeTask.Result;

                if (coorDetail.IsOppositeRoute)
                {
                    await AddSurcharge(coorDetail, surchargeType);
                    await AddExpense(coorDetail, surchargeType);
                }
                else
                {
                    await RemoveSurcharge(coorDetail, surchargeType);
                }
                Toast.Success("Thao tác thành công.\nVui lòng kiểm tra lại chi phí.");
            }, () =>
            {
                coorDetail.IsOppositeRoute = !coorDetail.IsOppositeRoute;
                _currentListView.UpdateRow(coorDetail, nameof(coorDetail.IsOppositeRoute));
            });
        }

        private async Task AddSurcharge(CoordinationDetail coorDetail, SurchargeType surchargeType)
        {
            if (coorDetail.Surcharge.Any(x => x.SurchargeTypeId == surchargeType.Id && x.StatusId == (int)ApprovalStatusEnum.Approved) || coorDetail.VendorId == Utils.SelfVendorId)
            {
                return;
            }
            var sur = new Surcharge()
            {
                SurchargeTypeId = surchargeType.Id,
                Quantity = 1,
                SupplierId = coorDetail.CustomerId,
                IsReceivable = true,
                CoordinationDetailId = coorDetail.Id,
                OrderDetailId = coorDetail.OrderDetailId,
                CollectOnBehalf = surchargeType.CollectOnBehalf,
                CostCenterId = CostCenterId,
            };
            if (coorDetail.Quotation != null)
            {
                await coorDetail.Quotation.CalcSurcharge(sur);
            }
            if (sur.PriceBeforeTax > 0)
            {
                sur.StatusId = (int)ApprovalStatusEnum.Approved;
            }
            coorDetail.Surcharge.Add(sur);
            await new Client(nameof(Surcharge)).CreateAsync<Surcharge>(sur);
        }

        private async Task AddExpense(CoordinationDetail coorDetail, SurchargeType surchargeType)
        {
            if (coorDetail.SurchargePayable.Any(x => x.SurchargeTypeId == surchargeType.Id))
            {
                return;
            }
            var expense = new SurchargePayable()
            {
                SurchargeTypeId = surchargeType.Id,
                Quantity = 1,
                SupplierId = coorDetail.VendorId,
                IsPayable = true,
                CoordinationDetailId = coorDetail.Id,
                OrderDetailId = coorDetail.OrderDetailId,
                CollectOnBehalf = surchargeType.CollectOnBehalf,
                CostCenterId = CostCenterId,
            };
            if (coorDetail.Quotation != null)
            {
                await coorDetail.VendorQuo.CalcExpense(expense);
            }
            if (expense.PriceBeforeTax > 0)
            {
                expense.StatusId = (int)ApprovalStatusEnum.Approved;
            }
            coorDetail.SurchargePayable.Add(expense);
            await new Client(nameof(SurchargePayable)).CreateAsync<SurchargePayable>(expense);
        }

        private async Task RemoveSurcharge(CoordinationDetail coorDetail, SurchargeType surchargeType)
        {
            var surcharge = coorDetail.Surcharge.FirstOrDefault(x => x.SurchargeTypeId == surchargeType.Id && x.StatusId == (int)ApprovalStatusEnum.Approved);
            var expense = coorDetail.SurchargePayable.FirstOrDefault(x => x.SurchargeTypeId == surchargeType.Id && x.StatusId == (int)ApprovalStatusEnum.Approved);
            if (surcharge != null)
            {
                coorDetail.Surcharge.Remove(surcharge);
                await new Client(nameof(Surcharge)).HardDeleteAsync(surcharge.Id);
            }
            if (expense != null)
            {
                coorDetail.SurchargePayable.Remove(expense);
                await new Client(nameof(SurchargePayable)).HardDeleteAsync(expense.Id);
            }
        }
    }
}
