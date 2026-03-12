import { EditableComponent } from './editableComponent.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { positionEnum, keyCodeEnum, ObservableList, Component, EventType, ValidationRule } from "./models/";
import { LangSelect } from "./utils/langSelect.js";
import { ComponentExt } from './utils/componentExt.js';
import { GridView } from './gridView.js';
import { Client } from './clients/client.js';
import { Toast } from './toast.js';

export class SearchEntry extends EditableComponent {
    isSearchEntry = true;
    isMultiple = false;
    /**
     * Create instance of component
     * @param {Component | null} ui 
     * @param {HTMLElement | null} ele 
     */
    constructor(ui, ele = null) {
        super(ui);
        this.defaultValue = '';
        this.sEntryClass = "search-entry"
        this.meta.componentGroup = null;
        this.meta.row = this.meta.row ?? 50;
        this.rowData = new ObservableList();
        /** @type {hTMLInputElement} */
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
        this.searchResultEle = null;
        this._gv = null;
        let containId = Utils.isNullOrWhiteSpace(this.meta.tabGroup) ? this.meta.fieldName.substr(this.meta.fieldName.length - 2) === this.idField : this.meta.tabGroup.substr(this.meta.tabGroup.length - 2) === this.idField;
        if (containId) {
            this.displayField = Utils.isNullOrWhiteSpace(this.meta.tabGroup) ? this.meta.fieldName.substr(0, this.meta.fieldName.length - 2) : this.meta.tabGroup.substr(0, this.meta.tabGroup.length - 2);
        }
        else {
            this.displayField = this.meta.fieldName + "masterData";
        }
        if (this.meta.fieldName == "currencyId") {
            this.isCurrency = true;
        }
    }

    Render() {
        this.setDefaultVal();
        this._value = this.entity[this.Name];
        this.renderInputAndEvents();
        if (this.meta.Events && this.meta.Events.includes('"add"')) {
            this.renderIcons();
        }
        this.findMatchText();
        this.searchResultEle = document.body;
    }

    renderInputAndEvents() {
        if (this.parentElement.tagName == "INPUT") {
            this.element = this._input = this.parentElement;
        }
        if (this.element == null) {
            this.element = this._input = Html.take(this.parentElement).div.position(positionEnum.relative).tabIndex(-1).className(this.sEntryClass).input.getContext();
            this._parentInput = this._input.parentElement;
        }
        else {
            this._input = this.element;
            if (!this._input.parentElement.classList.includes(this.sEntryClass)) {
                var parent = document.createElement("div");
                parent.classList.add(this.sEntryClass);
                this._input.parentElement.appendChild(parent);
                this._input.parentElement.insertBefore(parent, this._input);
            }
        }
        this._input.autocomplete = "off";
        Html.take(this._input).placeHolder(this.meta.plainText || "Select data")
            .event(EventType.contextMenu, () => this._contextMenu = true)
            .event(EventType.Focus, this.focusIn.bind(this))
            .event(EventType.focusOut, this.diposeGvWrapper.bind(this))
            .event(EventType.keyDown, this.sEKeydownHandler.bind(this))
            .event(EventType.Input, () => this.Search(this._input.value, true, null, true, false));
    }

    sEKeydownHandler(e) {
        if (this.disabled || e === null) {
            return;
        }
        let code = e.keyCodeEnum();
        switch (code) {
            case keyCodeEnum.escape:
                if (this._gv && this._gv.element !== null) {
                    e.stopPropagation();
                    this._gv.Show = false;
                }
                break;
            case keyCodeEnum.upArrow:
                if (this._gv && this._gv.element !== null && this._gv.Show) {
                    e.stopPropagation();
                    this._gv.moveUp();
                }
                break;
            case keyCodeEnum.downArrow:
                if (this._gv && this._gv.element !== null && this._gv.Show) {
                    e.stopPropagation();
                    this._gv.moveDown();
                }
                break;
            case keyCodeEnum.enter:
                this.enterKeydownHandler(code);
                break;
            case keyCodeEnum.F6:
                if (this._gv && this._gv.element !== null && this._gv.Show) {
                    e.preventDefault();
                    this._gv.hotKeyF6Handler(e, keyCodeEnum.F6);
                }
                break;
            default:
                if (e.shiftKey && code === keyCodeEnum.delete) {
                    this._input.value = null;
                    this.Search();
                }
                break;
        }
    }

