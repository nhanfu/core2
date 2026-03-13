import { Str } from "./utils/ext.js";
import { Utils } from "./utils/utils.js";
import { Component, Action, EventType, ValidationRule, keyCodeEnum } from "./models/";
import { Uuid7 } from "./structs/uuidv7.js";
import { html } from "./utils/html.js";
import { LangSelect } from "./utils/langSelect.js";
import decimal from "decimal.js";
import { Toast } from "./toast.js";
import { Client } from "./clients/";
import { Token } from "./models/";
import dayjs from "dayjs";
import timezone from "dayjs/plugin/timezone.js";
import customParseFormat from "dayjs/plugin/customParseFormat.js";
import utc from "dayjs/plugin/utc.js";
import "dayjs/locale/vi.js";
import quarterOfYear from "dayjs/plugin/quarterOfYear";
import { Spinner } from "./spinner.js";
dayjs.locale('vi');
dayjs.extend(utc);
dayjs.extend(timezone);
dayjs.extend(customParseFormat);
dayjs.extend(quarterOfYear);
/**
 * @typedef {import('./gridView.js').gridView} GridView
 * @typedef {import('./listViewItem.js').listViewItem} ListViewItem
 * @typedef {import('./searchEntry.js').searchEntry} SearchEntry
 * @typedef {import('./section.js').Section} Section
 * @typedef {import('./label.js').Label} Label
 * @typedef {import('./editForm.js').editForm} EditForm
 * @typedef {import('./tabEditor.js').tabEditor} TabEditor
 * @typedef {import('./models/observable.js').default} ObservableArgs
 * @typedef {{ [key: string] : (ValidationRule) }} validation
 */
/**
 * @class
 */
export class EditableComponent {
    static exchangeRateVND = {};
    static exchangeRateSaleVND = {};
    static exchangeRateProfitVND = {};
    static exchangeRateUSD = {};
    static exchangeRateSaleUSD = {};
    static exchangeRateProfitUSD = {};
    /** @type {Client} */
    Client = Client.instance;
    /** @type {Uuid7} */
    Uuid7 = Uuid7;
    /** @type {decimal} */
    decimal = decimal;
    /** @type {dayjs} */
    dayjs = dayjs;
    /** @type {LangSelect} */
    LangSelect = LangSelect;
    /** @type {any} */
    salesFunction;
    /** @type {Toast} */
    Toast = Toast;
    /**
     * @type {number}
     */
    static smallScreen = 768;
    defaultObject = {};
    /**
     * @type {boolean}
     */
    isListViewItem = false;
    /**
     * @type {boolean}
     */
    isAction = false;
    /**
     * @type {boolean}
     */
    isInput = false;
    /**
     * @type {boolean}
     */
    isVND = false;
    /**
     * @type {boolean}
     */
    isSection = false;
    /**
     * @type {number}
     */
    static exSmallScreen = 567;

    /**
     * @type {number}
     */
    static mediumScreen = 992;
    /**
     * @type {number}
     */
    static largeScreen = 1200;
    /**
     * @type {number}
     */
    static exLargeScreen = 1452;
    /** @type {EditableComponent} */
    parent;
    /** @type {EditableComponent[]} */
    children = [];
    /** @type {HTMLElement}*/
    parentElement;
    /** @type {HTMLElement} */
    element;
    /** @type {object} */
    entity = null;
    /** @type {Component} */
    meta;
    /** @type {any} */
    defaultValue;
    /** @type {string[]} classes - represent hierarchy class of the component instance. */
    classes = [];
    /** @type {boolean} emptyRow - true if the component is in empty row or screen, otherwise false. */
    #emptyRow;
    /** @type {Action} disposed - handle after dispose event. */
    disposed = new Action();
    /** @type {Action} disposed - handle dOM content loaded event. */
    dOMContentLoaded = new Action();
    /** @type {Action} handle toggle event. */
    onToggle = new Action();
    /** @type {TabEditor} handle toggle event. */
    #rootTab;
    userInput = new Action();
    /** @type {decimal} */
    oldValue;
    /** @type {{[key: string]: string}} */
    validationResult = {};
    get className() { return this.element.className; }
    set className(value) { this.element.className = value; }
    /** @type {validation} */
    validationRules = {};
    /** @type {boolean} */
    #disabled;
    /** @type {EditForm} */
    #editForm;
    /** @type {boolean} */
    _dirty;
    /** @type {boolean} */
    _dirtyEntity;
    /** @type {boolean} */
    alwaysValid;
    idField = 'id';
    statusIdField = 'statusId';
    afterSaved = new Action();
    beforeSaved = new Action();
    populateDirty = true;
    localData = [];
    isListViewItem = false;
    isButton = false;
    isRow = false;
    isListView = false;
    isSearchEntry = false;
    isTabComponent = false;
    isEditForm = false;
    /** @type {Token} */
    get Token() {
        return Client.token;
    }
    /** @type {TabEditor} handle toggle event. */
    get TabEditor() {
        return this.findClosest(x => x.isTab);
    }
    /** @type {EditForm} */
    get editForm() {
        return this.#editForm;
    }
    /**
     * @param {EditForm} editor
     */
    set editForm(editor) {
        this.#editForm = editor;
    }
    get isSmallUp() { return document.body.clientWidth > 768 }
    get isMediumUp() { return document.body.clientWidth > 992 }
    get isLargeUp() { return document.body.clientWidth > 1200 }
    /** @type {boolean} */
    get disabled() {
        return this.#disabled;
    }
    set disabled(value) {
        this.#disabled = value;
        this.setDisableUI(value);
        this.children.flattern(x => x.children).filter(y => y.meta && Utils.isNullOrWhiteSpace(y.meta.disabledExp) && !y.meta.disabled).forEach(x => {
            x.#disabled = value;
            x.setDisableUI(value);
        });
    }
    get dirty() {
        var check = this._dirty && !this.alwaysValid || this.filterChildren(x => x._dirty, x => !x.populateDirty || x.alwaysValid).length > 0;
        return check;
    }
    set dirty(value) {
        this._dirty = value;
        var name = "_dirty";
        if (!Utils.isNullOrWhiteSpace(this.meta.entityName)) {
            name = name + this.meta.entityName;
            this[name] = value;
        }
        if (!value) {
            this.filterChildren(x => x._dirty, x => !x.populateDirty || x.alwaysValid).forEach(x => x[name] = false);
        }
    }

