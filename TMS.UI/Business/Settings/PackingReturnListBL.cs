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

namespace TMS.UI.Business.Settings
{
    public class PackingReturnListBL : TabEditor
    {
        private HTMLInputElement _uploaderLine;
        private HTMLInputElement _uploaderVendor;
        private HTMLInputElement _uploaderList;
        private HTMLInputElement _uploaderGetOrder;
        public PackingReturnListBL() : base(nameof(Vendor))
        {
            Name = "Packing Return List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelLine(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderLine = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelVendor(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderVendor = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelList(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderList = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelGetOrder(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderGetOrder = Html.Context as HTMLInputElement;
            };
        }

        public async Task EditPackingReturn(Vendor entity)
        {
            GridView gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (gridView.Name.Contains("Location8"))
            {
                await this.OpenPopup(
                featureName: "GetOrder Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.GetOrderEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa đơn vị nhận lệnh";
                    instance.Entity = entity;
                    return instance;
                });
            }
            else
            {
                await this.OpenPopup(
                featureName: "Packing Return Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.PackingReturnEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa đơn vị đóng hạ hàng";
                    instance.Entity = entity;
                    return instance;
                });
            }
        }

        public async Task AddPackingReturn()
        {
            GridView gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (gridView.Name.Contains("Location8") == false)
            {
                await this.OpenPopup(
                featureName: "Packing Return Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.PackingReturnEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới đơn vị đóng hạ hàng";
                    instance.Entity = new Vendor()
                    {
                        TypeId = (int)VendorTypeEnum.Partner
                    };
                    return instance;
                });
            }
            else
            {
                await this.OpenPopup(
                featureName: "GetOrder Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.GetOrderEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới đơn vị nhận lệnh";
                    var vendor = new Vendor()
                    {
                        TypeId = (int)VendorTypeEnum.Partner
                    };
                    var vendorService = new VendorService()
                    {
                        ServiceId = 11839
                    };
                    vendor.VendorService.Add(vendorService);
                    instance.Entity = vendor;
                    return instance;
                });
            }
        }

        private async Task SelectedExcelLine(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderLine.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Vendor>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportLine",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportLine()
        {
            _uploaderLine.Click();
        }

        private async Task SelectedExcelVendor(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderVendor.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Vendor>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportVendorLocation",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportVendorLocation()
        {
            _uploaderVendor.Click();
        }

        private async Task SelectedExcelList(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderList.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Vendor>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportList",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportList()
        {
            _uploaderList.Click();
        }

        private async Task SelectedExcelGetOrder(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderGetOrder.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Vendor>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportGetOrder",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportGetOrder()
        {
            _uploaderGetOrder.Click();
        }
    }
}