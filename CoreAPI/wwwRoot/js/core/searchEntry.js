import EditableComponent from './editableComponent.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { ValidationRule } from "./models/validationRule.js";
import { LangSelect } from "./utils/langSelect.js";
import EventType from './models/eventType.js';
import ObservableArgs from './models/observable.js';
import { ObservableList } from './models/observableList.js';
import { PositionEnum ,KeyCodeEnum} from './models/enum.js';
import { ComponentExt } from './utils/componentExt.js';
import "./utils/fix.js";
import { ListViewItem } from 'listViewItem.js';
import { GridView } from 'gridView.js';
import { ListView } from 'listView.js';
import { TabEditor } from 'tabEditor.js';
import { MultipleSearchEntry } from 'multipleSearchEntry.js';
import { GroupGridView } from 'groupGridView.js';

export class SearchEntry extends EditableComponent {
    /**
     * @param {import('./models/component.js').Component} ui
     * @param {HTMLElement} [ele=null] 
     */

    constructor(ui, ele = null) {
        super(ui);
        this.DefaultValue = '';
        this.SEntryClass = "search-entry"
        this.DeserializeLocalData(ui);
        this.Meta.ComponentGroup = null;
        this.Meta.Row = this.Meta.Row ?? 50;
        this.RowData = new ObservableList();
        /** @type {HTMLInputElement} */
        this.Element = ele;
            /** @type {HTMLInputElement} */
        this._input = null;
        /** @type {HTMLElement} */
        this._rootResult = null;
        /** @type {HTMLElement} */
        this._parentInput = null;
        /** @type {HTMLElement} */
        this._backdrop = null;
        this._waitForInput = null;
        this._waitForDispose = null;
        this._contextMenu = false;
        this.SearchResultEle = null;
        this._gv = null;

        if (!this.Meta.FieldText) {
            let arr = this.Meta.FieldText.split('.');
            this.Meta.DisplayField = arr[0];
            this.Meta.DisplayDetail = arr[arr.length - 1];
        }
    }

    DeserializeLocalData(ui) {
        if (ui.LocalQuery.trim() !== "") {
            return;
        }
        this.Meta.LocalData = JSON.parse(ui.LocalQuery);
        this.Meta.LocalRender = true;
    }

    Render() {
        this.SetDefaultVal();
        let entityVal = Utils.GetPropValue(this.Entity, this.Name);
        if (typeof entityVal === 'string') {
            this._value = entityVal;
        }
        this.RenderInputAndEvents();
        this.RenderIcons();
        this.FindMatchText();
        // @ts-ignore
        this.SearchResultEle = this.FindClosest(ListView)?.Element ?? document.body;
        this.Element.Closest('td')?.addEventListener('keydown', this.ListViewItemTab.bind(this));
    }

    RenderInputAndEvents() {
        if (this.Element === null) {
            // @ts-ignore
            this.Element = this._input = Html.Take(this.ParentElement).Div.Position(PositionEnum.relative).ClassName(this.SEntryClass).Input.GetContext();
            this._parentInput = this._input.parentElement;
        } else {
            // @ts-ignore
            this._input = this.Element;
            if (!this._input.parentElement.HasClass(this.SEntryClass)) {
                let parent = document.createElement('div');
                parent.classList.add(this.SEntryClass);
                this._input.parentElement.appendChild(parent);
                this._input.parentElement.insertBefore(parent, this._input);
            }
        }
        this._input.autocomplete = 'off';
        Html.Take(this._input).PlaceHolder(this.Meta.PlainText).Attr('name', this.Name)
            .Event('contextmenu', () => this._contextMenu = true)
            .Event('focus', this.FocusIn.bind(this))
            .Event('blur', this.DiposeGvWrapper.bind(this))
            .Event('click', this.SEClickOpenRef.bind(this))
            .Event('change', this.SEChangeHandler.bind(this))
            .Event('keydown', this.SEKeydownHandler.bind(this))
            .Event('input', () => this.Search(this._input.value, true));
    }

    SEClickOpenRef() {
        if (this.Disabled && !this.Meta.FocusSearch) {
            this.OpenRefDetail();
        }
    }

    SEChangeHandler() {
        if (this._value === null) {
            this._input.value = '';
        }
    }