    dirtyEntity(entityName) {
        var name = "_dirty";
        var check = this[name + entityName] && !this.alwaysValid || this.filterChildren(x => x[name + entityName], x => !x.populateDirty || x.alwaysValid).length > 0;
        return check;
    }
    get isValid() {
        return object.keys(this.validationResult).length === 0;
    }
    get cacheName() {
        var exp = this.meta?.cacheName;
        if (!exp) return null;
        var data = Utils.isFunction(exp, false, this);
        return data ? data : exp;
    }
    get queueName() {
        return this.meta?.queueName;
    }
    /** @returns {string} Entity's id */
    get entityId() {
        return this.entity?.id;
    }
    set entityId(value) {
        if (this.entity == null) return;
        this.entity.id = value;
    }
    /** @returns {string} meta Label */
    get comLabel() {
        return this.meta?.Label;
    }
    /** @returns {string} meta fieldname */
    get name() {
        return this.meta?.fieldName;
    }
    set name(val) {
        if (this.meta == null)
            this.meta = new Component();
        this.meta.fieldName = val || null;
    }
    get ComponentType() {
        return this.meta?.componentType;
    }
    set ComponentType(val) {
        if (this.meta == null)
            this.meta = new Component();
        this.meta.componentType = val;
    }
    /** @type {boolean} emptyRow - true if the component is in empty row or screen, otherwise false. */
    get emptyRow() {
        return this.#emptyRow;
    }
    set emptyRow(val) {
        this.#emptyRow = val;
    }
    get metaConn() {
        return this.meta?.metaConn;
    }
    get dataConn() {
        return this.meta?.dataConn;
    }
    get fieldVal() { return !this.entity || !this.name ? null : this.entity[this.name]; }
    set fieldVal(val) {
        if (this.entity == null || this.name == null) return;
        this.entity[this.name] = val;
    }
    static tabContainer = document.getElementById("tab-content");
    popup = false;
    isTab = false;
    get firstChild() { return this.children.firstOrDefault(); }
    constructor(meta, element = null) {
        this.meta = meta;
        this.alwaysValid = false;
        this.parentElement = this.meta?.parentElement;
        this.element = element;
        this.alwaysLogHistory = false;
        this.entity = {};
        this.salesFunction = JSON.parse(localStorage.getItem("salesFunction"));
        this.dOMContentLoaded.add(() => {
            this.updateValidation();
            this.setRequired();
            this.sendQueueAction("subscribe");
            if (meta != null && meta.events) {
                this.dispatchEvent(meta.events, EventType.dOMContentLoaded, this, this.entity).then();
            }
            this.setOldTextAndVal();
        });
    }

    updateValidation(setRequired) {
        if (!Utils.isNullOrWhiteSpace(this.meta?.validation)) {
            /** @type {validation[]} */
            var rules = Utils.isFunction(this.meta.validation, false, this);
            if (rules.length > 0) {
                this.validationRules = rules.toDictionary(x => x.rule, x => x);
            }
            else {
                this.validationRules = [];
            }
        }
        if (setRequired) {
            this.setRequired();
        }
    }
    /**
     * @param {any} events
     * @param {string} eventType
     * @param {any[]} parameters
     */
    dispatchEvent(events, eventType, ...parameters) {
        if (!events) {
            return promise.resolve(true);
        }
        return promise.resolve(this.invokeEvent(events, eventType, ...parameters));
    }
    /**
     * @param {any} events
     * @param {string} eventType
     * @param {any[]} parameters
     */
    invokeEvent(events, eventTypeName, ...parameters) {
        let eventObj;
        try {
            eventObj = JSON.parse(events);
        } catch (error) {
            Spinner.hide();
            this.editForm?.openConfig?.("JSON parse error:" + error, () => {
            }, () => { }, false, [], true);
            return promise.resolve(false);
        }

        const eventName = eventObj[eventTypeName];
        if (!eventName) {
            return promise.resolve(false);
        }

        const data = Utils.isFunction(eventName, false, this);
        if (data) {
            Spinner.hide();
            return promise.resolve(true);
        }

        let form = this.editForm;
        if (!form) {
            Spinner.hide();
            return promise.resolve(false);
        }

        const method = form[eventName];
        if (!method) {
            Spinner.hide();
            return promise.resolve(false);
        }

        const tcs = new Promise((resolve, reject) => {
            try {
                let task = method.apply(form, parameters);
                if (!task || task.isCompleted == null) {
                    resolve(task);
                } else {
                    task.then(() => resolve(task)).catch(e => {
                        Spinner.hide();
                        this.editForm.openConfig(e, () => {
                        }, () => { }, false, [], true);
                        reject(e);
                    });
                }
            } catch (invokeError) {
                Spinner.hide();
                this.editForm.openConfig(invokeError, () => {
                }, () => { }, false, [], true);
                reject(invokeError);
            }
        }).catch(finalError => {
            Spinner.hide();
            this.editForm.openConfig(finalError, () => {
            }, () => { }, false, [], true);
        });;

        return tcs;
    }

