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
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class LocationListBL : TabEditor
    {
        private HTMLInputElement _uploader;
        private HTMLInputElement _uploaderPortLowerliftGoods;
        private HTMLInputElement _uploaderPortLowerliftHollow;
        public LocationListBL() : base(nameof(TMS.API.Models.Location))
        {
            Name = "Location List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcel(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelPortLowerliftGoods(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderPortLowerliftGoods = Html.Context as HTMLInputElement;
            };
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelPortLowerliftHollow(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploaderPortLowerliftHollow = Html.Context as HTMLInputElement;
            };
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

        private async Task SelectedExcel(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploader.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<TMS.API.Models.Location>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportLocation",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportLocation()
        {
            _uploader.Click();
        }

        private async Task SelectedExcelPortLowerliftGoods(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderPortLowerliftGoods.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<TMS.API.Models.Location>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportPortLowerliftGoods",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportPortLowerliftGoods()
        {
            _uploaderPortLowerliftGoods.Click();
        }

        private async Task SelectedExcelPortLowerliftHollow(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderPortLowerliftHollow.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<TMS.API.Models.Location>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportPortLowerliftHollow",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportPortLowerliftHollow()
        {
            _uploaderPortLowerliftHollow.Click();
        }
    }
}