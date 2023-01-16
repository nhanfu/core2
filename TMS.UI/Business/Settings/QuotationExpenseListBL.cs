using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class QuotationExpenseListBL : TabEditor
    {
        private HTMLInputElement _uploader;
        public QuotationExpenseListBL() : base(nameof(QuotationExpense))
        {
            Name = "QuotationExpense List";
            DOMContentLoaded += () =>
            {
                NotificationClient?.AddListener(Utils.GetEntity(nameof(Expense)).Id, RealtimeUpdate);
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcel(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
        }

        public async Task EditQuotationExpense(QuotationExpense entity)
        {
            await this.OpenPopup(
                featureName: "QuotationExpense Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationExpenseEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa cấu hình chính sách";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddQuotationExpense()
        {
            await this.OpenPopup(
                featureName: "QuotationExpense Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationExpenseEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới cấu hình chính sách";
                    instance.Entity = new QuotationExpense();
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
            await Client.SubmitAsync<List<Transportation>>(new XHRWrapper
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