    /**
     * @param {string} events
     * @param {string} eventType
     * @param {any[]} parameters
     */
    dispatchCustomEvent(events, eventType, ...parameters) {
        if (!events) {
            return promise.resolve(true);
        }
        const eventTypeName = eventType.toString();
        return promise.resolve(this.invokeEvent(events, eventTypeName, ...parameters));
    }

    setOldTextAndVal() {
        this.originalText = this.getValueText();
        this.oldValue = this.entity[this.meta.fieldName];
    }

    setRequired() {
        const ele = this.element;
        if (ele == null) return;
        if (this.validationRules?.hasOwnProperty(ValidationRule.required)) {
            ele.setAttribute(ValidationRule.required, true.toString());
        }
        else {
            ele.removeAttribute(ValidationRule.required);
        }
    }
    /**
     * 
     * @param {string} ruleType
     * @param {any} value
     * @param {(item: any, rule: any) => boolean} validPredicate 
     * @returns 
     */
    validate(ruleType, value, validPredicate) {
        if (!this.validationRules.hasOwnProperty(ruleType)) {
            return true;
        }
        let rule = this.validationRules[ruleType];
        if (rule === null || rule.value1 === null) {
            return true;
        }
        let field = rule.value1.toString();
        if (field === "") {
            return true;
        }
        let ruleValue = rule.value1;
        let rule2Value = rule.value2;
        let label = ruleValue;
        let fieldVal = this.entity[field];
        if (fieldVal) {
            label = this.meta?.Label;
            ruleValue = fieldVal;
        }
        if (!validPredicate.bind(this)(value, ruleValue, rule2Value)) {
            if (![ValidationRule.regEx, ValidationRule.replace].some(x => x == ruleType)) {
                this.validationResult[ruleType] = Str.format(rule.Message, this.meta.label, label);
            }
            else {
                delete this.validationResult[ruleType];
            }
            return true;
        }
        else {
            delete this.validationResult[ruleType];
        }
        return false;
    }
    /**
     * @param {boolean} [disabled]
     */
    setDisableUI(disabled) {
        const ele = this.element;
        if (ele == null) {
            return;
        }

        if (disabled) {
            ele.setAttribute("disabled", "disabled");
        }
        else {
            ele.removeAttribute("disabled");
        }
    }

    setDefaultVal() {
        if (Utils.isNullOrWhiteSpace(this.meta.defaultVal)) {
            return;
        }
        var data = Utils.isFunction(this.meta.defaultVal, true, this);
        if (!data) {
            data = this.meta.defaultVal;
        }
        if (data && this.entity && this.entity[this.name] == null && this.entity[this.idField] && this.entity[this.idField].startsWith("-")) {
            this.entity[this.name] = data;
            this.populateFields();
            window.setTimeout(() => {
                this.populateFields();
            }, 300);
        }
        else {

        }
    }

    async validateRequired(value) {
        if (this.element === null || object.keys(this.validationRules).length === 0 || this.emptyRow || this.alwaysValid) {
            return true;
        }

        if (!this.validationRules.hasOwnProperty(ValidationRule.required)) {
            this.element.removeAttribute(ValidationRule.required);
            return true;
        }

        const requiredRule = this.validationRules[ValidationRule.required];
        this.element.setAttribute(ValidationRule.required, true.toString());

        if (value === null || value === undefined || value.toString().trim() === "") {
            this.validationResult[ValidationRule.required] = requiredRule.Message.replace("{0}", LangSelect.get(this.meta.label)).replace("{1}", this.entity);
            return false;
        } else {
            delete this.validationResult[ValidationRule.required];
            return true;
        }
    }

    addRule(rule) {
        this.validationRules[rule.rule] = rule;
        if (rule.rule === ValidationRule.required) {
            this.element.setAttribute(ValidationRule.required, true.toString());
        }
    }