    enterKeydownHandler(code) {
        if (this._gv !== null && this._gv.Show) {
            this.enterKeydownTableStillShow(code);
        } else {
            this.Search(null, false, 0);
        }
    }

    enterKeydownTableStillShow(code) {
        if (this._gv.selectedIndex > -1) {
            let row = this._gv.allListViewItem[this._gv.selectedIndex].entity;
            this.entrySelected(row);
        } else {
            if (this._gv.allListViewItem && this._gv.allListViewItem.length === 1 && code === keyCodeEnum.enter) {
                this.entrySelected(this._gv.allListViewItem[0].entity);
            }
        }
    }

    focusIn() {
        this.parentElement.classList.add('cell-selected');
        if (this._contextMenu) {
            this._contextMenu = false;
            return;
        }
        if (this.disabled || this.meta.focusSearch) {
            return;
        }
        window.clearTimeout(this._waitForInput);
        this._waitForInput = window.setTimeout(() => {
            this.triggerSearch(null);
        }, 100);
    }

    focusOut() {
        this.parentElement.classList.remove('cell-selected');
    }

    Dispose() {
        if (this._gv !== null) {
            this._gv.Dispose();
        }
        if (this._rootResult !== null) {
            this._rootResult.remove();
            this._rootResult = null;
        }
        super.Dispose();
    }

    diposeGvWrapper(e = null) {
        window.clearTimeout(this._waitForDispose);
        this._waitForDispose = window.setTimeout(this.disposeGv.bind(this), 50);
    }

    disposeGv() {

        if (this._gv !== null) {
            this._gv.Show = false;
        }
        this._parentInput.appendChild(this._input);
    }

    renderIcons() {
        let title = LangSelect.get('Create new data');
        Html.take(this.element.parentElement).div.className('search-icons');
        let div = Html.instance.icon('fa fa-plus').title(`${title} ${LangSelect.get(this.meta.Label)}`).event('click', this.openRefAdd.bind(this)).end.getContext();
        if (this.element.nextElementSibling !== null) {
            this.element.parentElement.insertBefore(div, this.element.nextElementSibling);
        } else {
            this.element.parentElement.appendChild(div);
        }
    }

    openRefDetail() {
        if (Utils.isNullOrWhiteSpace(this.meta.Events)) {
            return;
        }
        this.dispatchCustomEvent(this.meta.Events, "edit", this).then();
    }

    openRefAdd() {
        if (Utils.isNullOrWhiteSpace(this.meta.Events)) {
            return;
        }
        this.dispatchCustomEvent(this.meta.Events, "add", this).then();
    }

    Search(term = null, changeEvent = true, timeout = 500, Delete = false, search = false) {
        if (!Utils.isNullOrWhiteSpace(this.meta.tabGroup)) {
            if (this._input.value != this.originalText) {
                this._value = this._input.value;
                this.entity[this.meta.fieldName] = this._input.value;
                this.entity[this.meta.tabGroup] = null;
                this.entity[this.displayField] = null;
                this.Matched = null;
                this.Dirty = true;
                if (this.isCurrency) {
                    this.entity.exchangeRateVND = null;
                    this.entity.exchangeRateUSD = null;
                    this.entity.currencyCode = null;
                    if (this.Parent.isListViewItem && this.Dirty) {
                        this.Parent.updateView(false, false, "exchangeRateVND", "exchangeRateUSD");
                    }
                }
            }
        }
        if (this.meta.hideGrid && !search) {
            return;
        }
        window.clearTimeout(this._waitForInput);
        this._waitForInput = window.setTimeout(() => {
            if (this._gv !== null) {
                this._gv.Wheres = [];
                this._gv.advSearchVM.Conditions = [];
                this._gv.cellSelected = [];
            }
            if (changeEvent && !this._input.value) {
                this.inputEmptyHandler();
                return;
            }
            var term2 = this._input.value;
            this.triggerSearch(term2);
        }, 100);
    }

    triggerSearch(term = null) {
        this.renderGridView(term);
    }

