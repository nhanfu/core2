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
using TMS.API.ViewModels;

namespace TMS.UI.Business.Accountant
{
    public class AccountListBL : TabEditor
    {
        private HTMLInputElement _uploader;
        public AccountListBL() : base(nameof(MasterData))
        {
            Name = "Account List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelAccount(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
        }

        public async Task AddAccount()
        {
            GridView gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (gridView.Name.Contains("AccountLevel2"))
            {
                await this.OpenPopup(
                featureName: "Account Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.AccountEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới tài khoản";
                    instance.Entity = new MasterData();
                    return instance;
                });
            }
            else
            {
                await this.OpenPopup(
                featureName: "Account Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.AccountEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới tài khoản";
                    instance.Entity = new MasterData() { ParentId = 23991 };
                    return instance;
                });
            }
        }

        public async Task EditAccount(MasterData masterData)
        {
            await this.OpenPopup(
                featureName: "Account Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.AccountEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa tài khoản ngân hàng";
                    instance.Entity = masterData;
                    return instance;
                });
        }

        public void BeforeMasterData(MasterData masterData)
        {
            masterData.ParentId = 23991;
            masterData.Path = @"\23991\";
            masterData.Level = 1;
        }

        private async Task SelectedExcelAccount(Event e)
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
                Url = "ImportExcelAccount",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportExcelAccount()
        {
            _uploader.Click();
        }
    }
}