    keydownHandler(e) {
        let code = e.keyCodeEnum();
        switch (code) {
            case keyCodeEnum.enter:
                e.preventDefault();
                if (!this.parent.isListViewItem) {
                    if (!Utils.isNullOrWhiteSpace(this.meta.groupBy)) {
                        var groups = this.editForm.childCom.filter(x => x.meta.groupBy == this.meta.groupBy);
                        var index = groups.indexOf(this);
                        if (groups[index + 1]) {
                            groups[index + 1].focus();
                        }
                        else {
                            var groupIndex = this.parent.parent.children.indexOf(this.parent);
                            if (this.parent.parent.children[groupIndex + 1] && this.parent.parent.children[groupIndex + 1].children[0]) {
                                this.parent.parent.children[groupIndex + 1].children[0].focus();
                            }
                        }
                    }
                    else {
                        var index = this.parent.children.indexOf(this);
                        if (this.parent.children[index + 1]) {
                            this.parent.children[index + 1].focus();
                        }
                        else {
                            var groupIndex = this.parent.parent.children.indexOf(this.parent);
                            if (this.parent.parent.children[groupIndex + 1] && this.parent.parent.children[groupIndex + 1].children[0]) {
                                this.parent.parent.children[groupIndex + 1].children[0].focus();
                            }
                        }
                    }
                }
                break;
            default:
                break;
        }
    }

    removeRule(ruleName) {
        delete this.validationRules[ruleName];
        if (!object.keys(this.validationRules).includes(ValidationRule.required)) {
            this.element.removeAttribute(ValidationRule.required);
        }
    }

    getInvalid() {
        return this.children.flattern(x => x.alwaysValid ? null : x.children).where(x => !x.isValid);
    }

    cascadeField() {
        if (!this.meta.cascadeField) {
            return;
        }

        const root = this.findClosest(x => x.isRow) ?? this.editForm;
        const cascadeFields = this.meta.cascadeField.split(",").map(field => field.trim()).filter(x => x !== "");
        if (cascadeFields.length === 0) {
            return;
        }

        cascadeFields.forEach(field => {
            root.filterChildren(x => x.name === field).forEach(target => {
                // @ts-ignore
                if (target instanceof SearchEntry && target !== null) {
                    // @ts-ignore
                    target.value = null;
                    target.meta.localData = null;
                } else {
                    target.updateView();
                }
            });
        });
    }

    populateFields(entity = null) {
        if (this.entity == null || this.meta.populateField == null) {
            return;
        }
        var isGrid = this.parent.isListViewItem;
        var root = this.editForm;
        if (isGrid) {
            root = this.parent;
        }
        Utils.isFunction(this.meta.populateField, false, this);
    }

    addIdToPatch(details) {
        const idFieldIndex = details.findIndex(x => x.field === Utils.idField);
        if (idFieldIndex !== -1) details.splice(idFieldIndex, 1);
        if (this.entityId === null) {
            details.push({ field: Utils.idField, value: Uuid7.id25() });
        } else {
            details.push({ field: Utils.idField, value: this.entityId, oldVal: this.entityId });
        }
    }

    _events = {};
    addEventListener(name, handler) {
        if (handler === null) throw new error("handler cannot be null");
        const handlers = this._events[name] || null;
        if (handlers == null) {
            this._events[name] = [handler];
        } else {
            handlers.push(handler);
        }
    }

    removeEventListener(name, handler) {
        if (handler === null) throw new error("handler cannot be null");
        const handlers = this._events[name] || null;
        if (handlers !== null) {
            delete this._events[name];
        }
    }

    findComponentByName(name, type) {
        return this.firstOrDefault(x => x.name === name && (type == null || x.classes.includes(type)));
    }

    /**
     * find closeset component
     * @param {(value: EditableComponent) => boolean} filter - filter component
     * @returns {EditableComponent} returns the closeset EditableComponent of the specified type
     */
    findClosest(filter = null) {
        /** @type {EditableComponent} */
        let found = this;
        while (found != null) {
            if (filter == null || filter(found)) return found;
            found = found.parent;
        }
        return null;
    }
    /** @type {boolean} */
    _show;
    get show() {
        return this._show;
    }
    set show(val) {
        this.toggle(val);
    }
    /** @param {boolean} value */
    toggle(value) {
        if (!this.element) {
            return;
        }
        this._show = value;
        if (!this._show) {
            if (this.meta && this.meta.showLabel && !this.isSection) {
                this.element.parentElement.style.display = "none";
            }
            else {
                this.element.style.display = "none";
            }
        }
        else {
            if (this.meta && this.meta.showLabel && !this.isSection) {
                this.element.parentElement.style.display = "";
            }
            else {
                this.element.style.display = "";
            }
        }
    }

    /**
     * show / hide the component
     * @param {string|boolean} showExp 
     */
    toggleShow(showExp) {
        if (showExp instanceof boolean) {
            this.show = showExp;
            return;
        }
        var shown = Utils.isFunction(showExp, false, this);
        this.show = shown;
        if (this.element.parentElement && this.element.parentElement.parentElement && ["Button", "Pdf", "excel", "email"].some(x => x == this.meta.componentType) && this.meta.groupFormat) {
            var parentElement = this.element.parentElement;
            var child = parentElement.querySelectorAll(".dropdown-content button");
            var parentArray = array.from(child);
            if (parentArray.some(x => x.style.display !== "none")) {
                parentElement.parentElement.style.display = "";
            } else {
                parentElement.parentElement.style.display = "none";
            }
        }
    }