    async renderGridView(term = null) {
        if (this._isRendering) {
            return;
        }
        this._isRendering = true;
        if (this._gv !== null) {
            this.renderRootResult();
            this._gv.parentElement = this._rootResult;
            this._gv.entity = this.entity;
            this._gv.listViewSearch.entityVM.searchTerm = term;
            this._gv.rowData.Data = [];
            this._gv.actionFilter();
            this._isRendering = false;
            return;
        }
        /**
             * @type {Component}
             */
        var newMeta = JSON.parse(JSON.stringify(this.meta));
        newMeta.disabledExp = null;
        newMeta.showExp = null;
        this._gv = new GridView(newMeta);
        newMeta.virtualScroll = true;
        this._gv.meta = newMeta;
        this.renderRootResult();
        this._gv.meta = newMeta;
        this.parentElement = this._rootResult;
        this._gv.editForm = this.editForm;
        this._gv.parentElement = this._rootResult;
        this._gv.entity = this.entity;
        this._gv.Parent = this;
        this._gv.alwaysValid = true;
        this._gv.populateDirty = false;
        this._gv.shouldSetEntity = false;
        this._gv.dOMContentLoaded.add(this.gridResultDomLoaded.bind(this));
        this._gv.addSections();
        this._gv.listViewSearch.entityVM.searchTerm = term;
        this._gv.rowData.Data = [];
        this._gv.render();
        this._gv.Show = false;
        this._gv.element.classList.add('floating');
        this._gv.rowClick.add(this.entrySelected.bind(this));
        this._isRendering = false;
        if (this._gv.paginator && this._gv.paginator?.element !== null) {
            this._gv.paginator.element.tabIndex = -1;
            this._gv.paginator.element.addEventListener('focusin', () => window.clearTimeout(this._waitForDispose));
            this._gv.paginator.element.addEventListener('focusout', this.diposeGvWrapper.bind(this));
        }
        if (this._gv.mainSection && this._gv.mainSection?.element !== null) {
            this._gv.mainSection.element.tabIndex = -1;
            this._gv.mainSection.element.addEventListener('focusin', () => window.clearTimeout(this._waitForDispose));
            this._gv.mainSection.element.addEventListener('focusout', this.diposeGvWrapper.bind(this));
        }
        if (this._gv.headerSection && this._gv.headerSection?.element !== null) {
            this._gv.headerSection.element.tabIndex = -1;
            this._gv.headerSection.element.addEventListener('focusin', () => window.clearTimeout(this._waitForDispose));
            this._gv.headerSection.element.addEventListener('focusout', this.diposeGvWrapper.bind(this));
        }
        if (this.meta.localHeader === null) {
            this.meta.localHeader = Array.from(this._gv.header.filter(x => x.id != null));
        }
        var crollElement = this.element.closest(".scroll-content");
        if (crollElement != null) {
            crollElement.addEventListener(EventType.Scroll, this.alterPositionGV.bind(this));
        }
    }

    renderRootResult() {
        if (this._rootResult !== null) {
            return;
        }
        this._rootResult = document.createElement('div');
        this._rootResult.classList.add('result-wrapper');
        this.searchResultEle.appendChild(this._rootResult);
    }

    async gridResultDomLoaded() {
        this.focusBackWithoutEvent();
        this._gv.selectedIndex = -1;
        this._gv.rowAction(x => {
            x.Selected = false;
        });
        this._gv.element.style.inset = null;
        this.renderRootResult();
        this._rootResult.appendChild(this._gv.element);
        if (!this.meta.hideGrid) {
            this._gv.Show = true;
        }
        if (this.meta.hideGrid) {
            this.entrySelected(this._gv?.rowData.Data[0]);
        }
        this.focusBackWithoutEvent();
        this.alterPositionGV();
    }

    alterPositionGV() {
        ComponentExt.alterPosition(this._gv.element, this._input);
    }

    focusBackWithoutEvent() {
        window.clearTimeout(this._waitForDispose);
        window.clearTimeout(this._waitForInput);
        if (!this.meta.isPivot) {
            this._input.focus();
        }
    }

