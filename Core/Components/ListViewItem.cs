using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class ListViewItem : Section
    {
        private static List<string> NotCellText = new List<string> { "Button", "Image", "Checkbox" };
        public const string EmptyRowClass = "empty-row";
        public const string SelectedClass = "__selected__";
        public const string FocusedClass = "focus";
        public const string HoveringClass = "hovering";
        public ListViewItem GroupSection { get; set; }
        public PatchUpdate lastpathModel { get; set; }
        public ListViewSection ListViewSection { get; internal set; }
        public ListView ListView { get; internal set; }
        public Function PreQueryFn { get; set; }
        protected bool _selected;
        protected bool _focused;
        private bool _emptyRow;
        public int RowNo { get; set; }
        public virtual bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                SetSelected(value);
                var id = Entity[IdField].As<int>();
                if (value)
                {
                    if (!ListViewSection.ListView.SelectedIds.Contains(id))
                    {
                        ListViewSection.ListView.SelectedIds.Add(id);
                    }
                }
                else
                {
                    ListViewSection.ListView.SelectedIds.Remove(id);
                }
            }
        }

        public virtual bool Focused
        {
            get => _focused;
            set
            {
                _focused = value;
                if (value)
                {
                    Element.AddClass(FocusedClass);
                }
                else
                {
                    Element.RemoveClass(FocusedClass);
                }
                var id = Entity[IdField].As<int>();
                if (value)
                {
                    ListViewSection.ListView.FocusId = id;
                }
                else
                {
                    ListViewSection.ListView.FocusId = null;
                }
            }
        }

        private void SetSelected(bool value)
        {
            if (value)
            {
                Element.AddClass(SelectedClass);
            }
            else
            {
                Element.RemoveClass(SelectedClass);
            }
        }

        public override bool EmptyRow
        {
            get => _emptyRow; set
            {
                _emptyRow = value;
                if (value)
                {
                    Element.AddClass(EmptyRowClass);
                }
                else
                {
                    Element.RemoveClass(EmptyRowClass);
                }
                FilterChildren<EditableComponent>().ForEach(x => x.EmptyRow = value);
                AlwaysValid = value;
            }
        }
        public bool GroupRow { get; internal set; }
        public ListViewItem(ElementType elementType = ElementType.tr) : base(elementType)
        {
            StopChildrenHistory = true;
            DOMContentLoaded += RowItemReady;
        }

        private void RowItemReady()
        {
            var historyBuilder = new StringBuilder();
            BuildTextHistory(historyBuilder);
            OriginalText = historyBuilder.ToString();
        }

        public override void Render()
        {
            ListViewSection = ListViewSection ?? this.FindClosest<ListViewSection>();
            ListView = ListView ?? this.FindClosest<ListView>();
            GuiInfo = ListView.GuiInfo;
            base.Render();
            if (_selected)
            {
                Element.AddClass(SelectedClass);
            }

            Html.Take(Element)
                .AsyncEvent(EventType.Click, RowItemClick)
                .AsyncEvent(EventType.DblClick, RowDblClick)
                .Event(EventType.FocusIn, () =>
                {
                    ListView.AllListViewItem.ForEach(x =>
                    {
                        if (x.Focused)
                        {
                            x.Focused = false;
                        }
                    });
                    Focused = true;
                })
                .Event(EventType.FocusOut, RowFocusOut)
                .Event(EventType.MouseEnter, MouseEnter)
                .Event(EventType.MouseLeave, MouseLeave);
        }

        internal void RenderRowData(List<GridPolicy> headers, object row, int? index = null, bool emptyRow = false)
        {
            if (index.HasValue)
            {
                if (index >= Element.ParentElement.Children.Count() || index < 0)
                {
                    index = 0;
                }

                Element.ParentElement.InsertBefore(Element, Element.ParentElement.Children[index.Value]);
            }
            if (Utils.IsFunction(GuiInfo.FormatEntity, out Function func))
            {
                var formatted = func.Call(this, this)?.ToString();
                Element.InnerHTML = formatted;
                EditForm.BindingTemplate(Element, this);
            }
            else
            {
                headers.Where(x => !x.Hidden).ForEach(header =>
                {
                    RenderTableCell(row, ListView.HeaderComponentMap[header.GetHashCode()], Element);
                });
            }
            var id = row[IdField].As<int?>();
            Dirty = (!id.HasValue || id <= 0) && !emptyRow;
        }

        internal virtual void RenderTableCell(object rowData, Component header, HTMLElement cellWrapper = null)
        {
            if (string.IsNullOrEmpty(header.FieldName))
            {
                return;
            }
            var component = ((header.Editable || NotCellText.Contains(header.ComponentType)) && ListViewSection.ListView.CanWrite)
                ? ComponentFactory.GetComponent(header, EditForm)
                : new CellText(header);
            if (component is CellText cellText)
            {
                cellText.RefData = ListView.RefData;
            }
            if (header.ReferenceId.HasValue)
            {
                var source = header.LocalData ?? (ListView.RefData.Nothing() ? null : ListView.RefData.GetValueOrDefault(header.RefName));
                header.LocalData = source;
            }
            if (component is SearchEntry searchEntry)
            {
                var matched = header.LocalData?.FirstOrDefault(x => (int)x[IdField] == (int?)rowData?.GetComplexPropValue(header.FieldName));
                searchEntry.Matched = matched;
            }
            component.Id = header.Id.ToString();
            component.Name = header.FieldName;
            component.Entity = rowData;
            component.ParentElement = cellWrapper ?? Html.Context;
            AddChild(component);
            if (Disabled || header.Disabled)
            {
                component.SetDisabled(true);
            }

            if (component.Element != null && !header.ChildStyle.IsNullOrWhiteSpace())
            {
                component.Element.Style.CssText = header.ChildStyle;
            }
            component.UserInput += async (arg) =>
            {
                if (arg.EvType == EventType.Change || arg.EvType == EventType.Abort)
                {
                    if (int.Parse(component.Entity[IdField].ToString()) > 0)
                    {
                        await ListView.RowChangeHandler(component.Entity, this, arg, component);
                        await ListView.RealtimeUpdateAsync(this, arg);
                    }
                    else
                    {
                        await ListView.RowChangeHandler(component.Entity, this, arg, component);
                    }
                }
            };
        }

        public bool CompareEx(object obj, object another)
        {
            var fileNames = ListViewSection.ListView.BasicHeaderSearch.Select(x => x.FieldName).ToList();
            var result = false;
            foreach (var property in obj.GetType().GetProperties().Where(x => fileNames.Contains(x.Name)).ToList())
            {
                var objValue = property.GetValue(obj);
                var anotherValue = property.GetValue(another);
                if (objValue == null && anotherValue == null)
                {
                    continue;
                }
                if ((objValue != null && anotherValue == null) || (objValue == null && anotherValue != null) || (objValue != null && anotherValue != null && objValue.ToString() != anotherValue.ToString()))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        public async Task PatchUpdate(bool noEvent = false)
        {
            if (!Dirty)
            {
                return;
            }
            var pathModel = GetPathEntity();
            await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforePatchUpdate, Entity, pathModel, this);
            if (pathModel.Changes.FirstOrDefault(x => x.Field == IdField).Value.IsNullOrWhiteSpace() && !noEvent)
            {
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterPatchUpdate, Entity, pathModel, this);
                return;
            }
            if (pathModel is null && !noEvent)
            {
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterPatchUpdate, Entity, pathModel, this);
                return;
            }
            lastpathModel = pathModel;
            var ignoreSync = "false";
            if (pathModel.Changes.Count == 2)
            {
                var header = ListViewSection.ListView.BasicHeader.FirstOrDefault(x => x.FieldName == pathModel.Changes.FirstOrDefault().Field && x.Editable);
                if (header != null && header.IgnoreSync)
                {
                    ignoreSync = "true";
                }
            }
            var rs = await new Client(GuiInfo.Reference.Name).PatchAsync<object>(pathModel, ig: $"&disableTrigger={ignoreSync}");
            Entity.CopyPropFrom(rs);
            if (GuiInfo.ComponentType == nameof(VirtualGrid))
            {
                ListViewSection.ListView.CacheData.FirstOrDefault(x => x[IdField] == rs[IdField]).CopyPropFrom(rs);
            }
            await ListViewSection.ListView.LoadMasterData(new object[] { rs });
            EmptyRow = false;
            UpdateView(true);
            var changing = BuildTextHistory().ToString();
            if (!changing.IsNullOrWhiteSpace())
            {
                await new Client(nameof(Models.History)).CreateAsync<Models.History>(new Models.History
                {
                    ReasonOfChange = "Auto update",
                    TextHistory = changing.ToString(),
                    RecordId = EntityId,
                    EntityId = Utils.GetEntity(GuiInfo.RefName).Id
                });
            }
            Dirty = false;
            if (rs != null && !noEvent)
            {
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterPatchUpdate, Entity, pathModel, this);
            }
        }

        public PatchUpdate GetPathEntity()
        {
            var details = Children
                .Where(child => child is EditableComponent editable && editable.Dirty)
                .Select(child =>
                {
                    var value = child.Entity.GetComplexPropValue(child.GuiInfo.FieldName);
                    var propType = child.Entity.GetType().GetComplexPropType(child.GuiInfo.FieldName, child.Entity);
                    return new PatchUpdateDetail
                    {
                        Field = child.GuiInfo.FieldName,
                        OldVal = (child.OldValue != null && propType.IsDate()) ? child.OldValue.ToString().DateConverter() : child.OldValue?.ToString(),
                        Value = (value != null && propType.IsDate()) ? value.ToString().DateConverter() : value?.ToString().Trim().EncodeSpecialChar(),
                    };
                }).ToList();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = EntityId.ToString() });
            return new PatchUpdate { Changes = details };
        }

        private async Task RowDblClick(Event e)
        {
            e.StopPropagation();
            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.DblClick, Entity);
        }

        protected virtual async Task RowItemClick(Event e)
        {
            var ctrl = e.CtrlOrMetaKey();
            var shift = e.ShiftKey();
            var target = e.Target as Node;
            var focusing = this.FirstOrDefault(x => x.Element == target || x.ParentElement.Contains(target)) != null;
            HotKeySelectRow(ctrl, shift, focusing);
            ListViewSection.ListView.RowClick?.Invoke(Entity);
            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Click, Entity);
        }

        private void HotKeySelectRow(bool ctrl, bool shift, bool focusing)
        {
            if (EmptyRow)
            {
                return;
            }
            if (ListViewSection.ListView.VirtualScroll)
            {
                if (ctrl || shift)
                {
                    Selected = !_selected;
                    if (_selected)
                    {
                        ListViewSection.ListView.SelectedIndex = ListViewSection.Children.IndexOf(this);
                    }
                }
                if (shift)
                {
                    var allListView = ListViewSection.ListView.AllListViewItem;
                    if (ListViewSection.ListView.LastShiftViewItem is null)
                    {
                        ListViewSection.ListView.LastShiftViewItem = this;
                        ListViewSection.ListView.LastIndex = RowNo;
                    }
                    var _lastIndex = ListViewSection.ListView.LastIndex;
                    var currentIndex = RowNo;
                    if (_lastIndex > currentIndex)
                    {
                        (_lastIndex, currentIndex) = (currentIndex, _lastIndex);
                    }
                    if (ListViewSection.ListView.VirtualScroll && currentIndex > _lastIndex)
                    {
                        var data = ListViewSection.ListView.CalcDatasourse(currentIndex - _lastIndex + 1, _lastIndex - 1, "false");
                        Task.Run(async () =>
                        {
                            var selectedOdataIds = await new Client(GuiInfo.RefName, GuiInfo.Reference?.Namespace).GetList<object>($"{data}&$select=Id", true);
                            var selectedIds = selectedOdataIds.Value.Select(x => x[IdField]).Cast<int>().ToList();
                            if (Selected)
                            {
                                selectedIds.Except(ListViewSection.ListView.SelectedIds).ForEach(ListViewSection.ListView.SelectedIds.Add);
                            }
                            else
                            {
                                selectedIds.ForEach(x => ListViewSection.ListView.SelectedIds.Remove(x));
                            }
                            SetSeletedListViewItem(allListView, _lastIndex, currentIndex);
                            ListViewSection.ListView.LastShiftViewItem = null;
                        });
                    }
                    else
                    {
                        SetSeletedListViewItem(allListView, _lastIndex, currentIndex);
                    }
                }
            }
            else
            {
                if (!ctrl && !shift)
                {
                    if (ListViewSection.ListView.SelectedIds.Count <= 1)
                    {
                        ListViewSection.ListView.ClearSelected();
                        Selected = !_selected;
                        if (_selected)
                        {
                            ListViewSection.ListView.SelectedIndex = ListViewSection.Children.IndexOf(this);
                        }
                    }
                    return;
                }
                Selected = !_selected;

                if (!shift && !ctrl && _selected)
                {
                    ListViewSection.ListView.SelectedIndex = ListViewSection.Children.IndexOf(this);
                }
                if (shift)
                {
                    var allListView = ListViewSection.ListView.AllListViewItem;
                    var selected = allListView.FirstOrDefault(x => x.Selected);
                    var _lastIndex = allListView.IndexOf(x => x == selected);
                    var currentIndex = ListViewSection.Children.IndexOf(this);
                    if (_lastIndex > currentIndex)
                    {
                        (_lastIndex, currentIndex) = (currentIndex, _lastIndex);
                    }
                    for (int i = _lastIndex; i <= currentIndex; i++)
                    {
                        if (ListViewSection.Children[i] is ListViewItem row)
                        {
                            row.Selected = true;
                        }
                    }
                }
            }
        }

        private void SetSeletedListViewItem(IEnumerable<ListViewItem> allListView, int _lastIndex, int currentIndex)
        {
            var start = allListView.FirstOrDefault().RowNo > _lastIndex ? allListView.FirstOrDefault().RowNo : _lastIndex;
            var items = ListViewSection.ListView.AllListViewItem.Where(x => x.RowNo >= start && x.RowNo <= currentIndex).ToList();
            if (!ListViewSection.ListView.VirtualScroll)
            {
                ListViewSection.ListView.SelectedIds = items.Select(x => x.Entity[IdField].As<int>()).As<HashSet<int>>();
            }
            foreach (var item in items)
            {
                var id = int.Parse(item.Entity[IdField].ToString());
                if (ListViewSection.ListView.SelectedIds.Contains(id))
                {
                    item.Selected = Selected;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        private void ToastSelected()
        {
            if (ListViewSection.ListView.SelectedIds.Count % 10 == 0 && ListViewSection.ListView.SelectedIds.Count > 0)
            {
                Toast.Success($"Bạn đã chọn {ListViewSection.ListView.SelectedIds.Count} dữ liệu", 100);
            }
        }

        protected virtual void RowFocusOut()
        {
            Focused = false;
            Task.Run(async () => await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.RowFocusOut, Entity));
        }

        internal void MouseEnter()
        {
            Element.AddClass(HoveringClass);
            Task.Run(async () => await this.DispatchCustomEventAsync(ListViewSection.ListView.GuiInfo.Events, CustomEventType.RowMouseEnter, Entity));
        }

        internal void MouseLeave()
        {
            Element.RemoveClass(HoveringClass);
            Task.Run(async () => await this.DispatchCustomEventAsync(ListViewSection.ListView.GuiInfo.Events, CustomEventType.RowMouseLeave, Entity));
        }

        public override StringBuilder BuildTextHistory(StringBuilder builder = null, HashSet<object> visited = null)
        {
            var buildFromRow = builder is null;
            if (buildFromRow)
            {
                builder = new StringBuilder();
            }
            if (visited is null)
            {
                visited = new HashSet<object>();
            }
            if (visited.Contains(this))
            {
                return builder;
            }
            if (Children.Nothing())
            {
                return builder;
            }
            visited.Add(this);
            if (!buildFromRow)
            {
                builder.Append(Utils.Indent).Append(BasicUpdateText).Append(" dòng ").Append(RowNo + 1).Append(":").Append(Utils.NewLine);
            }
            Children.ForEach(x =>
            {
                if (!buildFromRow)
                {
                    builder.Append(Utils.Indent);
                }
                x.BuildTextHistory(builder);
            });
            return builder;
        }

        public override bool Show { get => base.Show; set => Toggle(value); }
        public const string CmdUrl = "Cmd";
    }

    public class GroupViewItem : ListViewItem
    {
        private const string ChevronDown = "fa-chevron-down";
        private const string ChevronRight = "fa-chevron-right";
        private bool _showChildren;
        public ListViewItem ParentItem { get; set; }
        public List<ListViewItem> ChildrenItems { get; set; }
        public HTMLElement GroupText { get; internal set; }
        public HTMLElement Chevron { get; internal set; }

        public GroupViewItem(ElementType elementType) : base(elementType)
        {
            GroupRow = true;
            ChildrenItems = new List<ListViewItem>();
        }

        public override void Render()
        {
            base.Render();
            Element.AddClass(GroupGridView.GroupRowClass);
        }

        public override bool Selected { get => false; set => _selected = false; }

        public void AppendGroupText(string text)
        {
            if (GroupText is null)
            {
                return;
            }
            GroupText.InnerHTML = GroupText.FirstElementChild.OuterHTML + text;
        }

        public void SetGroupText(string text)
        {
            if (GroupText is null)
            {
                return;
            }

            GroupText.InnerHTML = text;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async Task RowItemClick(Event e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return;
        }

        public bool ShowChildren
        {
            get => _showChildren; set
            {
                _showChildren = value;
                ChildrenItems.ForEach(x => x.Show = value);
                if (value)
                {
                    Chevron.ReplaceClass(ChevronDown, ChevronRight);
                }
                else
                {
                    Chevron.ReplaceClass(ChevronRight, ChevronDown);
                }
            }
        }
    }
}
