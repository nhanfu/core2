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
    public class ContainerTypeListBL : TabEditor
    {
        private HTMLInputElement _uploader;
        public ContainerTypeListBL() : base(nameof(MasterData))
        {
            Name = "Container Type List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelContainer(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
        }

        public void BeforeCreatedMasterData(MasterData masterData)
        {
            masterData.ParentId = 7565;
        }

        public async Task EditContainerType(MasterData entity)
        {
            await this.OpenPopup(
                featureName: "ContainerType Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.ContainerTypeEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa loại xe công";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddContainerType()
        {
            await this.OpenPopup(
                featureName: "ContainerType Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.ContainerTypeEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới loại xe công";
                    instance.Entity = new MasterData()  {ParentId = 7565} ;
                    return instance;
                });
        }

        private async Task SelectedExcelContainer(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploader.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var response = await Client.SubmitAsync<List<MasterData>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportContainerType",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportContainerType()
        {
            _uploader.Click();
        }
    }
}
