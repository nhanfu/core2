using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class ShipListBL : TabEditor
    {
        private HTMLInputElement _uploader;
        private HTMLInputElement _uploaderShip;
        public ShipListBL() : base(nameof(Ship))
        {
            Name = "Ship List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelShip(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelShipNew(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderShip = Html.Context as HTMLInputElement;
            };
        }

        public async Task EditShip(Ship entity)
        {
            await this.OpenPopup(
                featureName: "Ship Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.ShipEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa tàu";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddShip()
        {
            await this.OpenPopup(
                featureName: "Ship Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.ShipEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới tàu";
                    instance.Entity = new Ship();
                    return instance;
                });
        }

        private async Task SelectedExcelShip(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploader.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Ship>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportShip",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportShip()
        {
            _uploader.Click();
        }

        private async Task SelectedExcelShipNew(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderShip.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<Ship>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportShipNew",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportShipNew()
        {
            _uploaderShip.Click();
        }
    }
}