    /**
     * 
     * @param {boolean | string | function} disabled 
     */
    toggleDisabled(disabled) {
        if (this.entity && this.entity.isLockEdit && this.meta && !["Button", "Pdf", "excel", "email"].includes(this.meta.componentType)) {
            return;
        }
        if (disabled instanceof boolean) {
            this.disabled = disabled;
            return;
        }
        if (!["isPaid", "paidDate", "btnEdit"].includes(this.meta.fieldName)
            && !this.entity["isLock"] && ((this.parent && this.parent.isListViewItem
                && !this.entity["noSubmit"] && !this.entity["isPayment"] && !this.entity["isInvoice"])
                || (this.parent && !this.parent.isListViewItem)) && !this.meta.disabled || this.meta.canWriteAll) {
            var disabledFn = Utils.isFunction(disabled, false, this);
            this.disabled = disabledFn || false;
        }
    }
    /**
     * 
     * @param {eve} e 
     */
    listViewItemTab(e) {
    }

    updateView(force = false, dirty = null, ...componentNames) {
        this.prepareUpdateView(force, dirty);
        if (!this.children || this.children.length === 0) {
            return;
        }
        if (componentNames && componentNames.length > 0) {
            const coms = this.filterChildren(x => x.isSection && componentNames.includes(x.meta.fieldName))
                .flatMap(x => x.filterChildren(x => !x.isSection));
            const coms2 = this.filterChildren(x => componentNames.includes(x.meta.fieldName) && !x.isSection);
            const shouldUpdate = [...new set([...coms, ...coms2].filter(x => !x.isSection))];
            shouldUpdate.forEach(child => {
                child.prepareUpdateView(force, dirty);
                child.entity = this.entity;
                child.updateView(force, dirty, ...componentNames);
            });
        } else {
            const shouldUpdate = this.filterChildren(x => !x.isSection && !x.isListView);
            shouldUpdate.forEach(child => {
                child.prepareUpdateView(force, dirty);
                child.entity = this.entity;
                child.updateView(force, dirty, ...componentNames);
            });
        }
    }

    setPropValue(obj, propName, value) {
        obj[propName] = value;
    }

    resetObject(res) {
        this.setPropValue(res, this.statusIdField, 1);
        if (res["noSubmit"] != undefined || res["noSubmit"] != null) {
            this.setPropValue(res, "parentId", null);
        }
        this.setPropValue(res, "tariffChargeId", null);
        this.setPropValue(res, "hblNo", null);
        this.setPropValue(res, "code", null);
        this.setPropValue(res, "allocationId", null);
        this.setPropValue(res, "noSubmit", false);
        this.setPropValue(res, "entityContainerId", null);
        this.setPropValue(res, "isAllocation", false);
        this.setPropValue(res, "allocationId", null);
        this.setPropValue(res, "isLock", false);
        this.setPropValue(res, "isSend", false);
        this.setPropValue(res, "isObh", false);
        this.setPropValue(res, "isLockEdit", false);
        this.setPropValue(res, "shipmentRequestId", null);
        this.setPropValue(res, "isLockExchange", false);
        this.setPropValue(res, "shipmentInvoiceDetailId", null);
        this.setPropValue(res, "shipmentInvoiceId", null);
        this.setPropValue(res, "shipmentInvoiceCode", null);
        this.setPropValue(res, "shipmentInvoiceDate", null);
        this.setPropValue(res, "paymentRequestId", null);
        this.setPropValue(res, "paymentRequestDetailId", null);
        this.setPropValue(res, "paymentCode", null);
        this.setPropValue(res, "paymentDate", null);
        this.setPropValue(res, "isPayment", false);
        this.setPropValue(res, "invoiceId", null);
        this.setPropValue(res, "invoiceDetailId", null);
        this.setPropValue(res, "invoiceCode", null);
        this.setPropValue(res, "invoiceDate", null);
        this.setPropValue(res, "isPaid", false);
        this.setPropValue(res, "paidDate", null);
        this.setPropValue(res, "debtCode", null);
        this.setPropValue(res, "debtDate", null);
        this.setPropValue(res, "debtId", null);
        this.setPropValue(res, "isDebtAcc", false);
        this.setPropValue(res, "paymentAccId", null);
        this.setPropValue(res, "paymentAccCode", null);
        this.setPropValue(res, "paymentAccDate", null);
        this.setPropValue(res, "isPaymentAcc", false);
    }

    u(force = false, dirty = null, ...componentNames) {
        this.updateView(force, dirty, componentNames);
    }