    inputEmptyHandler() {
        let oldValue = this._value;
        let oldMatch = this.Matched;
        this.Matched = null;
        this.entity[this.displayField] = null;
        this.entity[this.Name + "Text"] = null;
        this._value = null;
        this._input.value = '';
        this.Dirty = true;
        if (oldMatch !== this.Matched) {
            this.entity[this.Name] = null;
            if (!Utils.isNullOrWhiteSpace(this.meta.tabGroup)) {
                this.entity[this.meta.tabGroup] = null;
            }
            if (this.isCurrency) {
                this.entity.exchangeRateVND = null;
                this.entity.exchangeRateUSD = null;
                this.entity.currencyCode = null;
                this.Parent.updateView(false, false, "exchangeRateVND", "exchangeRateUSD");
            }
            this.populateFields(this.Matched);
            this.dispatchEvent(this.meta.Events, EventType.Change, this, this.entity, this.Matched, oldMatch).then();
            // @ts-ignore
            this.userInput?.invoke({ newData: this._value, oldData: oldValue, evType: EventType.Change });
        }
        this.triggerSearch(null);
    }

    _findMatchTextAwaiter;

    findMatchText() {
        if (!Utils.isNullOrWhiteSpace(this.meta.tabGroup)) {
            if (this.entity[this.meta.tabGroup]) {
                if (Utils.isNullOrWhiteSpace(this.meta.refName)) {
                    window.setTimeout(() => {
                        var data = Utils.isFunction(this.meta.Query, false, this);
                        this.Matched = data.find(x => {
                            const xId = x?.Id != null ? x.Id.toString() : null;
                            const entityValue = this.entity?.[this.meta.fieldName] != null ? this.entity[this.meta.fieldName].toString() : null;
                            return xId === entityValue;
                        });
                        this.setMatchedValue();
                    }, 500);
                }
                else {
                    Client.instance.getByIdAsync(this.meta.refName, [this.entity[this.meta.tabGroup]]).then(data => {
                        this.Matched = data.data ? data.data[0] : null;
                        this.setMatchedValue();
                    })
                }
            }
            else {
                this._input.value = this.entity[this.meta.fieldName] || '';
                this.setMatchedValue();
            }
            return;
        }
        if (Utils.isNullOrWhiteSpace(this.meta.refName)) {
            window.setTimeout(() => {
                var data = Utils.isFunction(this.meta.Query, false, this);
                this.Matched = data.find(x => {
                    const xId = x?.Id != null ? x.Id.toString() : null;
                    const entityValue = this.entity?.[this.meta.fieldName] != null ? this.entity[this.meta.fieldName].toString() : null;
                    return xId === entityValue;
                });
                this.setMatchedValue();
            }, 500);
        }
        else {
            this.Matched = this.entity[this.displayField] || null;
            if ((this._value && this.Matched && this.Matched.Id != this._value) || (!this.Matched && this._value)) {
                Client.instance.getByIdAsync(this.meta.refName, [this._value]).then(data => {
                    this.Matched = data.data ? data.data[0] : null;
                    this.setMatchedValue();
                    if (this.isCurrency) {
                        var code = this.getMatchedText(this.Matched);
                        this.entity.currencyCode = code;
                        if (this._value != this.Matched.Id || this.entityId.startsWith("-")) {
                            this.entity.exchangeRateVND = EditableComponent.exchangeRateVND[code];
                            this.entity.exchangeRateUSD = EditableComponent.exchangeRateUSD[code];
                        }
                        if (this.Parent.isListViewItem && this.Dirty) {
                            this.Parent.updateView(false, false, "exchangeRateVND", "exchangeRateUSD");
                        }
                    }
                });
            }
            else {
                this.setMatchedValue();
            }
        }
    }

    setMatchedValue() {
        if (!Utils.isNullOrWhiteSpace(this.meta.tabGroup)) {
            this._input.value = this.Matched ? this.getMatchedText(this.Matched) : (this.entity[this.meta.fieldName] || '');
            this.entity[this.meta.tabGroup] = this.Matched ? this.Matched[this.idField] : null;
            this.entity[this.Name] = this._input.value;
        }
        else {
            this._input.value = this.emptyRow ? "" : this.getMatchedText(this.Matched);
            this.entity[this.Name + "Text"] = this._input.value;
        }
        if (this.isCurrency) {
            this.entity.currencyCode = this._input.value;
            this.entity.exchangeRateINV2 = EditableComponent.exchangeRateUSD[this._input.value];
            if (!this.entity.exchangeRateVND) {
                this.entity.exchangeRateVND = EditableComponent.exchangeRateVND[this._input.value];
                this.entity.exchangeRateUSD = EditableComponent.exchangeRateUSD[this._input.value];
            }
        }
        this.updateValue();
    }