    SEKeydownHandler(e) {
        if (this.Disabled || e === null) {
            return;
        }
        let code = e.KeyCodeEnum();
        switch (code) {
            case KeyCodeEnum.Escape:
                if (this._gv !== null) {
                    e.stopPropagation();
                    this._gv.Show = false;
                }
                break;
            case KeyCodeEnum.UpArrow:
                if (this._gv?.Element !== null && this._gv.Show) {
                    e.stopPropagation();
                    this._gv.MoveUp();
                }
                break;
            case KeyCodeEnum.DownArrow:
                if (this._gv?.Element !== null && this._gv.Show) {
                    e.stopPropagation();
                    this._gv.MoveDown();
                }
                break;
            case KeyCodeEnum.Enter:
                this.EnterKeydownHandler(code);
                break;
            case KeyCodeEnum.F6:
                if (this._gv !== null && this._gv.Show) {
                    e.preventDefault();
                    this._gv.HotKeyF6Handler(e, KeyCodeEnum.F6);
                }
                break;
            default:
                if (e.shiftKey && code === KeyCodeEnum.Delete) {
                    this._input.value = null;
                    this.Search();
                }
                break;
        }
    }

    EnterKeydownHandler(code) {
        if (this.Meta.HideGrid) {
            this.Search(this._input.value, true, 0, true);
            return;
        }
        if (this.EditForm.Feature.CustomNextCell && (this._gv === null || !this._gv.Show)) {
            return;
        }
        if (this._gv !== null && this._gv.Show) {
            this.EnterKeydownTableStillShow(code);
        } else {
            this.Search(null, true, 0);
        }
    }

    EnterKeydownTableStillShow(code) {
        if (this._gv.SelectedIndex >= 0) {
            let row = this._gv?.AllListViewItem.find(x => x.RowNo === this._gv.SelectedIndex).Entity;
            this.EntrySelected(row);
        } else {
            if (this._gv?.RowData !== null && this._gv.RowData.Data.length === 1 && code === KeyCodeEnum.Enter) {
                this.EntrySelected(this._gv?.RowData.Data[0]);
            }
        }
    }

    FocusIn() {
        this.ParentElement.classList.add('cell-selected');
        if (this._contextMenu) {
            this._contextMenu = false;
            return;
        }
        if (this.Disabled || this.Meta.FocusSearch) {
            return;
        }

        this.Search(null, false, 0);
    }

    FocusOut() {
        this.ParentElement.classList.remove('cell-selected');
    }

    Dispose() {
        this.DisposeGv();
        super.Dispose();
    }

    DiposeGvWrapper(e = null) {
        if (e !== null && e['shiftKey'] !== null && e.shiftKey()) {
            return;
        }
        window.clearTimeout(this._waitForDispose);
        this._waitForDispose = window.setTimeout(this.DisposeGv.bind(this), 300);
    }

    DisposeGv() {
        this.DisposeMobileSearchResult();
        if (this._gv !== null) {
            this._gv.Show = false;
        }
        this._parentInput.appendChild(this._input);
    }

    RenderIcons() {
        let title = LangSelect.Get('Tạo mới dữ liệu ');
        Html.Take(this.Element.parentElement).Div.ClassName('search-icons');
        let div = Html.Instance.Icon('fa fa-info-circle').Title(LangSelect.Get('Thông tin chi tiết ') + LangSelect.Get(this.Meta.Label).toLowerCase())
            .Event('click', this.OpenRefDetail.bind(this)).End
            .Icon('fa fa-plus').Title(`${title} ${LangSelect.Get(this.Meta.Label).toLowerCase()}`).Event('click', this.OpenRefDetail.bind(this)).End.GetContext();
        if (this.Element.nextElementSibling !== null) {
            this.Element.parentElement.insertBefore(div, this.Element.nextElementSibling);
        } else {
            this.Element.parentElement.appendChild(div);
        }
    }

    OpenRefDetail() {
        if (!this.Meta.RefClass|| this.Matched === null) {
            return;
        }

        ComponentExt.LoadFeature(this.Meta.RefClass).Done(this.FeatureLoaded.bind(this));
    }