    /**
     * @param {boolean} force
     * @param {boolean} dirty
     */
    prepareUpdateView(force, dirty) {
        if (this.meta && this.meta.disabledExp) {
            this.toggleDisabled(this.meta.disabledExp);
        }
        if (this.meta
            && !Utils.isNullOrWhiteSpace(this.meta.componentType)
            && !Utils.isNullOrWhiteSpace(this.entity["insertedBy"])
            && !this.isSection
            && !this.isListViewItem
            && this.entity && this.entityId && !this.entityId.startsWith("-")
            && !this.isTabComponent
            && !this.meta.canWriteAll
            && !this.meta.editable
            && !this.isButton
            && !["isPaid", "paidDate", "btnEdit"].includes(this.meta.fieldName)
            && ((this.editForm && this.editForm.entity && this.editForm.entity["insertedBy"] != this.Token.userId && this.editForm.entity["assignId"] != this.Token.userId && !this.parent.isListViewItem)
                || this.entity["noSubmit"] || this.entity["isLock"] || this.entity["isPayment"] || this.entity["isInvoice"] || (this.entity["isPaid"] && this.parent.isListViewItem))) {
            this.disabled = true;
        }
        var userAuthentication = this.Token.userAuthorization && this.Token.userAuthorization.length > 0 ? this.Token.userAuthorization.filter(x => x.canWrite).map(x => x.userId) : [];
        if (this.meta
            && !Utils.isNullOrWhiteSpace(this.meta.componentType)
            && !Utils.isNullOrWhiteSpace(this.entity["insertedBy"])
            && !this.isSection
            && !this.isListViewItem
            && !this.isTabComponent
            && !this.meta.canWriteAll
            && ((this.parent.isListViewItem && this.parent.parent.isListView && !this.parent.parent.disabled) || !this.parent.isListViewItem)
            && this.entity && this.entityId && !this.entityId.startsWith("-")
            && !this.isButton
            && !["isPaid", "paidDate", "btnEdit"].includes(this.meta.fieldName)
            && !this.isTabComponent && userAuthentication.length > 0
            && !((this.entity["noSubmit"] || this.entity["isLock"] || this.entity["isPayment"] || this.entity["isInvoice"] || (this.entity["isPaid"] && this.parent.isListViewItem)))
            && userAuthentication.includes(this.entity["insertedBy"])) {
            this.disabled = false;
        }
        if (this.meta && this.meta.disabledExp && (this.meta.disabledExp.includes("canWrite") || this.meta.disabledExp.includes("insertedBy") || this.meta.disabledExp.includes("statusId") || this.meta.disabledExp.includes("progressId") || this.meta.disabledExp.includes("actionId") || this.meta.disabledExp.includes("this.Token"))) {
            this.toggleDisabled(this.meta.disabledExp);
        }
        if (force) {
            this.emptyRow = false;
        }
        if (this.meta && this.meta.defaultVal) {
            this.setDefaultVal();
        }
        if (this.meta && this.meta.showExp) {
            this.toggleShow(this.meta.showExp);
        }
        if (this.meta && this.meta.addRowExp) {
            this.toggleAddRow(this.meta.addRowExp);
        }
        if (dirty) {
            this._setDirty = dirty;
        }
        if (this.entity && this.entity.isLockEdit && this.meta && !["Button", "Pdf", "excel", "email"].includes(this.meta.componentType)) {
            this.disabled = true;
        }
    }

    nothing() {
        return !this.children || this.children.length === 0;
    }

    disposeChildren() {
        if (this.nothing()) {
            return;
        }
        let leaves = this.flatten(node => node.children)
            .filter(node => node.element !== null && node.parent !== null && node.nothing());

        while (leaves.length > 0 && (leaves == 1 && leaves[0] != this)) {
            leaves.forEach(node => {
                if (node === null) {
                    return;
                }
                node.dispose();
                if (node.parent && node.parent.children) {
                    node.parent.children = node.parent.children.filter(child => child !== node);
                }
            });

            // recalculate leaves after disposing the current leaves
            leaves = this.flatten(node => node.children)
                .filter(node => node.element !== null && node.parent !== null && node.nothing());
        }
    }

    flatten(fn) {
        const result = [];
        const stack = [this];
        while (stack.length > 0) {
            const node = stack.pop();
            result.push(node);
            const children = fn(node);
            if (children) {
                stack.push(...children);
            }
        }
        return result;
    }

    dispose() {
        this.sendQueueAction("unsubscribe");
        this.disposeChildren();
        this.removeDOM();
        this.children = [];
        this.dOMContentLoaded = null;
        this.onToggle = null;
        if (this.parent != null && this.parent.children != null
            && this.parent.children.toArray().hasElement()
            && this.parent.children.toArray().contains(this)) {
            this.parent.children.toArray().remove(this);
        }
        if (this.editForm && this.editForm.childCom) {
            this.editForm.childCom.toArray().remove(this);
        }
    }

    removeDOM() {
        if (this.element != null) {
            this.element.remove();
            this.element = null;
        }
    }

    sendQueueAction(action) {
        var queueName = this.queueName;
        if (!queueName) return;
        const param = { queueName: queueName, Action: action };
        // @ts-ignore
        this.editForm?.notificationClient?.send(JSON.stringify(param));
        if (action == "subscribe")
            // @ts-ignore
            window.addEventListener(queueName, this.queueHandler);
        else
            // @ts-ignore
            window.removeEventListener(queueName, this.queueHandler);
    }

    getValueText() {
        if (this.element === null) {
            return "";
        }
        if (this.element instanceof hTMLInputElement) {
            return this.element.value;
        }
        if (this.element instanceof hTMLTextAreaElement) {
            return this.element.value;
        }
        return this.element.textContent;
    }

    getValue() {
        return this.entity[this.name];
    }

    validateAsync() {
        return promise.resolve(true);
    }

    static isEmpty(array) {
        return array && array.length === 0;
    }

