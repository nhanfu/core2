using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
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
        public string IdFieldName { get; private set; }

        private const string SEntryClass = "search-entry";
        private int? _value;

        public int? Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    Dirty = true;
                }
                _value = value;
                Entity?.SetComplexPropValue(GuiInfo.FieldName, value);
                Task.Run(async () =>
                {
                    _value = value;
                    await FindMatchTextAsync();
                });
            }
        }
        public string Text { get { return _input.Value; } set { _input.Value = value; } }
        protected HTMLInputElement _input;
        public HTMLElement SearchResultEle { get; private set; }

        public GridView _gv;
        protected int _waitForInput;
        protected int _waitForDispose;
        private int _findMatchTextAwaiter;
        private bool _contextMenu;
        public ObservableList<object> RowData;
        private string dataSourceFilter;
        private HTMLElement _rootResult;
        private HTMLElement _parentInput;
        private HTMLElement _backdrop;

        public string DataSourceFilter { get => dataSourceFilter; set => dataSourceFilter = value.DecodeSpecialChar(); }
        public object Matched { get; set; }

        public SearchEntry(Component ui, HTMLElement ele = null) : base(ui)
        {
            DataSourceFilter = ui.DataSourceFilter;
            DeserializeLocalData(ui);
            GuiInfo.ComponentGroup = null;
            GuiInfo.Row = 20;
            RowData = new ObservableList<object>();
            Element = ele;
            IdFieldName = GuiInfo.FieldName;
        }

        private void DeserializeLocalData(Component ui)
        {
            if (ui.Query.IsNullOrEmpty())
            {
                return;
            }
            GuiInfo.LocalData = JsonConvert.DeserializeObject<List<object>>(ui.Query);
            GuiInfo.LocalRender = true;
        }

        public override void Render()
        {
            SetDefaultVal();
            var entityVal = Entity.GetComplexPropValue(IdFieldName);
            if (entityVal is string str_value)
            {
                _value = str_value.TryParseInt();
            }
            else
            {
                _value = entityVal as int?;
            }
            RenderInputAndEvents();
            RenderIcons();
            FindMatchText();
            SearchResultEle = this.FindClosest<ListView>()?.Element ?? Document.Body;
        }

        private void RenderInputAndEvents()
        {
            if (Element == null)
            {
                Element = _input = Html.Take(ParentElement).Div.Position(Position.relative).ClassName(SEntryClass).Input.GetContext() as HTMLInputElement;
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
            Html.Take(_input).PlaceHolder(GuiInfo.PlainText).Attr("name", IdFieldName)
                .Event(EventType.ContextMenu, () => _contextMenu = true)
                .Event(EventType.Focus, FocusIn)
                .Event(EventType.Blur, DiposeGvWrapper)
                .AsyncEvent(EventType.Click, SEClickOpenRef)
                .Event(EventType.Change, SEChangeHandler)
                .Event(EventType.KeyDown, SEKeydownHanlder)
                .Event(EventType.Input, () => Search(_input.Value));
        }

        private async Task SEClickOpenRef()
        {
            if (Disabled && !GuiInfo.FocusSearch)
            {
                await OpenRefDetail();
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
                case KeyCodeEnum.F4:
                    DiposeGvWrapper();
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
            if (_gv != null && _gv.Show)
            {
                EnterKeydownTableStillShow(code);
            }
            else if (_gv != null && !_gv.Show)
            {
                GridResultDomLoaded();
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
                EntrySelected(_gv?.RowData?.Data[_gv.SelectedIndex]);
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
            if (_contextMenu)
            {
                _contextMenu = false;
                return;
            }
            if (Disabled || GuiInfo.FocusSearch)
            {
                return;
            }

            Search(changeEvent: false, timeout: 0);
        }

        public override void Dispose()
        {
            DisposeGv();
            base.Dispose();
        }

        protected virtual void DiposeGvWrapper()
        {
            if (_gv is null || !_gv.Show)
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
            var div = Html.Instance.Icon("fa fa-info-circle").Title(LangSelect.Get("Thông tin chi tiết ") + LangSelect.Get(GuiInfo.Label).ToLower()).AsyncEvent(EventType.Click, OpenRefDetail).End
                .Icon("fa fa-plus").Title($"{title} {LangSelect.Get(GuiInfo.Label).ToLower()}").AsyncEvent(EventType.Click, CreateNewRef).End.GetContext();
            if (Element.NextElementSibling != null)
            {
                Element.ParentElement.InsertBefore(div, Element.NextElementSibling);
            }
            else
            {
                Element.ParentElement.AppendChild(div);
            }
        }

        private async Task OpenRefDetail()
        {
            if (GuiInfo.RefClass.IsNullOrEmpty() || Matched is null)
            {
                return;
            }

            var feature = await ComponentExt.LoadFeatureByNameOrViewClass(GuiInfo.RefClass);
            if (feature is null)
            {
                return;
            }

            var type = Type.GetType(GuiInfo.RefClass);
            if (type is null)
            {
                return;
            }
            var entity = await new Client(GuiInfo.RefName, GuiInfo.Reference != null ? GuiInfo.Reference.Namespace : null).GetRawAsync(Value.Value);
            var instance = Activator.CreateInstance(type) as TabEditor;
            instance.Id = feature.Name;
            instance.Entity = entity;
            instance.ParentForm = TabEditor;
            instance.ParentElement = TabEditor.Element;
            TabEditor.AddChild(instance);
            await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterCreated, TabEditor);
        }

        private async Task CreateNewRef()
        {
            if (GuiInfo.RefClass.IsNullOrEmpty())
            {
                return;
            }

            var feature = await ComponentExt.LoadFeatureByNameOrViewClass(GuiInfo.RefClass);
            var instance = Activator.CreateInstance(Type.GetType(GuiInfo.RefClass)) as TabEditor;
            instance.Id = feature.Name;
            instance.ParentForm = TabEditor;
            instance.ParentElement = TabEditor.Element;
            TabEditor.AddChild(instance);
            string res;
            if (!GuiInfo.Template.IsNullOrWhiteSpace())
            {
                if (Utils.IsFunction(GuiInfo.Template, out var fn))
                {
                    res = fn.Call(this, Matched, Entity).ToString();
                }
                else
                {
                    res = Utils.FormatEntity(GuiInfo.Template, null, Matched, Utils.EmptyFormat, Utils.EmptyFormat);
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
                Html.Take(htmlTd).Button.ClassName("btn btn-secondary").Icon("fal fa-file-check").End.IText("Áp dụng").Event(EventType.Click, async () =>
                {
                    if (!(await instance.IsFormValid()))
                    {
                        return;
                    }
                    instance.Entity.ClearReferences();
                    var rs = await instance.AddOrUpdate(instance.Entity);
                    if (rs != null)
                    {
                        SaveAndApply(rs);
                        instance.Dispose();
                    }
                }).End.Render();
            };
        }

        private void SaveAndApply(object entity)
        {
            var currentItem = Parent as ListViewItem;
            var oldValue = Value;
            Value = entity[IdField].As<int>();
            Dirty = true;
            Matched = entity;
            if (currentItem is null)
            {
                Value = entity[IdField].As<int>();
                Dirty = true;
                if (UserInput != null)
                {
                    CascadeAndPopulate();
                    if (UserInput != null)
                    {
                        UserInput.Invoke(new ObservableArgs { NewData = _value, OldData = oldValue, EvType = EventType.Change });
                    }
                }
                return;
            }
            if (UserInput != null)
            {
                CascadeAndPopulate();
                Task.Run(async () =>
                {
                    await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity, currentItem.Entity, Matched);
                });
                if (UserInput != null)
                {
                    UserInput.Invoke(new ObservableArgs { NewData = _value, OldData = oldValue, EvType = EventType.Change });
                }
            }
        }

        private void Search(string term = null, bool changeEvent = true, int timeout = 500)
        {
            Window.ClearTimeout(_waitForInput);
            _waitForInput = Window.SetTimeout(() =>
            {
                if (changeEvent && _input.Value.IsNullOrEmpty())
                {
                    InputEmptyHandler();
                    return;
                }
                TriggerSearch(term);
            }, timeout);
        }

        private void TriggerSearch(string term = null)
        {
            if (_input.NextElementSibling != null)
            {
                _input.NextElementSibling.Style.Display = Display.Block;
            }
            RenderGridView(term.DecodeSpecialChar());
        }

        private bool _isRendering;
        public void RenderGridView(string term = null)
        {
            if (_isRendering)
            {
                return;
            }
            _isRendering = true;
            if (_gv != null)
            {
                Task.Run(async () =>
                {
                    RenderRootResult();
                    _gv.ParentElement = _rootResult;
                    _gv.Entity = Entity;
                    _gv.ListViewSearch.EntityVM.SearchTerm = term;
                    _gv.ClearRowData();
                    await _gv.ApplyFilter(true);
                    GridResultDomLoaded();
                    _isRendering = false;
                });
                return;
            }
            if (GuiInfo.GroupBy.IsNullOrWhiteSpace())
            {
                _gv = new GridView(GuiInfo);
            }
            else
            {
                _gv = new GroupGridView(GuiInfo);
            }
            _gv.FeatureId = "null";
            RenderRootResult();
            ParentElement = _rootResult;
            _gv.EditForm = EditForm;
            _gv.GuiInfo = GuiInfo;
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
            }
            if (GuiInfo.LocalHeader is null)
            {
                GuiInfo.LocalHeader = new List<GridPolicy>(_gv.Header.Where(x => x.Id > 0));
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
            _gv.Show = true;
            if (IsSmallUp)
            {
                _gv.Element.AlterPosition(_input);
            }
            else
            {
                _gv.Element.Style.MaxWidth = "100%";
                _gv.Element.Style.MinWidth = "calc(100% - 2rem)";
            }
        }

        private void FocusBackWithoutEvent()
        {
            Window.ClearTimeout(_waitForDispose);
            Window.ClearTimeout(_waitForInput);
            _input.Focus();
        }

        private void InputEmptyHandler()
        {
            var oldValue = _value;
            var oldMatch = Matched;
            Matched = null;
            _value = null;
            _input.Value = string.Empty;
            Entity?.SetComplexPropValue(GuiInfo.FieldName, null);
            Dirty = true;
            CascadeAndPopulate();
            Task.Run(async () =>
            {
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity, Matched, oldMatch);
            });
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = null, OldData = oldValue, EvType = EventType.Change });
            }
            TriggerSearch(null);
        }

        public virtual void FindMatchText(int delay = 0)
        {
            if (delay == 0)
            {
                Task.Run(async () => await FindMatchTextAsync());
                return;
            }
            Window.ClearTimeout(_findMatchTextAwaiter);
            _findMatchTextAwaiter = Window.SetTimeout(async () => await FindMatchTextAsync(), delay);
        }

        protected virtual async Task FindMatchTextAsync(bool force = false)
        {
            if (EmptyRow || !force && ProcessLocalMatch())
            {
                return;
            }

            string query;
            OdataResult<object> list = null;
            if (GuiInfo.DefaultVal?.Trim() == 0.ToString() && Value is null && Entity[IdField].As<int>() <= 0)
            {
                query = FormattedDataSource + "&$top=1";
                list = await new Client(GuiInfo.RefName).GetList<object>(query);
            }
            else if (Value.HasValue)
            {
                var formatted = FormattedDataSource;
                if (formatted.StartsWith("/"))
                {
                    formatted = OdataExt.ApplyClause(formatted, $"Id in ({Value})");
                    list = await new Client(GuiInfo.RefName).GetList<object>(formatted + "&$top=1");
                }
                else
                {
                    list = await new Client(GuiInfo.RefName).LoadById(Value.ToString());
                }
            }
            else if (Value is null)
            {
                Matched = null;
                _input.Value = null;
                return;
            }
            if (list is null || list.Value is null)
            {
                return;
            }

            Matched = list.Value.FirstOrDefault(x => (int)x[IdField] == _value);
            SetMatchedValue();
        }

        protected virtual bool ProcessLocalMatch()
        {
            var isLocalMatched = _gv != null && RowData.Data.HasElement() || GuiInfo.LocalData != null;
            if (isLocalMatched)
            {
                Matched = GuiInfo.LocalData.HasElement() ? GuiInfo.LocalData.FirstOrDefault(x => (int)x[IdField] == _value)
                    : RowData.Data.FirstOrDefault(x => (int)x[IdField] == Value.Value);
            }
            if (isLocalMatched
                || Matched != null && (int?)Matched[IdField] == Value
                || Matched is null && (Value is null || Value == 0))
            {
                SetMatchedValue();
                return true;
            }
            return false;
        }

        protected void CascadeAndPopulate()
        {
            CascadeField();
            PopulateFields(Matched);
        }

        public virtual void SetMatchedValue()
        {
            _input.Value = EmptyRow ? string.Empty : GetMatchedText(Matched);
            if (GuiInfo.AutoFit)
            {
                this.SetAutoWidth(_input.Value, _input.GetComputedStyle().Font, 48);
            }
            if (!Dirty)
            {
                DOMContentLoaded?.Invoke();
                OriginalText = _input.Value;
                OldValue = _value.ToString();
            }
        }

        protected string GetMatchedText(object matched)
        {
            if (matched is null)
            {
                return string.Empty;
            }
            string res;
            if (GuiInfo.FormatEntity.HasNonSpaceChar())
            {
                if (Utils.IsFunction(GuiInfo.FormatEntity, out var fn))
                {
                    res = fn.Call(this, matched, Entity, Element).ToString();
                }
                else
                {
                    res = Utils.FormatEntity(GuiInfo.FormatEntity, null, matched, Utils.EmptyFormat, Utils.EmptyFormat);
                }
            }
            else
            {
                res = matched != null ? Utils.FormatEntity(GuiInfo.FormatData, null, matched, Utils.EmptyFormat, Utils.EmptyFormat) : string.Empty;
            }
            return res.DecodeSpecialChar();
        }

        public string FormattedDataSource
        {
            get
            {
                if (Utils.IsFunction(DataSourceFilter, out Function fn))
                {
                    return fn.Call(this, this, EditForm).ToString();
                }
                var dataSourceFilter = DataSourceFilter.HasAnyChar() ? DataSourceFilter : string.Empty;
                var checkContain = dataSourceFilter.Contains(nameof(EditForm) + ".")
                    || dataSourceFilter.Contains(nameof(TabEditor) + ".")
                    || dataSourceFilter.Contains(nameof(Entity) + ".");
                var dataSource = Utils.FormatEntity(dataSourceFilter, null, checkContain ? this : Entity, notFoundHandler: x => "null");
                return dataSource;
            }
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
            _value = (int)rowData[IdField];
            if (Entity != null && GuiInfo.FieldName.HasAnyChar())
            {
                Entity.SetComplexPropValue(GuiInfo.FieldName, _value);
                Entity.SetComplexPropValue(GuiInfo.FieldName.Substr(0, GuiInfo.FieldName.Length - 2), rowData);
            }
            Dirty = true;
            Matched = rowData;
            SetMatchedValue();
            if (_gv != null)
            {
                _gv.Show = false;
            }
            CascadeAndPopulate();
            Task.Run(async () =>
            {
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity, rowData, oldMatch);
            });
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = _value, OldData = oldValue, EvType = EventType.Change });
            }
            DiposeGvWrapper();
            MoveNextCell();
        }

        private void MoveNextCell()
        {
            if (Parent is ListViewItem parent && parent.GuiInfo.IsRealtime && GuiInfo.FocusSearch)
            {
                var headers = parent.ListViewSection.ListView.Header.Where(x => x.Editable).ToList();
                var currentComponent = headers.FirstOrDefault(x => x.FieldName == GuiInfo.FieldName);
                var index = headers.IndexOf(currentComponent);
                if (headers.Count > index + 1)
                {
                    var nextGrid = headers[index + 1];
                    var nextComponent = parent.FilterChildren<EditableComponent>(x => x.GuiInfo.FieldName == nextGrid.FieldName).FirstOrDefault();
                    nextComponent.Focus();
                }
            }
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            int? updatedValue = null;
            var fieldVal = Entity?.GetComplexPropValue(GuiInfo.FieldName);
            if (fieldVal != null)
            {
                if (fieldVal.GetType().IsNumber())
                {
                    updatedValue = Convert.ToInt32(fieldVal);
                }
            }
            else
            {
                updatedValue = null;
            }
            if (updatedValue == Value && !_input.Value.IsNullOrWhiteSpace())
            {
                return;
            }
            _value = updatedValue;
            if (updatedValue is null)
            {
                Matched = null;
                _input.Value = null;
                return;
            }
            Task.Run(async () => await FindMatchTextAsync(force));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task<bool> ValidateAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (ValidationRules.Nothing())
            {
                return true;
            }
            ValidationResult.Clear();
            ValidateRequired(_value);
            Validate(ValidationRule.Equal, _value, (long? value, long? ruleValue) => value == ruleValue);
            Validate(ValidationRule.NotEqual, _value, (long? value, long? ruleValue) => value != ruleValue);
            return IsValid;
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