    FeatureLoaded(feature) {
        let instance = new (eval(this.Meta.RefClass))();
        instance.Id = feature.Name;
        instance.ParentForm = this.TabEditor;
        instance.ParentElement = this.TabEditor.Element;
        this.TabEditor.AddChild(instance);
        let res;
        // @ts-ignore
        if (!this.Meta.Template.trim() !== "") {
            if (Utils.IsFunction(this.Meta.Template)) {
                Utils.IsFunction(this.Meta.Template)?.call(null, res, this);
            } else {
                res = Utils.FormatEntity2(this.Meta.Template, null, this.Matched, Utils.EmptyFormat, Utils.EmptyFormat);
            }
            let entity = JSON.parse(res);
            instance.Entity = entity;
        }
        instance.DOMContentLoaded += () => {
            let groupButton = instance.FindComponentByName('Button');
            let htmlTd = document.createElement('td');
            let htmlTr = groupButton.Element.querySelector('tr');
            htmlTr.prepend(htmlTd);
            Html.Take(htmlTd).Button.ClassName('btn btn-secondary').Icon('fal fa-file-check').End.IText('Apply').Event('click', () => {
                instance.IsFormValid().Done(valid => {
                    if (!valid) return;
                    instance.SavePatch().Done(success => {
                        if (success) {
                            this.SaveAndApply(instance.Entity);
                            instance.Dispose();
                        }
                    });
                });
            }).End.Render();
        };
    }

    SaveAndApply(entity) {
        let oldValue = this.Value;
        this.Dirty = true;
        this.Matched = entity;
        if (!(this.Parent instanceof ListViewItem)) {
            this.Value = entity[this.IdField]?.toString();
            this.Dirty = true;
            if (this.UserInput !== null) {
                this.CascadeAndPopulate();
                // @ts-ignore
                this.UserInput.Invoke(new ObservableArgs ({ NewData: this._value, OldData: oldValue, EvType: EventType.Change }));
            }
            return;
        }
        if (this.UserInput !== null) {
            this.CascadeAndPopulate();
            this.DispatchEvent(this.Meta.Events, EventType.Change, this.Entity, this.Parent.Entity, this.Matched);
            // @ts-ignore
            this.UserInput.Invoke(new ObservableArgs ({ NewData: this._value, OldData: oldValue, EvType: EventType.Change }));
        }
    }

    Search(term = null, changeEvent = true, timeout = 500, Delete = false, search = false) {
        if (this.Meta.HideGrid && !search) {
            return;
        }
        window.clearTimeout(this._waitForInput);
        this._waitForInput = window.setTimeout(() => {
            if (this._gv !== null) {
                this._gv.Wheres.Clear();
                this._gv.AdvSearchVM.Conditions.Clear();
                this._gv.CellSelected.Clear();
            }
            if (changeEvent && !this._input.value) {
                this.InputEmptyHandler(Delete);
                return;
            }
            this.TriggerSearch(term);
        }, timeout);
    }

    TriggerSearch(term = null) {
        this.RenderGridView(Utils.DecodeSpecialChar(term));
    }

    RenderGridView(term = null) {
        if (this._isRendering) {
            return;
        }
        this._isRendering = true;
        if (this._gv !== null) {
            this.RenderRootResult();
            this._gv.ParentElement = this._rootResult;
            this._gv.Entity = this.Entity;
            this._gv.ListViewSearch.EntityVM.SearchTerm = term;
            this._gv.RowData.Data = [];
            this._gv.ActionFilter();
            this.GridResultDomLoaded();
            this._isRendering = false;
            return;
        }
        // @ts-ignore
        if (this.Meta.GroupBy.trim() !== ""()) {
            this._gv = new GridView(this.Meta);
        } else {
            this._gv = new GroupGridView(this.Meta);
        }
        this.RenderRootResult();
        this.ParentElement = this._rootResult;
        if (this instanceof MultipleSearchEntry) {
            this._gv.RowData.Data = [];
        }
        this._gv.EditForm = this.EditForm;
        this._gv.Meta = this.Meta;
        this._gv.ParentElement = this._rootResult;
        this._gv.Entity = this.Entity;
        this._gv.Parent = this;
        this._gv.AlwaysValid = true;
        this._gv.PopulateDirty = false;
        this._gv.ShouldSetEntity = false;
        this._gv.DOMContentLoaded = this.GridResultDomLoaded.bind(this);
        this._gv.AddSections();
        this._gv.Show = false;
        this._gv.Render();
        this._gv.Element.classList.add('floating');
        this._gv.RowClick = this.EntrySelected.bind(this);
        this._isRendering = false;
        if (this._gv.Paginator?.Element !== null) {
            this._gv.Paginator.Element.tabIndex = -1;
            this._gv.Paginator.Element.addEventListener('focusin', () => window.clearTimeout(this._waitForDispose));
            this._gv.Paginator.Element.addEventListener('focusout', this.DiposeGvWrapper.bind(this));
        }
        if (this._gv.MainSection?.Element !== null) {
            this._gv.MainSection.Element.tabIndex = -1;
            this._gv.MainSection.Element.addEventListener('focusin', () => window.clearTimeout(this._waitForDispose));
            this._gv.MainSection.Element.addEventListener('focusout', this.DiposeGvWrapper.bind(this));
        }
        if (this._gv.HeaderSection?.Element !== null) {
            this._gv.HeaderSection.Element.tabIndex = -1;
            this._gv.HeaderSection.Element.addEventListener('focusin', () => window.clearTimeout(this._waitForDispose));
            this._gv.HeaderSection.Element.addEventListener('focusout', this.DiposeGvWrapper.bind(this));
        }
        if (this.Meta.LocalHeader === null) {
            this.Meta.LocalHeader = Array.from(this._gv.header.filter(x => x.id != null));
        }
    }

