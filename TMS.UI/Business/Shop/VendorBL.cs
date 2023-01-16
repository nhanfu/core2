using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using Core.Clients;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;
using System.Linq;
using Core.Components;
using Core.Extensions;
using Bridge.Html5;
using Core.MVVM;
using System.Collections.Generic;
using Core.Enums;
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

        public virtual void CheckQuotationTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            gridView.BodyContextMenuShow += () =>
            {
                ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                {
                    new ContextMenuItem { Icon = "fas fa-pen", Text = "Gộp khách hàng", Click = CompareCustomer},
                };
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

        public async Task SetSale(Vendor vendor)
        {
            gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            vendor.TypeId = (int)VendorTypeEnum.Boss;
            vendor.UserId = Client.Token.UserId;
            var fullName = Client.Token.FullName;
            if (fullName is null || !vendor.Code.IsNullOrEmpty())
            {
                return;
            }
            var name = string.Join("", fullName.Split(' ').Select(x => x[0].ToString().ToUpper()));
            var lastVendor = new Client(nameof(Vendor)).FirstOrDefaultAsync<Vendor>($"?$filter=UserId eq {vendor.UserId} and TypeId eq {(int)VendorTypeEnum.Boss}&$orderby=Id desc&$take=1"); ;
            var count = new Client(nameof(Vendor)).GetList<Vendor>($"?$filter=UserId eq {vendor.UserId} and TypeId eq {(int)VendorTypeEnum.Boss}&$count=true");
            await Task.WhenAll(lastVendor, count);
            index = index ?? count.Result.Odata.Count;
            index = index + 1;
            if (lastVendor.Result != null && lastVendor.Result.Code.Contains(name))
            {
                var index1 = int.Parse(lastVendor.Result.Code.Replace(name, ""));
                if (index <= index1)
                {
                    index = index + index1 - index + 1;
                }
            }
            vendor.Code = vendor.Code is null || vendor.Code == "" ? name + index : vendor.Code;
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
                        TypeId = (int)VendorTypeEnum.Boss
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
