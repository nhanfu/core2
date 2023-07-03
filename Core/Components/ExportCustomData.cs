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
        public HTMLElement _tbody;
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
                Move();
            };
        }

        private void Move()
        {
            var seft = this;
            /*@
                const table = document.getElementById('exTable');

                let draggingEle;
                let draggingRowIndex;
                let placeholder;
                let list;
                let isDraggingStarted = false;

                // The current position of mouse relative to the dragging element
                let x = 0;
                let y = 0;

                // Swap two nodes
                const swap = function (nodeA, nodeB) {
                    const parentA = nodeA.parentNode;
                    const siblingA = nodeA.nextSibling === nodeB ? nodeA : nodeA.nextSibling;

                    // Move `nodeA` to before the `nodeB`
                    nodeB.parentNode.insertBefore(nodeA, nodeB);

                    // Move `nodeB` to before the sibling of `nodeA`
                    parentA.insertBefore(nodeB, siblingA);
                };

                // Check if `nodeA` is above `nodeB`
                const isAbove = function (nodeA, nodeB) {
                    // Get the bounding rectangle of nodes
                    const rectA = nodeA.getBoundingClientRect();
                    const rectB = nodeB.getBoundingClientRect();

                    return rectA.top + rectA.height / 2 < rectB.top + rectB.height / 2;
                };

                const cloneTable = function () {
                    const rect = table.getBoundingClientRect();
                    const width = parseInt(window.getComputedStyle(table).width);

                    list = document.createElement('div');
                    list.classList.add('clone-list');
                    list.style.position = 'absolute';
                    table.parentNode.insertBefore(list, table);

                    // Hide the original table
                    table.style.visibility = 'hidden';

                    table.querySelectorAll('tr').forEach(function (row) {
                        // Create a new table from given row
                        const item = document.createElement('div');
                        item.classList.add('draggable');

                        const newTable = document.createElement('table');
                        newTable.setAttribute('class', 'clone-table');
                        newTable.style.width = `${width}px`;

                        const newRow = document.createElement('tr');
                        const cells = [].slice.call(row.children);
                        cells.forEach(function (cell) {
                            const newCell = cell.cloneNode(true);
                            newCell.style.width = `${parseInt(window.getComputedStyle(cell).width)}px`;
                            newRow.appendChild(newCell);
                        });

                        newTable.appendChild(newRow);
                        item.appendChild(newTable);
                        list.appendChild(item);
                    });
                };

                const mouseDownHandler = function (e) {
                    // Get the original row
                    const originalRow = e.target.parentNode;
                    draggingRowIndex = [].slice.call(table.querySelectorAll('tr')).indexOf(originalRow);

                    // Determine the mouse position
                    x = e.clientX;
                    y = e.clientY;

                    // Attach the listeners to `document`
                    document.addEventListener('mousemove', mouseMoveHandler);
                    document.addEventListener('mouseup', mouseUpHandler);
                };

                const mouseMoveHandler = function (e) {
                    if (!isDraggingStarted) {
                        isDraggingStarted = true;

                        cloneTable();

                        draggingEle = [].slice.call(list.children)[draggingRowIndex];
                        draggingEle.classList.add('dragging');

                        // Let the placeholder take the height of dragging element
                        // So the next element won't move up
                        placeholder = document.createElement('div');
                        placeholder.classList.add('placeholder');
                        draggingEle.parentNode.insertBefore(placeholder, draggingEle.nextSibling);
                        placeholder.style.height = `${draggingEle.offsetHeight}px`;
                    }

                    // Set position for dragging element
                    draggingEle.style.position = 'absolute';
                    draggingEle.style.top = `${draggingEle.offsetTop + e.clientY - y}px`;
                    draggingEle.style.left = `${draggingEle.offsetLeft + e.clientX - x}px`;

                    // Reassign the position of mouse
                    x = e.clientX;
                    y = e.clientY;

                    // The current order
                    // prevEle
                    // draggingEle
                    // placeholder
                    // nextEle
                    const prevEle = draggingEle.previousElementSibling;
                    const nextEle = placeholder.nextElementSibling;

                    // The dragging element is above the previous element
                    // User moves the dragging element to the top
                    // We don't allow to drop above the header
                    // (which doesn't have `previousElementSibling`)
                    if (prevEle && prevEle.previousElementSibling && isAbove(draggingEle, prevEle)) {
                        // The current order    -> The new order
                        // prevEle              -> placeholder
                        // draggingEle          -> draggingEle
                        // placeholder          -> prevEle
                        swap(placeholder, draggingEle);
                        swap(placeholder, prevEle);
                        return;
                    }

                    // The dragging element is below the next element
                    // User moves the dragging element to the bottom
                    if (nextEle && isAbove(nextEle, draggingEle)) {
                        // The current order    -> The new order
                        // draggingEle          -> nextEle
                        // placeholder          -> placeholder
                        // nextEle              -> draggingEle
                        swap(nextEle, placeholder);
                        swap(nextEle, draggingEle);
                    }
                };

                const mouseUpHandler = function () {
                    // Remove the placeholder
                    placeholder && placeholder.parentNode.removeChild(placeholder);

                    draggingEle.classList.remove('dragging');
                    draggingEle.style.removeProperty('top');
                    draggingEle.style.removeProperty('left');
                    draggingEle.style.removeProperty('position');

                    // Get the end index
                    const endRowIndex = [].slice.call(list.children).indexOf(draggingEle);

                    isDraggingStarted = false;

                    // Remove the `list` element
                    list.parentNode.removeChild(list);

                    // Move the dragged row to `endRowIndex`
                    let rows = [].slice.call(table.querySelectorAll('tr'));
                    draggingRowIndex > endRowIndex
                        ? rows[endRowIndex].parentNode.insertBefore(rows[draggingRowIndex], rows[endRowIndex])
                        : rows[endRowIndex].parentNode.insertBefore(
                              rows[draggingRowIndex],
                              rows[endRowIndex].nextSibling
                          );

                    // Bring back the table
                    table.style.removeProperty('visibility');

                    // Remove the handlers of `mousemove` and `mouseup`
                    document.removeEventListener('mousemove', mouseMoveHandler);
                    document.removeEventListener('mouseup', mouseUpHandler);
                    seft.OrderBy();
                };

                table.querySelectorAll('tr').forEach(function (row, index) {
                    // Ignore the header
                    // We don't want user to change the order of header
                    if (index === 0) {
                        return;
                    }

                    const firstCell = row.firstElementChild;
                    firstCell.classList.add('draggable');
                    firstCell.addEventListener('mousedown', mouseDownHandler);
                });
             */
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
            _headers = _headers.OrderBy(x => x.OrderExport).ToList();
            var content = this.FindComponentByName<Section>("Content");
            Html.Take(content.Element).Table.ClassName("table").Id("exTable")
                .Thead
                    .TRow.TData.Text("STT").End
                    .TData.Checkbox(false).Event(EventType.Input, (e) => TongleAll(e)).End.End
                    .TData.Text("Tên cột").EndOf(ElementType.thead);
            Html.Instance.TBody.Render();
            _tbody = Html.Context;
            var i = 1;
            foreach (var item in _headers)
            {
                Html.Instance.TRow.DataAttr("id", item.Id).TData.Style("padding:0").IText(i.ToString()).End
                    .TData.Style("padding:0").Checkbox(item.IsExport).Event(EventType.Input, (e) => item.IsExport = e.Target.Cast<HTMLInputElement>().Checked).End.End
                    .TData.Style("padding:0").ClassName("text-left").IText(item.ShortDesc).End
                    .EndOf(ElementType.tr);
                i++;
            }
        }

        private void TongleAll(Event e)
        {
            Html.Take(_tbody).Clear();
            _headers.ForEach(x => x.IsExport = e.Target.Cast<HTMLInputElement>().Checked);
            var i = 1;
            foreach (var item in _headers)
            {
                Html.Instance.TRow.DataAttr("id", item.Id).TData.DataAttr("id", item.Id).Style("padding:0").IText(i.ToString()).End
                    .TData.Style("padding:0").Checkbox(item.IsExport).Event(EventType.Input, (e1) => item.IsExport = e1.Target.Cast<HTMLInputElement>().Checked).End.End
                    .TData.Style("padding:0").ClassName("text-left").IText(item.ShortDesc).End
                    .EndOf(ElementType.tr);
                i++;
            }
            Move();
        }

        private void OrderBy()
        {
            var j = 1;
            _tbody.Children.ForEach(y =>
            {
                _headers.FirstOrDefault(x => x.Id == int.Parse(y.GetAttribute("data-id"))).OrderExport = j;
                j++;
            });
        }

        public async Task ExportData()
        {
            Toast.Success("Đang xuất excel");
            var userSetting = await new Client(nameof(UserSetting)).FirstOrDefaultAsync<UserSetting>(
                $"?$filter=UserId eq {Client.Token.UserId} and Name eq 'Export-{ParentListView.GuiInfo.Id}'");
            if (userSetting is null)
            {
                userSetting = new UserSetting()
                {
                    Name = $"Export-{ParentListView.GuiInfo.Id}",
                    UserId = Client.Token.UserId,
                    Value = JsonConvert.SerializeObject(_headers)
                };
                await new Client(nameof(UserSetting)).CreateAsync<UserSetting>(userSetting);
            }
            else
            {
                userSetting.Value = JsonConvert.SerializeObject(_headers);
                await new Client(nameof(UserSetting)).UpdateAsync<UserSetting>(userSetting);
            }
            var orderbyList = ParentListView.AdvSearchVM.OrderBy.Select(orderby => $"[{ParentListView.GuiInfo.RefName}].{orderby.Field.FieldName} {orderby.OrderbyOptionId.ToString().ToLowerCase()}");
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
            var filter = ParentListView.Wheres.Where(x => !x.Group).Select(x => x.FieldName).Combine(" and ");
            var filter1 = ParentListView.Wheres.Where(x => x.Group).Select(x => x.FieldName).Combine(" or ");
            var wh = new List<string>();
            if (!filter.IsNullOrWhiteSpace())
            {
                wh.Add($"({filter})");
            }
            if (!filter1.IsNullOrWhiteSpace())
            {
                wh.Add($"({filter1})");
            }
            var stringWh = wh.Any() ? $"({wh.Combine(" and ")})" : "";
            var pre = ParentListView.GuiInfo.PreQuery;
            if (pre != null && Utils.IsFunction(pre, out Function fn))
            {
                pre = fn.Call(this, this, EditForm).ToString();
            }
            var path = await new Client(ParentListView.GuiInfo.RefName).GetAsync<string>($"/ExportExcel?componentId={ParentListView.GuiInfo.Id}" +
                $"&sql={ParentListView.Sql}" +
                $"&showNull={ParentListView.GuiInfo.ShowNull ?? false}" +
                $"&where={stringWh} {(pre.IsNullOrWhiteSpace() ? "" : $"{(ParentListView.Wheres.Any() ? " and " : "")} {pre}")}" +
                $"&custom=true&featureId={Parent.EditForm.Feature.Id}&orderby={finalFilter}");
            Client.Download($"/excel/Download/{path}");
            Toast.Success("Xuất file thành công");
        }
    }
}