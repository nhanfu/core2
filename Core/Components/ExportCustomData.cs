using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class ExportCustomData : PopupEditor
    {
        public ListView ParentListView;
        public HTMLElement _tbody;
        private List<Component> _headers;
        private dynamic _userSetting;
        private HTMLElement _table;
        private bool _hasLoadSetting;

        public ExportCustomData(ListView parent) : base(nameof(Component))
        {
            Name = "Export CustomData";
            Title = "Xuất excel tùy chọn";
            DOMContentLoaded += () =>
            {
                LocalRender();
            };
            ParentListView = parent;
            _headers = ParentListView.Header.Where(x => x.ComponentType != nameof(Button) && !x.ShortDesc.IsNullOrWhiteSpace()).ToList();
        }

        private void Move()
        {
            var seft = this;
            /*@
                const table = this._table;

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

        private void LocalRender()
        {
            var getUsrSettingTask = GetUserSetting();
            Client.ExecTask(getUsrSettingTask, x => UserSettingLoaded(x, true));
        }

        private Task<object[][]> GetUserSetting()
        {
            return Client.Instance.SubmitAsync<object[][]>(new XHRWrapper
            {
                Url = Utils.UserSvc,
                Value = new SqlViewModel
                {
                    ComId = "GridView",
                    Action = "GetUserSettingByListViewId",
                    Entity = JSON.Stringify(new { GridId = ParentListView.GuiInfo.Id })
                },
                Method = HttpMethod.POST
            });
        }

        private void UserSettingLoaded(object[][] res, bool render = true)
        {
            _hasLoadSetting = true;
            _userSetting = res[0].Length > 0 ? res[0][0] : null as dynamic;
            if (_userSetting != null)
            {
                var usrHeaders = JSON.Parse(_userSetting.Value as string).As<Component[]>()
                    .ToDictionary(x => x.Id);
                _headers.ForEach(x =>
                {
                    x.IsExport = true;
                    var current = usrHeaders.GetValueOrDefault(x.Id);
                    if (current != null)
                    {
                        x.IsExport = current.IsExport;
                        x.OrderExport = current.OrderExport;
                    }
                });
            }
            _headers = _headers.OrderBy(x => x.OrderExport).ToList();
            if (!render) return;
            var content = this.FindComponentByName<Section>("Content");
            Html.Take(content.Element).Table.ClassName("table");
            _table = Html.Context;
            Html.Instance.Thead
                    .TRow.TData.Text("STT").End
                    .TData.Checkbox(false).Event(EventType.Input, (e) =>
                    {
                        _headers.ForEach(x => x.IsExport = e.GetChecked());
                        RenderDetails();
                    }).End.End
                    .TData.Text("Tên cột").EndOf(ElementType.thead);
            Html.Instance.TBody.Render();
            _tbody = Html.Context;
            RenderDetails();
            Move();
        }

        private void SetChecked(Component item, Event e)
        {
            item.IsExport = e.Target.Cast<HTMLInputElement>().Checked;
            Dirty = true;
        }

        public override void DirtyCheckAndCancel()
        {
            Dirty = true;
            base.DirtyCheckAndCancel();
        }

        private void RenderDetails()
        {
            Html.Take(_tbody).Clear();
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
            _tbody.Children.SelectForeach(y =>
            {
                _headers.FirstOrDefault(x => x.Id == y.GetAttribute("data-id")).OrderExport = j;
                j++;
            });
        }

        public void ExportAll() => Export();
        public void ExportSelected() 
        {
            if (ParentListView.SelectedIds.Nothing()) {
                Toast.Warning("Select at lease 1 row to export");
                return;
            }
            Export(selectedIds: ParentListView.SelectedIds.ToArray());
        }

        public void Export(int? skip = null, int? pageSize = null, string[] selectedIds = null)
        {
            Toast.Success("Đang xuất excel");
            if (_hasLoadSetting)
            {
                Client.ExecTask(UpdateSetting(), res => { Console.Write(res); });
                ExportWithSetting(skip, pageSize, selectedIds);
                return;
            }
            var getUsrSettingTask = GetUserSetting();
            Client.ExecTask(getUsrSettingTask, x =>
            {
                UserSettingLoaded(x, render: false);
                ExportWithSetting(skip, pageSize, selectedIds);
            });
        }

        private void ExportWithSetting(int? skip, int? pageSize, string[] selectedIds)
        {
            var sql = ParentListView.GetSql(skip, pageSize);
            sql.Count = false;
            if (_headers.HasElement())
            {
                sql.FieldName = _headers
                    .Where(x => x.IsExport)
                    .Select(x => x.TextField.IsNullOrWhiteSpace() ? x.FieldName : x.TextField)
                    .ToArray();
                sql.Select = sql.FieldName.HasElement() ? sql.FieldName.Combine() : null;
            }
            if (selectedIds.HasElement())
            {
                var ids = selectedIds.CombineStrings();
                sql.Where = $"Id in ({ids})";
            }
            sql.Entity = ParentListView.GuiInfo.Label ?? ParentListView.GuiInfo.EntityName;
            var pathTask = Client.Instance.SubmitAsync<string>(new XHRWrapper
            {
                Value = JSON.Stringify(sql),
                Url = Utils.ExportExcel,
                IsRawString = true,
                Method = HttpMethod.POST
            });
            Client.ExecTask(pathTask, path =>
            {
                Client.Download($"/excel/Download/{path}");
                Toast.Success("Xuất file thành công");
            });
        }

        private Task<bool> UpdateSetting()
        {
            if (!Dirty) return Task.FromResult(true);
            var tcs = new TaskCompletionSource<bool>();
            PatchUpdate patch;
            if (_userSetting is null)
            {
                _userSetting = new UserSetting()
                {
                    Name = $"Export-{ParentListView.GuiInfo.Id}",
                    UserId = Client.Token.UserId,
                    Value = JsonConvert.SerializeObject(_headers)
                };
                patch = CreatePatch(System.Id.NewGuid());
            }
            else
            {
                _userSetting.Value = JsonConvert.SerializeObject(_headers);
                patch = CreatePatch(_userSetting.Id, _userSetting.Id);
            }
            var task = Client.Instance.PatchAsync(patch);
            Client.ExecTask(task, r =>
            {
                tcs.TrySetResult(r);
            }, e => tcs.TrySetException(e));
            return tcs.Task;
        }

        private PatchUpdate CreatePatch(string newId, string oldId = null)
        {
            var patch = new PatchUpdate
            {
                Changes = new List<PatchUpdateDetail> {
                    new PatchUpdateDetail { Field = nameof(UserSetting.Name), Value = $"Export-{ParentListView.GuiInfo.Id}" },
                    new PatchUpdateDetail { Field = nameof(UserSetting.UserId), Value = Client.Token.UserId },
                    new PatchUpdateDetail { Field = nameof(UserSetting.Value), Value = JsonConvert.SerializeObject(_headers) },
                },
                ComId = ParentListView.GuiInfo.Id,
                Table = nameof(UserSetting),
                ConnKey = ParentListView.GuiInfo.ConnKey ?? Utils.DefaultConnKey
            };
            patch.Changes.Add(new PatchUpdateDetail { Field = IdField, Value = newId, OldVal = oldId });
            return patch;
        }
    }
}