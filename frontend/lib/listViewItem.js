import { Section } from "./section.js";
import { Html } from "./utils/html.js";
import Decimal from "decimal.js";
import {
    Action, PatchDetail, PatchVM, customEventType,
    ObservableArgs, EventType, Component, SavePatchVM
} from "./models/";
import { Utils } from "./utils/utils.js";
import { ComponentFactory } from "./utils/componentFactory.js";
import { Label } from "./label.js";
import { EditableComponent } from "./editableComponent.js";
import { Client } from "./clients/client.js";
import { Toast } from "./toast.js";
import { ElementType } from './models/elementType.js';
import { Checkbox } from "./checkbox.js";
import { keyCodeEnum } from "./models/";
import tippy from 'tippy.js';
import 'tippy.js/dist/tippy.css';
import 'tippy.js/animations/scale.css';

/**
 * @typedef {import('./section.js').listViewSection} ListViewSection
 * @typedef {import('./listView.js').listView} ListView
 * @typedef {import('./gridView.js').gridView} GridView
 * Represents a list view item.
 * @extends Section
 */
export class ListViewItem extends Section {
    isRow = true;
    /**
     * Creates an instance of ListViewItem.
     * @param {ElementType} [elementType=ElementType.tr] - The type of HTML element.
     */
    constructor(elementType = ElementType.tr) {
        super(elementType);
        // Initialize properties
        /** @type {ListViewSection} */
        this.listViewSection = null;
        /** @type {GridView} */
        this.listView = null;
        this.preQueryFn = null;
        this._selected = false;
        this._focused = false;
        this._emptyRow = false;
        this.rowNo = 0;
        this.focusEvent = new Action();
        this.groupRow = false;
        this._focusAwaiter = 0;
        /** @type {PatchDetail[]} */
        this.patchModel = [];
        this.showMessage = true;
        this.isListViewItem = true;
    }

    /**
     * Gets or sets whether the item is selected.
     * @type {boolean}
     */
    get selected() {
        return this._selected;
    }
    groupSection;
    set selected(value) {
        this._selected = value;
        this.setSelected(value);
        if (this.checkbox) {
            this.checkbox.value = value;
        }
        const id = this.entityId;
        const selectedIds = this.listView.selectedIds;
        if (value) {
            if (!selectedIds.includes(id)) {
                selectedIds.push(id);
            }
        } else {
            const index = selectedIds.indexOf(id);
            if (index !== -1) {
                selectedIds.splice(index, 1);
            }
        }
        this.listView.addSummaries();
    }

    static notCellText = ["Button", "Image", "Checkbox"];
    static emptyRowClass = "empty-row";
    static selectedClass = "__selected__";
    static focusedClass = "focus";
    static hoveringClass = "hovering";
    static groupRowClass = "group-row";

    /**
     * Handles focus event.
     * @param {boolean} [value=null] - The new focus state.
     * @param {boolean} [triggerEvent=true] - Whether to trigger the focus event.
     * @returns {boolean} The focus state.
     */
    set focused(value) {
        if (value === null) return this._focused;
        this._focused = value;
        if (this._focused) {
            this.listView.lastListViewItem = this;
            this.element.classList.add(ListViewItem.focusedClass);
        } else {
            this.element.classList.remove(ListViewItem.focusedClass);
        }
        return this._focused;
    }

    get focused() {
        return this._focused;
    }

    /**
     * Sets the selected state of the element.
     * @param {boolean} value - The selected state.
     */
    setSelected(value) {
        window.setTimeout(() => {
            if (!this.element) {
                return;
            }
            if (value) {
                this.element.classList.add(ListViewItem.selectedClass);
            } else {
                this.element.classList.remove(ListViewItem.selectedClass);
            }
        }, 50);
    }

    /**
     * Gets or sets whether the item represents an empty row.
     * @type {boolean}
     */
    get emptyRow() {
        return this._emptyRow;
    }

    set emptyRow(value) {
        this._emptyRow = value;
        this.filterChildren().forEach(x => x.emptyRow = value);
        this.alwaysValid = value;
        if (this.element == null) return;
        if (value) {
            this.element.classList.add(ListViewItem.emptyRowClass);
        } else {
            this.element.classList.remove(ListViewItem.emptyRowClass);
        }
    }

