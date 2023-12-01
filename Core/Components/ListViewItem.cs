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
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class ListViewItem : Section
    {
        private static List<string> NotCellText;
        public const string EmptyRowClass = "empty-row";
        public const string SelectedClass = "__selected__";
        public const string FocusedClass = "focus";
        public const string HoveringClass = "hovering";
        public ListViewItem GroupSection { get; set; }
        public PatchVM lastpathModel { get; set; }
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
                var id = Entity[IdField]?.ToString();
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
                var id = Entity[IdField].As<string>();
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
                FilterChildren<EditableComponent>().SelectForeach(x => x.EmptyRow = value);
                AlwaysValid = value;
            }
        }
        public bool GroupRow { get; internal set; }
        public ListViewItem(ElementType elementType = ElementType.tr) : base(elementType)
        {
            StopChildrenHistory = true;
            NotCellText = new List<string> { "Button", "Image", "Checkbox" };
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
                .Event(EventType.Click, RowItemClick)
                .Event(EventType.DblClick, RowDblClick)
                .Event(EventType.FocusIn, () =>
                {
                    ListView.AllListViewItem.SelectForeach(x =>
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

        internal void RenderRowData(List<Component> headers, object row, int? index = null, bool emptyRow = false)
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
                    RenderTableCell(row, header, Element);
                });
            }
            var id = row[IdField].As<int?>();
            Dirty = (!id.HasValue || id <= 0) && !emptyRow;
        }

        public List<PatchDetail> PatchModel = new List<PatchDetail>();

        private bool CanDo(IEnumerable<FeaturePolicy> gridPolicies, Func<FeaturePolicy, bool> permissionPredicate)
        {
            var grid = gridPolicies.FirstOrDefault(x => x.UserId == Client.Token.UserId);
            if (grid != null)
            {
                return grid.CanWrite;
            }
            else
            {
                var featurePolicy = EditForm.Feature.FeaturePolicy.Where(x => x.EntityId == null).Any(permissionPredicate);
                if (!featurePolicy)
                {
                    return false;
                }
                var Component = gridPolicies.Any();
                if (!Component)
                {
                    return true;
                }
                return gridPolicies.Any(permissionPredicate);
            }
        }

        internal virtual void RenderTableCell(object rowData, Component header, HTMLElement cellWrapper = null)
        {
            if (string.IsNullOrEmpty(header.FieldName))
            {
                return;
            }
            var gridPolicies = EditForm.GetGridPolicies(header.Id, Utils.ComponentId);
            var canWrite = CanDo(gridPolicies, x => x.CanWrite);
            var component = ((header.Editable || NotCellText.Contains(header.ComponentType)) && ListViewSection.ListView.CanWrite && canWrite)
                ? ComponentFactory.GetComponent(header, EditForm)
                : new CellText(header);
            if (component is CellText cellText)
            {
                cellText.RefData = ListView.RefData;
            }
            if (header.ReferenceId.HasAnyChar())
            {
                var source = header.LocalData ?? (ListView.RefData.Nothing() ? null : ListView.RefData.GetValueOrDefault(header.RefName));
                header.LocalData = source;
            }
            if (component is SearchEntry searchEntry && !(component is MultipleSearchEntry))
            {
                var matched = header.LocalData?.FirstOrDefault(x => (string)x[IdField] == rowData?.GetPropValue(header.FieldName)?.ToString());
                searchEntry.Matched = matched;
            }
            component.Id = header.Id;
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
                if (component.ComponentType == "Input" || component.ComponentType == nameof(Textbox) || component.ComponentType == "Textarea")
                {
                    if (arg.EvType == EventType.Abort || arg.EvType == EventType.Change || arg.EvType == EventType.Blur)
                    {
                        if (component.Disabled)
                        {
                            return;
                        }
                        await ListView.RowChangeHandler(component.Entity, this, arg, component);
                        ListView.RealtimeUpdate(this, arg);
                    }
                }
                else
                {
                    await ListView.RowChangeHandler(component.Entity, this, arg, component);
                    if (arg.EvType == EventType.Change)
                    {
                        if (component.Disabled)
                        {
                            return;
                        }
                        ListView.RealtimeUpdate(this, arg);
                    }
                }
            };
        }

        public void PatchUpdateOrCreate()
        {
            if (!Dirty)
            {
                return;
            }
            var patchModel = GetPatchEntity();
            Client.ExecTaskNoResult(this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforePatchUpdate, Entity, patchModel, this), () =>
            {
                lastpathModel = patchModel;
                Client.ExecTask(Client.Instance.PatchAsync(patchModel), success =>
                {
                    PatchUpdateCb(success, patchModel);
                });
            });
        }

        private void PatchUpdateCb(bool success, PatchVM patchModel)
        {
            if (!success)
            {
                Toast.Warning("Save data was not succeded");
            }
            else
            {
                EntityId = patchModel.EntityId;
                Dirty = false;
                EmptyRow = false;
            }
            Client.ExecTaskNoResult(this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterPatchUpdate, Entity, patchModel, this));
        }

        public PatchVM GetPatchEntity()
        {
            var shouldGetAll = EntityId is null;
            var dirtyPatch = Children
                .Where(child => child is EditableComponent editable && (shouldGetAll || editable.Dirty))
                .SelectMany(child =>
                {
                    if (child[nameof(PatchDetail)] is Func<PatchDetail[]> fn)
                    {
                        return fn.Call(child) as PatchDetail[];
                    }
                    var value = Utils.GetPropValue(child.Entity, child.FieldName);
                    var propType = child.Entity.GetType().GetComplexPropType(child.FieldName, child.Entity);
                    var patch = new PatchDetail
                    {
                        Label = child.Label,
                        Field = child.FieldName,
                        OldVal = (child.OldValue != null && propType.IsDate()) ? child.OldValue.ToString().DateConverter() : child.OldValue?.ToString(),
                        Value = (value != null && propType.IsDate()) ? value.ToString().DateConverter() : !EditForm.Feature.IgnoreEncode ? value?.ToString().Trim().EncodeSpecialChar() : value?.ToString().Trim(),
                    };
                    return new PatchDetail[] { patch };
                }).ToList();
            AddIdToPatch(dirtyPatch);
            PatchModel.AddRange(dirtyPatch);
            return new PatchVM
            {
                Changes = dirtyPatch,
                Table = ListView.GuiInfo.RefName,
                ConnKey = ListView.GuiInfo.ConnKey ?? Utils.DefaultConnKey,
            };
        }

        private void RowDblClick(Event e)
        {
            e.StopPropagation();
            ListViewSection.ListView.DblClick?.Invoke(Entity);
            Client.ExecTaskNoResult(this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.DblClick, Entity));
        }

        protected virtual void RowItemClick(Event e)
        {
            e.StopPropagation();
            var ctrl = e.CtrlOrMetaKey();
            var shift = e.ShiftKey();
            var target = e.Target as Node;
            var focusing = this.FirstOrDefault(x => x.Element == target || x.ParentElement.Contains(target)) != null;
            HotKeySelectRow(ctrl, shift, focusing);
            if (!e.ShiftKey())
            {
                ListViewSection.ListView.RowClick?.Invoke(Entity);
            }
            ListViewSection.ListView.LastListViewItem = this;
            Client.ExecTaskNoResult(this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Click, Entity));
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
                        var sql = ListView.GetSql(_lastIndex - 1, currentIndex - _lastIndex + 1, true);
                        Client.ExecTask(Client.Instance.GetIds(sql), selectedIds => {
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
                ListViewSection.ListView.SelectedIds = new HashSet<string>(items.Select(x => x.Entity[IdField]?.ToString()));
            }
            foreach (var item in items)
            {
                var id = item.Entity[IdField].ToString();
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

        protected virtual void RowFocusOut()
        {
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

        public bool ShowChildren
        {
            get => _showChildren; set
            {
                _showChildren = value;
                ChildrenItems.ForEach(x => x.Show = value);
                if (value)
                {
                    Chevron.ReplaceClass(ChevronRight, ChevronDown);
                }
                else
                {
                    Chevron.ReplaceClass(ChevronDown, ChevronRight);
                }
            }
        }
    }
}
