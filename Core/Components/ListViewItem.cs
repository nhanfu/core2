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
    public class ListViewItem : Section
    {
        private readonly static string[] NotCellText = new string[] { "Button", "Image", "Checkbox" };
        public const string EmptyRowClass = "empty-row";
        public const string SelectedClass = "__selected__";
        public const string FocusedClass = "focus";
        public const string HoveringClass = "hovering";
        public ListViewItem GroupSection { get; set; }
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

        public Action<bool> FocusEvent;
        public virtual bool Focused(bool? value = null, bool triggerEvent = true)
        {
            if (value == null) return _focused;
            _focused = value.Value;
            var id = Entity[IdField] as string;
            if (_focused)
            {
                Element.AddClass(FocusedClass);
                ListViewSection.ListView.FocusId = id;
            }
            else
            {
                Element.RemoveClass(FocusedClass);
                ListViewSection.ListView.FocusId = null;
            }
            if (triggerEvent) FocusEvent?.Invoke(_focused);

            return _focused;
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
                FilterChildren<EditableComponent>().SelectForEach(x => x.EmptyRow = value);
                AlwaysValid = value;
            }
        }
        public bool GroupRow { get; internal set; }
        public ListViewItem(ElementType elementType = ElementType.tr) : base(elementType)
        {
        }

        public ListViewItem() : base()
        {
        }

        private int _focusAwaiter;
        protected void SaveEvent()
        {
            AfterSaved += AfterSaveHandler;
            EditForm.AfterSaved += AfterSaveHandler;
            FocusEvent += (focus) =>
            {
                Window.ClearTimeout(_focusAwaiter);
                _focusAwaiter = Window.SetTimeout(() =>
                {
                    if (!focus && Dirty && Meta.IsRealtime) PatchUpdateOrCreate().Done();
                }, 100);
            };
        }

        public override void Render()
        {
            ListViewSection = ListViewSection ?? this.FindClosest<ListViewSection>();
            ListView = ListView ?? this.FindClosest<ListView>();
            Meta = ListView.Meta;
            base.Render();
            if (_selected)
            {
                Element.AddClass(SelectedClass);
            }
            SaveEvent();
        }

        private void AfterSaveHandler(bool success)
        {
            if (!success)
            {
                EntityId = null;
            }
        }

        protected void BindingEvents()
        {
            if (Element is null) return;
            Html.Take(Element)
                .Event(EventType.Click, RowItemClick)
                .Event(EventType.DblClick, RowDblClick)
                .Event(EventType.FocusIn, () =>
                {
                    ListView.AllListViewItem.SelectForEach(x =>
                    {
                        if (x._focused)
                        {
                            x._focused = false;
                        }
                    });
                    _focused = true;
                    FocusEvent?.Invoke(true);
                })
                .Event(EventType.FocusOut, RowFocusOut)
                .Event(EventType.MouseEnter, MouseEnter)
                .Event(EventType.MouseLeave, MouseLeave);
        }

        internal virtual void RenderRowData(List<Component> headers, object row, int? index = null, bool emptyRow = false)
        {
            if (index.HasValue)
            {
                if (index >= Element.ParentElement.Children.Count() || index < 0)
                {
                    index = 0;
                }

                Element.ParentElement.InsertBefore(Element, Element.ParentElement.Children[index.Value]);
            }
            if (Utils.IsFunction(Meta.Renderer, out Function func, shouldAddReturn: false))
            {
                func.Call(this, this, headers);
            }
            else
            {
                headers.Where(x => !x.Hidden).ForEach(header =>
                {
                    RenderTableCell(row, header, Element);
                });
            }
            BindingEvents();
        }

        public List<PatchDetail> PatchModel = new List<PatchDetail>();

        internal virtual void RenderTableCell(object rowData, Component header, HTMLElement cellWrapper = null)
        {
            if (string.IsNullOrEmpty(header.FieldName))
            {
                return;
            }
            var component = ((header.Editable || NotCellText.Contains(header.ComponentType)) && ListViewSection.ListView.CanWrite)
                ? ComponentFactory.GetComponent(header, EditForm)
                : new Label(header);
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
            component.UserInput += (arg) => UserInputHandler(arg, component);
        }

        private void UserInputHandler(ObservableArgs arg, EditableComponent component)
        {
            if (component.Disabled)
            {
                return;
            }
            ListView.RowChangeHandler(component.Entity, this, arg, component).Done();
        }

        public Task<bool> PatchUpdateOrCreate(bool showMessage = true)
        {
            if (!Dirty)
            {
                return Task.FromResult(false);
            }
            var tcs = new TaskCompletionSource<bool>();
            var patchModel = GetPatchEntity();
            this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforePatchUpdate, Entity, patchModel, this)
            .Done(() =>
            {
                ShowMessage = showMessage;
                ValidateAsync().Done(isValid =>
                {
                    if (!IsValid) return;
                    Client.Instance.PatchAsync(patchModel).Done(success =>
                    {
                        PatchUpdateCb(success > 0, patchModel);
                        tcs.TrySetResult(success > 0);
                    });
                });
            });
            return tcs.Task;
        }

        private void PatchUpdateCb(bool success, PatchVM patchModel)
        {
            if (!success)
            {
                Toast.Warning("Save data was not succeded");
            }
            else
            {
                Toast.Success("Save data success");
                EntityId = patchModel.EntityId;
                Dirty = false;
                EmptyRow = false;
            }
            AfterSaved?.Invoke(success);
            this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterPatchUpdate, Entity, patchModel, this).Done();
        }

        public PatchVM GetPatchEntity()
        {
            var shouldGetAll = EntityId is null;
            var dirtyPatch = Children
                .Where(child =>
                {
                    return child is EditableComponent editable && !(child is Button)
                        && (shouldGetAll || editable.Dirty) && child.Meta != null
                        && child.Meta.FieldName.HasNonSpaceChar();
                })
                .SelectMany(child =>
                {
                    if (child[nameof(PatchDetail)] is Func<PatchDetail[]> fn)
                    {
                        return fn.Call(child) as PatchDetail[];
                    }
                    var value = Utils.GetPropValue(child.Entity, child.FieldName);
                    var propType = child.Entity.GetType().GetComplexPropType(child.FieldName, child.Entity);
                    var actValue = string.Empty;
                    switch (child.ComponentType)
                    {
                        case nameof(Datepicker):
                            actValue = value.ToString().DateConverter();
                            break;
                        case nameof(Checkbox):
                            actValue = Convert.ToBoolean(value) ? "1" : "0";
                            break;
                        default:
                            actValue = !EditForm.Feature.IgnoreEncode ? value?.ToString().Trim().EncodeSpecialChar() : value?.ToString().Trim();
                            break;
                    }
                    if (actValue.IsNullOrWhiteSpace())
                    {
                        actValue = null;
                    }
                    var patch = new PatchDetail
                    {
                        Label = child.Label,
                        Field = child.FieldName,
                        OldVal = (child.OldValue != null && propType.IsDate()) ? child.OldValue.ToString().DateConverter() : child.OldValue?.ToString(),
                        Value = actValue,
                    };
                    var listDetail = new PatchDetail[] { patch };
                    return listDetail;
                }).DistinctBy(x => x.Field).ToList();
            if (!ListView.Meta.DefaultVal.IsNullOrWhiteSpace() && Utils.IsFunction(ListView.Meta.DefaultVal, out var fnDetail))
            {
                var dfObj = fnDetail.Call(this, EditForm);
                var patchDetail = JsonConvert.DeserializeObject<PatchDetail>(dfObj.ToString());
                var defaultValue = dirtyPatch.FirstOrDefault(x => x.Field == patchDetail.Field);
                if (defaultValue != null)
                {
                    defaultValue.Value = patchDetail.Value;
                }
                else
                {
                    dirtyPatch.Add(patchDetail);
                }
            }
            AddIdToPatch(dirtyPatch);
            dirtyPatch.ForEach(x =>
            {
                if (x.Value.IsNullOrWhiteSpace())
                {
                    x.Value = null;
                }
            });
            PatchModel.AddRange(dirtyPatch);
            return new PatchVM
            {
                CacheName = CacheName,
                QueueName = QueueName,
                Changes = dirtyPatch,
                Table = ListView.Meta.RefName,
                MetaConn = ListView.MetaConn,
                DataConn = ListView.DataConn,
            };
        }

        private void RowDblClick(Event e)
        {
            e.StopPropagation();
            ListViewSection.ListView.DblClick?.Invoke(Entity);
            this.DispatchEvent(Meta.Events, EventType.DblClick, Entity).Done();
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
            this.DispatchEvent(Meta.Events, EventType.Click, Entity).Done();
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
                        Client.Instance.GetIds(sql).Done(selectedIds =>
                        {
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
                ListViewSection.ListView.SelectedIds = items.Select(x => x.EntityId).ToList();
            }
            foreach (var item in items)
            {
                var id = item.EntityId;
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
            Focused(false);
            Task.Run(async () => await this.DispatchCustomEvent(Meta.Events, CustomEventType.RowFocusOut, Entity));
        }

        internal void MouseEnter()
        {
            Element.AddClass(HoveringClass);
            Task.Run(async () => await this.DispatchCustomEvent(ListViewSection.ListView.Meta.Events, CustomEventType.RowMouseEnter, Entity));
        }

        internal void MouseLeave()
        {
            Element.RemoveClass(HoveringClass);
            Task.Run(async () => await this.DispatchCustomEvent(ListViewSection.ListView.Meta.Events, CustomEventType.RowMouseLeave, Entity));
        }

        public override bool Show { get => base.Show; set => Toggle(value); }
        public bool ShowMessage { get; set; } = true;

        public override Task<bool> ValidateAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            var allValid = FilterChildren(x => x.Children.Nothing(), x => x.AlwaysValid).ForEachAsync(x => x.ValidateAsync());
            allValid.Done((validities) =>
            {
                var res = validities.ToArray();
                var allOk = res.All(x => x.IsValid);
                tcs.TrySetResult(allOk);
                if (!allOk && ShowMessage)
                {
                    var message = validities.Where(x => !x.IsValid)
                        .Combine(x => x.ValidationResult.Values.Combine(Utils.BreakLine), Utils.BreakLine);
                    Toast.Warning(message);
                }
            });
            return tcs.Task;
        }
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