    /**
     * Renders the item.
     */
    render() {
        // @ts-ignore
        this.listView = this.listView ?? this.findClosest(x => x.isListView);
        this.meta = this.meta ?? this.listView.meta;
        super.render();
        if (this._selected) {
            this.element.classList.add(ListViewItem.selectedClass);
        }
        Html.instance.take(this.element)
            .event(EventType.Click, this.rowItemClick.bind(this))
            .event(EventType.dblClick, this.rowDblClick.bind(this))
            .event(EventType.focusIn, () => {
                if (this.meta.canAdd) {
                    this.listView.emptySection.Children.forEach(x => {
                        if (x.focused) {
                            x.focused = false;
                        }
                    });
                }
                this.listView.allListViewItem.forEach(x => {
                    if (x.focused) {
                        x.focused = false;
                    }
                });
                this.focused = true;
            })
            .event(EventType.focusOut, this.rowFocusOut.bind(this))
            .event(EventType.mouseEnter, this.mouseEnter.bind(this))
            .event(EventType.mouseLeave, this.mouseLeave.bind(this));
    }

    /**
     * @param {any} success
     */
    afterSaveHandler(success) {
        if (!success) {
            this.entityId = null;
        }
    }

    /**
     * Renders row data.
     * @param {Component[]} headers - The list of table headers.
     * @param {object} row - The row data.
     * @param {number} [index=null] - The index of the row.
     * @param {boolean} [emptyRow=false] - Whether the row is empty.
     */
    renderRowData(headers, row, index = null, emptyRow = false) {
        if (index !== null) {
            if (index >= this.element.parentElement.children.length || index < 0) {
                index = 0;
            }
            this.element.parentElement.insertBefore(this.element, this.element.parentElement.children[index]);
        }
        const fn = Utils.isFunction(this.meta.renderer);
        if (!fn) {
            headers.filter(header => !header.hidden).forEach(header => {
                this.renderTableCell(row, header, this.element);
            });
        }
    }

    setChooseCell() {
        var entity = this.listView.entity;
        if (this.listView.meta.isMultiple) {
            if (entity[this.listView.meta.fieldName].toString().split(",").includes(this.entity[this.idField].toString())) {
                this.element.classList.add('cell-choose');
            }
            else {
                this.element.classList.remove('cell-choose');
            }
        }
    }

