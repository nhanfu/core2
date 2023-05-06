using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class TransportationRequestDetailsBL : PopupEditor
    {
        public Transportation transportationEntity => Entity as Transportation;
        public List<string> propNameChanges = new List<string>();
        public TransportationRequestDetailsBL() : base(nameof(TransportationRequest))
        {
            Name = "Transportation Request Details";
        }

        private int awaiter;

        public async Task SetGridView()
        {
            GridView grid;
            this.SetShow(false, "btnCreate", "btnSend");
            if (Parent.Name == "Transportation Return Plan List")
            {
                this.SetShow(false, "TransportationRequestDetails2", "Transportation2");
                grid = this.FindComponentByName<GridView>("TransportationRequestDetails1");
            }
            else if (Parent.Name == "ReturnPlan List")
            {
                this.SetShow(false, "TransportationRequestDetails1", "Transportation1");
                grid = this.FindComponentByName<GridView>("TransportationRequestDetails2");
            }
            else
            {
                grid = this.FindComponentByName<GridView>(nameof(TransportationRequestDetails));
            }
            var listViewItems = grid.RowData.Data.Cast<TransportationRequestDetails>().ToList();
            var check = await new Client(nameof(TransportationRequestDetails)).FirstOrDefaultAsync<TransportationRequestDetails>($"?$orderby=Id desc&$filter=Active eq true and TransportationId eq {transportationEntity.Id}");
            if (check != null && check.StatusId == (int)ApprovalStatusEnum.New)
            {
                this.SetShow(true, "btnSend");
            }
            else
            {
                this.SetShow(true, "btnCreate");
            }
            listViewItems.ForEach(x =>
            {
                var listViewItem = grid.GetListViewItems(x).FirstOrDefault();
                if (listViewItem is null)
                {
                    return;
                }
                listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
                if (x.StatusId != (int)ApprovalStatusEnum.New)
                {
                    listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                }
            });
            var bl = Parent as TransportationListBL;
            if (Parent.Name == "Transportation List Accountant" || Parent.Name == "List Ship Book" || bl.getCheckView())
            {
                listViewItems.ForEach(x =>
                {
                    var listViewItem = grid.GetListViewItems(x).FirstOrDefault();
                    if (listViewItem is null)
                    {
                        return;
                    }
                    listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
                    listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                });
                this.SetShow(false, "btnCreate", "btnSend");
                Window.ClearTimeout(awaiter);
                awaiter = Window.SetTimeout(() =>
                {
                    bl.setFalseCheckView();
                    if (Parent.Name == "ReturnPlan List")
                    {
                        if (bl.getCheckView() == false)
                        {
                            if (check != null && check.StatusId == (int)ApprovalStatusEnum.New)
                            {
                                this.SetShow(true, "btnSend");
                            }
                            else
                            {
                                this.SetShow(true, "btnCreate");
                            }
                        }
                    }
                }, 500);
            }
        }

        private void CompareChanges(object change, object cutting)
        {
            if (change != null)
            {
                var listItem = change.GetType().GetProperties();
                GridView grid;
                if (Parent.Name == "Transportation Return Plan List")
                {
                    grid = this.FindComponentByName<GridView>("TransportationRequestDetails1");
                }
                else if (Parent.Name == "ReturnPlan List")
                {
                    grid = this.FindComponentByName<GridView>("TransportationRequestDetails2");
                }
                else
                {
                    grid = this.FindComponentByName<GridView>(nameof(TransportationRequestDetails));
                }
                var listViewItem = grid.GetListViewItems(change).FirstOrDefault();
                GridView gridViewCutting;
                if (Parent.Name == "Transportation Return Plan List")
                {
                    gridViewCutting = this.FindComponentByName<GridView>("Transportation1");
                }
                else if (Parent.Name == "ReturnPlan List")
                {
                    gridViewCutting = this.FindComponentByName<GridView>("Transportation2");
                }
                else
                {
                    gridViewCutting = this.FindComponentByName<GridView>(nameof(Transportation));
                }
                var listViewItemCutting = gridViewCutting.GetListViewItems(cutting).FirstOrDefault();
                listViewItem.FilterChildren(x => true).ForEach(x => x.Element.RemoveClass("text-warning-2"));
                listViewItemCutting.FilterChildren(x => true).ForEach(x => x.Element.RemoveClass("text-warning-2"));
                foreach (var item in listItem)
                {
                    var a1 = change[item.Name];
                    var a2 = cutting[item.Name];
                    if (a1 == null && a2 == null)
                    {
                        continue;
                    }
                    if (((a1 != null && a2 == null) || (a1 == null && a2 != null) || (a1 != null && a2 != null) && (a1.ToString() != a2.ToString()))
                        && item.Name != "Id"
                        && item.Name != "InsertedDate"
                        && item.Name != "InsertedBy"
                        && item.Name != "UpdatedDate"
                        && item.Name != "UpdatedBy"
                        && item.Name != "TransportationId"
                        && item.Name != "TransportationRequestId"
                        && item.Name != "StatusId"
                        && item.Name != "Reason"
                        && item.Name != "ReasonReject")
                    {
                        listViewItem.FilterChildren(x => x.Name == item.Name).FirstOrDefault()?.Element?.AddClass("text-warning-2");
                        listViewItemCutting.FilterChildren(x => x.Name == item.Name).FirstOrDefault()?.Element?.AddClass("text-warning-2");
                        propNameChanges.Add(item.Name);
                    }
                }
            }
        }

        public void SelectedCompare(TransportationRequestDetails transportationRequestDetails)
        {
            CompareChanges(transportationRequestDetails, transportationEntity);
        }

        public async Task CreateRequestChange()
        {
            GridView grid;
            if (Parent.Name == "Transportation Return Plan List")
            {
                grid = this.FindComponentByName<GridView>("TransportationRequestDetails1");
            }
            else if (Parent.Name == "ReturnPlan List")
            {
                grid = this.FindComponentByName<GridView>("TransportationRequestDetails2");
            }
            else
            {
                grid = this.FindComponentByName<GridView>(nameof(TransportationRequestDetails));
            }
            var checkExist = await new Client(nameof(TransportationRequestDetails)).FirstOrDefaultAsync<TransportationRequestDetails>($"?$orderby=Id desc&$filter=TransportationId eq {transportationEntity.Id} and StatusId eq {(int)ApprovalStatusEnum.New}");
            if (checkExist != null)
            {
                Toast.Warning("Có yêu cầu thay đổi đã được tạo trước đó vui lòng thay đổi ở dưới là gửi đi");
                return;
            }
            var requestChange = new TransportationRequestDetails();
            requestChange.CopyPropFrom(transportationEntity);
            requestChange.Id = 0;
            requestChange.TransportationId = transportationEntity.Id;
            requestChange.InsertedBy = Client.Token.UserId;
            requestChange.InsertedDate = DateTime.Now;
            requestChange.StatusId = (int)ApprovalStatusEnum.New;
            var rs = await new Client(nameof(TransportationRequestDetails)).CreateAsync<TransportationRequestDetails>(requestChange);
            if(rs != null) 
            {
                await grid.ApplyFilter();
                this.SetShow(false, "btnCreate");
                this.SetShow(true, "btnSend");
                SetGridView();
            }
        }

        public void SendRequestApprove()
        {
            GridView grid;
            if (Parent.Name == "Transportation Return Plan List")
            {
                grid = this.FindComponentByName<GridView>("TransportationRequestDetails1");
            }
            else if (Parent.Name == "ReturnPlan List")
            {
                grid = this.FindComponentByName<GridView>("TransportationRequestDetails2");
            }
            else
            {
                grid = this.FindComponentByName<GridView>(nameof(TransportationRequestDetails));
            }
            var listViewItem = grid.RowData.Data.Cast<TransportationRequestDetails>().OrderByDescending(x => x.Id).FirstOrDefault();
            var selected = grid.GetSelectedRows().Cast<TransportationRequestDetails>().OrderByDescending(x => x.Id).FirstOrDefault();
            if (listViewItem.Id != selected.Id)
            {
                Toast.Warning("Bạn chưa chọn thông tin thay đổi có thể gửi");
                return;
            }
            if (transportationEntity.IsLocked)
            {
                var confirm = new ConfirmDialog
                {
                    NeedAnswer = true,
                    ComType = nameof(Textbox),
                    Content = $"Bạn có muốn gửi yêu cầu mở khóa không?<br />" +
                        "Hãy nhập lý do",
                };
                confirm.Render();
                confirm.YesConfirmed += async () =>
                {
                    listViewItem.Reason = confirm.Textbox?.Text;
                    Toast.Success("Đã gửi yêu cầu thành công");
                    this.Dispose();
                    await new Client(nameof(Transportation)).PostAsync<bool>(listViewItem, "RequestUnLockAll");
                };
            }
            else
            {
                if (propNameChanges.Any(x => x == nameof(TransportationRequestDetails.ShipPrice) ||
                x == nameof(TransportationRequestDetails.PolicyId) ||
                x == nameof(TransportationRequestDetails.RouteId) ||
                x == nameof(TransportationRequestDetails.BrandShipId) ||
                x == nameof(TransportationRequestDetails.LineId) ||
                x == nameof(TransportationRequestDetails.ShipId) ||
                x == nameof(TransportationRequestDetails.Trip) ||
                x == nameof(TransportationRequestDetails.StartShip) ||
                x == nameof(TransportationRequestDetails.ContainerTypeId) ||
                x == nameof(TransportationRequestDetails.SocId) ||
                x == nameof(TransportationRequestDetails.ShipNotes) ||
                x == nameof(TransportationRequestDetails.BookingId)))
                {
                    if (transportationEntity.LockShip)
                    {
                        var confirm = new ConfirmDialog
                        {
                            NeedAnswer = true,
                            ComType = nameof(Textbox),
                            Content = $"Bạn có muốn gửi yêu cầu mở khóa không?<br />" +
                        "Hãy nhập lý do",
                        };
                        confirm.Render();
                        confirm.YesConfirmed += async () =>
                        {
                            listViewItem.Reason = confirm.Textbox?.Text;
                            Toast.Success("Đã gửi yêu cầu thành công");
                            this.Dispose();
                            await new Client(nameof(Transportation)).PostAsync<bool>(listViewItem, "RequestUnLockShip");
                        };
                    }
                }
                if (propNameChanges.Any(x => x == nameof(TransportationRequestDetails.MonthText)
                || x == nameof(TransportationRequestDetails.YearText)
                || x == nameof(TransportationRequestDetails.ExportListId)
                || x == nameof(TransportationRequestDetails.RouteId)
                || x == nameof(TransportationRequestDetails.ShipId)
                || x == nameof(TransportationRequestDetails.Trip)
                || x == nameof(TransportationRequestDetails.ClosingDate)
                || x == nameof(TransportationRequestDetails.StartShip)
                || x == nameof(TransportationRequestDetails.ContainerTypeId)
                || x == nameof(TransportationRequestDetails.ContainerNo)
                || x == nameof(TransportationRequestDetails.SealNo)
                || x == nameof(TransportationRequestDetails.BossId)
                || x == nameof(TransportationRequestDetails.UserId)
                || x == nameof(TransportationRequestDetails.CommodityId)
                || x == nameof(TransportationRequestDetails.Cont20)
                || x == nameof(TransportationRequestDetails.Cont40)
                || x == nameof(TransportationRequestDetails.Weight)
                || x == nameof(TransportationRequestDetails.ReceivedId)
                || x == nameof(TransportationRequestDetails.FreeText2)
                || x == nameof(TransportationRequestDetails.ShipDate)
                || x == nameof(TransportationRequestDetails.ReturnDate)
                || x == nameof(TransportationRequestDetails.ReturnId)
                || x == nameof(TransportationRequestDetails.FreeText3)))
                {
                    if (transportationEntity.IsKt)
                    {
                        var confirm = new ConfirmDialog
                        {
                            NeedAnswer = true,
                            ComType = nameof(Textbox),
                            Content = $"Bạn có muốn gửi yêu cầu mở khóa không?<br />" +
                            "Hãy nhập lý do",
                        };
                        confirm.Render();
                        confirm.YesConfirmed += async () =>
                        {
                            listViewItem.Reason = confirm.Textbox?.Text;
                            Toast.Success("Đã gửi yêu cầu thành công");
                            this.Dispose();
                            await new Client(nameof(Transportation)).PostAsync<bool>(listViewItem, "RequestUnLock");
                        };
                    }
                }
            }
        }
    }
}