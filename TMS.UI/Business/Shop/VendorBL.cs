using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using TMS.API.Models;

namespace TMS.UI.Business.Shop
{
    public class VendorBL : TabEditor
    {
        private HTMLInputElement _uploader;
        public VendorBL() : base(nameof(Vendor))
        {
            Name = "Vendor List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, SelectedExcel).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
        }

        public void EditVendor(Vendor entity)
        {
            var task = this.OpenPopup(
                featureName: "Vendor Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Shop.VendorEditorBL");
                    var instance = new PopupEditor(nameof(Vendor));
                    instance.Title = "Thông tin chi tiết NCC";
                    instance.Entity = entity ?? new Vendor();
                    return instance;
                });
            Client.ExecTask(task);
        }

        private void SelectedExcel(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploader.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var task = Client.SubmitAsync<List<Vendor>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportVendor",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
            Client.ExecTask(task);
        }

        public void ImportVendor()
        {
            _uploader.Click();
        }
    }
}