    /**@type {Checkbox} */
    checkbox;
    masterDataComponent = ["Dropdown", "Select2", "MultipleSearchEntry", "SearchEntry"];
    /**
         * Renders a table cell.
         * @param {object} rowData - The row data.
         * @param {Component} header - The table header component.
         * @param {HTMLElement} [cellWrapper=null] - The wrapper element for the cell.
         */
    renderTableCell(rowData, header, cellWrapper = null) {
        if (header.statusBar && this.meta.validation) {
            var cont = Utils.isFunction(this.meta.validation, false, this)
            tippy(cellWrapper.parentElement, {
                allowHTML: true,
                placement: 'right',
                arrow: true,
                delay: [10, 10],
                content: cont
            });
        }
        if (header.statusBar && !this.listView.isSearchEntry && this.listView.meta.isMultiple) {
            header.componentType = "Checkbox";
            header.editable = true;
            header.canWriteAll = true;
            header.canReadAll = true;
            header.fieldName = "Selected";
            cellWrapper.style.justifyContent = 'center';
        }
        if (!header.fieldName) {
            return;
        }
        if (this.masterDataComponent.includes(header.componentType)) {
            header.localData = header.localData;
        }
        var canW = header.editable;
        var com = ((canW && header.editable)
            || (!header.editable && header.componentType == "Button")) ? ComponentFactory.getComponent(header, this.editForm, null, canW) : new Label(header);
        if (!com) return;
        com.id = header.id;
        com.Name = header.fieldName;
        com.entity = rowData;
        com.parentElement = cellWrapper || Html.context;
        this.addChild(com);
        if (header.statusBar && !this.isSearchEntry && this.listView.meta.isMultiple) {
            this.checkbox = com;
            this.checkbox.disabled = false;
            this.checkbox.element.addEventListener(EventType.Change, (e) => {
                this.checkbox.Dirty = false;
                if (this.emptyRow) {
                    return;
                }
                e.preventDefault();
                e.stopPropagation();
                this.selected = !this.selected;
                this.dispatchEvent(this.meta.events, EventType.Click, this, this.entity).then();
            });
            this.checkbox.element.parentElement.addEventListener(EventType.keyDown, (e) => {
                if (this.emptyRow) {
                    return;
                }
                let code = e.keyCodeEnum();
                if (code == keyCodeEnum.space) {
                    e.preventDefault();
                    this.checkbox.value = !this.checkbox.value;
                    const check = this.checkbox._input.checked;
                    this.checkbox.dataChanged(check);
                    this.selected = !this.selected;
                }
            });
        }
        if (!header.statusBar && !["isPaid", "paidDate", "btnEdit"].includes(header.fieldName) && (this.disabled
            || header.disabled
            || this.parent.disabled
            || rowData["noSubmit"]
            || rowData["isLock"]
            || rowData["isPayment"]
            || rowData["isInvoice"]
            || rowData["isPaymentAcc"]
            || rowData["isDebtAcc"]
            || rowData["isPaid"]
            || this.listView.disabled)) {
            com.setDisabled(true);
        }
        if (rowData["isAbs"]) {
            cellWrapper.parentElement.parentElement.classList.add('cell-abs')
        }
        var entity = this.listView.entity;
        if (this.listView.meta.componentType == "Dropdown" && entity[this.listView.meta.fieldName]) {
            if (this.listView.meta.isMultiple) {
                if (entity[this.listView.meta.fieldName].toString().split(",").includes(rowData[this.idField].toString())) {
                    cellWrapper.parentElement.parentElement.classList.add('cell-choose');
                }
            }
            else {
                if (entity[this.listView.meta.fieldName].toString() == rowData[this.idField].toString()) {
                    cellWrapper.parentElement.parentElement.classList.add('cell-choose');
                }
            }
        }
        if (com.element && header.childStyle) {
            com.element.style.cssText = header.childStyle;
        }
        if (!header.statusBar) {
            com.userInput.add(arg => this.userInputHandler(arg, com));
        }
        if (header.editable && header.id) {
            if (this.listView.meta.isRealtime) {
                return;
            }
            var copyButton = document.createElement("button");
            copyButton.className = "button-copy";
            copyButton.tabIndex = -1;
            copyButton.addEventListener("click", async () => {
                var value = com.entity[header.fieldName];
                var index = this.listView.Item.indexOf(this);
                var newUpdate = this.listView.Item.splice(index + 1);
                for (const element of newUpdate) {
                    var comp = element.Children.find(x => x.Meta.fieldName == header.fieldName);
                    if (comp.Disabled) {
                        continue;
                    }
                    comp.entity[header.fieldName] = value;
                    if (com.Meta.componentType == "Dropdown") {
                        comp.entity[comp.displayField] = com.entity[comp.displayField];
                        comp.Matched = com.Matched;
                    }
                    element.updateView(true, true, header.fieldName);
                    if (comp.isCurrency) {
                        if (comp.Matched) {
                            var code = comp.getMatchedText(com.Matched);
                            comp.entity.exchangeRateVND = EditableComponent.exchangeRateVND[code];
                            comp.entity.exchangeRateUSD = EditableComponent.exchangeRateUSD[code];
                            comp.entity.currencyCode = code == "" ? null : code;
                        }
                        else {
                            comp.entity.exchangeRateVND = null;
                            comp.entity.exchangeRateUSD = null;
                        }
                    }
                    comp.populateFields(comp.entity);
                    await comp.dispatchEvent(comp.Meta.events, EventType.Input, comp, comp.entity);
                    await comp.dispatchEvent(comp.Meta.events, EventType.Change, comp, comp.entity);
                    element.Dirty = true;
                }
                await this.listView.dispatchEvent(this.listView.meta.events, EventType.Change, this.listView);
            });
            cellWrapper.appendChild(copyButton);
        }
    }

