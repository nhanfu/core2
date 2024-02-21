using Bridge.Html5;
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
    public class SearchEntry : EditableComponent
    {
        protected string FieldText => Meta.FieldText;
        private const string SEntryClass = "search-entry";
        private string _value;
        private readonly string DisplayField;
        private readonly string DisplayDetail;

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    Dirty = true;
                }
                _value = value;
                Entity?.SetComplexPropValue(FieldName, value);
                FindMatchText();
            }
        }
        public HTMLInputElement _input;
        public HTMLElement SearchResultEle { get; private set; }

        public GridView _gv;
        protected int _waitForInput;
        protected int _waitForDispose;
        private bool _contextMenu;
        public ObservableList<object> RowData;
        private HTMLElement _rootResult;
        private HTMLElement _parentInput;
        private HTMLElement _backdrop;
        public object Matched { get; set; }

        public SearchEntry(Component ui, HTMLElement ele = null) : base(ui)
        {
            DeserializeLocalData(ui);
            Meta.ComponentGroup = null;
            Meta.Row = Meta.Row ?? 50;
            RowData = new ObservableList<object>();
            Element = ele;
            if (Meta.FieldText.HasNonSpaceChar())
            {
                var arr = Meta.FieldText.Split('.');
                Meta.DisplayField = arr[0];
                Meta.DisplayDetail = arr.Last();
            }
        }

        private void DeserializeLocalData(Component ui)
        {
            if (ui.LocalQuery.IsNullOrWhiteSpace())
            {
                return;
            }
            Meta.LocalData = JsonConvert.DeserializeObject<List<object>>(ui.LocalQuery);
            Meta.LocalRender = true;
        }

        public override void Render()
        {
            SetDefaultVal();
            var entityVal = Utils.GetPropValue(Entity, FieldName);
            if (entityVal is string str_value)
            {
                _value = str_value;
            }
            RenderInputAndEvents();
            RenderIcons();
            FindMatchText();
            SearchResultEle = this.FindClosest<ListView>()?.Element ?? Document.Body;
            Element.Closest("td")?.AddEventListener(EventType.KeyDown, ListViewItemTab);
        }

        private void RenderInputAndEvents()
        {
            if (Element == null)
            {
                Element = _input = Html.Take(ParentElement).Div.Position(PositionEnum.relative).ClassName(SEntryClass).Input.GetContext() as HTMLInputElement;
                _parentInput = _input.ParentElement;
            }
            else
            {
                _input = Element as HTMLInputElement;
                if (!_input.ParentElement.HasClass(SEntryClass))
                {
                    var parent = Document.CreateElement(ElementType.div.ToString());
                    parent.AddClass(SEntryClass);
                    _input.ParentElement.AppendChild(parent);
                    _input.ParentElement.InsertBefore(parent, _input);
                }
            }
            _input.AutoComplete = AutoComplete.Off;
            Html.Take(_input).PlaceHolder(Meta.PlainText).Attr("name", FieldName)
                .Event(EventType.ContextMenu, () => _contextMenu = true)
                .Event(EventType.Focus, FocusIn)
                .Event(EventType.Blur, DiposeGvWrapper)
                .Event(EventType.Click, SEClickOpenRef)
                .Event(EventType.Change, SEChangeHandler)
                .Event(EventType.KeyDown, SEKeydownHanlder)
                .Event(EventType.Input, () => Search(_input.Value, delete: true));
        }

        private void SEClickOpenRef()
        {
            if (Disabled && !Meta.FocusSearch)
            {
                OpenRefDetail();
            }
        }

        private void SEChangeHandler()
        {
            if (_value is null)
            {
                _input.Value = string.Empty;
            }
        }

        private void SEKeydownHanlder(Event e)
        {
            if (Disabled || e is null)
            {
                return;
            }
            var code = e.KeyCodeEnum();
            switch (code)
            {
                case KeyCodeEnum.Escape when _gv != null:
                    e.StopPropagation();
                    _gv.Show = false;
                    break;
                case KeyCodeEnum.UpArrow when _gv?.Element != null:
                    if (_gv.Show)
                    {
                        e.StopPropagation();
                        _gv.MoveUp();
                    }
                    break;
                case KeyCodeEnum.DownArrow when _gv?.Element != null:
                    if (_gv.Show)
                    {
                        e.StopPropagation();
                        _gv.MoveDown();
                    }
                    break;
                case KeyCodeEnum.Enter:
                    EnterKeydownHandler(code);
                    break;
                case KeyCodeEnum.F6:
                    if (_gv != null && _gv.Show)
                    {
                        e.PreventDefault();
                        _gv.HotKeyF6Handler(e, KeyCodeEnum.F6);
                    }
                    break;
                default:
                    if (e.ShiftKey() && code == KeyCodeEnum.Delete)
                    {
                        _input.Value = null;
                        Search();
                    }
                    break;
            }
        }

        private void EnterKeydownHandler(KeyCodeEnum? code)
        {
            if (Meta.HideGrid)
            {
                Search(term: _input.Value, timeout: 0, search: true);
                return;
            }
            if (EditForm.Feature.CustomNextCell && (_gv is null || !_gv.Show))
            {
                return;
            }
            if (_gv != null && _gv.Show)
            {
                EnterKeydownTableStillShow(code);
            }
            else
            {
                Search(timeout: 0);
            }
        }

        private void EnterKeydownTableStillShow(KeyCodeEnum? code)
        {
            if (_gv.SelectedIndex >= 0)
            {
                var row = _gv?.AllListViewItem.FirstOrDefault(x => x.RowNo == _gv.SelectedIndex).Entity;
                EntrySelected(row);
            }
            else
            {
                if (_gv?.RowData != null && _gv.RowData.Data.Count == 1 && code == KeyCodeEnum.Enter)
                {
                    EntrySelected(_gv?.RowData.Data[0]);
                }
            }
        }

        private void FocusIn()
        {
            ParentElement.AddClass("cell-selected");
            if (_contextMenu)
            {
                _contextMenu = false;
                return;
            }
            if (Disabled || Meta.FocusSearch)
            {
                return;
            }

            Search(changeEvent: false, timeout: 0);
        }

        private void FocusOut()
        {
            ParentElement.RemoveClass("cell-selected");
        }

        public override void Dispose()
        {
            DisposeGv();
            base.Dispose();
        }

        protected virtual void DiposeGvWrapper(Event e = null)
        {
            if (e != null && e["shiftKey"] != null && e.ShiftKey())
            {
                return;
            }
            Window.ClearTimeout(_waitForDispose);
            _waitForDispose = Window.SetTimeout(DisposeGv, 300);
        }

        private void DisposeGv()
        {
            DisposeMobileSearchResult();
            if (_gv != null)
            {
                _gv.Show = false;
            }
            _parentInput.AppendChild(_input);
        }

        private void RenderIcons()
        {
            var title = LangSelect.Get("Tạo mới dữ liệu ");
            Html.Take(Element.ParentElement).Div.ClassName("search-icons");
            var div = Html.Instance.Icon("fa fa-info-circle").Title(LangSelect.Get("Thông tin chi tiết ") + LangSelect.Get(Meta.Label).ToLower())
                .Event(EventType.Click, OpenRefDetail).End
                .Icon("fa fa-plus").Title($"{title} {LangSelect.Get(Meta.Label).ToLower()}").Event(EventType.Click, OpenRefDetail).End.GetContext();
            if (Element.NextElementSibling != null)
            {
                Element.ParentElement.InsertBefore(div, Element.NextElementSibling);
            }
            else
            {
                Element.ParentElement.AppendChild(div);
            }
        }

        private void OpenRefDetail()
        {
            if (Meta.RefClass.IsNullOrEmpty() || Matched is null)
            {
                return;
            }

            ComponentExt.LoadFeature(ConnKey, Meta.RefClass).Done(FeatureLoaded);
        }

        private void FeatureLoaded(Feature feature)
        {
            var instance = Activator.CreateInstance(Type.GetType(Meta.RefClass)) as TabEditor;
            instance.Id = feature.Name;
            instance.ParentForm = TabEditor;
            instance.ParentElement = TabEditor.Element;
            TabEditor.AddChild(instance);
            string res;
            if (!Meta.Template.IsNullOrWhiteSpace())
            {
                if (Utils.IsFunction(Meta.Template, out var fn))
                {
                    res = fn.Call(this, Matched, Entity).ToString();
                }
                else
                {
                    res = Utils.FormatEntity(Meta.Template, null, Matched, Utils.EmptyFormat, Utils.EmptyFormat);
                }
                var entity = JsonConvert.DeserializeObject<object>(res);
                instance.Entity = entity;
            }
            instance.DOMContentLoaded += () =>
            {
                var groupButton = instance.FindComponentByName<Section>("Button");
                var htmlTd = Document.CreateElement(ElementType.td.ToString());
                var htmlTr = groupButton.Element.QuerySelector(ElementType.tr.ToString()) as HTMLElement;
                htmlTr.Prepend(htmlTd);
                Html.Take(htmlTd).Button.ClassName("btn btn-secondary").Icon("fal fa-file-check").End.IText("Apply").Event(EventType.Click, () =>
                {
                    instance.IsFormValid().Done(valid =>
                    {
                        if (!valid) return;
                        instance.SavePatch().Done(success =>
                        {
                            if (success)
                            {
                                SaveAndApply(instance.Entity);
                                instance.Dispose();
                            }
                        });
                    });
                }).End.Render();
            };
        }

        private void SaveAndApply(object entity)
        {
            var oldValue = Value;
            Dirty = true;
            Matched = entity;
            if (!(Parent is ListViewItem currentItem))
            {
                Value = entity[IdField]?.ToString();
                Dirty = true;
                if (UserInput != null)
                {
                    CascadeAndPopulate();
                    UserInput.Invoke(new ObservableArgs { NewData = _value, OldData = oldValue, EvType = EventType.Change });
                }
                return;
            }
            if (UserInput != null)
            {
                CascadeAndPopulate();
                this.DispatchEvent(Meta.Events, EventType.Change, Entity, currentItem.Entity, Matched).Done();
                UserInput.Invoke(new ObservableArgs { NewData = _value, OldData = oldValue, EvType = EventType.Change });
            }
        }

        private void Search(string term = null, bool changeEvent = true, int timeout = 500, bool delete = false, bool search = false)
        {
            if (Meta.HideGrid && !search)
            {
                return;
            }
            Window.ClearTimeout(_waitForInput);
            _waitForInput = Window.SetTimeout(() =>
            {
                if (_gv != null)
                {
                    _gv.Wheres.Clear();
                    _gv.AdvSearchVM.Conditions.Clear();
                    _gv.CellSelected.Clear();
                }
                if (changeEvent && _input.Value.IsNullOrEmpty())
                {
                    InputEmptyHandler(delete);
                    return;
                }
                TriggerSearch(term);
            }, timeout);
        }

        private void TriggerSearch(string term = null)
        {
            RenderGridView(term.DecodeSpecialChar());
        }

        private bool _isRendering;
        protected string OriginalText;

        public void RenderGridView(string term = null)
        {
            if (_isRendering)
            {
                return;
            }
            _isRendering = true;
            if (_gv != null)
            {
                RenderRootResult();
                _gv.ParentElement = _rootResult;
                _gv.Entity = Entity;
                _gv.ListViewSearch.EntityVM.SearchTerm = term;
                _gv.RowData.Data = new List<object>();
                _gv.ActionFilter();
                GridResultDomLoaded();
                _isRendering = false;
                return;
            }
            if (Meta.GroupBy.IsNullOrWhiteSpace())
            {
                _gv = new GridView(Meta);
            }
            else
            {
                _gv = new GroupGridView(Meta);
            }
            RenderRootResult();
            ParentElement = _rootResult;
            if (this is MultipleSearchEntry)
            {
                _gv.RowData.Data = new List<object>();
            }
            _gv.EditForm = EditForm;
            _gv.Meta = Meta;
            _gv.ParentElement = _rootResult;
            _gv.Entity = Entity;
            _gv.Parent = this;
            _gv.AlwaysValid = true;
            _gv.PopulateDirty = false;
            _gv.ShouldSetEntity = false;
            _gv.DOMContentLoaded = GridResultDomLoaded;
            _gv.AddSections();
            _gv.Show = false;
            _gv.ListViewSearch.EntityVM.SearchTerm = term;
            _gv.Render();
            _gv.Element.AddClass("floating");
            _gv.RowClick = EntrySelected;
            _isRendering = false;
            if (_gv.Paginator?.Element != null)
            {
                _gv.Paginator.Element.TabIndex = -1;
                _gv.Paginator.Element.AddEventListener(EventType.FocusIn, () => Window.ClearTimeout(_waitForDispose));
                _gv.Paginator.Element.AddEventListener(EventType.FocusOut, DiposeGvWrapper);
            }
            if (_gv.MainSection?.Element != null)
            {
                _gv.MainSection.Element.TabIndex = -1;
                _gv.MainSection.Element.AddEventListener(EventType.FocusIn, () => Window.ClearTimeout(_waitForDispose));
                _gv.MainSection.Element.AddEventListener(EventType.FocusOut, DiposeGvWrapper);
            }
            if (_gv.HeaderSection?.Element != null)
            {
                _gv.HeaderSection.Element.TabIndex = -1;
                _gv.HeaderSection.Element.AddEventListener(EventType.FocusIn, () => Window.ClearTimeout(_waitForDispose));
                _gv.HeaderSection.Element.AddEventListener(EventType.FocusOut, DiposeGvWrapper);
            }
            if (Meta.LocalHeader is null)
            {
                Meta.LocalHeader = new List<Component>(_gv.Header.Where(x => x.Id != null));
            }
        }

        private void RenderRootResult()
        {
            if (_rootResult != null)
            {
                return;
            }
            if (!IsSmallUp && _backdrop == null)
            {
                Html.Take(TabEditor.TabContainer).Div.ClassName("backdrop");
                _backdrop = Html.Context;
                Html.Instance.Div.ClassName("popup-content").Style("top: 0;width: 100%;")
                .Div.ClassName("popup-title").Span.IconForSpan("fa fal fa-search").End
                .Span.IText("Search").End.Div.ClassName("icon-box")
                .Span.ClassName("fa fa-times").Event(EventType.Click, DisposeMobileSearchResult).End
                .End.End.Div.ClassName("popup-body scroll-content");
                _rootResult = Html.Context;
                _rootResult.AppendChild(_input);
            }
            else if (IsSmallUp)
            {
                _rootResult = Document.CreateElement(ElementType.div.ToString());
                _rootResult.AddClass("result-wrapper");
                SearchResultEle.AppendChild(_rootResult);
            }
        }

        private void DisposeMobileSearchResult()
        {
            _parentInput.AppendChild(_input);
            _backdrop?.Remove();
            _backdrop = null;
            _rootResult?.Remove();
            _rootResult = null;
        }

        internal virtual void GridResultDomLoaded()
        {
            FocusBackWithoutEvent();
            _gv.SelectedIndex = -1;
            _gv.RowAction(x => x.Selected = false);
            _gv.Element.Style["inset"] = null;
            RenderRootResult();
            _rootResult.AppendChild(_gv.Element);
            if (!Meta.HideGrid)
            {
                _gv.Show = true;
            }
            if (IsSmallUp)
            {
                _gv.Element.AlterPosition(_input);
            }
            else
            {
                _gv.Element.Style.MaxWidth = "100%";
                _gv.Element.Style.MinWidth = "calc(100% - 2rem)";
            }
            if (Meta.HideGrid)
            {
                EntrySelected(_gv?.RowData.Data[0]);
            }
            FocusBackWithoutEvent();
        }

        private void FocusBackWithoutEvent()
        {
            Window.ClearTimeout(_waitForDispose);
            Window.ClearTimeout(_waitForInput);
            if (!Meta.IsPivot)
            {
                _input.Focus();
            }
        }

        private void InputEmptyHandler(bool delete)
        {
            var oldValue = _value;
            var oldMatch = Matched;
            Matched = null;
            _value = null;
            _input.Value = string.Empty;
            if (oldMatch != Matched)
            {
                Entity?.SetComplexPropValue(FieldName, null);
                Dirty = true;
                CascadeAndPopulate();
                this.DispatchEvent(Meta.Events, EventType.Change, Entity, Matched, oldMatch).Done();
                UserInput?.Invoke(new ObservableArgs { NewData = null, OldData = oldValue, EvType = EventType.Change });
            }
            if (delete && _input.Value.IsNullOrEmpty())
            {
                return;
            }
            if (this is MultipleSearchEntry)
            {
                _isRendering = false;
            }
            TriggerSearch(null);
        }

        public virtual void FindMatchText() => ProcessLocalMatch();

        protected virtual bool ProcessLocalMatch()
        {
            if (EmptyRow || Value is null)
            {
                Matched = null;
                _input.Value = null;
                return true;
            }
            Matched = Meta.LocalData.HasElement()
                ? Meta.LocalData.FirstOrDefault(x => x[IdField] as string == _value)
                : RowData.Data.FirstOrDefault(x => x[IdField] as string == Value);

            SetMatchedValue();
            return true;
        }

        protected void CascadeAndPopulate()
        {
            CascadeField();
            PopulateFields(Matched);
        }

        public virtual void SetMatchedValue()
        {
            string origin = null;
            var displayObj = Entity.GetPropValue(Meta.DisplayField);
            var isString = displayObj is string;
            if (isString && Meta.DisplayField != Meta.DisplayDetail)
            {
                displayObj = JSON.Parse(displayObj as string);
                origin = displayObj.GetPropValue(Meta.DisplayDetail) as string;
            }
            else if (!isString)
            {
                origin = displayObj as string;
            }
            Entity.SetPropValue(Meta.DisplayField, displayObj);
            OriginalText = origin;
            _input.Value = EmptyRow ? string.Empty : GetMatchedText(Matched);
            if (displayObj != null && !(displayObj is string))
            {
                displayObj.SetPropValue(Meta.DisplayDetail, _input.Value);
            }
            UpdateValue();
        }

        private void UpdateValue()
        {
            if (!Dirty)
            {
                OriginalText = _input.Value;
                DOMContentLoaded?.Invoke();
                OldValue = _value?.ToString();
            }
        }

        public PatchDetail[] PatchDetail()
        {
            var res = new List<PatchDetail>
            {
                new PatchDetail
                {
                    Label = Label + "(value)",
                    Field = FieldName,
                    Value = _value,
                    OldVal = OldValue
                }
            };
            if (Meta.FieldText.HasNonSpaceChar())
            {
                var display = Entity.GetPropValue(DisplayField) ?? new object();
                display[Meta.DisplayDetail] = _input.Value;
                res.Add(new PatchDetail
                {
                    Label = Label + "(text)",
                    Field = Meta.DisplayField,
                    Value = JSON.Stringify(display),
                    HistoryValue = _input.Value,
                    OldVal = OriginalText,
                });
            }
            return res.ToArray();
        }

        protected string GetMatchedText(object matched)
        {
            if (matched is null && Entity is null)
            {
                return string.Empty;
            }
            var res = matched.GetPropValue(Meta.FormatData) ?? Entity.GetPropValue(Meta.FieldText);
            return (res as string).DecodeSpecialChar();
        }

        protected virtual void EntrySelected(object rowData)
        {
            Window.ClearTimeout(_waitForDispose);
            EmptyRow = false;
            if (rowData is null || Disabled)
            {
                return;
            }

            var oldMatch = Matched;
            Matched = rowData;
            var oldValue = _value;
            _value = rowData[IdField] as string;
            if (Entity != null && FieldName.HasAnyChar())
            {
                Entity.SetComplexPropValue(FieldName, _value);
            }
            Dirty = true;
            Matched = rowData;
            SetMatchedValue();
            if (_gv != null)
            {
                _gv.Show = false;
            }
            CascadeAndPopulate();
            this.DispatchEvent(Meta.Events, EventType.Change, Entity, rowData, oldMatch).Done(() =>
            {
                UserInput?.Invoke(new ObservableArgs { NewData = _value, OldData = oldValue, EvType = EventType.Change });
                DiposeGvWrapper();
            });
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            _value = Entity?.GetPropValue(FieldName) as string;
            if (_value is null)
            {
                Matched = null;
                _input.Value = null;
                UpdateValue();
                return;
            }
            var txt = Entity.GetPropValue(Meta.FieldText) as string;
            _input.Value = txt;
            ProcessLocalMatch();
        }

        public override Task<bool> ValidateAsync()
        {
            if (ValidationRules.Nothing())
            {
                return Task.FromResult(true);
            }
            ValidationResult.Clear();
            ValidateRequired(_value);
            Validate(ValidationRule.Equal, _value, (string value, string ruleValue) => value == ruleValue);
            Validate(ValidationRule.NotEqual, _value, (string value, string ruleValue) => value != ruleValue);
            return Task.FromResult(IsValid);
        }

        protected override void SetDisableUI(bool value)
        {
            if (_input != null)
            {
                _input.ReadOnly = value;
            }
        }

        protected override void RemoveDOM()
        {
            if (_input != null && _input.ParentElement != null)
            {
                _input.ParentElement.Remove();
            }
        }
    }
}