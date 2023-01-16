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
    public class RouteListBL : TabEditor
    {
        private HTMLInputElement _uploader;
        public RouteListBL() : base(nameof(Route))
        {
            Name = "Route List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcel(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
        }

        public async Task EditRoute(Route entity)
        {
            await this.OpenPopup(
                featureName: "Route Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.RouteEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa tuyến vận chuyển";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddRoute()
        {
            await this.OpenPopup(
                featureName: "Route Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.RouteEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới tuyến vận chuyển";
                    instance.Entity = new Route();
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
            var response = await Client.SubmitAsync<List<Route>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportRoute",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportRoute()
        {
            _uploader.Click();
        }
    }
}