    /**
     * Handles user input event.
     * @param {ObservableArgs} arg - The observable arguments.
     * @param {EditableComponent} component - The editable component.
     */
    userInputHandler(arg, component) {
        if (component.disabled || component.statusBar) {
            return;
        }
        if (component.componentType == "Input" || component.componentType == "Textarea") {
            if (arg.evType == EventType.Abort || arg.evType == EventType.Change || arg.evType == EventType.Blur) {
                if (component.disabled) {
                    return;
                }
                this.listView.rowChangeHandler(component.entity, this, arg, component).then(() => {
                    this.updateValidation();
                    this.listView.realtimeUpdateAsync(this, arg).then();
                })
            }
        }
        else {
            this.listView.rowChangeHandler(component.entity, this, arg, component).then(() => {
                if (component.componentType != "Button" && arg.evType == EventType.Change) {
                    if (component.disabled) {
                        return;
                    }
                    this.updateValidation();
                    this.listView.realtimeUpdateAsync(this, arg).then();
                }
            });
        }
    }

    updateValidation() {
        this.Children.filter(x => x.Meta.Validation).forEach(x => x.updateValidation());
    }

    /**
     * Updates or creates a patch.
     * @param {boolean} [showMessage=true] - Whether to show a message.
     * @returns {Promise<boolean>} A promise that resolves to true if successful, otherwise false.
     */
    async patchUpdateOrCreate(showMessage = true) {
        if (!this.Dirty) {
            return false;
        }
        return new Promise((resolve) => {
            const patchModel = this.getPatchEntity();
            if (patchModel.Changes.length == 1) {
                return;
            }
            this.dispatchCustomEvent(this.meta.events, customEventType.beforePatchUpdate, this.entity, patchModel, this)
                .then(() => {
                    this.element.classList.add("loading");
                    this.element.classList.remove("focus");
                    this.element.classList.remove("__selected__");
                    this.showMessage = showMessage;
                    this.validateAsync().then(isValid => {
                        if (!isValid) return;
                        Client.instance.patchAsync(patchModel).then(rs => {
                            this.patchUpdateCb(rs);
                            resolve(rs.updatedItem[0]);
                        });
                    });
                });
        });
    }

    getPatchVM() {
        let dirtyPatch = [];
        Object.getOwnPropertyNames(this.entity).forEach(cell => {
            if (this.entity[cell] instanceof Array || (this.entity[cell] instanceof Object && !(this.entity[cell] instanceof Decimal)) || cell == this._groupKey) {
                return;
            }
            let val;
            if (typeof this.entity[cell] === "boolean") {
                val = this.entity[cell] ? "1" : "0";
            } else {
                val = this.entity[cell];
            }
            let patchDetail = new PatchDetail();
            patchDetail.Label = cell;
            patchDetail.Field = cell;
            patchDetail.oldVal = null;
            patchDetail.value = val;
            var component = this.Children.find(x => x.Meta.fieldName == cell)
            if (component) {
                let text = component.getValueText();
                let actText = Utils.isNullOrWhiteSpace(text) ? 'N/A' : text;
                let oldText = Utils.isNullOrWhiteSpace(component.originalText) ? 'N/A' : component.originalText;
                if (actText != oldText) {
                    patchDetail.historyValue = `${component.Meta.Label}: ${oldText} => ${actText}`;
                }
            }
            dirtyPatch.push(patchDetail);
        });
        let patchModel = new SavePatchVM();
        patchModel.Changes = dirtyPatch;
        patchModel.table = this.meta.refName;
        patchModel.Detail = [];
        patchModel.Delete = [];
        return patchModel;
    }

    async sendEntity() {
        this.entity.statusId = 2;
        var patchModel = this.getPatchVM();
        var res = await Client.instance.postAsync(patchModel, "/api/feature/sendEntity");
        if (res.status == 200) {
            return true;
        }
        else {
            return false;
        }
    }