    updateValue() {
        if (!this.Dirty) {
            this.originalText = this._input.value;
            this.dOMContentLoaded?.invoke();
            this.oldValue = this._value?.toString();
        }
    }

    getMatchedText(matched) {
        if (matched === null && this.entity === null || !matched) {
            this.entity[this.displayField] = null;
            return '';
        }
        this.entity[this.displayField] = matched;
        let res = Utils.formatEntity(this.meta.formatData, matched);
        return res || "";
    }

    actEntrySelected(rowData) {
        window.clearTimeout(this._waitForDispose);
        this.emptyRow = false;
        if (rowData === null || this.disabled) {
            return;
        }
        if ((!this._value && rowData) || (this._value !== rowData.Id)) {
            if (this.isCurrency) {
                var code = this.getMatchedText(rowData);
                this.entity.exchangeRateVND = EditableComponent.exchangeRateVND[code];
                this.entity.exchangeRateUSD = EditableComponent.exchangeRateUSD[code];
                this.entity.currencyCode = code;
            }
        }
        let oldMatch = this.Matched;
        this.Matched = rowData;
        let oldValue = this._value;
        if (!Utils.isNullOrWhiteSpace(this.meta.tabGroup)) {
            this._value = rowData[this.idField];
            this.entity[this.meta.tabGroup] = rowData[this.idField];
        }
        else {
            this._value = rowData[this.idField];
        }
        if (this.entity !== null && this.Name) {
            if (Utils.isNullOrWhiteSpace(this.meta.tabGroup)) {
                this.entity[this.Name] = this._value;
            }
            else {
                this.entity[this.meta.fieldName] = this.getMatchedText(this.Matched);
                this.entity[this.meta.tabGroup] = rowData[this.idField];
            }
        }
        this.Dirty = true;
        this.Matched = rowData;
        this.setMatchedValue();
        if (this._gv !== null) {
            this._gv.Show = false;
        }
        this.populateFields(this.Matched);
        this.dispatchEvent(this.meta.Events, EventType.Change, this, this.entity, rowData, oldMatch).then(() => {
            // @ts-ignore
            this.userInput?.invoke({ newData: this._value, oldData: oldValue, evType: EventType.Change });
            this.diposeGvWrapper();
        });
        if (this.Parent.isListViewItem && this.Dirty) {
            window.setTimeout(() => {
                if (this.isCurrency && this.Dirty) {
                    this.Parent.updateView(false, false, "exchangeRateVND", "exchangeRateUSD");
                }
                this._input.focus();
            }, 200);
        }
        else {
            if (!Utils.isNullOrWhiteSpace(this.meta.groupBy)) {
                var groups = this.editForm.childCom.filter(x => x.meta.groupBy == this.meta.groupBy);
                var index = groups.indexOf(this);
                if (groups[index + 1]) {
                    groups[index + 1].Focus();
                }
                else {
                    var groupIndex = this.Parent.Parent.Children.indexOf(this.Parent);
                    if (this.Parent.Parent.Children[groupIndex + 1] && this.Parent.Parent.Children[groupIndex + 1].Children[0]) {
                        this.Parent.Parent.Children[groupIndex + 1].Children[0].Focus();
                    }
                }
            }
            else {
                var rangeCom = this.editForm.childCom.filter(x => !x.isButton && !x.isListView);
                var index = rangeCom.indexOf(this);
                if (rangeCom[index + 1]) {
                    rangeCom[index + 1].Focus();
                }
            }
        }
    }