    /**
     * 
     * @param {(item: EditableComponent) => boolean} filter 
     * @param {(item: EditableComponent) => boolean} ignore 
     * @returns {EditableComponent[] | null}
     */
    filterChildren(predicate, ignoree, stopWhere = null) {
        return this.filterChildrenTyped(predicate, ignoree, stopWhere);
    }
    /**
     * @returns {EditableComponent[] | null}
     */
    filterChildrenTyped(predicate = null, ignorePredicate = null, visited = new set()) {
        visited = visited == null ? new set() : visited;
        if (EditableComponent.isEmpty(this.children) || !this.children) {
            return [];
        }
        /**
         * @type {EditableComponent[]}
         */
        let result = [];
        for (const child of this.children) {
            let t = child instanceof EditableComponent ? child : null;

            if (t === null && EditableComponent.isEmpty(child.children)) {
                continue;
            }

            if (ignorePredicate && ignorePredicate(t)) {
                continue;
            }

            if (t !== null && (!predicate || predicate(t)) && !visited.has(t)) {
                visited.add(t);
                result.push(t);
            }

            result = result.concat(child.filterChildrenTyped(predicate, ignorePredicate, visited));
        }
        return result;
    }
    /**
     * 
     * @param {(ele: HTMLElement) => boolean} predicate 
     * @returns {EditableComponent[]}
     */
    findActiveComponent(predicate) {
        const showPredicate = (/** @type {HTMLElement} */ e) => {
            return !e.hidden() && predicate(e);
        }
        // @ts-ignore
        return this.children.where(showPredicate).flattern(x => showPredicate(x) ? x.children : null);
    }
    /**
     * returns the first element of the collection that satisfies the specified condition, or null if no such element is found.
     * @param {(item: EditableComponent) => boolean} filter - the condition to check for each element.
     * @returns {EditableComponent|null} the first element that satisfies the condition, or null if no such element is found.
     */
    firstOrDefault(filter) {
        return this.children.flattern(x => x.children).firstOrDefault(filter);
    }
    /**
     * 
     * @param {EditableComponent} child 
     * @param {number} index 
     * @param {(e: EditableComponent) => boolean | string} showExp 
     * @param {(e: EditableComponent) => boolean | string} disabledExp 
     * @returns 
     */
    addChild(child, index = null, showExp = null, disabledExp = null) {
        if (child.isSingleton) {
            child.render();
            return;
        }
        if (!child.parentElement) {
            if (child.componentType == null && this.tabEditor) {
                if (child.popup) {
                    child.parentElement = element || this.tabEditor.tabContainer;
                } else {
                    child.parentElement = this.tabEditor.tabContainer;
                }
            } else {
                child.parentElement = html.context;
            }
        }

        if (!child.editForm && !child.popup) {
            child.editForm = this.editForm;
        }
        if (child.meta && child.meta.entityName) {
            if (!child.entity || this.deepEqual(child.entity, this.defaultObject)) {
                child.entity = child.editForm[child.meta.entityName] ?? {
                    id: Uuid7.newGuid()
                };
            }
        }
        else {
            if (!child.entity || this.deepEqual(child.entity, this.defaultObject)) {
                child.entity = child.editForm.entity ?? {
                    id: Uuid7.newGuid()
                };
            }
        }

        if (index === null || index >= this.children.length || index < 0) {
            this.children.push(child);
        } else {
            this.children.splice(index, 0, child);
        }

        if (!child.parent) {
            child.parent = this;
        }
        html.take(child.parentElement);
        child.render();
        if (disabledExp || (child.meta && child.meta.disabledExp && child.entity)) {
            child.toggleDisabled(disabledExp || child.meta.disabledExp);
        }
        if (child.meta
            && !Utils.isNullOrWhiteSpace(child.meta.componentType)
            && !Utils.isNullOrWhiteSpace(child.entity["insertedBy"])
            && !child.isSection
            && !child.isListViewItem
            && !child.isTabComponent
            && child.entity && child.entityId && !child.entityId.startsWith("-")
            && !child.meta.canWriteAll
            && !child.isButton
            && !["isPaid", "paidDate", "btnEdit"].includes(child.meta.fieldName)
            && ((child.editForm && child.editForm.entity && child.editForm.entity["insertedBy"] != this.Token.userId && child.editForm.entity["assignId"] != this.Token.userId && !child.parent.isListViewItem)
                || child.entity["noSubmit"] || child.entity["isLock"] || child.entity["isPayment"] || child.entity["isInvoice"] || (child.entity["isPaid"] && child.parent.isListViewItem))) {
            child.disabled = true;
        }
        if (child.parent && child.parent.meta && child.parent.meta.isPublic) {
            child.disabled = false;
        }
        var userAuthentication = this.Token.userAuthorization && this.Token.userAuthorization.length > 0 ? this.Token.userAuthorization.filter(x => x.canWrite).map(x => x.userId) : [];
        if (child.meta
            && !Utils.isNullOrWhiteSpace(child.meta.componentType)
            && !Utils.isNullOrWhiteSpace(child.entity["insertedBy"])
            && !child.isSection
            && !child.isListViewItem
            && !child.isTabComponent
            && !child.meta.canWriteAll
            && ((child.parent.isListViewItem && child.parent.parent.isListView && !child.parent.parent.disabled) || !child.parent.isListViewItem)
            && !child.isButton
            && child.entity && child.entityId && !child.entityId.startsWith("-")
            && !["isPaid", "paidDate", "btnEdit"].includes(child.meta.fieldName)
            && !((child.entity["noSubmit"] || child.entity["isLock"] || child.entity["isPayment"] || child.entity["isInvoice"] || (child.entity["isPaid"] && this.parent.isListViewItem)))
            && userAuthentication.includes(child.entity["insertedBy"])) {
            child.disabled = false;
        }
        if (child.meta && child.meta.disabledExp && (child.meta.disabledExp.includes("canWrite") || child.meta.disabledExp.includes("insertedBy") || child.meta.disabledExp.includes("statusId") || child.meta.disabledExp.includes("this.Token"))) {
            child.toggleDisabled(disabledExp || child.meta.disabledExp);
        }
        // @ts-ignore
        if (showExp || (child.meta && child.meta.showExp)) {
            child.toggleShow(showExp || child.meta.showExp);
        }
        if (child.entity.isLockEdit && child.meta && !["Button", "Pdf", "excel", "email"].includes(child.meta.componentType)) {
            child.disabled = true;
        }
    }