    patchUpdateCb(data) {
        if (data && data.status == 200) {
            this.entityId = data.updatedItem[0][this.idField];
            this.Dirty = false;
            this.emptyRow = false;
        }
        if (this.entity[this.idField].startsWith("-")) {
            this.entity[this.idField] = data.updatedItem[0][this.idField];
        }
        var dataEntity = data.updatedItem[0];
        this.listView.loadMasterData([dataEntity]).then(() => {
            Object.assign(this.entity, dataEntity);
            this.updateView(true);
            this.afterSaved?.invoke(dataEntity);
            this.dispatchCustomEvent(this.meta.events, customEventType.afterPatchUpdate, this.entity, this).then();
            this.element.classList.remove("loading");
            this.element.classList.add("focus");
            this.element.classList.add("__selected__");
        });
    }

    updateView(force = false, dirty = null, ...componentNames) {
        this.Children.filter(x => x.Meta && !x.Meta.statusBar).forEach(/**@param {EditableComponent} child **/ child => {
            child.entity = this.entity;
            child.prepareUpdateView(force, dirty, componentNames);
            child.updateView(force, dirty, componentNames);
            child.updateValidation();
        });
    }

    /**
     * Retrieves the patch entity.
     * @returns {PatchVM} The patch entity.
     */
    getPatchEntity() {
        var dirtyPatch = [];
        var row = this.entity;
        Object.getOwnPropertyNames(row).forEach(cell => {
            if (row[cell] instanceof Array || (row[cell] instanceof Object && !(row[cell] instanceof Decimal) && !(row[cell] instanceof Date)) || cell == this._groupKey) {
                return;
            }
            let val;
            if (typeof row[cell] === "boolean") {
                val = row[cell] ? "1" : "0";
            } else {
                val = row[cell];
            }

            let patchDetail = new PatchDetail();
            patchDetail.Label = cell;
            patchDetail.Field = cell;
            patchDetail.oldVal = null;
            patchDetail.value = val;
            var component = this.Children.find(y => y.Meta.fieldName == cell)
            if (component && component.Meta.Editable) {
                let text = component.changeValue || component.getValueText();
                let actText = Utils.isNullOrWhiteSpace(text) ? 'N/A' : text;
                let oldText = Utils.isNullOrWhiteSpace(component.originalText) ? 'N/A' : component.originalText;
                if (actText != oldText) {
                    patchDetail.historyValue = `${component.Meta.Label}: ${oldText} => ${actText}`;
                }
            }
            dirtyPatch.push(patchDetail);
        });
        // @ts-ignore
        return {
            Changes: dirtyPatch,
            Table: this.listView.meta.refName,
            Delete: [],
            Detail: []
        };
    }

    /**
     * Handles double click event on a row.
     * @param {Event} e - The event object.
     */
    rowDblClick(e) {
        e.stopPropagation();
        this.listView.dblClick?.invoke(this);
        this.dispatchEvent(this.meta.events, EventType.dblClick, this, this.entity).then();
    }

    /**
     * Handles row item click event.
     * @param {Event} e - The event object.
     */
    rowItemClick(e) {
        if (this.meta.isMultiple && this.componentType == "GridView") {
            return;
        }
        const ctrl = e.ctrlOrMetaKey();
        const shift = e.shiftKey();
        /** @type {HTMLElement} */
        const target = e.target;
        const focusing = this.firstOrDefault(x => x.element === target || x.parentElement.contains(target)) !== null;
        this.hotKeySelectRow(ctrl, shift, focusing);
        if (!e.shiftKey()) {
            this.listView.rowClick?.invoke(this.entity);
        }
        this.listView.lastListViewItem = this;
        this.Focus = true;
        this.dispatchEvent(this.meta.events, EventType.Click, this, this.entity).then();
    }

