using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;
using static Retyped.dom.Literals.Types;
using TaskNotification = TMS.API.Models.TaskNotification;

namespace TMS.UI.Business.Accountant
{
    public class FreightRateListBL : TabEditor
    {
        public FreightRateListBL() : base(nameof(FreightRate))
        {
            Name = "FreightRate List";
        }

        public void SetClosing()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var listViewItems = gridView.RowData.Data.Cast<FreightRate>().ToList();
            listViewItems.ForEach(x =>
            {
                var listViewItem = gridView.GetListViewItems(x).FirstOrDefault();
                if (listViewItem is null)
                {
                    return;
                }
                if (!Client.Token.AllRoleIds.Contains(31) && !Client.Token.AllRoleIds.Contains(8))
                {
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "IsClosing" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                    if (x.IsClosing)
                    {
                        listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
                        listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                    }
                    else
                    {
                        listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                    }
                }
                else
                {
                    if (x.IsClosing)
                    {
                        if (x.IsApproveClosing)
                        {
                            listViewItem.Element.AddClass("bg-red1");
                            listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                        }
                        else
                        {
                            listViewItem.Element.RemoveClass("bg-red1");
                            listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "IsClosing" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                        }
                    }
                    else
                    {
                        listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                    }
                }
                if (x.IsChange && x.IsApproveClosing == false)
                {
                    listViewItem.Element.AddClass("bg-red");
                }
                else if(x.IsChange == false && x.IsApproveClosing == false)
                {
                    listViewItem.Element.RemoveClass("bg-red");
                }
            });
            gridView.BodyContextMenuShow += () =>
            {
                ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                {
                        new ContextMenuItem { Icon = "fas fa-exchange-alt mr-1", Text = "Xem thay đổi", Click = ChangeFreightRate },
                };
            };
        }

