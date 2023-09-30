using Bridge.Html5;
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
using VendorTypeEnum = TMS.API.Enums.VendorTypeEnum;

namespace TMS.UI.Business.Shop
{
    public class VendorBL : TabEditor
    {
        private GridView gridView;
        private int? index;
        private HTMLInputElement _uploader;
        public VendorBL() : base(nameof(Vendor))
        {
            Name = "Vendor List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcel(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
        }

        private void CompareCustomer(object arg)
        {

        }

        public async Task EditVendor(Vendor entity)
        {
            await this.OpenPopup(
                featureName: "Vendor Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Shop.VendorEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa chủ hàng";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddVendor()
        {
            await this.OpenPopup(
                featureName: "Vendor Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Shop.VendorEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới chủ hàng";
                    instance.Entity = new Vendor()
                    {
                        TypeId = ((int)VendorTypeEnum.Boss).ToString()
                    };
                    return instance;
                });
        }

        private async Task SelectedExcel(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploader.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Vendor>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportVendor",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportVendor()
        {
            _uploader.Click();
        }

        public virtual void DOMContentLoadedVendor()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Vendor));
            if (gridView is null)
            {
                return;
            }
            gridView.BodyContextMenuShow += () =>
            {
                ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                {
                        new ContextMenuItem { Icon = "fas fa-pen", Text = "Gộp chủ hàng", Click = CompareVendor},
                };
            };
        }

        private void CompareVendor(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Vendor));
            Task.Run(async () =>
            {
                var rs = await Client.PostAsync<bool>(gridView.GetSelectedRows().FirstOrDefault(), "CompareVendor");
                if (rs)
                {
                    Toast.Success("Hợp nhất chủ hàng thành công!");
                }
                else
                {
                    Toast.Success("Chưa chọn chủ hàng gốc!");
                }
            });
        }
    }
}