    /**
     * Handles hotkey selection of rows.
     * @param {boolean} ctrl - Whether the control key is pressed.
     * @param {boolean} shift - Whether the shift key is pressed.
     * @param {boolean} focusing - Whether the row is focusing.
     */
    hotKeySelectRow(ctrl, shift, focusing) {
        if (this.emptyRow) {
            return;
        }
        let allListView = this.listView.allListViewItem;
        if (this.meta.isMultiple) {
            this.listView.clearSelected();
            this.selected = true;
            this.listView.selectedIndex = allListView.indexOf(this);
            return;
        }
        if (!ctrl && !shift) {
            if (this.listView.selectedIds.length <= 1) {
                this.listView.clearSelected();
                this.selected = !this._selected;
                if (this._selected) {
                    this.listView.selectedIndex = this.listView.Children.indexOf(this);
                }
            }
            return;
        }
        this.selected = !this._selected;

        if (!shift && !ctrl && this._selected) {
            this.listView.selectedIndex = this.listView.Children.indexOf(this);
        }
        if (shift) {
            const selected = allListView.find(x => x.selected);
            let _lastIndex = allListView.indexOf(selected);
            var currentIndex = allListView.indexOf(this);
            if (currentIndex < _lastIndex) {
                let temp = currentIndex;
                currentIndex = _lastIndex;
                _lastIndex = temp;
            }
            for (let i = _lastIndex; i <= currentIndex; i++) {
                /** @type {ListViewItem} */
                let listViewItem = allListView[i];
                if (listViewItem instanceof ListViewItem) {
                    listViewItem.selected = true;
                }
            }
        }
    }

    /**
     * Sets selected list view items.
     * @param {ListViewItem[]} allListView - The list of all list view items.
     * @param {number} _lastIndex - The last index.
     * @param {number} currentIndex - The current index.
     */
    setSeletedListViewItem(allListView, _lastIndex, currentIndex) {
        const start = allListView[0].rowNo > _lastIndex ? allListView[0].rowNo : _lastIndex;
        const items = this.listView.allListViewItem.filter(x => x.rowNo >= start && x.rowNo <= currentIndex);
        this.listView.selectedIds = items.map(x => x.entityId);
        items.forEach(item => {
            const id = item.entityId;
            if (this.listView.selectedIds.includes(id)) {
                item.selected = this.selected;
            } else {
                item.selected = false;
            }
        });
    }
    /**
     * Handles row focus out event.
     */
    rowFocusOut() {
        this.Focus = false;
        return this.dispatchCustomEvent(this.meta.events, customEventType.rowFocusOut, this.entity);
    }

    /**
     * Handles mouse enter event.
     */
    mouseEnter() {
        this.element.classList.add(ListViewItem.hoveringClass);
        return this.dispatchCustomEvent(this.listView.meta.events, customEventType.rowMouseEnter, this.entity);
    }

    /**
     * Handles mouse leave event.
     */
    mouseLeave() {
        this.element.classList.remove(ListViewItem.hoveringClass);
        return this.dispatchCustomEvent(this.listView.meta.events, customEventType.rowMouseLeave, this.entity);
    }

    /**
     * Gets or sets whether to show a message.
     * @type {boolean}
     */
    get showMessage() { return this._showMessage; }
    set showMessage(value) { this._showMessage = value; }

    /**
     * Validates asynchronously.
     * @returns {Promise<boolean>} A promise that resolves to true if all validations pass, otherwise false.
     */
    validateAsync() {
        return new Promise((ok, err) => {
            const allValid = this.filterChildren(
                x => x.Children.length === 0,
                x => x.alwaysValid
            ).forEachAsync(x => x.validateAsync());
            allValid.then(res => {
                const allOk = res.every(x => x.isValid);
                ok(allOk);
                if (!allOk && this.showMessage) {
                    const message = res.filter(x => !x.isValid)
                        .map(x => Object.values(x.validationResult).Combine(null, Utils.breakLine))
                        .Combine(null, Utils.breakLine);
                    Toast.warning(message);
                }
            }).catch(err);
        });
    }

    async updateEntity() {
        if (!this.entityId.startsWith("-")) {
            var updateRow = await Client.instance.getByIdAsync(this.listView.meta.refName, [this.entity.id]);
            var entity = updateRow.data[0];
            if (entity) {
                await this.listView.loadMasterData([entity]);
                this.entity = entity;
                this.updateView(true);
            }
        }
    }
}