        public async Task AddFreightRate()
        {
            await this.OpenPopup(
                featureName: "FreightRate Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.FreightRateEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới biểu giá CVC";
                    instance.Entity = new FreightRate()
                    {
                        TypeId = 25156
                    };
                    return instance;
                });
        }

        public async Task EditFreightRate(FreightRate freightRate)
        {
            await this.OpenPopup(
                featureName: "FreightRate Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.FreightRateEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa biểu giá CVC";
                    instance.Entity = freightRate;
                    return instance;
                });
        }

        public void ChangeFreightRate(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var freightRate = gridView.GetSelectedRows().FirstOrDefault().Cast<FreightRate>();
            Task.Run(async () =>
            {
                await this.OpenPopup(
                   featureName: "FreightRate Change",
                   factory: () =>
                   { 
                       var type = Type.GetType("TMS.UI.Business.Accountant.FreightRateChangeBL");
                       var instance = Activator.CreateInstance(type) as PopupEditor;
                       instance.Title = "Thông tin chỉnh sửa";
                       instance.Entity = freightRate;
                       return instance;
                   });
            });
        }

        public async Task BusinessFreightRate()
        {
            await this.OpenPopup(
                featureName: "Business Freight Rate",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.BusinessFreightRateBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Bảng giá tham khảo";
                    instance.Entity = new FreightRate()
                    {
                        TypeId = 25157
                    };
                    return instance;
                });
        }

        public async Task EditBusinessFreightRate(FreightRate freightRate)
        {
            await this.OpenPopup(
                featureName: "Business Freight Rate",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.BusinessFreightRateBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá tham khảo";
                    instance.Entity = freightRate;
                    return instance;
                });
        }

        public void RequestUnClosing()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var freightRate = gridView.GetSelectedRows().FirstOrDefault().Cast<FreightRate>();
            if(freightRate == null)
            {
                return;
            }
            if (freightRate.IsApproveClosing && (Client.Token.AllRoleIds.Contains(12) || Client.Token.AllRoleIds.Contains(8)))
            {
                var confirm = new ConfirmDialog
                {
                    NeedAnswer = true,
                    ComType = nameof(Textbox),
                    Content = $"Bạn có chắc chắn muốn duyệt mở khóa<br />" +
                    "Với lý do",
                };
                confirm.Textbox.Text = freightRate.Reason;
                confirm.Render();
                confirm.YesConfirmed += async () =>
                {
                    freightRate.IsApproveClosing = false;
                    freightRate.IsClosing = false;
                    freightRate.IsChange = true;
                    await new Client(nameof(FreightRate)).PatchAsync<FreightRate>(GetPatchEntity(freightRate));
                    var listViewItem = gridView.GetListViewItems(freightRate).FirstOrDefault();
                    listViewItem.Element.RemoveClass("bg-red1");
                    listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                };
            }
            else if(!freightRate.IsApproveClosing && !Client.Token.AllRoleIds.Contains(12) && !Client.Token.AllRoleIds.Contains(8))
            {
                var confirm = new ConfirmDialog
                {
                    NeedAnswer = true,
                    ComType = nameof(Textbox),
                    Content = $"Bạn có chắc chắn muốn gửi yêu cầu mở khóa?<br />" +
                    "Hãy nhập lý do",
                };
                confirm.Render();
                confirm.YesConfirmed += async () =>
                {
                    freightRate.IsApproveClosing = true;
                    freightRate.Reason = confirm.Textbox?.Text;
                    var res = await new Client(nameof(FreightRate)).PatchAsync<FreightRate>(GetPatchEntity(freightRate));
                    if (res != null)
                    {
                        await new Client(nameof(FreightRate)).PostAsync<FreightRate>(freightRate, "RequestUnLock");
                    }
                };
            }
        }

        public async Task LockAllFreightRate()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(FreightRate));
            if (gridView is null)
            {
                return;
            }
            var listViewItems = (await gridView.GetRealTimeSelectedRows()).Cast<FreightRate>().Where(x => x.IsClosing == false).ToList();
            if (listViewItems.Count() <= 0)
            {
                listViewItems = gridView.RowData.Data.Cast<FreightRate>().Where(x => x.IsClosing == false).ToList();
            }
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Không có biểu giá cvc cần khóa");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn khóa " + listViewItems.Count() + " biểu giá CVC ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                foreach (var item in listViewItems)
                {
                    item.IsClosing = true;
                    var listViewItem = gridView.GetListViewItems(item).FirstOrDefault();
                    await new Client(nameof(FreightRate)).PatchAsync<FreightRate>(GetPatchEntity(item));
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                }
            };
        }

        public async Task UnLockAllFreightRate()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(FreightRate));
            if (gridView is null)
            {
                return;
            }
            var listViewItems = (await gridView.GetRealTimeSelectedRows()).Cast<FreightRate>().Where(x => x.IsClosing).ToList();
            if (listViewItems.Count() <= 0)
            {
                listViewItems = gridView.RowData.Data.Cast<FreightRate>().Where(x => x.IsClosing).ToList();
            }
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Không có biểu giá cvc cần mở khóa");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn mở khóa " + listViewItems.Count() + " biểu giá CVC ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var requests = listViewItems.Where(x => x.IsApproveClosing).ToList();
                if (requests.Count > 0)
                {
                    var confirmRequest = new ConfirmDialog
                    {
                        Content = $"Có {listViewItems.Count()} biểu giá CVC cần duyệt bạn có muốn duyệt mở khóa toàn bộ không ?",
                    };
                    confirmRequest.Render();
                    confirmRequest.YesConfirmed += async () =>
                    {
                        foreach (var item in requests)
                        {
                            item.IsApproveClosing = false;
                            item.IsClosing = false;
                            item.IsChange = true;
                            await new Client(nameof(FreightRate)).PatchAsync<FreightRate>(GetPatchEntity(item));
                            var listViewItem = gridView.GetListViewItems(item).FirstOrDefault();
                            listViewItem.UpdateView();
                            listViewItem.Element.RemoveClass("bg-red1");
                            listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
                            listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                        }
                    };
                    foreach (var item in listViewItems.Where(x=>x.IsApproveClosing == false))
                    {
                        item.IsClosing = false;
                        var listViewItem = gridView.GetListViewItems(item).FirstOrDefault();
                        await new Client(nameof(FreightRate)).PatchAsync<FreightRate>(GetPatchEntity(item));
                        listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                    }
                }
                else if (requests.Count <= 0)
                {
                    foreach (var item in listViewItems)
                    {
                        item.IsClosing = false;
                        var listViewItem = gridView.GetListViewItems(item).FirstOrDefault();
                        await new Client(nameof(FreightRate)).PatchAsync<FreightRate>(GetPatchEntity(item));
                        listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                    }
                }
            };
        }

        public void BeforeCreated(FreightRate freightRate)
        {
            freightRate.TypeId = 25156;
        }

        public PatchUpdate GetPatchEntity(FreightRate freightRate)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = freightRate.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(FreightRate.IsApproveClosing), Value = freightRate.IsApproveClosing.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(FreightRate.IsClosing), Value = freightRate.IsClosing.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(FreightRate.IsChange), Value = freightRate.IsChange.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}
