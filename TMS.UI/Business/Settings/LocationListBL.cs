using Bridge;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class LocationListBL : TabEditor
    {
        public LocationListBL() : base(nameof(TMS.API.Models.Location))
        {
            Name = "Location List";
        }

        public async Task EditLocation(TMS.API.Models.Location entity)
        {
            await this.OpenPopup(
                featureName: "Location Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.LocationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa địa điểm";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddLocation()
        {
            await this.OpenPopup(
                featureName: "Location Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.LocationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới địa điểm";
                    instance.Entity = new TMS.API.Models.Location();
                    return instance;
                });
        }

        public virtual void CustomMenu()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.RefName == nameof(Location));
            if (gridView is null)
            {
                return;
            }
            gridView.BodyContextMenuShow += () =>
            {
                var menus = new List<ContextMenuItem>();
                menus.Clear();
                menus.Add(new ContextMenuItem
                {
                    Icon = "fas fa-pen",
                    Text = "Cập nhật cước",
                    MenuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem { Text = "Cước đóng hàng", Click = UpdateQuotationClosing },
                        new ContextMenuItem { Text = "Cước trả hàng", Click = UpdateQuotationReturn },
                    }
                });
                ContextMenu.Instance.MenuItems = menus;
            };
        }

        private void UpdateQuotationReturn(object arg)
        {
            Task.Run(async () =>
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.RefName == nameof(Location));
                if (gridView is null)
                {
                    return;
                }
                var location = gridView.EntityFocusId;
                await this.OpenPopup(
                    featureName: "Quotation Editor",
                    factory: () =>
                    {
                        var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                        var instance = Activator.CreateInstance(type) as PopupEditor;
                        instance.Title = "Chỉnh sửa bảng giá trả hàng";
                        instance.Entity = new Quotation()
                        {
                            LocationId = location,
                            TypeId = 7593
                        };
                        return instance;
                    });
            });
        }

        private void UpdateQuotationClosing(object arg)
        {
            Task.Run(async () =>
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.RefName == nameof(Location));
                if (gridView is null)
                {
                    return;
                }
                var location = gridView.EntityFocusId;
                await this.OpenPopup(
                    featureName: "Quotation Editor",
                    factory: () =>
                    {
                        var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                        var instance = Activator.CreateInstance(type) as PopupEditor;
                        instance.Title = "Chỉnh sửa bảng giá đóng hàng";
                        instance.Entity = new Quotation()
                        {
                            LocationId = location,
                            TypeId = 7592
                        };
                        return instance;
                    });
            });
        }
    }
}