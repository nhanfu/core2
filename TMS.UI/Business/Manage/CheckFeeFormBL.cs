using Bridge.Html5;
using Core.Clients;
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

namespace TMS.UI.Business.Manage
{
    public class CheckFeeFormBL : PopupEditor
    {
        public CheckFeeHistory AEntity => Entity as CheckFeeHistory;
        private HTMLInputElement _uploaderCheckFee;
        public CheckFeeFormBL() : base(nameof(Allotment))
        {
            Name = "CheckFee Form";
            DOMContentLoaded += () =>
            {

                Html.Take(ParentElement).Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcelCheckFee(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileCheckFee").Attr("accept", ".xlsx");
                _uploaderCheckFee = Html.Context as HTMLInputElement;
            };
        }

        public async void CheckFee()
        {
            if (!(await IsFormValid()))
            {
                return;
            }
            _uploaderCheckFee.Click();
        }

        private async Task SelectedExcelCheckFee(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploaderCheckFee.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            _uploaderCheckFee.Value = null;
            formData.Append(nameof(CheckFeeHistory.FromDate), AEntity.FromDate.ToString());
            formData.Append(nameof(CheckFeeHistory.ToDate), AEntity.ToDate.ToString());
            formData.Append(nameof(CheckFeeHistory.ClosingId), AEntity.ClosingId.ToString());
            var rs = await new Client(nameof(Transportation)).SubmitAsync<List<Transportation>>(new XHRWrapper
            {
                FormData = formData,
                Url = "CheckFee?type=" + (TabEditor.Name == "Transportation List" ? "1" : "0"),
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
            Dispose();
            Window.SetTimeout(async () =>
            {
                if (rs != null)
                {
                    var entity = await new Client(nameof(CheckFeeHistory)).FirstOrDefaultAsync<CheckFeeHistory>($"?$filter=Id eq {rs.FirstOrDefault(x => x.CheckFeeHistoryId != null).CheckFeeHistoryId}");
                    if (entity.Id > 0)
                    {
                        await this.OpenTab(
                        id: "CheckFee Editor" + rs.FirstOrDefault().CheckFeeHistoryId,
                        featureName: "CheckFee Editor",
                        factory: () =>
                        {
                            var type = Type.GetType("TMS.UI.Business.Manage.CheckFeeEditorBL");
                            var instance = Activator.CreateInstance(type) as TabEditor;
                            instance.Icon = "fal fa-sitemap mr-1";
                            instance.Title = "Kiểm tra phí đóng hàng";
                            instance.Entity = entity;
                            instance.Entity.SetPropValue("TransportationList", rs);
                            return instance;
                        });
                    }
                }
            }, 2000);

        }
    }
}