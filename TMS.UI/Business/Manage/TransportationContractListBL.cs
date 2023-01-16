using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using System;
using TMS.API.Enums;
using Core.Clients;
using System.Collections.Generic;
using Bridge.Html5;
using Core.MVVM;
using Core.Enums;

namespace TMS.UI.Business.Manage
{
    public class TransportationContractListBL : TabEditor
    {
        private HTMLInputElement _uploader;
        public TransportationContractListBL() : base(nameof(TransportationContract))
        {
            Name = "TransportationContract List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcel(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
        }

        public async Task EditTransportationContract(TransportationContract entity)
        {
            await this.OpenPopup(
                featureName: "TransportationContract Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationContractEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa kế hoạch vận chuyển";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddTransportationContract()
        {
            await this.OpenPopup(
                featureName: "TransportationContract Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationContractEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới kế hoạch vận chuyển";
                    instance.Entity = new TransportationContract();
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
            var response = await Client.SubmitAsync<List<TransportationContract>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportExcel",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportExcel()
        {
            _uploader.Click();
        }
    }
}