    RenderRootResult() {
        if (this._rootResult !== null) {
            return;
        }
        if (!this.IsSmallUp && this._backdrop === null) {
            Html.Take(TabEditor.TabContainer).Div.ClassName('backdrop');
            this._backdrop = Html.Context;
            Html.Instance.Div.ClassName('popup-content').Style('top: 0;width: 100%;')
            .Div.ClassName('popup-title').Span.IconForSpan('fa fal fa-search').End
            .Span.IText('Search').End.Div.ClassName('icon-box')
            .Span.ClassName('fa fa-times').Event('click', this.DisposeMobileSearchResult.bind(this)).End
            .End.End.Div.ClassName('popup-body scroll-content');
            this._rootResult = Html.Context;
            this._rootResult.appendChild(this._input);
        } else if (this.IsSmallUp) {
            this._rootResult = document.createElement('div');
            this._rootResult.classList.add('result-wrapper');
            this.SearchResultEle.appendChild(this._rootResult);
        }
    }

    DisposeMobileSearchResult() {
        this._parentInput.appendChild(this._input);
        if (this._backdrop !== null) {
            this._backdrop.remove();
            this._backdrop = null;
        }
        if (this._rootResult !== null) {
            this._rootResult.remove();
            this._rootResult = null;
        }
    }

    GridResultDomLoaded() {
        this.FocusBackWithoutEvent();
        this._gv.SelectedIndex = -1;
        this._gv.RowAction(x => {
            if (x instanceof ListViewItem) {
                x.Selected = false;
            }
        });
        this._gv.Element.style.inset = null;
        this.RenderRootResult();
        this._rootResult.appendChild(this._gv.Element);
        if (!this.Meta.HideGrid) {
            this._gv.Show = true;
        }
        if (this.IsSmallUp) {
            ComponentExt.AlterPosition(this._gv.Element, this._input);
        } else {
            this._gv.Element.style.maxWidth = '100%';
            this._gv.Element.style.minWidth = 'calc(100% - 2rem)';
        }
        if (this.Meta.HideGrid) {
            this.EntrySelected(this._gv?.RowData.Data[0]);
        }
        this.FocusBackWithoutEvent();
    }

    FocusBackWithoutEvent() {
        window.clearTimeout(this._waitForDispose);
        window.clearTimeout(this._waitForInput);
        if (!this.Meta.IsPivot) {
            this._input.focus();
        }
    }

    InputEmptyHandler(deleteFlag) {
        let oldValue = this._value;
        let oldMatch = this.Matched;
        this.Matched = null;
        this._value = null;
        this._input.value = '';
        if (oldMatch !== this.Matched) {
            this.Entity?.SetComplexPropValue(this.Name, null);
            this.Dirty = true;
            this.CascadeAndPopulate();
            this.DispatchEvent(this.Meta.Events, EventType.Change, this.Entity, this.Matched, oldMatch).Done();
            // @ts-ignore
            this.UserInput?.Invoke(new ObservableArgs ({ NewData: null, OldData: oldValue, EvType: EventType.Change }));
        }
        if (deleteFlag && !this._input.value) {
            return;
        }
        if (this instanceof MultipleSearchEntry) {
            this._isRendering = false;
        }
        this.TriggerSearch(null);
    }

    FindMatchText() {
        this.ProcessLocalMatch();
    }

    ProcessLocalMatch() {
        if (this.EmptyRow || this.Value === null) {
            this.Matched = null;
            this._input.value = null;
            return true;
        }
        this.Matched = this.Meta.LocalData.HasElement()
            ? this.Meta.LocalData.find(x => x[this.IdField] === this._value)
            : this.RowData.Data.find(x => x[this.IdField] === this.Value);

        this.SetMatchedValue();
        return true;
    }