    entrySelected(rowData) {
        if (this.meta.isPrivate && (rowData["debitDay"] || rowData["creditLimit"])) {
            if (rowData["debitDay"] && rowData["debitDate"] && rowData["debitDay"] > 0) {
                var checkDate = this.dayjs(rowData["debitDate"]).add(rowData["debitDay"], "day");
                if (checkDate.isBefore(this.dayjs(), "day")) {
                    window.clearTimeout(this._waitForDispose);
                    this._waitForDispose = window.setTimeout(() => {
                        this._input.focus();
                    }, 200);
                    if (!this.salesFunction["ALLOW_SELECT_OVERDUE_OBJECTS"]) {
                        this.Matched = null;
                        this.entity[this.displayField] = null;
                        this._input.value = null;
                        this.updateValue();
                        this.editForm.openConfig(LangSelect.get("You cannot select overdue objects"), () => {
                        }, () => { }, false, [], true);
                        return;
                    }
                    window.clearTimeout(this._waitForDispose);
                    this._waitForDispose = window.setTimeout(() => {
                        this._input.focus();
                    }, 200);
                    this.editForm.openConfig(LangSelect.get("You cannot select overdue objects"), () => {
                        this.actEntrySelected(rowData);
                    }, () => { }, false, [], true);
                    return;
                }
            }
            if (this.Decimal(rowData["debitAmountVND"] || 0).gt(this.Decimal(rowData["creditLimit"] || 0))) {
                if (!this.salesFunction["ALLOW_SELECT_OVERDUE_OBJECTS"]) {
                    window.clearTimeout(this._waitForDispose);
                    this._waitForDispose = window.setTimeout(() => {
                        this._input.focus();
                    }, 200);
                    this.Matched = null;
                    this.entity[this.displayField] = null;
                    this._input.value = null;
                    this.updateValue();
                    this.editForm.openConfig(LangSelect.get("You cannot select overdue objects"), () => {
                    }, () => { }, false, [], true);
                    return;
                }
                else {
                    window.clearTimeout(this._waitForDispose);
                    this._waitForDispose = window.setTimeout(() => {
                        this._input.focus();
                    }, 200);
                    this.editForm.openConfig(LangSelect.get("You cannot select overdue objects"), () => {
                        this.actEntrySelected(rowData);
                    }, () => { }, false, [], true);
                    return;
                }
            }
        }
        if (!Utils.isNullOrWhiteSpace(rowData["toastWarning"])) {
            this.editForm.openConfig(rowData["toastWarning"], () => {
                this.actEntrySelected(rowData);
            }, () => { }, false, [], true)
        }
        else {
            this.actEntrySelected(rowData);
        }
    }

    updateView(force = false, dirty = null, ...componentNames) {
        var fieldName = Utils.isNullOrWhiteSpace(this.meta.tabGroup) ? this.meta.fieldName : this.meta.tabGroup;
        var newValue = this.entity[fieldName];
        this._value = this.entity[fieldName];
        if (newValue === null) {
            if (!Utils.isNullOrWhiteSpace(this.meta.tabGroup) && this.entity[this.Name]) {
                newValue = this.entity[this.Name];
            }
            this.Matched = null;
            this.entity[this.displayField] = null;
            this._input.value = newValue;
            this.updateValue();
            if (this.isCurrency) {
                this.entity.exchangeRateVND = null;
                this.entity.exchangeRateUSD = null;
                this.entity.currencyCode = null;
            }
            return;
        }
        this.findMatchText();
    }

    async validateAsync() {
        if (this.validationRules.length == 0) {
            return true;
        }
        this.validationResult = [];
        this.validateRequired(this._value);
        this.Validate(ValidationRule.Equal, this._value, (value, ruleValue) => value === ruleValue);
        this.Validate(ValidationRule.notEqual, this._value, (value, ruleValue) => value !== ruleValue);
        return this.isValid;
    }

    setDisableUI(value) {
        if (this._input !== null) {
            this._input.readOnly = value;
        }
    }

    removeDOM() {
        if (this._input !== null && this._input.parentElement !== null) {
            this._input.parentElement.remove();
        }
    }

    setDefaultVal() {
        if (Utils.isNullOrWhiteSpace(this.meta.defaultVal)) {
            return;
        }
        var data = this.meta.defaultVal;
        if (!data) {
            data = this.meta.defaultVal;
        }
        if (data && this.entity && this.entity[this.Name] == null && this.entity[this.idField] && this.entity[this.idField].startsWith("-")) {
            this.entity[this.Name] = data;
            this.populateFields();
            window.setTimeout(() => {
                this.populateFields();
            }, 300);
        }
    }
}