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
    public class RevenueRequestBL : PopupEditor
    {
        public Revenue revenueEntity => Entity as Revenue;
        public List<string> propNameChanges = new List<string>();
        public RevenueRequestBL() : base(nameof(RevenueRequest))
        {
            Name = "Revenue Request";
        }

        private int awaiter;

        public async Task SetGridView()
        {
            this.SetShow(false, "btnCreate", "btnSend");
            GridView grid = this.FindComponentByName<GridView>(nameof(RevenueRequest));
            var listViewItems = grid.RowData.Data.Cast<RevenueRequest>().ToList();
            var check = await new Client(nameof(RevenueRequest)).FirstOrDefaultAsync<RevenueRequest>($"?$orderby=Id desc&$filter=Active eq true and RevenueId eq {revenueEntity.Id}");
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
            var bl = Parent as TransportationListAccountantBL;
            if (bl.getCheckView())
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
                }, 500);
            }
        }

        private void CompareChanges(object change, object cutting)
        {
            if (change != null)
            {
                var listItem = change.GetType().GetProperties();
                GridView grid = this.FindComponentByName<GridView>(nameof(RevenueRequest));
                var listViewItem = grid.GetListViewItems(change).FirstOrDefault();
                GridView gridViewCutting = this.FindComponentByName<GridView>(nameof(Revenue));
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
                        && item.Name != "RevenueId"
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

        public void SelectedCompare(RevenueRequest revenueRequest)
        {
            CompareChanges(revenueRequest, revenueEntity);
        }

        public async Task CreateRequestChange()
        {
            GridView grid = this.FindComponentByName<GridView>(nameof(RevenueRequest));
            var checkExist = await new Client(nameof(RevenueRequest)).FirstOrDefaultAsync<RevenueRequest>($"?$orderby=Id desc&$filter=RevenueId eq {revenueEntity.Id} and StatusId eq {(int)ApprovalStatusEnum.New}");
            if (checkExist != null)
            {
                Toast.Warning("Có yêu cầu thay đổi đã được tạo trước đó vui lòng thay đổi ở dưới là gửi đi");
                return;
            }
            var requestChange = new RevenueRequest();
            requestChange.CopyPropFrom(revenueEntity);
            requestChange.Id = 0;
            requestChange.RevenueId = revenueEntity.Id;
            requestChange.InsertedBy = Client.Token.UserId;
            requestChange.InsertedDate = DateTime.Now;
            requestChange.StatusId = (int)ApprovalStatusEnum.New;
            var rs = await new Client(nameof(RevenueRequest)).CreateAsync<RevenueRequest>(requestChange);
            if(rs != null) 
            {
                await grid.ApplyFilter();
                this.SetShow(false, "btnCreate");
                this.SetShow(true, "btnSend");
                await SetGridView();
            }
        }

        public void SendRequestApprove()
        {
            GridView grid = this.FindComponentByName<GridView>(nameof(RevenueRequest));
            var listViewItem = grid.RowData.Data.Cast<RevenueRequest>().OrderByDescending(x => x.Id).FirstOrDefault();
            var selected = grid.GetSelectedRows().Cast<RevenueRequest>().OrderByDescending(x => x.Id).FirstOrDefault();
            if (listViewItem.Id != selected.Id)
            {
                Toast.Warning("Bạn chưa chọn thông tin thay đổi có thể gửi");
                return;
            }
            var confirm = new ConfirmDialog
            {
                NeedAnswer = true,
                ComType = nameof(Textbox),
                Content = $"Bạn có muốn gửi yêu cầu thay đổi không?<br />" +
                        "Hãy nhập lý do",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                listViewItem.Reason = confirm.Textbox?.Text;
                Toast.Success("Đã gửi yêu cầu thành công");
                this.Dispose();
                await new Client(nameof(Revenue)).PostAsync<bool>(listViewItem, "RequestUnLock");
            };
        }
    }
}