    deepEqual(obj1, obj2) {
        if (obj1 === obj2) {
            return true;
        }

        if (typeof obj1 !== 'object' || obj1 === null || typeof obj2 !== 'object' || obj2 === null) {
            return false;
        }

        let keys1 = object.keys(obj1);
        let keys2 = object.keys(obj2);

        if (keys1.length !== keys2.length) {
            return false;
        }

        for (let key of keys1) {
            if (!keys2.includes(key) || !deepEqual(obj1[key], obj2[key])) {
                return false;
            }
        }

        return true;
    }

    removeChild(child) {
        const index = this.children.indexOf(child);
        if (index > -1) {
            this.children.splice(index, 1);
        }
    }

    focus() {
        // @ts-ignore
        this.element?.focus();
    }

    /**
     * @param {boolean} disabled
     * @param {string[]} name
     */
    setDisabled(disabled, ...name) {
        if (name == null || name.length == 0) this.disabled = disabled;
        else {
            this.filterChildren(x => name.includes(x.name)).forEach(x => x.disabled = disabled);
        }
    }

    getCircularReplacer() {
        const seen = new weakSet();
        return (key, value) => {
            if (typeof value === "object" && value !== null) {
                if (seen.has(value)) {
                    return;
                }
                seen.add(value);
            }
            return value;
        };
    }

    async runQuery() {
        const submitEntity = Utils.isFunction(this.meta.preQuery, false, this);
        const entity = {
            params: submitEntity ? JSON.stringify(submitEntity) : null,
            comId: this.meta.id,
        };
        return await Client.instance.submitAsync({
            url: "/api/feature/report",
            isRawString: true,
            jsonData: JSON.stringify(entity),
            method: "pOST"
        });
    }

    async runQuerys() {
        const submitEntity = Utils.isFunction(this.meta.preQuery, false, this);
        const entity = {
            params: submitEntity ? JSON.stringify(submitEntity) : null,
            comId: this.meta.id,
        };
        return await Client.instance.submitAsync({
            url: "/api/feature/sql",
            isRawString: true,
            jsonData: JSON.stringify(entity),
            method: "pOST"
        });
    }

    async submitObject(component, tablename) {
        Spinner.appendTo();
        if (component.id && component.id.startsWith("-")) {
            this.resetObject(component);
        }
        let componentPatch = [];
        object.getOwnPropertyNames(component).forEach(cell => {
            if (component[cell] instanceof array || (component[cell] instanceof object && !(component[cell] instanceof decimal))) {
                return;
            }
            let val;
            if (typeof component[cell] === "boolean") {
                val = component[cell] ? "1" : "0";
            } else {
                val = component[cell];
            }
            let prop = {
                Label: cell,
                field: cell,
                value: val,
            };
            componentPatch.push(prop);
        });
        let componentModel = {
            changes: componentPatch,
            table: tablename
        };
        await Client.instance.patchAsync(componentModel);
        Spinner.hide();
    }

    readObject(component, tableName) {
        if (component.id && component.id.startsWith("-")) {
            this.resetObject(component);
        }
        const changes = [];
        object.getOwnPropertyNames(component).forEach((key) => {
            const v = component[key];

            if (array.isArray(v) || (v instanceof object && !(v instanceof decimal))) return;

            changes.push({
                Label: key,
                field: key,
                value: (typeof v === "boolean") ? (v ? "1" : "0") : v
            });
        });

        return { changes: changes, table: tableName };
    }

    async readObjects(components, tableName, pre) {
        if (!array.isArray(components) || components.length === 0) return;
        const client = this.editForm.Client;
        const batchSize = 200;
        const chunks = (arr, size) => {
            const out = [];
            for (let i = 0; i < arr.length; i += size) out.push(arr.slice(i, i + size));
            return out;
        };

        const prepared = components.map((obj, i) => {
            if (typeof pre === "function") {
                const r = pre(obj, i);
                return r ?? obj;
            }
            return obj;
        });

        const models = prepared.map(o => this.readObject(o, tableName));
        for (const batch of chunks(models, batchSize)) {
            Spinner.appendTo();
            await client.patchAsync2(batch);
        }
        Spinner.hide();
    }

    GET(fieldName) {
        return this.editForm.childCom.find(x => x.meta.fieldName == fieldName);
    }

    reloadUI() {
        this.dispose();
        this.render();
    }
}
