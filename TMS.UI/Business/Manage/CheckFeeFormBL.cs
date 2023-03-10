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

namespace TMS.UI.Business.Manage
{
    public class CheckFeeFormBL : PopupEditor
    {
        public CheckFeeHistory AEntity => Entity as CheckFeeHistory;
        private HTMLInputElement _uploaderCheckFee;
        public CheckFeeFormBL() : base(nameof(CheckFeeHistory))
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
            if (AEntity.RouteIds is null || AEntity.RouteIds.Nothing())
            {
                Toast.Warning("Vui lòng chọn chuyến xe");
                return;
            }
            _uploaderCheckFee.Click();
        }

        public async Task ExportCheckFeeSelected()
        {
            if (!(await IsFormValid()))
            {
                return;
            }
            var path = await new Client(nameof(Transportation)).PostAsync<string>(AEntity, "ExportCheckFee?Type=2");
            Client.Download($"/excel/Download/{path.EncodeSpecialChar()}");
            Toast.Success("Xuất file thành công");
        }

        private async Task SelectedExcelCheckFee(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }
            LocalStorage.SetItem("RouteCheckFeeClosing", AEntity.RouteIds);
            LocalStorage.SetItem("FromDateCheckFeeClosing", AEntity.FromDate?.ToString("MM/dd/yyyy"));
            LocalStorage.SetItem("ToDateCheckFeeClosing", AEntity.ToDate?.ToString("MM/dd/yyyy"));
            LocalStorage.SetItem("ClosingIdCheckFeeClosing", AEntity.ClosingId);
            var uploadForm = _uploaderCheckFee.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            _uploaderCheckFee.Value = null;
            formData.Append(nameof(CheckFeeHistory.FromDate), AEntity.FromDate.ToString());
            formData.Append(nameof(CheckFeeHistory.ToDate), AEntity.ToDate.ToString());
            formData.Append(nameof(CheckFeeHistory.ClosingId), AEntity.ClosingId.ToString());
            formData.Append(nameof(CheckFeeHistory.RouteIds), AEntity.RouteIds.Combine());
            var rs = await new Client(nameof(Transportation)).SubmitAsync<List<Transportation>>(new XHRWrapper
            {
                FormData = formData,
                Url = "CheckFee?type=" + AEntity.TypeId,
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
            Dispose();
            Window.SetTimeout(async () =>
            {
                if (rs != null)
                {
                    if (AEntity.TypeId == 1)
                    {
                        var entity = await new Client(nameof(CheckFeeHistory)).FirstOrDefaultAsync<CheckFeeHistory>($"?$filter=Id eq {rs.FirstOrDefault(x => x.CheckFeeHistoryId != null).CheckFeeHistoryId}");
                        if (entity.Id > 0)
                        {
                            await this.OpenTab(
                            id: "CheckFee Editor" + rs.FirstOrDefault().CheckFeeHistoryId,
                            featureName: "Add CheckFee Editor",
                            factory: () =>
                            {
                                var type = Type.GetType("TMS.UI.Business.Manage.CheckFeeEditorBL");
                                var instance = Activator.CreateInstance(type) as TabEditor;
                                instance.Icon = "fal fa-sitemap mr-1";
                                instance.Title = "Thêm mới kiểm tra phí đóng hàng";
                                instance.Entity = entity;
                                instance.Entity.SetPropValue("TransportationList", rs);
                                return instance;
                            });
                        }
                    }
                    else
                    {
                        var entity = await new Client(nameof(CheckFeeHistory)).FirstOrDefaultAsync<CheckFeeHistory>($"?$filter=Id eq {rs.FirstOrDefault(x => x.CheckFeeHistoryReturnId != null).CheckFeeHistoryReturnId}");
                        if (entity.Id > 0)
                        {
                            await this.OpenTab(
                            id: "CheckFee Return Editor" + rs.FirstOrDefault().CheckFeeHistoryReturnId,
                            featureName: "CheckFee Return Editor",
                            factory: () =>
                            {
                                var type = Type.GetType("TMS.UI.Business.Manage.CheckFeeReturnEditorBL");
                                var instance = Activator.CreateInstance(type) as TabEditor;
                                instance.Icon = "fal fa-sitemap mr-1";
                                instance.Title = "Kiểm tra phí trả hàng";
                                instance.Entity = entity;
                                instance.Entity.SetPropValue("TransportationList", rs);
                                return instance;
                            });
                        }
                    }
                }
            }, 2000);

        }
    }
}