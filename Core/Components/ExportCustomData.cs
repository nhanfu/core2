using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;
using System.Globalization;
using Core.MVVM;
using Newtonsoft.Json;

namespace Core.Components
{
    internal class ExportCustomData : PopupEditor
    {
        public ListView ParentListView;
        public HTMLUListElement _ul;
        public HTMLUListElement _ul1;
        public UserSetting _settings;
        private List<GridPolicy> _headers;
        public ExportCustomData(ListView parent) : base(nameof(GridPolicy))
        {
            Name = "Export CustomData";
            Title = "Xuất excel tùy chọn";
            DOMContentLoaded += async () =>
            {
                await LocalRender();
            };
        }

        private async Task LocalRender()
        {
            _headers = ParentListView.BasicHeader.Where(x => x.ComponentType != nameof(Button) && !x.ShortDesc.IsNullOrWhiteSpace()).ToList();
            var userSetting = await new Client(nameof(UserSetting)).FirstOrDefaultAsync<UserSetting>(
                $"?$filter=UserId eq {Client.Token.UserId} and Name eq 'Export-{ParentListView.GuiInfo.Id}'");
            if (userSetting != null)
            {
                var userSettings = JsonConvert.DeserializeObject<List<GridPolicy>>(userSetting.Value).ToDictionary(x => x.Id);
                _headers.ForEach(x =>
                {
                    var current = userSettings.GetValueOrDefault(x.Id);
                    if (current != null)
                    {
                        x.IsExport = current.IsExport;
                        x.OrderExport = current.OrderExport;
                    }
                });
            }
            Html.Take("#Content11203").Div.ClassName("row")
                .Ul.Render();
            _ul = Html.Context as HTMLUListElement;
            foreach (var item in _headers)
            {
                Html.Instance.Li.ClassName("text-left").Checkbox(item.IsExport).Event(EventType.Input, (e) => item.IsExport = e.Target.Cast<HTMLInputElement>().Checked).End.Span.ClassName("ml-4").IText(item.ShortDesc).End.End.Render();
            }
        }

        public async Task ExportData()
        {
            Toast.Success("Đang xuất excel");
            var exportExcel = _headers.Where(x => x.IsExport).ToList();
            var userSetting = await new Client(nameof(UserSetting)).FirstOrDefaultAsync<UserSetting>(
                $"?$filter=UserId eq {Client.Token.UserId} and Name eq 'Export-{ParentListView.GuiInfo.Id}'");
            if (userSetting is null)
            {
                userSetting = new UserSetting()
                {
                    Name = $"Export-{ParentListView.GuiInfo.Id}",
                    UserId = Client.Token.UserId,
                    Value = JsonConvert.SerializeObject(exportExcel)
                };
                await new Client(nameof(UserSetting)).CreateAsync<UserSetting>(userSetting);
            }
            else
            {
                userSetting.Value = JsonConvert.SerializeObject(exportExcel);
                await new Client(nameof(UserSetting)).UpdateAsync<UserSetting>(userSetting);
            }
            var orderbyList = ParentListView.AdvSearchVM.OrderBy.Select(orderby => $"[{ParentListView.GuiInfo.RefName}].[{orderby.Field.FieldName}] {orderby.OrderbyOptionId.ToString().ToLowerCase()}");
            var finalFilter = string.Empty;
            if (orderbyList.HasElement())
            {
                finalFilter = orderbyList.Combine();
            }
            if (finalFilter.IsNullOrWhiteSpace())
            {
                finalFilter = OdataExt.GetClausePart(ParentListView.FormattedDataSource, OdataExt.OrderByKeyword);
                if (finalFilter.Contains(","))
                {
                    var k = finalFilter.Split(",").ToList();
                    finalFilter = k.Select(x => $"[{ParentListView.GuiInfo.RefName}].{x}").Combine();
                }
                else
                {
                    finalFilter = $"[{ParentListView.GuiInfo.RefName}].{finalFilter}";
                }
            }
            var path = await new Client(ParentListView.GuiInfo.RefName).GetAsync<string>($"/ExportExcel?componentId={ParentListView.GuiInfo.Id}&sql={ParentListView.Sql}&where={ParentListView.Wheres.Combine(" and ")}&custom=true&featureId={Parent.EditForm.Feature.Id}&orderby={finalFilter}");
            Client.Download($"/excel/Download/{path}");
            Toast.Success("Xuất file thành công");
        }
    }
}