    CascadeAndPopulate() {
        this.CascadeField();
        this.PopulateFields(this.Matched);
    }

    SetMatchedValue() {
        let origin = null;
        let displayObj = Utils.GetPropValue(this.Entity,this.Meta.DisplayField);
        let isString = typeof displayObj === 'string';
        if (isString && this.Meta.DisplayField !== this.Meta.DisplayDetail) {
            try {
                displayObj = JSON.parse(displayObj);
                origin = Utils.GetPropValue(displayObj,this.Meta.DisplayDetail);
            } catch (e) {}
        } else if (!isString) {
            origin = displayObj;
        }
        Utils.SetPropValue(this.Entity, this.Meta.DisplayField, displayObj);
        this.OriginalText = origin;
        this._input.value = this.EmptyRow ? '' : this.GetMatchedText(this.Matched);
        if (displayObj !== null && typeof displayObj !== 'string') {
            Utils.SetPropValue(displayObj, this.Meta.DisplayDetail, this._input.value);
        }
        this.UpdateValue();
    }

    UpdateValue() {
        if (!this.Dirty) {
            this.OriginalText = this._input.value;
            this.DOMContentLoaded?.invoke();
            this.OldValue = this._value?.toString();
        }
    }

    PatchDetail() {
        let res = [
            {
                Label: this.ComLabel + '(value)',
                Field: this.Name,
                Value: this._value,
                OldVal: this.OldValue
            }
        ];
        if (!this.Meta.FieldText) {
            let display = Utils.GetPropValue(this.Entity, this.Meta.DisplayField) ?? {};
            display[this.Meta.DisplayDetail] = this._input.value;
            res.push({
                Label: this.ComLabel + '(text)',
                Field: this.Meta.DisplayField,
                Value: JSON.stringify(display),
                HistoryValue: this._input.value,
                OldVal: this.OriginalText,
            });
        }
        return res;
    }

    GetMatchedText(matched) {
        if (matched === null && this.Entity === null) {
            return '';
        }
        let res = Utils.GetPropValue(matched, this.Meta.FormatData) ?? Utils.GetPropValue(this.Entity, this.Meta.FieldText);
        return Utils.DecodeSpecialChar((res ?? ''));
    }

    EntrySelected(rowData) {
        window.clearTimeout(this._waitForDispose);
        this.EmptyRow = false;
        if (rowData === null || this.Disabled) {
            return;
        }

        let oldMatch = this.Matched;
        this.Matched = rowData;
        let oldValue = this._value;
        this._value = rowData[this.IdField];
        if (this.Entity !== null && this.Name.HasAnyChar()) {
            this.Entity.SetComplexPropValue(this.Name, this._value);
        }
        this.Dirty = true;
        this.Matched = rowData;
        this.SetMatchedValue();
        if (this._gv !== null) {
            this._gv.Show = false;
        }
        this.CascadeAndPopulate();
        this.DispatchEvent(this.Meta.Events, EventType.Change, this.Entity, rowData, oldMatch).Done(() => {
            // @ts-ignore
            this.UserInput?.Invoke(new ObservableArgs ({ NewData: this._value, OldData: oldValue, EvType: EventType.Change }));
            this.DiposeGvWrapper();
        });
    }

    UpdateView(force = false, dirty = null, ...componentNames) {
        this._value = Utils.GetPropValue(this.Entity, this.Name);
        if (this._value === null) {
            this.Matched = null;
            this._input.value = null;
            this.UpdateValue();
            return;
        }
        let txt = Utils.GetPropValue(this.Entity, this.Meta.FieldText);
        this._input.value = txt;
        this.ProcessLocalMatch();
    }

    async ValidateAsync() {
        if (this.ValidationRules.Nothing()) {
            return true;
        }
        this.ValidationResult.Clear();
        this.ValidateRequired(this._value);
        this.Validate(ValidationRule.Equal, this._value, (value, ruleValue) => value === ruleValue);
        this.Validate(ValidationRule.NotEqual, this._value, (value, ruleValue) => value !== ruleValue);
        return this.IsValid;
    }

    SetDisableUI(value) {
        if (this._input !== null) {
            this._input.readOnly = value;
        }
    }

    RemoveDOM() {
        if (this._input !== null && this._input.parentElement !== null) {
            this._input.parentElement.remove();
        }
    }
}
