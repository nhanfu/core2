import react from "react";
import { createRoot } from 'react-dom/client';
import { Utils } from "./utils/utils.js";
import { EditableComponent } from "./editableComponent.js";
import { ComponentExt } from './utils/componentExt.js';
import { html } from "./utils/html.js";
import { Client } from "./clients/";
import { Str } from "./utils/ext.js";
import {
    ComponentType, Action, PatchDetail,
    PatchVM, Feature, EventType, Component, SavePatchVM,
    ElementType,
} from "./models/";
import { Message } from "./utils/message.js";
import { StringBuilder } from "./utils/stringBuilder.js";
import { ComponentFactory } from "./utils/componentFactory.js";
import { Toast } from "./toast.js";
import { Label } from "./label.js";
import { ConfirmDialog } from "./confirmDialog.js";
import { ContextMenu } from "./contextMenu.js";
import decimal from "decimal.js";
import { Uuid7 } from "./structs/uuidv7.js";
import { GridView } from "./gridView.js";
import { Spinner } from "./spinner.js";
import { LangSelect } from "./utils/langSelect.js";
import { ChromeTabs } from "./chrometab.js";
import { TabComponent } from "./tabComponent.js";
import { TabEditor } from "./index.js";
import { Section } from "./section.js";
import { DevTools } from './devTools.js';
/**
 * @typedef {import('./listView.js').listView} ListView
 * represents an editable form component.
 */
export class EditForm extends EditableComponent {
    /** @type {TabEditor[]} */
    static tabs = [];
    isEditForm = true;
    Spinner = Spinner;
    /** @type {EditForm} */
    static layoutForm;
    /** @type {EditForm} */
    openFrom;
    /** @type {ListView[]} */
    listViews = [];
    /** @type {EditableComponent[]} */
    childCom = [];
    /** @type {Section[]} */
    childSection = [];
    static expiredDate = "expiredDate";
    pop = false;
    static btnExpired = "btnExpired";
    static btnSave = "btnSave";
    static btnSend = "btnSend";
    static btnApprove = "btnApprove";
    static btnReject = "btnReject";
    static statusIdField = "statusId";
    static btnCancel = "btnCancel";
    static btnPrint = "btnPrint";
    static btnPreview = "btnPreview";
    static specialEntryPoint = 'entry';
    portal = true;
    /** @type {object} */
    entity;
    isLock = false;
    /** @type {Feature} */
    meta;
    /** @type {Component[]} */
    _allCom = [];
    _li;
    get allCom() {
        if (this._allCom !== null) return this._allCom;
        if (EditForm.layoutForm === null) {
            this._allCom = this.meta.Component.slice(); // assuming Feature.Component is an array
        } else {
            this._allCom = this.meta.Component.concat(EditForm.layoutForm.meta.Component);
        }
        return this._allCom;
    }

    get featureName() {
        return this.name || this.meta?.name;
    }

    // standard property getter and setter for href
    get href() {
        return this.href;
    }

    set href(value) {
        this.href = value;
    }

    static portal = false;



    /**
     * constructor for EditForm.
     * @param {string | null} entity - the entity associated with this form.
     */
    constructor(entity = null) {
        super(null);
        this.urlSearch = new URLSearchParams(window.location.search);
        this.entity = entity;
        this.meta = new Feature();
        this.refData = [];
    }

    /**
     * gets patch data from the form entity.
     * @returns {PatchVM} the patch view model.
     */
    getPatchEntity() {
        const shouldGetAll = this.entityId == null;
        const details = this.filterChildren(child => {
            return !(child.isButton)
                && (shouldGetAll || child.dirty) && child.meta != null
                && child.name != null;
        }, x => x.isListView || x.alwaysValid || !x.populateDirty);
        const patches = details.selectMany(child => {
            if (typeof child['getPatchDetail'] === 'function') {
                return child['getPatchDetail']();
            }
            const value = Utils.getPropValue(child.entity, child.name);
            /**
             * @type {PatchDetail}
             */
            // @ts-ignore
            const patch = {
                Label: child.comLabel,
                field: child.name,
                oldVal: (child.oldValue != null) ? child.oldValue.toString() : child.oldValue?.toString(),
                value: value?.toString().trim(),
            };
            return [patch];
        })
            .distinctBy(x => x.field);
        this.addIdToPatch(details);
        /** @type {PatchVM} */
        // @ts-ignore
        const patchVM = { changes: patches, table: this.meta.entityName, queueName: this.queueName, cacheName: this.cacheName };
        return patchVM;
    }

    async saveAdd() {
        var rs = await this.savePatch();
        if (rs) {
            /**
             * @type {GridView[]}
             */
            var data = this.childCom.filter(x => x.isListView && x.meta.editable && !x.meta.isRealtime);
            /**
            * @type {GridView[]}
            */
            await this.dispatchCustomEvent(this.meta.events, "onsaveadd", this, data)
            for (const grid of data) {
                await grid.reloadData();
            }
            this.updateView();
        }
    }

    isArrayOfSavePatchVM(x) {
        if (!array.isArray(x)) {
            return false;
        }
        return x.every(item => item instanceof SavePatchVM);
    }

    async applyChanges() {
        /** @type {GridView[]}*/
        var data = this.childCom.filter(x => x.isListView && x.meta.editable && !x.meta.isRealtime);
        /**  @type {GridView[]}  */
        var gridItem = data.filter(x => x.rowData.data.length > 0);
        await this.dispatchCustomEvent(this.meta.events, "onsave", this, gridItem);
        Spinner.appendTo();
        const valid = await this.isFormValid();
        if (!valid) {
            Spinner.hide();
            return false;
        }
        var patchModels = gridItem.map(item => {
            var deletePatch = [];
            var rowData = item.allListViewItem.flatMap(x => x.entity);
            /**
             * @type {object[]}
             */
            var itemEntity = rowData.map((row) => {
                let dirtyPatchDetail = [];
                object.getOwnPropertyNames(row).forEach(cell => {
                    if (row[cell] instanceof array || (row[cell] instanceof object && !(row[cell] instanceof decimal) && !(row[cell] instanceof date))) {
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
                    patchDetail.field = cell;
                    patchDetail.oldVal = null;
                    patchDetail.value = val;
                    dirtyPatchDetail.push(patchDetail);
                });
                var patch = new SavePatchVM();
                patch.changes = dirtyPatchDetail;
                patch.table = item.meta.refName;
                patch.comId = item.meta.id;
                return patch;
            });
            if (item.deleteTempIds && item.deleteTempIds.length > 0) {
                deletePatch.push({
                    table: item.meta.refName,
                    ids: item.deleteTempIds
                });
            }
            let patchModel = new SavePatchVM();
            patchModel.changes = itemEntity;
            patchModel.table = item.meta.refName;
            patchModel.delete = deletePatch;
            return patchModel;
        });
        this.entity.relationshipDetail = patchModels;
        this.dirty = false;
        this.dispose();
    }

    getEntityPatchVM(entityForm, tableName) {
        var entity = this[entityForm];
        entity.featureName = this.meta.label;
        entity.featureName2 = this.meta.name;
        if (entity.id.startsWith("-")) {
            entity.departmentId = Client.token.departmentId;
        }
        entity.featureName3 = this.meta.name.includes("editor") ? this.meta.name.replace("-editor", "") : (this.openFrom ? this.tabEditor.meta.name : "");
        var gridItem = this.childCom.filter(x => x.isListView && x.meta.editable && !x.meta.isRealtime && x.meta.entityName == entityForm);
        let dirtyPatch = [];
        object.getOwnPropertyNames(entity).forEach(cell => {
            if (entity[cell] instanceof array || (entity[cell] instanceof object && !(entity[cell] instanceof decimal)) || cell == this._groupKey) {
                return;
            }
            let val;
            if (typeof entity[cell] === "boolean") {
                val = entity[cell] ? "1" : "0";
            } else {
                val = entity[cell];
            }
            let patchDetail = new PatchDetail();
            patchDetail.Label = cell;
            patchDetail.field = cell;
            patchDetail.oldVal = null;
            patchDetail.value = val;
            var component = this.childCom.find(x => x.meta.fieldName == cell && x.meta.entityName == entityForm && x.componentType != "word" && x.componentType != "CodeEditor")
            if (component) {
                let text = component.getValueText()?.toString();
                let actText = Utils.isNullOrWhiteSpace(text) ? 'n/A' : text;
                let oldText = Utils.isNullOrWhiteSpace(component.originalText) ? 'n/A' : component.originalText;
                if (actText != oldText) {
                    patchDetail.historyValue = `[${component.meta.label}]: ${oldText} => ${actText}`;
                }
            }
            dirtyPatch.push(patchDetail);
        });
        var deletePatch = [];
        var patchModels = gridItem.map(item => {
            if (item.deleteTempIds && item.deleteTempIds.length > 0) {
                deletePatch.push({
                    table: item.meta.refName,
                    ids: item.deleteTempIds
                });
            }
            item.allListViewItem.filter(x => !x.groupRow).forEach((it, index) => {
                it.entity.order = index + 1;
            });
            var allItem = item.allListViewItem.filter(x => !x.groupRow && !x.entity.noSubmit && !x.entity.isLock);
            if (allItem && allItem.length > 0) {
                allItem.forEach(it => {
                    var multiples = it.children.filter(x => x.isMultiple);
                    if (multiples && multiples.length > 0) {
                        multiples.forEach(item1 => {
                            it.entity[item1.meta.fieldName + "text"] = item1.matchedItems && item1.matchedItems.length > 0 ? item1.matchedItems.map(item2 => item1.getMatchedText(item2)).join(item1.meta.groupFormat || ',') : null;
                        })
                    }
                })
                var rowData = allItem;
                /**
                 * @type {object[]}
                 */
                var itemEntity = rowData.map((rowItem, index3) => {
                    var row = rowItem.entity;
                    if (row.id.startsWith("-")) {
                        row.departmentId = Client.token.departmentId;
                    }
                    let dirtyPatchDetail = [];
                    object.getOwnPropertyNames(row).forEach(cell => {
                        if (row[cell] instanceof array || (row[cell] instanceof object && !(row[cell] instanceof decimal) && !(row[cell] instanceof date)) || cell == this._groupKey) {
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
                        patchDetail.field = cell;
                        patchDetail.oldVal = null;
                        patchDetail.value = val;
                        var component = rowItem.children.find(x => x.meta.fieldName == cell)
                        if (component) {
                            let text = component.getValueText();
                            let actText = Utils.isNullOrWhiteSpace(text) ? 'n/A' : text;
                            let oldText = Utils.isNullOrWhiteSpace(component.originalText) ? 'n/A' : component.originalText;
                            var index2 = index3 + 1;
                            if (actText != oldText) {
                                patchDetail.historyValue = `table: [${(item.parent.meta.label || item.meta.label)}] row: [${index2}] [${component.meta.label}]: ${oldText} => ${actText}`;
                            }
                        }
                        dirtyPatchDetail.push(patchDetail);
                    });
                    var patch = new PatchVM();
                    patch.changes = dirtyPatchDetail;
                    patch.table = item.meta.refName;
                    patch.comId = item.meta.id;
                    return patch;
                });
                return itemEntity;
            }
        });
        let patchModel = new SavePatchVM();
        patchModel.changes = dirtyPatch;
        patchModel.table = tableName;
        patchModel.detail = patchModels.filter(x => x);
        patchModel.delete = deletePatch;
        return patchModel;
    }

    getObjectPatchVM(entity, tableName) {
        let dirtyPatch = [];
        object.getOwnPropertyNames(entity).forEach(cell => {
            if (entity[cell] instanceof array || (entity[cell] instanceof object && !(entity[cell] instanceof decimal)) || cell == this._groupKey) {
                return;
            }
            let val;
            if (typeof entity[cell] === "boolean") {
                val = entity[cell] ? "1" : "0";
            } else {
                val = entity[cell];
            }
            let patchDetail = new PatchDetail();
            patchDetail.Label = cell;
            patchDetail.field = cell;
            patchDetail.oldVal = null;
            patchDetail.value = val;
            dirtyPatch.push(patchDetail);
        });
        let patchModel = new SavePatchVM();
        patchModel.changes = dirtyPatch;
        patchModel.table = tableName;
        return patchModel;
    }
    /**
     * 
     * @returns {SavePatchVM}
     */
    getPatchVM() {
        var gridItem = this.childCom.filter(x => x.isListView && x.meta.editable && !x.meta.isRealtime && Utils.isNullOrWhiteSpace(x.meta.entityName));
        let dirtyPatch = [];
        if (this.entityId && this.entityId.startsWith("-")) {
            this.entity.departmentId = Client.token.departmentId;
        }
        object.getOwnPropertyNames(this.entity).forEach(cell => {
            if (this.entity[cell] instanceof array || (this.entity[cell] instanceof object && !(this.entity[cell] instanceof decimal)) || cell == this._groupKey) {
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
            patchDetail.field = cell;
            patchDetail.oldVal = null;
            patchDetail.value = val;
            var component = this.childCom.find(x => x.meta.fieldName == cell && Utils.isNullOrWhiteSpace(x.meta.entityName))
            if (component) {
                let text = component.getValueText();
                let actText = Utils.isNullOrWhiteSpace(text) ? 'n/A' : text;
                let oldText = Utils.isNullOrWhiteSpace(component.originalText) ? 'n/A' : component.originalText;
                if (actText != oldText) {
                    patchDetail.historyValue = `[${component.meta.label}]: ${oldText} => ${actText}`;
                }
                if (component.isInput) {
                    patchDetail.value = patchDetail.value ? patchDetail.value?.toString().trim() : patchDetail.value;
                }
            }
            dirtyPatch.push(patchDetail);
        });
        var deletePatch = [];
        var patchModels = gridItem.map(item => {
            if (item.deleteTempIds && item.deleteTempIds.length > 0) {
                deletePatch.push({
                    table: item.meta.refName,
                    ids: item.deleteTempIds
                });
            }
            item.allListViewItem.filter(x => !x.groupRow).forEach((it, index) => {
                it.entity.order = index + 1;
            });
            var allItem = item.allListViewItem.filter(x => !x.groupRow);
            if (allItem && allItem.length > 0) {
                allItem.forEach(it => {
                    var multiples = it.children.filter(x => x.isMultiple);
                    if (multiples && multiples.length > 0) {
                        multiples.forEach(item1 => {
                            it.entity[item1.meta.fieldName + "text"] = item1.matchedItems && item1.matchedItems.length > 0 ? item1.matchedItems.map(item2 => item1.getMatchedText(item2)).join(item1.meta.groupFormat || ',') : null;
                        })
                    }
                })
                var itemEntity = allItem.map((rowItem, index3) => {
                    var row = rowItem.entity;
                    let dirtyPatchDetail = [];
                    if (row.id.startsWith("-")) {
                        row.departmentId = Client.token.departmentId;
                    }
                    object.getOwnPropertyNames(row).forEach(cell => {
                        if (row[cell] instanceof array || (row[cell] instanceof object && !(row[cell] instanceof decimal) && !(row[cell] instanceof date)) || cell == this._groupKey) {
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
                        patchDetail.field = cell;
                        patchDetail.oldVal = null;
                        patchDetail.value = val;
                        var component = rowItem.children.find(x => x.meta.fieldName == cell);
                        if (component) {
                            let text = component.getValueText();
                            let actText = Utils.isNullOrWhiteSpace(text) ? 'n/A' : text;
                            let oldText = Utils.isNullOrWhiteSpace(component.originalText) ? 'n/A' : component.originalText;
                            var index2 = index3 + 1;
                            if (actText != oldText) {
                                patchDetail.historyValue = `table: [${(item.parent.meta.label || item.meta.label)}] row: [${index2}] [${component.meta.label}]: ${oldText} => ${actText}`;
                            }
                            if (component.isInput) {
                                if (patchDetail.value instanceof string) {
                                    patchDetail.value = patchDetail.value ? patchDetail.value.toString().trim() : patchDetail.value;
                                }
                                else {
                                    patchDetail.value = patchDetail.value;
                                }
                            }
                        }
                        dirtyPatchDetail.push(patchDetail);
                    });
                    var patch = new PatchVM();
                    patch.changes = dirtyPatchDetail;
                    patch.table = item.meta.refName;
                    patch.comId = item.meta.id;
                    return patch;
                });
                return itemEntity;
            }
        });
        let patchModel = new SavePatchVM();
        patchModel.changes = dirtyPatch;
        patchModel.table = this.meta.entityId;
        patchModel.detail = patchModels.filter(x => x);
        patchModel.delete = deletePatch;
        return patchModel;
    }
    /**
     * 
     * @returns {SavePatchVM}
     */
    getPatchSelectVM() {
        var gridItem = this.childCom.filter(x => x.isListView && x.meta.editable && !x.meta.isRealtime && Utils.isNullOrWhiteSpace(x.meta.entityName));
        let dirtyPatch = [];
        if (this.entityId && this.entityId.startsWith("-")) {
            this.entity.departmentId = Client.token.departmentId;
        }
        object.getOwnPropertyNames(this.entity).forEach(cell => {
            if (this.entity[cell] instanceof array || (this.entity[cell] instanceof object && !(this.entity[cell] instanceof decimal)) || cell == this._groupKey) {
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
            patchDetail.field = cell;
            patchDetail.oldVal = null;
            patchDetail.value = val;
            var component = this.childCom.find(x => x.meta.fieldName == cell && Utils.isNullOrWhiteSpace(x.meta.entityName))
            if (component) {
                let text = component.getValueText();
                let actText = Utils.isNullOrWhiteSpace(text) ? 'n/A' : text;
                let oldText = Utils.isNullOrWhiteSpace(component.originalText) ? 'n/A' : component.originalText;
                if (actText != oldText) {
                    patchDetail.historyValue = `[${component.meta.label}]: ${oldText} => ${actText}`;
                }
            }
            dirtyPatch.push(patchDetail);
        });
        var deletePatch = [];
        var patchModels = gridItem.map(item => {
            if (item.deleteTempIds && item.deleteTempIds.length > 0) {
                deletePatch.push({
                    table: item.meta.refName,
                    ids: item.deleteTempIds
                });
            }
            item.allListViewItem.filter(x => !x.groupRow).forEach((it, index) => {
                it.entity.order = index + 1;
            });
            var allItem = item.allListViewItem.filter(x => !x.groupRow && !x.entity.noSubmit && !x.entity.isLock && x.selected);
            if (allItem && allItem.length > 0) {
                allItem.forEach(it => {
                    var multiples = it.children.filter(x => x.isMultiple);
                    if (multiples && multiples.length > 0) {
                        multiples.forEach(item1 => {
                            it.entity[item1.meta.fieldName + "text"] = item1.matchedItems && item1.matchedItems.length > 0 ? item1.matchedItems.map(item2 => item1.getMatchedText(item2)).join(item1.meta.groupFormat || ',') : null;
                        })
                    }
                })
                var itemEntity = allItem.map((rowItem, index3) => {
                    var row = rowItem.entity;
                    if (row.id.startsWith("-")) {
                        row.departmentId = Client.token.departmentId;
                    }
                    let dirtyPatchDetail = [];
                    object.getOwnPropertyNames(row).forEach(cell => {
                        if (row[cell] instanceof array || (row[cell] instanceof object && !(row[cell] instanceof decimal) && !(row[cell] instanceof date)) || cell == this._groupKey) {
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
                        patchDetail.field = cell;
                        patchDetail.oldVal = null;
                        patchDetail.value = val;
                        var component = rowItem.children.find(x => x.meta.fieldName == cell)
                        if (component) {
                            let text = component.getValueText();
                            let actText = Utils.isNullOrWhiteSpace(text) ? 'n/A' : text;
                            let oldText = Utils.isNullOrWhiteSpace(component.originalText) ? 'n/A' : component.originalText;
                            var index2 = index3 + 1;
                            if (actText != oldText) {
                                patchDetail.historyValue = `table: [${(item.parent.meta.label || item.meta.label)}] row: [${index2}] [${component.meta.label}]: ${oldText} => ${actText}`;
                            }
                        }
                        dirtyPatchDetail.push(patchDetail);
                    });
                    var patch = new PatchVM();
                    patch.changes = dirtyPatchDetail;
                    patch.table = item.meta.refName;
                    patch.comId = item.meta.id;
                    return patch;
                });
                return itemEntity;
            }
        });
        let patchModel = new SavePatchVM();
        patchModel.changes = dirtyPatch;
        patchModel.table = this.meta.entityId;
        patchModel.detail = patchModels.filter(x => x);
        patchModel.delete = deletePatch;
        return patchModel;
    }

    async reRenderUI() {
        this.childCom = [];
        this.childSection = [];
        this.tabGroup = [];
        this.tabComponents = [];
        this.children = [];
        this.listViews = [];
        html.take(this.titleCenterElement).clear();
        html.take(this.popUpMenu).clear();
        await this.loadMeta();
    }

    async loadMeta() {
        var feature = await ComponentExt.loadFeature(this.entity);
        if (!feature) {
            return null;
        }
        this.meta = feature;
        if (feature.codeId) {
            var featureParent = await Client.instance.getByIdAsync("Feature", [feature.codeId]);
            ComponentExt.assignMethods(featureParent.data[0], this);
        }
        if (feature.script) {
            ComponentExt.assignMethods(feature, this);
        }
        this.groupTree = [];
        this.setCurrentUserProperties();
        this.groupTree = this.buildTree(feature.componentGroup);
        this.setFeatureProperties(feature);
        this.setFeatureStyleSheet(feature.styleSheet);
        this.policies = feature.featurePolicies;
    }

    getEntityIds(header, entities) {
        if (!entities || entities.length === 0) {
            return [];
        }

        let ids = [];
        entities.forEach(x => {
            let id = !x[header.fieldName] ? null : x[header.fieldName].toString();
            if (!id) {
                return;
            } else if (id.includes(',')) {
                ids.push(...id.split(',').map(y => y));
            } else {
                ids.push(id);
            }
        });
        return ids;
    }

    formatDataSourceByEntity(currentHeader, allHeaders, entities) {
        let entityIds = allHeaders
            .filter(x => x.refName === currentHeader.refName)
            .flatMap(x => this.getEntityIds(x, entities))
            .filter((v, i, a) => a.indexOf(v) === i);

        if (entityIds.length === 0) {
            return null;
        }

        currentHeader.dataSourceOptimized = entityIds.sort();
        return currentHeader;
    }

    async loadMasterData(entity) {
        var newEntity = entity || this.entity;
        if (!newEntity) {
            return;
        }
        var headers = this.meta.componentGroup.flatMap(x => x.components).filter(x => x && !Utils.isNullOrWhiteSpace(x.refName) && x.componentType == "dropdown");
        this.header = headers;
        var rows = [newEntity];
        let dataSource = headers.filter((obj, index, self) =>
            index === self.findIndex((t) => (
                t.refName === obj.refName
            ))
        ).map(x => this.formatDataSourceByEntity(x, headers, rows)).filter(x => x !== null);
        if (dataSource.length == 0) {
            return;
        }

        let dataTasks = dataSource.filter(x => x.dataSourceOptimized).map(x => ({
            tableName: x.refName,
            ids: x.dataSourceOptimized,
            header: x
        }));
        var results2 = await Client.instance.getByIdsAsync(dataTasks);
        results2.forEach((task, index) => {
            if (task && task.length == 0) {
                return;
            }
            this.setRemoteSource(task, dataTasks[index].header.refName, dataTasks[index].header);
        });
        this.syncMasterData(rows, headers);
    }

    setRemoteSource(remoteData, typeName, header) {
        let localSource = this.refData[typeName];
        if (!localSource) {
            this.refData[typeName] = remoteData;
        } else {
            remoteData.forEach(item => {
                if (!localSource.some(localItem => localItem[this.idField] === item[this.idField])) {
                    localSource.push(item);
                }
            });
        }
        var headers = this.header.filter(x => x.refName == header.refName);
        headers.forEach(item => {
            item.localData = remoteData;
        })
    }

    syncMasterData(rows = null, headers = null) {
        rows = rows || this.rowData.data;
        headers = headers || this.header;

        headers.filter(x => x.refName).forEach(header => {
            if (!header.fieldName || header.fieldName.length <= 2) {
                return;
            }
            let containId = header.fieldName.substr(header.fieldName.length - 2) === this.idField;
            let objField = "";
            if (containId) {
                objField = header.fieldName.substr(0, header.fieldName.length - 2);
            }
            else {
                objField = header.fieldName + "masterData";
            }
            rows.forEach(row => {
                let propType = header.refName;
                if (!propType) {
                    return;
                }

                let propVal = row[objField];
                let found = this.refData[propType]?.find(source => source[this.idField] === row[header.fieldName]);
                if (found) {
                    row[objField] = found;
                } else if (propVal && !found) {
                    this.refData[propType] = this.refData[propType] || [];
                    this.refData[propType].push(propVal);
                }
            });
        });
        var locals = this.header.filter(x => ["dropdown"].some(y => y == x.componentType) && Utils.isNullOrWhiteSpace(x.refName));
        for (const header of locals) {
            let containId = header.fieldName.substr(header.fieldName.length - 2) === this.idField;
            let objField = "";
            if (containId) {
                objField = header.fieldName.substr(0, header.fieldName.length - 2);
            }
            else {
                objField = header.fieldName + "masterData";
            }
            rows.forEach(row => {
                var data = Utils.isFunction(header.query, false, this);
                if (data) {
                    let found = data.find(source => source[this.idField] === row[header.fieldName]);
                    if (found) {
                        row[objField] = found;
                    }
                }
            });
        }
    }

    _groupKey = "__groupkey__";
    async savePatch(element, entity, dirty, reloadData, message = true) {
        if (this.openFrom && this.openFrom.isTab) {
            this.entity.Url = `${Client.baseUri}/#/${(!this.Token ? "app" : this.Token.tenantCode)}/${this.openFrom.featureName}?popup=${this.featureName}&id=${(this.entity.id.startsWith("-") ? "" : "")}`;
        }
        if (!this.dirty && !dirty && this.entityId && !this.entityId.startsWith("-")) {
            Toast.warning(Message.notDirty);
            return false;
        }
        var methodUnique = this.checkSave;
        if (methodUnique) {
            let taskUnique = await methodUnique.apply(this, this);
            if (taskUnique) {
                await this.dispatchCustomEvent(this.meta.events, "unique", this);
                return;
            }
        }
        try {
            if (this.entity.id.startsWith("-")) {
                this.entity.isSend = false;
                this.entity.isLockEdit = false;
            }
            this.entity.featureName = this.meta.label;
            this.entity.featureName2 = this.meta.name;
            this.entity.featureName3 = this.meta.name.includes("editor") ? this.meta.name.replace("-editor", "") : (this.openFrom ? this.tabEditor.meta.name : "");
            /** @type {GridView[]}*/
            var gridItem = this.childCom.filter(x => x.isListView && x.meta.editable && !x.meta.isRealtime && Utils.isNullOrWhiteSpace(x.meta.entityName));
            var multiples = this.childCom.filter(x => x.isMultiple && Utils.isNullOrWhiteSpace(x.meta.entityName));
            if (multiples && multiples.length > 0) {
                multiples.forEach(item => {
                    this.entity[item.meta.fieldName + "text"] = item.matchedItems && item.matchedItems.length > 0 ? item.matchedItems.map(item1 => item.getMatchedText(item1)).join(item.meta.groupFormat || ',') : null;
                })
            }
            Spinner.appendTo();
            if (!dirty) {
                const valid = await this.isFormValid();
                if (!valid) {
                    Spinner.hide();
                    return false;
                }
            }
            await this.dispatchCustomEvent(this.meta.events, "onsave", this, gridItem);
            var patchModel = this.getPatchVM();
            const rs = await Client.instance.patchAsync(patchModel);
            var childEntity = this.childCom.find(x => !Utils.isNullOrWhiteSpace(x.meta.entityName) && !Utils.isNullOrWhiteSpace(x.meta.tableName));
            Spinner.hide();
            if (rs.status == 200) {
                var codeEditor = this.childCom.filter(x => (x.meta.componentType == "CodeEditor" || x.meta.componentType == "word") && !Utils.isNullOrWhiteSpace(x.meta.refName) && Utils.isNullOrWhiteSpace(x.meta.entityName));
                codeEditor.forEach(async item => {
                    if (rs.updatedItem[0][item.meta.fieldName] == item.oldValue) {
                        return;
                    }
                    let dirtyPatchDetail = [
                        {
                            Label: "value",
                            field: "value",
                            oldVal: null,
                            value: rs.updatedItem[0][item.meta.fieldName],
                        },
                        {
                            Label: "oldValue",
                            field: "oldValue",
                            oldVal: null,
                            value: item.oldValue,
                        },
                        {
                            Label: this.idField,
                            field: this.idField,
                            oldVal: null,
                            value: Uuid7.newGuid(),
                        },
                        {
                            Label: "componentId",
                            field: "componentId",
                            oldVal: null,
                            value: item.meta.id
                        },
                        {
                            Label: "recordId",
                            field: "recordId",
                            oldVal: null,
                            value: this.entity.id
                        }
                    ]
                    let patchModelDetail = {
                        changes: dirtyPatchDetail,
                        table: item.meta.refName,
                        notMessage: true
                    };
                    await Client.instance.patchAsync(patchModelDetail);
                })
                this.entity = rs.updatedItem[0];
                this.dirty = false;
                if (this.openFrom && this.openFrom.devTools) {
                    await this.openFrom.devTools.reRenderUI();
                }
                if (rs.detail && rs.detail.length > 0) {
                    for (const grid of gridItem) {
                        grid.deleteTempIds = [];
                        if (!grid.allListViewItem || grid.allListViewItem.length == 0) {
                            continue;
                        }
                        var dataItem = rs.detail.find(x => x.comId == grid.meta.id).data;
                        if (!dataItem) {
                            continue;
                        }
                        await grid.loadMasterData(dataItem);
                        grid.rowData.data = dataItem;
                        for (const item of grid.allListViewItem) {
                            if (item.entity && item.entity.id) {
                                var entity = dataItem.find(x => item.entity.id.includes(x.id));
                                if (entity) {
                                    item.entity = entity;
                                    item.updateView(true);
                                }
                            }
                        }
                    }
                }
                if (childEntity) {
                    if (!this.dirtyEntity(childEntity.meta.entityName)) {
                        if (message) {
                            Toast.success("update success");
                        }
                    }
                }
                else {
                    if (message) {
                        Toast.success("update success");
                    }
                }
                await this.dispatchCustomEvent(this.meta.events, "saved", this, this.openFrom);
                this.updateView(true);
                if (this.openFrom && this.openFrom.isTab) {
                    var parent = this.openFrom.tabComponents.filter(x => Utils.isNullOrWhiteSpace(x.meta.entityName))
                    if (parent && parent.length == 0) {
                        var gridDetail = this.openFrom.childCom.find(x => x.isListView && x.meta.refName == this.meta.entityId);
                        if (reloadData && gridDetail) {
                            gridDetail.reloadData();
                        }
                        else {
                            if (gridDetail) {
                                var listViewItem = gridDetail.allListViewItem.find(x => x.entity.id == this.entity.id);
                                if (listViewItem != null) {
                                    listViewItem.entity = this.entity;
                                    await gridDetail.loadMasterData([listViewItem.entity]);
                                    listViewItem.updateView(false);
                                }
                                else {
                                    gridDetail.reloadData();
                                }
                            }
                        }
                    }
                    else {
                        for (const element of parent) {
                            await element.countBadge();
                            var gridDetail = element.filterChildren(x => x.isListView).find(x => x.isListView && x.meta.refName == this.meta.entityId);
                            if (gridDetail) {
                                if (reloadData) {
                                    gridDetail.reloadData();
                                }
                                else {
                                    var listViewItem = gridDetail.allListViewItem.find(x => x.entity.id == this.entity.id);
                                    if (listViewItem != null) {
                                        listViewItem.entity = this.entity;
                                        await gridDetail.loadMasterData([listViewItem.entity]);
                                        listViewItem.updateView(false);
                                    }
                                    else {
                                        gridDetail.reloadData();
                                    }
                                }
                            }
                        }
                    }
                }
                this.focus();
                return true;
            }
            else {
                if (rs.Message) {
                    this.editForm.openConfig(rs.Message, () => {
                    }, () => { }, false, [], true);
                }
            }
            return false;
        } catch (error) {
            if (error.Message) {
                this.editForm.openConfig(error.Message, () => {
                }, () => { }, false, [], true);
            }
            else {
                //Toast.warning("unstable network connection. please check your internet connection and press ctrl+f5 to refresh.");
            }
            Spinner.hide();
            return false;
        }
    }

    async saveSelectedDetail() {
        try {
            /** @type {GridView[]}*/
            var multiples = this.childCom.filter(x => x.isMultiple && Utils.isNullOrWhiteSpace(x.meta.entityName));
            if (multiples && multiples.length > 0) {
                multiples.forEach(item => {
                    this.entity[item.meta.fieldName + "text"] = item.matchedItems && item.matchedItems.length > 0 ? item.matchedItems.map(item1 => item.getMatchedText(item1)).join(item.meta.groupFormat || ',') : null;
                })
            }
            Spinner.appendTo();
            const valid = await this.isFormValid();
            if (!valid) {
                Spinner.hide();
                return false;
            }
            this.entity.featureName = this.meta.label;
            this.entity.featureName2 = this.meta.name;
            this.entity.featureName3 = this.meta.name.includes("editor") ? this.meta.name.replace("-editor", "") : (this.openFrom ? this.tabEditor.meta.name : "");
            var patchModel = this.getPatchSelectVM();
            const rs = await Client.instance.patchAsync(patchModel);
            if (rs.status == 200) {
                this.entity = rs.updatedItem[0];
                this.dirty = false;
                this.updateView(true);
                this.focus();
                return true;
            }
            else {
                if (error.Message) {
                    this.editForm.openConfig(error.Message, () => {
                    }, () => { }, false, [], true);
                }
                else {
                }
            }
            return false;
        } catch (error) {
            Toast.warning(error.Message);
            Spinner.hide();
            return false;
        }
    }
    async saveEntity(entityForm, tableName) {
        var currentEntity = this[entityForm];
        if (!this.dirtyEntity(entityForm) && !this.entityId.startsWith("-")) {
            return false;
        }
        try {
            Spinner.appendTo();
            /** @type {GridView[]}*/
            var multiples = this.childCom.filter(x => x.isMultiple && x.meta.entityName == entityForm);
            if (multiples && multiples.length > 0) {
                multiples.forEach(item => {
                    currentEntity[item.meta.fieldName + "text"] = item.matchedItems && item.matchedItems.length > 0 ? item.matchedItems.map(item1 => item.getMatchedText(item1)).join(item.meta.groupFormat || ',') : null;
                })
            }
            var patchModel = this.getEntityPatchVM(entityForm, tableName);
            var addRow = patchModel.changes.find(x => x.field == this.idField).value.startsWith("-");
            const rs = await Client.instance.patchAsync(patchModel);
            Spinner.hide();
            if (rs.status == 200) {
                this[entityForm] = rs.updatedItem[0];
                this.dirty = false;
                var comListView = this.childCom.find(x => x.meta.refName == tableName && x.meta.fieldName == entityForm && x.isListView);
                if (comListView) {
                    if (!addRow) {
                        comListView.allListViewItem.forEach(listItem => {
                            if (listItem.entity.id == rs.updatedItem[0].id) {
                                listItem.entity = rs.updatedItem[0];
                            }
                            listItem.updateView(true);
                        });
                    }
                    else {
                        comListView.reloadData();
                    }
                }
                Toast.success("update success");
                this.updateView2(true, false, entityForm);
                window.setTimeout(async () => {
                    var methodUnique = this.checkSaveDetail;
                    if (methodUnique) {
                        await methodUnique.apply(this, this);
                    }
                }, 1000)
                Spinner.hide();
                return true;
            }
            else {
                if (!addRow && rs.updatedItem[0]) {
                    var comListView = this.childCom.find(x => x.meta.refName == tableName && x.isListView);
                    if (comListView) {
                        if (!addRow) {
                            comListView.allListViewItem.forEach(listItem => {
                                if (listItem.entity.id == rs.updatedItem[0].id) {
                                    listItem.entity = rs.updatedItem[0];
                                }
                                listItem.updateView(true);
                            });
                        }
                        else {
                            comListView.reloadData();
                        }
                    }
                }
                this.editForm.openConfig(rs.Message || "update detail fail", () => {
                }, () => { }, false, [], true)
            }
            return false;
        } catch (error) {
            if (error.Message) {
                this.editForm.openConfig(error.Message, () => {
                }, () => { }, false, [], true);
            }
            else {
            }
            Spinner.hide();
            return false;
        }
    }
    /** @type {TabComponent[]} */
    TabGroup = [];
    /** @type {TabComponent[]} */
    tabComponents = [];
    /** @type {import('./section.js')} */
    sectionMd;
    popup = false;
    /**
     * loads and renders features based on the current entity setup.
     * @param {function} callback - Optional callback to run after loading and rendering.
     */
    async loadFeatureAndRender(callback = null) {
        Spinner.appendTo();
        this.sectionMd = this.sectionMd || await import('./section.js');
        var feature = await ComponentExt.loadFeature(this.entity);
        if (!feature) {
            return null;
        }
        this.meta = feature;
        if (feature.codeId) {
            var featureParent = await Client.instance.getByIdAsync("Feature", [feature.codeId]);
            ComponentExt.assignMethods(featureParent.data[0], this);
        }
        if (feature.script) {
            ComponentExt.assignMethods(feature, this);
        }
        var entity = await this.loadEntity();
        if (this.entityId && this.entityId.startsWith("-")) {
            if (!feature.featurePolicies.some(x => Client.token.roleIds.includes(x.roleId) && (x.canWrite || x.canWriteAll))) {
                Spinner.hide();
                this.openConfig("access denied", () => {
                }, () => { }, false, [], true)
                return;
            }
        }
        if (this.popup) {
            const handler = this.dirtyCheckAndCancel.bind(this);
            const handlerHistory = await this.viewHistory.bind(this);
            const handlerTrash = await this.hardDeleteSelected.bind(this);
            if (!this.isChild) {
                html.take(this.parentElement ?? this.parent?.element ?? TabEditor.tabContainer)
                    .div.className("backdrop").tabIndex(-1).trigger(EventType.focus).event(EventType.keyDown, this.hotKeyHandler.bind(this));
                this._backdrop = html.context;
                html.instance.div.className("popup-content").style(this.meta.style);
                //code cho phép kéo thả popup
                this.popupContent = html.context;
                html.instance.div.className("popup-title").span.iText(this.title, feature.id);
                this.titleElement = html.context;
                html.instance.end.div.className("title-center");
                this.titleCenterElement = html.context;
                if (Client.systemRole) {
                    this.titleElement.addEventListener("contextmenu", (e) => this.sysConfigMenu(e, null, null, null));
                }
                html.instance.end.div.className("icon-box d-flex").style("display: flex; gap: 20px; align-items: center;");
                html.span.className("fal fa-history")
                    .event(EventType.click, handlerHistory).end
                    .span.className("fa fa-times")
                    .event(EventType.click, handler).end.end.end.div.className("popup-body");
                this.element = html.context;
                html.instance.end.div.className("popup-footer");
                this.popUpMenu = html.context;
            }
            else {
                html.take(this.parentElement);
                this.element = html.context;
            }
        }
        Spinner.hide();
        this.layoutLoaded(feature, callback, entity);
    }

    async hardDeleteSelected() {
        var deletedItems = [this.entity];
        var check = deletedItems.some(x => (x["statusId"] && [2, 3].includes(x["statusId"]) && !x["noApproved"] && !x["isUse"]) || x["noSubmit"] || x["isLock"] || x["isPayment"] || x["isInvoice"] || x["isPaymentAcc"] || x["isDebtAcc"]);
        if (deletedItems.length == 0 || check) {
            return;
        }
        const confirmDialog = new ConfirmDialog();
        confirmDialog.title = "are you sure you want to delete the selected entity?";
        confirmDialog.pElement = this.editForm.element;
        confirmDialog.editForm = this.editForm;
        confirmDialog.render();
        confirmDialog.yesConfirmed.add(() => {
            this.hardDeleteConfirmed(deletedItems).then(async rs => {
                if (rs) {
                    await this.dispatchCustomEvent(this.meta.events, customEventType.afterDeleted, this, deletedItems);
                }
            });
        });
    }

    async hardDeleteConfirmed(deletedItems) {
        const ids = deletedItems.map(x => x[this.idField]).filter(x => !x.startsWith('-'));
        var grid = this.childCom.find(boolean);
        const result = await Client.instance.hardDeleteAsync(ids, this.meta.entityId, null, grid.meta.id);
        if (result) {
            Toast.success("deleted successfully");
            this.dirtyCheckAndCancel();
            return true;
        } else {
            this.editForm.openConfig("the selected data cannot be deleted. please check the data.", () => {
            }, () => { }, false, [], true);
            return false;
        }
    }

    disposeViewHistory() {
        this._history.innerHTML = null;
    }

    /**
    * renders the view history popup for the selected row.
    * @param {object} currentItem the currently selected row item.
    */
    async viewHistory() {
        var currentItem = this.editForm.entity;
        if (!currentItem) {
            return;
        }
        html.take(this.tabEditor.element).div.className("backdrop")
            .style("align-items: baseline;");
        this._history = html.context;
        html.instance.div.tabIndex(-1).escape((e) => this.disposeViewHistory.bind(this)).className("popup-content confirm-dialog history-view").style("top: 0;")
            .div.className("popup-title").innerHTML("view history change")
            .div.className("icon-box").span.className("fal fa-times")
            .event(EventType.click, () => this._history.remove())
            .endOf(".popup-title")
            .div.className("card-body panel group");
        const body = html.context;
        var coms = await Client.instance.getService("history change");
        var com = coms[0][0];
        com.row = 50;
        var params = {
            recordId: currentItem.id,
            tableName: this.meta.entityId
        }
        com.columns = [
            {
                statusBar: true,
                order: 0,
                Label: '',
                frozen: true
            },
            {
                fieldName: "textContent",
                order: 1,
                componentType: "input",
                Label: "history",
                canRead: true,
                canWrite: true,
                canReadAll: true,
                canWriteAll: true,
                width: "80%",
                minWidth: "80%",
                maxWidth: "80%",
            },
            {
                fieldName: "insertedBy",
                componentType: "dropdown",
                refName: "user",
                order: 2,
                canRead: true,
                canWrite: true,
                canReadAll: true,
                canWriteAll: true,
                formatData: `<div class="user-avatar">
                    <img src="{avatar}" alt="{fullName}" class="avatar">
                    <a class="full-name">{fullName}</a>
                </div>`,
                Label: "inserted by",
                width: "10%",
                minWidth: "10%",
                maxWidth: "10%",
            },
            {
                fieldName: "insertedDate",
                order: 3,
                canRead: true,
                canWrite: true,
                canReadAll: true,
                canWriteAll: true,
                componentType: "Datepicker",
                formatData: "DD/MM/YYYY hH:mm",
                Label: "inserted date",
                width: "10%",
                minWidth: "10%",
                maxWidth: "10%",
            }
        ]
        com.preQuery = JSON.stringify(params);
        com.canSearch = false;
        const md = await import('./gridView.js');
        const _filterGrid = new md.gridView(com);
        _filterGrid.canDelete = false;
        _filterGrid.parentElement = body;
        this.tabEditor.addChild(_filterGrid);
        _filterGrid.element.style.width = "100%";
        _filterGrid.element.style.height = "calc(100vh - 22rem)";
    }

    render() {
        if (!this.meta.layout) {
            this.loadFeatureAndRender();
        }
        else {
            if (!this.element) {
                this.element = this.parentElement;
            }
            html.take(this.element);
            html.instance.clear();
            html.instance.div.render();
            this.element = html.context;
            let root = createRoot(this.element);
            let reactElement = react.createElement(this.meta.layout);
            root.render(reactElement);
            new Promise(resolve => setTimeout(resolve, 0)).then(() => {
                if (this.meta.Javascript && !Utils.isNullOrWhiteSpace(this.meta.Javascript)) {
                    try {
                        let fn = new Function("editForm", this.meta.Javascript);
                        let obj = fn.call(null, this.editForm);
                        for (let prop in obj) {
                            this[prop] = obj[prop].bind(this);
                        }
                        const method = this["init"];
                        if (method) {
                            new Promise((resolve, reject) => {
                                let task = method.apply(this, this);
                                if (!task || task.isCompleted == null) {
                                    resolve(task);
                                } else {
                                    task.then(() => resolve(task)).catch(e => reject(e));
                                }
                            });
                        }
                    } catch (e) {
                        console.log(e.Message);
                    }
                }
            });
            this.focus();
        }
        this.lastForm = this;
    }
    /** @type {HTMLElement} */
    popupFooter;
    /** @type {HTMLElement} */
    popUpMenu;
    /**
     * focuses the tab editor component, updating the document title and potentially the uRL.
     */
    focus() {
        if (!this.popup && this.isLargeUp && !this.login) {
            if (ChromeTabs.el) {
                if (!this._li) {
                    this._li = ChromeTabs.addTab({
                        title: this.meta.title == null ? this.tabTitle : this.title,
                        favicon: this.meta.icon == null ? this.meta.icon : this.icon,
                        content: this,
                    })
                }
                ChromeTabs.tabs.filter(x => x.content).forEach(x => x.content.show = false);
            }
            if (this.featureName) {
                this.href = `${Client.baseUri}/#/${(!this.Token ? "app" : this.Token.tenantCode)}/${this.featureName}`;
                var popupDetail = this.children.find(x => x.popup);
                if (popupDetail) {
                    this.href += `?popup=${popupDetail.featureName}&id=${popupDetail.entityId}`;
                    window.history.pushState(null, LangSelect.get(popupDetail.tabTitle), this.href);
                }
                else {
                    window.history.pushState(null, LangSelect.get(this.tabTitle), this.href);
                }
            }
        }
        else {
            if (!this.popup && !this.openFrom) {
                TabEditor.tabs.filter(x => x != this).forEach(x => x.dispose());
            }
            if (this.openFrom && this.openFrom.isTab) {
                this.href = `${Client.baseUri}/#/${(!this.Token ? "app" : this.Token.tenantCode)}/${this.openFrom.featureName}?popup=${this.featureName}&id=${this.entityId}`;
                window.history.pushState(null, LangSelect.get(this.tabTitle), this.href);
            }
            else if (this.openFrom && this.openFrom.openFrom.isTab) {
                this.href = `${Client.baseUri}/#/${(!this.Token ? "app" : this.Token.tenantCode)}/${this.openFrom.openFrom.featureName}?popup=${this.openFrom.featureName}&id=${this.openFrom.entityId}&popup2=${this.featureName}&id2=${this.entityId}`;
                window.history.pushState(null, LangSelect.get(this.tabTitle), this.href);
            }
            else {
                TabEditor.tabs.push(this);
            }
        }
        this.show = true;
        document.title = LangSelect.get(this.tabTitle || this.title);
    }
    /**
     * @type {Component[]}
     */
    groupTree = [];
    /**
     * handles the loaded layout and setups the form with loaded features.
     * @param {Feature} feature - the loaded feature.
     * @param {object} entity - the entity data.
     * @param {function} loadedCallback - callback function to execute after loading.
     */
    layoutLoaded(feature, loadedCallback = null, entity = null) {
        this.setCurrentUserProperties();
        this.setFeatureProperties(feature);
        if (entity != null) {
            this.entity = entity;
        }
        if (!feature.componentGroup) {
            this.groupTree = [];
        }
        else {
            this.groupTree = this.buildTree(feature.componentGroup);
        }
        this.element = this.renderTemplate(null, feature);
        this.setFeatureStyleSheet(feature.styleSheet);
        this.policies = feature.featurePolicies;
        this.renderTabOrSection(this.groupTree.filter(x => x.active), this);
        this.initDOMEvents();
        loadedCallback?.call(null);
        this.dispatchFeatureEvent(feature.events, EventType.dOMContentLoaded);
        this.focus();
    }

    /**
     * initializes dOM events for the form.
     */
    initDOMEvents() {
        html.take(this.element).tabIndex(-1).trigger('focus')
            .event(EventType.focusIn, () => this.dispatchFeatureEvent(this.meta.events, EventType.focusIn))
            .event(EventType.focusOut, () => this.dispatchFeatureEvent(this.meta.events, EventType.focusOut));
        if (!this.popup) {
            html.instance.className("tab-item");
            if (Client.systemRole) {
                html.instance.context.addEventListener("contextmenu", (e) => this.sysConfigMenu(e, null, null, null));
            }
        }
    }

    /**
     * sets the current user properties from the token.
     */
    setCurrentUserProperties() {
        const token = Client.token;
        this.currentUserId = token?.userId;
        this.regionId = token?.regionId;
        this.centerIds = token?.centerIds ? token.centerIds.join(Str.comma) : Str.empty;
        this.roleIds = token?.roleIds ? token.roleIds.join(Str.comma) : Str.empty;
        this.costCenterId = token?.costCenterId;
        this.roleNames = token?.roleNames ? token.roleNames.join(Str.comma) : Str.empty;
    }

    setShow(show, ...field) {
        var childs = this.children.filter(x => x.isSection && field.includes(x.meta.fieldName));
        if (childs && childs.length == 0) {
            childs = this.children.filter(x => x.isSection);
            childs.forEach(item => {
                item.setShow(show, ...field);
            })
        }
        else {
            childs.forEach(item => {
                item.show = show;
            })
        }
        this.childCom.filter(x => field.includes(x.meta.fieldName)).forEach(item => {
            item.show = show;
        })
    }

    beforeSaved = new Action();
    afterSaved = new Action();

    /**
     * updates grids that are independent of the main form's entity.
     * @returns {PatchVM[]} list of patch view models.
     */
    updateIndependantGridView() {
        const dirtyGrid = this.getDirtyGrid();
        if (!dirtyGrid.length) {
            return null;
        }
        return dirtyGrid.flatMap(grid => grid.getPatches());
    }

    /**
     * gets the list of grids that have unsaved changes.
     * @returns {ListView[]} array of dirty list views.
     */
    getDirtyGrid() {
        return this.listViews
            .filter(grid => grid.meta.id && grid.meta.canAdd)
            .filter(grid => grid.filterChildren(child => child.dirty, child => !child.populateDirty).length > 0);
    }

    /**
     * deletes data from temporary grids.
     */
    deleteGridView() {
        const dirtyGrid = this.getDeleteGrid();
        dirtyGrid.forEach(grid => {
            Client.instance.hardDeleteAsync(grid.deleteTempIds, grid.meta.refName)
                .then(deleteSuccess => {
                    if (!deleteSuccess) {
                        Toast.warning('error deleting details, please check again');
                        return;
                    }
                    grid.rowAction(row => {
                        if (grid.deleteTempIds.includes(row.entityId)) {
                            row.dispose();
                        }
                    });
                    grid.deleteTempIds.clear();
                });
        });
    }

    getDeleteGrid() {
        return this.listViews
            .filter(grid => grid.meta.id)
            .filter(grid => grid.deleteTempIds.length > 0);
    }

    /**
     * builds a tree structure from a list of components.
     * @param {Component[]} componentGroup - the list of components to build the tree from.
     * @returns {Component[]} - the root components of the built tree.
     */
    buildTree(componentGroup) {
        var componentGroupMap = new map(componentGroup.map(x => [x.id, x]));
        let parent;

        for (const item of componentGroup) {
            if (!item.parentId) {
                continue;
            }

            if (!componentGroupMap.has(item.parentId)) {
                continue;
            }

            parent = componentGroupMap.get(item.parentId);

            if (!parent.children) {
                parent.children = [];
            }

            if (!parent.children.includes(item)) {
                parent.children.push(item);
            }

            item.parent = parent;
        }

        for (const item of componentGroup) {
            if (!item.children || !item.children.length) {
                item.children = [];
                continue;
            }

            for (const ui of item.children) {
                ui.parent = item;
            }

            if (item.children) {
                item.children = item.children.sort((a, b) => a.order - b.order);
            }
        }

        componentGroup.forEach(x => this.calcItemInRow(x.children.slice()));
        const res = componentGroup.filter(x => !x.parentId);

        if (!res.length) {
            console.log("no component group is root component. wrong feature name or the configuration is wrong");
        }

        return res;
    }

    /**
     * calculates the number of items in each row of a component group.
     * @param {Component[]} componentGroup - the list of components in the group.
     */
    calcItemInRow(componentGroup) {
        let cumulativeColumn = 0;
        let itemInRow = 0;
        let startRowIndex = 0;

        for (let i = 0; i < componentGroup.length; i++) {
            const group = componentGroup[i];
            const parentInnerCol = this.getInnerColumn(group.parent);
            const outerCol = this.getOuterColumn(group);

            if (parentInnerCol <= 0) {
                continue;
            }

            itemInRow++;
            cumulativeColumn += outerCol;

            if (cumulativeColumn % parentInnerCol === 0) {
                let sameRow = i;
                while (sameRow >= startRowIndex) {
                    componentGroup[sameRow].itemInRow = itemInRow;
                    sameRow--;
                }
                itemInRow = 0;
                startRowIndex = i;
            }
        }
    }

    /**
     * calculates the appropriate column width based on the component group and screen width.
     * @param {Component} group - the component group to evaluate.
     * @returns {number} the number of columns the component should span.
     */
    getInnerColumn(group) {
        if (!group) return 0;

        const screenWidth = this.element.clientWidth;
        let res;

        if (screenWidth < EditableComponent.exSmallScreen && group.smCol > 0) {
            res = group.smCol;
        } else if (screenWidth < EditableComponent.smallScreen && group.smCol > 0) {
            res = group.smCol;
        } else if (screenWidth < EditableComponent.mediumScreen && group.smCol > 0) {
            res = group.smCol;
        } else {
            res = group.xxlCol || group.column;
        }

        return res || 0;
    }

    /**
     * calculates the appropriate outer column width based on the component group and screen width.
     * @param {Component} group - the component group to evaluate.
     * @returns {number} the number of columns including the outer margin/padding.
     */
    getOuterColumn(group) {
        if (!group) return 0;
        const screenWidth = this.element.clientWidth;
        let res;
        if (screenWidth < EditableComponent.exSmallScreen && group.smOuterColumn > 0) {
            res = group.smOuterColumn;
        } else if (screenWidth < EditableComponent.smallScreen && group.smOuterColumn > 0) {
            res = group.smOuterColumn;
        } else if (screenWidth < EditableComponent.mediumScreen && group.smOuterColumn > 0) {
            res = group.smOuterColumn;
        } else {
            res = group.xxlOuterColumn || group.outerColumn;
        }

        return res || 0;
    }

    /**
     * binds the template with components.
     * @param {HTMLElement} ele - the HTML element to bind.
     * @param {EditableComponent} parent - the parent component.
     * @param {object} entity - the entity object.
     * @param {function} [factory] - the factory function to create components.
     * @param {set<HTMLElement>} [visited] - the set of visited elements.
     */
    bindingTemplate(ele, parent, entity = null, factory = null, visited = new set()) {
        if (!ele || visited.has(ele)) {
            return;
        }
        visited.add(ele);
        if (ele.children.length === 0 && this.renderCellText(ele, entity) !== null) {
            return;
        }
        const meta = this.resolveMeta(ele);
        const newCom = factory ? factory(ele, meta, parent, entity) : this.bindingCom(ele, meta, parent, entity);
        parent = newCom instanceof this.sectionMd.Section ? newCom : parent;
        // @ts-ignore
        ele.children.forEach(child => this.bindingTemplate(child, parent, entity, factory, visited));
    }

    /**
     * resolves meta information for an HTML element.
     * @param {HTMLElement} ele - the HTML element.
     * @returns {Component} - the resolved component.
     */
    resolveMeta(ele) {
        /** @type {Component} */
        let component = new Component();
        const id = ele.dataset[this.idField.toLowerCase()];
        if (id) {
            component = this.allCom.find(x => x.id === id);
        }
        for (const prop of object.getOwnPropertyNames(Component.prototype)) {
            const value = ele.dataset[prop.toLowerCase()];
            if (!value) {
                continue;
            }
            let propVal = null;
            try {
                propVal = typeof component[prop] === 'string' ? value : JSON.parse(value);
                component = component || new Component();
                component[prop] = propVal;
            } catch {
                continue;
            }
        }
        return component;
    }

    /**
     * renders the text content of a cell.
     * @param {HTMLElement} ele - the HTML element.
     * @param {object} entity - the entity object.
     * @returns {Label} - the rendered label if applicable, otherwise null.
     */
    renderCellText(ele, entity) {
        const text = ele.textContent.trim();
        if (text && text.startsWith("{") && text.endsWith("}")) {
            /** @type {Component} */
            // @ts-ignore
            const meta = {
                fieldName: text.slice(1, -1)
            };
            const cellText = new Label(meta, ele);
            cellText.entity = entity;
            if (EditForm.layoutForm) {
                EditForm.layoutForm.addChild(cellText);
            } else {
                cellText.render();
            }
            return cellText;
        }
        return null;
    }

    static getFeatureNameFromUrl() {
        let builder = new StringBuilder();
        let feature = window.location.pathname.toLowerCase().replace(Client.baseUri.toLowerCase(), "");
        if (feature.includes(Utils.slash)) {
            let segments = feature.split("/");
            feature = segments[segments.length - 1] || segments[segments.length - 2];
        }
        if (!feature.trim()) {
            return null;
        }
        for (let i = 0; i < feature.length; i++) {
            if (feature[i] === '?' || feature[i] === '#') break;
            builder.append(feature[i]);
        }
        return builder.toString();
    }

    shouldLoadEntity = false;
    get entityName() { return this.meta.entityName; }
    /**
     * loads the entity based on the uRL or the given entity ID.
     * @returns {promise<object>} A promise that resolves to the loaded entity object.
     */
    async loadEntity() {
        const urlFeature = EditForm.getFeatureNameFromUrl();
        const urlId = urlFeature === this.featureName ? Utils.getUrlParam(Utils.idField) : this.entityId;
        if (!this.shouldLoadEntity || !urlId) {
            await this.loadMasterData();
            return null;
        }
        try {
            const ds = await Client.instance.getByIdAsync(this.meta.entityId, [urlId]);
            if (!ds.data) {
                return null;
            }
            this.entity = ds.data[0];
            await this.loadMasterData();
            return ds.data[0];
        } catch (error) {
            console.error("failed to load entity:", error);
            return null;
        }
    }

    /**
     * locks updates if the user does not have permission.
     */
    lockUpdate() {
        this.meta.featurePolicy = this.meta.featurePolicy ?? this.meta.featurePolicies;
        const generalRule = this.meta.featurePolicy.filter(x => x.recordId);
        const noPermission = (!this.meta.isPublic &&
            (!Utils.isOwner(this.entity)) && generalRule.every(x => !x.canWrite && !x.canWriteAll));
        if (noPermission) {
            this.lockUpdateButCancel();
        }
    }

    /**
     * locks all updates except for the cancel operation.
     */
    lockUpdateButCancel() {
        this.disabled = true;
        this.setDisabled(false, EditForm.btnCancel);
    }

    /** @type {HTMLElement} */
    iconElement = null;
    /** @type {HTMLElement} */
    titleElement = null;
    /** @type {HTMLElement} */
    titleCenterElement = null;
    get icon() {
        return this._icon;
    }

    set icon(value) {
        this._icon = value;
        if (this.iconElement !== null) {
            html.take(this.iconElement).iconForSpan(value);
        }
    }

    get title() {
        return this._title;
    }

    set title(value) {
        this._title = value;
        if (this.titleElement !== null) {
            this.titleElement.innerHTML = ''; // clear inner HTML
            html.take(this.titleElement).iText(value, this.meta.id);
        }
    }
    /** @type {HTMLElement} */
    popupContent;
    /**
     * sets feature properties such as title and icon based on the provided feature object.
     * @param {Feature} feature - the feature to set properties from.
     */
    setFeatureProperties(feature) {
        if (!feature) return;
        this.meta = feature;
        if (feature.className) {
            this.element.classList.add(feature.className);
        }
        if (!this.icon) {
            this.icon = feature.icon;
        }
        if (!this.title) {
            this.title = feature.Label;
        }
        if (this.popupContent) {
            this.popupContent.style.cssText = feature.style;
        }
    }

    /**
     * sets the stylesheet for the feature if provided.
     * @param {string} styleSheet - the stylesheet to apply.
     */
    setFeatureStyleSheet(styleSheet) {
        if (!styleSheet) return;
        const style = document.createElement('style');
        style.appendChild(document.createTextNode(styleSheet));
        style.setAttribute('source', 'feature');
        this.element.appendChild(style);
    }

    /**
     * renders tabs or sections based on the component group structure.
     * @param {Component[]} componentGroup - the components to render.
     */
    renderTabOrSection(componentGroup, editForm) {
        if (!editForm.editForm) {
            editForm.editForm = editForm;
        }
        componentGroup = this.getComPolicies(componentGroup);
        componentGroup.sort((a, b) => a.order - b.order).forEach(group => {
            group.disabled = this.disabled || group.disabled;
            if (group.isTab) {
                this.sectionMd.Section.renderTabGroup(editForm ?? this, group);
            } else {
                this.sectionMd.Section.renderSection(editForm ?? this, group);
            }
        });
    }

    /**
     * ensures the feature's events are dispatched to the dOM.
     * @param {object} events - events to be dispatched.
     * @param {string} eventType - type of the event.
     */
    dispatchFeatureEvent(events, eventType) {
        // example dispatch, needs specific implementation
        if (events && events[eventType]) {
            const event = new customEvent(eventType, { detail: this.entity });
            this.element.dispatchEvent(event);
        }
    }

    /**
     * renders a template based on the feature configuration.
     * @param {Component} feature - the feature configuration.
     * @returns {HTMLElement} the rendered template element.
     */
    renderTemplate(layout, feature) {
        let entryPoint = document.getElementById(EditForm.specialEntryPoint) || document.getElementById("template") || this.element;
        if (this.parentForm && this.portal) {
            this.parentForm.element = null;
            this.parentForm.dispose();
            this.parentForm = null;
        }
        entryPoint.innerHTML = Str.empty;
        if (feature.template) {
            entryPoint.innerHTML = feature.template;
            this.bindingTemplate(entryPoint, this);
            const innerEntry = array.from(entryPoint.querySelectorAll("[id='inner-entry']")).shift();
            this.resetEntryPoint(innerEntry);
            // @ts-ignore
            entryPoint = innerEntry || entryPoint;
            if (entryPoint.style.display === 'none') {
                entryPoint.style.display = Str.empty;
            }
        }
        return entryPoint;
    }

    /**
     * resets the entry point for rendering.
     * @param {element} entryPoint - the entry point to reset.
     */
    resetEntryPoint(entryPoint) {
        if (entryPoint) {
            entryPoint.innerHTML = Str.empty;
        }
    }

    /**
     * binds a component to an HTML element.
     * @param {HTMLElement} ele - the element to bind to.
     * @param {Component} com - the component metadata.
     * @param {EditableComponent} parent - the parent component.
     * @param {object} entity - the entity to bind to.
     * @returns {EditableComponent|undefined} the bound component, or undefined if not applicable.
     */
    bindingCom(ele, com, parent, entity) {
        if (!ele || !com || !com.componentType) {
            return null;
        }
        let child = null;
        if (com.componentType === ComponentType.Section) {
            child = new this.sectionMd.Section(null, ele);
            child.meta = com;
            child.meta = com;
        } else {
            child = ComponentFactory.getComponent(com, this, ele);
        }
        if (!child) return null;
        child.parentElement = child.parentElement || ele;
        child.entity = entity || child.editForm?.entity || this.entity;
        parent.addChild(child);
        return child;
    }

    /**
     * cancels the current form action, with a dirty check.
     */
    cancel() {
        this.dirtyCheckAndCancel();
    }

    /**
     * cancels the current form action without asking, directly disposing of the form.
     */
    cancelWithoutAsk() {
        this.dispose();
    }

    /**
     * checks if the form is dirty before cancelling. optionally provides a callback to execute after cancellation.
     * @param {function|null} closeCallback - Optional callback to execute after closing.
     */
    dirtyCheckAndCancel(closeCallback = null) {
        if (!this.dirty) {
            this.dispose();
            if (this.isTab) {
                ChromeTabs.removeTab(this._li);
                let existingTabIndex = ChromeTabs.tabs.findIndex(tab => tab.ul === this._li);
                if (existingTabIndex !== -1) {
                    ChromeTabs.tabs.splice(existingTabIndex, 1);
                }
            }
            if (closeCallback && closeCallback instanceof Function) closeCallback();
            return;
        }

        // confirm dialog setup assumed
        const confirm = new ConfirmDialog();
        confirm.title = "data has been changed. do you want to save?";
        confirm.pElement = this.tabEditor.element;
        confirm.yesConfirmed.add(() => {
            this.savePatch().then((rs) => {
                if (rs) {
                    this.dispose();
                    if (this.isTab) {
                        ChromeTabs.removeTab(this._li);
                        let existingTabIndex = ChromeTabs.tabs.findIndex(tab => tab.ul === this._li);
                        if (existingTabIndex !== -1) {
                            ChromeTabs.tabs.splice(existingTabIndex, 1);
                        }
                    }
                    if (closeCallback && closeCallback instanceof Function) closeCallback();
                }
            });
        });
        confirm.noConfirmed.add(async () => {
            var parent = this.openFrom.tabGroup.flatMap(x => x.children);
            if (parent.length > 0) {
                for (const element of parent) {
                    var gridDetail = element.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                    if (gridDetail) {
                        var rowItem = gridDetail.allListViewItem.find(x => x.entityId == this.entityId);
                        if (rowItem) {
                            await rowItem.updateEntity();
                        }
                    }
                }
            } else {
                var gridDetail = this.openFrom.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                if (gridDetail) {
                    var rowItem = gridDetail.allListViewItem.find(x => x.entityId == this.entityId);
                    if (rowItem) {
                        await rowItem.updateEntity();
                    }
                }
            }
            this.dispose();
            if (this.isTab) {
                ChromeTabs.removeTab(this._li);
                let existingTabIndex = ChromeTabs.tabs.findIndex(tab => tab.ul === this._li);
                if (existingTabIndex !== -1) {
                    ChromeTabs.tabs.splice(existingTabIndex, 1);
                }
            }
            if (closeCallback && closeCallback instanceof Function) closeCallback();
        });
        confirm.editForm = this;
        confirm.ignoreCancelButton = true;
        confirm.render();
    }

    openMail(closeCallback = null) {
        this.openPopup("mail-editor", null);
    }

    /**
     * disposes the form, removing it from the dOM and cleaning up resources.
     */
    dispose() {
        if (this.configEditor) {
            this.configEditor.dirty = false;
            this.configEditor.dispose();
            this.configEditor = null;
        }
        if (this.configSectionEditor) {
            this.configSectionEditor.dirty = false;
            this.configSectionEditor.dispose();
            this.configSectionEditor = null;
        }
        if (this.devToolsElement) {
            this.devToolsElement.remove();
            this.devToolsElement = null;
        }
        if (this.openFrom && this.openFrom.isTab) {
            window.history.pushState(null, LangSelect.get(this.openFrom.tabTitle), `${Client.baseUri}/#/${(!this.Token ? "app" : this.Token.tenantCode)}/${this.openFrom.featureName}`);
        }
        super.dispose();
        if (this.parent && this.parent.popup) {
            this.parent.focus();
            if (!this.parent.dirty && this.parent.entityId) {
                this.updateData(this.parent);
            }
        }
    }
    /**
     * @param {EditForm} form
     */
    updateData(form) {
        if (!form) {
            form = this;
        }
        var childEntity = form.childCom.find(x => !Utils.isNullOrWhiteSpace(x.meta.entityName) && !Utils.isNullOrWhiteSpace(x.meta.tableName));
        if (childEntity && form[childEntity.meta.entityName] && !form[childEntity.meta.entityName].id.startsWith("-")) {
            Client.instance.getByIdAsync(childEntity.meta.tableName, [form[childEntity.meta.entityName].id]).then(async entity => {
                if (entity.data && entity.data[0]) {
                    var updateEntity = entity.data[0];
                    await form.loadMasterData(updateEntity);
                    form[childEntity.meta.entityName] = updateEntity;
                    form.updateView2(false, false, childEntity.meta.entityName);
                }
            });
        }
        Client.instance.getByIdAsync(form.meta.entityId, [form.entityId]).then(async entity => {
            if (entity.data && entity.data[0]) {
                var updateEntity = entity.data[0];
                await form.loadMasterData(updateEntity);
                form.entity = updateEntity;
                form.updateView(false, false);
            }
        });
    }

    /**
     * validates the entire form or specific components within it.
     * @param {boolean} showMessage - whether to show validation messages.
     * @param {(item: EditableComponent) => boolean} predicate - function to determine which components to validate.
     * @param {(item: EditableComponent) => boolean} ignorePredicate - function to determine which components to ignore.
     * @returns {promise<boolean>} A promise that resolves to the validation status of the form.
     */
    async isFormValid(showMessage = true) {
        const validationPromises = this.childCom.filter(x => x.meta.validation).map(x => {
            return { isValid: x.validateAsync(), com: x }
        });
        /**
         * @type {GridView[]}
         */
        var gridView = this.childCom.filter(x => x.isListView);
        var gridViewItems = gridView.flatMap(x => x.item);
        const validationPromises2 = gridViewItems.flatMap(x => x.children).filter(x => x.meta.validation).map(x => {
            return { isValid: x.validateAsync(), com: x }
        });
        await promise.all(validationPromises.map(x => x.isValid).concat(
            validationPromises2.map(x => x.isValid)
        ));
        const invalidComponents = validationPromises.concat(validationPromises2).filter(result => !result.com.isValid).map(x => x.com);
        if (invalidComponents.length > 0) {
            if (showMessage) {
                invalidComponents.forEach(comp => { comp.disabled = false; });
                const firstInvalid = invalidComponents[0];
                firstInvalid.focus();
                invalidComponents.forEach(x => {
                    Toast.warning(x.validationResult.required);
                })
            }
            return false;
        }
        return true;
    }

    /**
     * @param {Component[]} components
     * @return {Component[]}
     */
    getComPolicies(components) {
        if (this.meta.isPublic || components.every(x => x.isPublic)) {
            components.forEach(com => {
                var defaultVal = (this.meta.componentDefaultValue || []).find(x => x.componentId == com.id);
                if (defaultVal) {
                    com.defaultVal = defaultVal.value;
                    com.componentDefaultValueId = defaultVal.id;
                }
                com.canWrite = true;
                com.canWriteAll = true;
                com.canRead = true;
                com.canReadAll = true;
                com.canDelete = true;
                com.canDeleteAll = true;
                com.canDeactivate = true;
                com.canDeactivateAll = true;
                com.canExport = true;
            });
            return components.filter(x => x.active);
        }
        var policyFeature = this.policies.map(x => {
            if (x.canReadAll) {
                x.canRead = x.canReadAll;
            }
            if (x.canWriteAll) {
                x.canWrite = x.canWriteAll;
                x.canRead = x.canWriteAll;
                x.canReadAll = x.canWriteAll;
            }
            return x;
        }).sort((a, b) => {
            if (b.canDeleteAll !== a.canDeleteAll) {
                return b.canDeleteAll - a.canDeleteAll;
            }
            if (b.canDelete !== a.canDelete) {
                return b.canDelete - a.canDelete;
            }
            if (b.canWriteAll !== a.canWriteAll) {
                return b.canWriteAll - a.canWriteAll;
            }
            if (b.canWrite !== a.canWrite) {
                return b.canWrite - a.canWrite;
            }
            if (b.canReadAll !== a.canReadAll) {
                return b.canReadAll - a.canReadAll;
            }
            if (b.canRead !== a.canRead) {
                return b.canRead - a.canRead;
            }
            return 0;
        }).find(x => !x.recordId && (Client.token.roleIds.includes(x.roleId) || Client.token.userId == x.userId));


        var newComponents = components.map(com => {
            var defaultVal = (this.meta.componentDefaultValue || []).find(x => x.componentId == com.id);
            if (defaultVal) {
                com.defaultVal = defaultVal.value;
                com.componentDefaultValueId = defaultVal.id;
            }
            var check2 = this.policies.sort((a, b) => b.canRead - a.canRead).find(x => x.recordId && x.recordId == com.id && (Client.token.roleIds.includes(k == x.roleId) || Client.token.userId == x.userId));
            if (check2 && check2.canRead) {
                com.canWrite = check2.canWrite;
                com.canWriteAll = check2.canWriteAll;
                com.canRead = check2.canRead;
                com.canReadAll = check2.canReadAll;
                com.canDelete = check2.canDelete;
                com.canDeleteAll = check2.canDeleteAll;
                com.canDeactivate = check2.canDeactivate;
                com.canDeactivateAll = check2.canDeactivateAll;
                com.canExport = check2.canExport;
                return com;
            }
            else if (policyFeature) {
                com.canWrite = policyFeature.canWrite || false;
                com.canWriteAll = policyFeature.canWriteAll || false;
                com.canRead = policyFeature.canRead || false;
                com.canReadAll = policyFeature.canReadAll || false;
                com.canDelete = policyFeature.canDelete || false;
                com.canDeleteAll = policyFeature.canDeleteAll || false;
                com.canDeactivate = policyFeature.canDeactivate || false;
                com.canDeactivateAll = policyFeature.canDeactivateAll || false;
                com.canExport = policyFeature.canExport || false;
                return com;
            }
            else {
                return null;
            }
        });
        return newComponents.filter(x => x != null && x.active);
    }
    /**
     * deletes the entity associated with the form.
     */
    delete() {
        const confirm = new ConfirmDialog();
        confirm.pElement = this.tabEditor.element;
        confirm.content = "are you sure you want to delete this?";
        confirm.yesConfirmed = async () => {
            try {
                const success = await Client.instance.hardDeleteAsync([this.entityId], this.meta.entityName);
                if (success) {
                    Toast.success("data deleted successfully");
                    this.parentForm?.updateView();
                    this.dispose();
                } else {
                    Toast.warning("An error occurred while deleting data");
                }
            } catch (error) {
                Toast.warning("An error occurred: " + error.Message);
            }
        };
        confirm.editForm = this;
        confirm.render();
    }
    /** @type {EditableComponent} */
    ctxCom;
    /**
     * 
     * @param {event} e 
     * @param {Component} component 
     * @param {Component} group 
     * @param {EditableComponent} ctx 
     * @returns 
     */
    sysConfigMenu(e, component, group, ctx) {
        e.preventDefault();
        e.stopPropagation();
        this.ctxCom = ctx;
        if (!Client.systemRole && component) {
            if (Client.bodRole) {
                if (component.componentType == "Pdf") {
                    const ctxMenu = ContextMenu.instance;
                    ctxMenu.top = e.top();
                    ctxMenu.left = e.left();
                    ctxMenu.menuItems = [];
                    ctxMenu.editForm = this;
                    if (component !== null) {
                        ctxMenu.menuItems.push({ icon: "fal fa-cog", text: "Pdf properties", click: this.componentProperties.bind(this), parameter: component });
                        ctxMenu.menuItems.push({ icon: "fal fa-sync-alt", text: "async Pdf", click: this.asyncProperties.bind(this), parameter: component });
                    }
                    ctxMenu.render();
                }
                else {
                    if (component !== null) {
                        const ctxMenu = ContextMenu.instance;
                        ctxMenu.top = e.top();
                        ctxMenu.left = e.left();
                        ctxMenu.menuItems = [];
                        ctxMenu.editForm = this;
                        if (["input", "dropdown", "Select", "Checkbox", "Textarea", "word"].some(x => x == component.componentType)) {
                            ctxMenu.menuItems.push({ icon: "fal fa-copy", text: "set default value", click: this.setDefaultValue.bind(this), parameter: component });
                        }
                        ctxMenu.render();
                    }
                }
            }
            else {
                if (component !== null) {
                    const ctxMenu = ContextMenu.instance;
                    ctxMenu.top = e.top();
                    ctxMenu.left = e.left();
                    ctxMenu.menuItems = [];
                    ctxMenu.editForm = this;
                    if (["input", "dropdown", "Select", "Checkbox", "Textarea", "word"].some(x => x == component.componentType)) {
                        ctxMenu.menuItems.push({ icon: "fal fa-copy", text: "set default value", click: this.setDefaultValue.bind(this), parameter: component });
                    }
                    ctxMenu.render();
                }
            }
            return;
        }
        const ctxMenu = ContextMenu.instance;
        ctxMenu.top = e.top();
        ctxMenu.left = e.left();
        ctxMenu.menuItems = [];
        if (Client.systemRole) {
            if (component !== null) {
                ctxMenu.menuItems.push({ icon: "fal fa-cog", text: "Component properties", click: this.componentProperties.bind(this), parameter: component });
                ctxMenu.menuItems.push({ icon: "fal fa-sync-alt", text: "async Pdf", click: this.asyncProperties.bind(this), parameter: component });
            }
            if (group !== null) {
                ctxMenu.menuItems.push({ icon: "fal fa-cogs", text: "Section properties", click: this.sectionProperties.bind(this), parameter: group });
            }
            ctxMenu.menuItems.push({ icon: "fal fa-folder-open", text: "screen properties", line: true, click: this.featureProperties.bind(this) });
        }
        if (component !== null) {
            ctxMenu.menuItems.push({ icon: "fal fa-copy", text: "set default value", click: this.setDefaultValue.bind(this), parameter: component });
        }
        ctxMenu.editForm = this;
        if (Client.systemRole && this.Token.tenantCode === "forwardx" && this.Token.userId == "1") {
            ctxMenu.menuItems.push({ icon: "fal fa-clone", text: "clone screen", click: this.cloneFeature.bind(this) });
            ctxMenu.menuItems.push({ icon: "fal fa-sync-alt", text: "async to", line: true, click: this.asyncTo.bind(this) });
            if (group !== null) {
                ctxMenu.menuItems.push({ icon: "fal fa-puzzle-piece", text: "inspect", shortcut: "f12", click: this.configProperties.bind(this), parameter: group });
            }
        }
        ctxMenu.render();
    }

    async sectionProperties(group) {
        this.openPopup("section-editor", group, true);
    }

    /**
     * @type {DevTools}
     */
    devTools = null;
    async configProperties(group) {
        if (!this.devTools) {
            this.devTools = new DevTools(this, group);
        }
        await this.devTools.show(group);
    }
    currentTab = "";

    static configSection;
    /**
     * @type {EditForm}
     */
    static editFormSection;

    featureProperties() {
        this.openPopup("feature-editor", this.meta, true);
    }

    componentProperties(component) {
        this.openPopup("component-editor", component, true);
    }

    setDefaultValue(component) {
        if (component.componentType == "GridView") {
            return;
        }
        var com = JSON.parse(JSON.stringify(component));
        com.fieldName = 'defaultValue' + com.fieldName;
        var name = com.entityName || "Entity";
        this[name][com.fieldName] = com.defaultVal;
        this.openConfig("set default value", async () => {
            if (Client.systemRole) {
                let dirtyPatchDetail = [
                    {
                        Label: "id",
                        field: "id",
                        oldVal: null,
                        value: com.id,
                    },
                    {
                        Label: "featureId",
                        field: "featureId",
                        oldVal: null,
                        value: com.featureId,
                    },
                    {
                        Label: "defaultVal",
                        field: "defaultVal",
                        oldVal: null,
                        value: this[name][com.fieldName],
                    }
                ]
                let patchModelDetail = {
                    changes: dirtyPatchDetail,
                    table: "Component",
                    notMessage: true
                };
                component.defaultVal = this[name][com.fieldName];
                await Client.instance.patchAsync(patchModelDetail);
                this.dirty = false;
            }
            else {
                let dirtyPatchDetail = [
                    {
                        Label: "value",
                        field: "value",
                        oldVal: null,
                        value: this[name][com.fieldName],
                    },
                    {
                        Label: this.idField,
                        field: this.idField,
                        oldVal: null,
                        value: component.componentDefaultValueId || Uuid7.newGuid(),
                    },
                    {
                        Label: "componentId",
                        field: "componentId",
                        oldVal: null,
                        value: com.id
                    },
                    {
                        Label: "userId",
                        field: "userId",
                        oldVal: null,
                        value: this.Token.userId
                    }
                ]
                let patchModelDetail = {
                    changes: dirtyPatchDetail,
                    table: "componentDefaultValue",
                    notMessage: true
                };
                var data = await Client.instance.patchAsync(patchModelDetail);
                component.defaultVal = this[name][com.fieldName];
                component.componentDefaultValueId = data.updatedItem[0].id;
                this.dirty = false;
            }
        }, () => { }, true, [com], null, null, null, true);
    }

    async actCloneFeature() {
        var featureId = Uuid7.guid();
        var entity = this.meta;
        entity.id = "-" + featureId;
        entity.name = entity.name + "-new";
        let featurePatch = [];
        object.getOwnPropertyNames(entity).forEach(cell => {
            if (entity[cell] instanceof array || (entity[cell] instanceof object && !(entity[cell] instanceof decimal))) {
                return;
            }
            let val;
            if (typeof entity[cell] === "boolean") {
                val = entity[cell] ? "1" : "0";
            } else {
                val = entity[cell];
            }
            let prop = new PatchDetail();
            prop.Label = cell;
            prop.field = cell;
            prop.oldVal = null;
            prop.value = val;
            featurePatch.push(prop);
        });
        let featureModel = {
            changes: featurePatch,
            table: "Feature",
            delete: [],
            detail: []
        };
        await Client.instance.patchAsync(featureModel);
        for (const group of this.groupTree) {
            await this.buildComponentGroup(group, null, featureId);
        }
        for (const keyDetail of this.meta.gridPolicies) {
            var component = keyDetail;
            component.id = Uuid7.newGuid();
            component.componentGroupId = null;
            component.featureId = featureId;
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
                let prop = new PatchDetail();
                prop.Label = cell;
                prop.field = cell;
                prop.oldVal = null;
                prop.value = val;
                componentPatch.push(prop);
            });
            let componentModel = {
                changes: componentPatch,
                table: "Component",
                delete: [],
                detail: []
            };
            await Client.instance.patchAsync(componentModel);
        }
    }

    async buildComponentGroup(componentGroup, parentId = null, featureId) {
        var newcomponentGroup = componentGroup;
        var newcomponentGroupId = Uuid7.guid();
        newcomponentGroup.id = "-" + newcomponentGroupId;
        newcomponentGroup.featureId = featureId;
        newcomponentGroup.parentId = parentId;
        let newcomponentGroupPatch = [];
        object.getOwnPropertyNames(newcomponentGroup).forEach(cell => {
            if (newcomponentGroup[cell] instanceof array || (newcomponentGroup[cell] instanceof object && !(newcomponentGroup[cell] instanceof decimal))) {
                return;
            }
            let val;
            if (typeof newcomponentGroup[cell] === "boolean") {
                val = newcomponentGroup[cell] ? "1" : "0";
            } else {
                val = newcomponentGroup[cell];
            }
            let prop = new PatchDetail();
            prop.Label = cell;
            prop.field = cell;
            prop.oldVal = null;
            prop.value = val;
            newcomponentGroupPatch.push(prop);
        });
        let newcomponentGroupModel = {
            changes: newcomponentGroupPatch,
            table: "Component",
            delete: [],
            detail: []
        };
        await Client.instance.patchAsync(newcomponentGroupModel);
        if (componentGroup.children) {
            for (const keyDetail of componentGroup.children) {
                await this.buildComponentGroup(keyDetail, newcomponentGroupId, featureId);
            }
        }
        if (componentGroup.components) {
            for (const keyDetail of componentGroup.components) {
                await this.buildComponent(keyDetail, newcomponentGroupId, featureId);
            }
        }
    }

    async buildComponent(keyDetail, componentgroupId, featureId) {
        var component = keyDetail;
        this.meta.components = this.meta.components || [];
        var childs = this.meta.components.filter(x => x.componentGroupId == keyDetail.id);
        var newcomponentGroupId = Uuid7.guid();
        component.id = "-" + newcomponentGroupId;
        component.componentGroupId = componentgroupId;
        component.featureId = featureId;
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
            let prop = new PatchDetail();
            prop.Label = cell;
            prop.field = cell;
            prop.oldVal = null;
            prop.value = val;
            componentPatch.push(prop);
        });
        let componentModel = {
            changes: componentPatch,
            table: "Component",
            delete: [],
            detail: []
        };
        await Client.instance.patchAsync(componentModel);
        if (childs) {
            for (const keyDetail2 of childs) {
                await this.buildComponent(keyDetail2, newcomponentGroupId, featureId);
            }
        }
    }
    /**
     * clones a feature by prompting the user for confirmation and then executing a clone operation.
     * @param {object} ev - the event object which should contain a feature to clone.
     */
    cloneFeature(ev) {
        const confirmDialog = new ConfirmDialog();
        confirmDialog.title = "do you want to clone this feature?";
        confirmDialog.pElement = this.tabEditor.element;
        confirmDialog.editForm = this;
        confirmDialog.yesConfirmed.add(() => {
            this.actCloneFeature(this);
        })
        this.addChild(confirmDialog);
    }

    /**
     * clones a feature by prompting the user for confirmation and then executing a clone operation.
     * @param {object} ev - the event object which should contain a feature to clone.
     */
    asyncTo(ev) {
        Spinner.appendTo();
        Client.instance.postAsync({}, `/api/feature/asyncTo/all/${this.meta.name}`).then((res) => {
            if (res.status == 200) {
                Toast.success("async to successful.")
            }
            else {
                Toast.warning(res.Message)
            }
            Spinner.hide();
        });

    }

    /**
     * @param {object} com 
     */
    asyncProperties(com) {
        Spinner.appendTo();
        Client.instance.postAsync({}, `/api/component/asyncTo/${com.id}`).then((res) => {
            if (res.status == 200) {
                Toast.success("async to successful.")
            }
            else {
                Toast.warning(res.Message)
            }
            Spinner.hide();
        });

    }

    async openPopup(featureName, entity, loadEntity, entitys, element = null) {
        if (!entity) {
            entity = {
                id: Uuid7.newGuid()
            }
        }
        else if (!entity.id) {
            entity.id = Uuid7.newGuid();
        }
        const tcs = new Promise((resolve, reject) => {
            import('./popupEditor.js').then((instanse) => {
                const popup = new instanse.popupEditor(featureName);
                popup.entity = entity;
                popup.shouldLoadEntity = loadEntity || false;
                popup.parentElement = element || (!this.tabEditor ? document.querySelector("#tab-content") : this.tabEditor.element);
                popup.isChild = element ? true : false;
                popup.openFrom = this;
                popup.name = featureName;
                if (entitys) {
                    object.getOwnPropertyNames(entitys).forEach(item => {
                        popup[item] = entitys[item];
                    });
                }
                this.addChild(popup);
                resolve(popup);
            })
        });
        return tcs;
    }

    async openTab(featureName, entity) {
        var tab1 = ChromeTabs.tabs.find(x => x.content.meta.name === featureName)
        if (tab1) {
            tab1.content.focus();
            return;
        }
        if (!entity) {
            entity = {
                id: Uuid7.newGuid()
            }
        }
        else if (!entity.id) {
            entity.id = Uuid7.newGuid();
        }
        import('./tabEditor.js').then((instanse) => {
            const popup = new instanse.tabEditor(featureName);
            popup.entity = entity;
            popup.name = featureName;
            this.addChild(popup);
            EditForm.tabs.push(popup);
        })
    }

    /**
     * @param {function} yesConfirmed
     * @param {function} noConfirmed
     * @param {string} title
     */
    async openConfirmDialog(yesConfirmed, noConfirmed, title, needAnswer, com, ignoreNoButton) {
        const confirmDialog = new ConfirmDialog();
        confirmDialog.title = title;
        confirmDialog.editForm = this;
        confirmDialog.ignoreNoButton = ignoreNoButton;
        confirmDialog.needAnswer = needAnswer;
        confirmDialog.Component = com;
        confirmDialog.pElement = this.editForm.element;
        confirmDialog.render();
        confirmDialog.yesConfirmed.add(yesConfirmed);
        confirmDialog.noConfirmed.add(noConfirmed);
    }

    u(force = false, dirty = null, ...componentNames) {
        this.updateView(force, dirty, componentNames);
    }

    u2(force = false, dirty = null, entityName = null, ...componentNames) {
        this.updateView(force, dirty, entityName, componentNames);
    }

    updateView(force = false, dirty = null, ...componentNames) {
        if (componentNames && componentNames.length > 0) {
            this.childCom.filter(x => componentNames.includes(x.meta.fieldName) && Utils.isNullOrWhiteSpace(x.meta.entityName)).forEach(child => {
                child.entity = child.meta.entityName ? this[child.meta.entityName] : this.entity;
                child.prepareUpdateView(force, dirty);
                child.updateView(force, dirty);
            });
            return;
        }
        this.filterChildren().filter(x => x.meta && Utils.isNullOrWhiteSpace(x.meta.entityName) && x.isSection && !x.isListViewItem).forEach(child => {
            child.entity = child.meta.entityName ? this[child.meta.entityName] : this.entity;
            child.prepareUpdateView(force, dirty);
        });
        this.childCom.filter(x => Utils.isNullOrWhiteSpace(x.meta.entityName) && !x.isListView).forEach(child => {
            child.entity = child.meta.entityName ? this[child.meta.entityName] : this.entity;
            child.prepareUpdateView(force, dirty);
            child.updateView(force, dirty, ...componentNames);
        });
        var com = this.childCom.find(x => !Utils.isNullOrWhiteSpace(x.meta.entityName) && x.isListView);
        this.childCom.filter(x => Utils.isNullOrWhiteSpace(x.meta.entityName) && x.isListView).forEach(/**@param {GridView} child **/ child => {
            child.entity = child.meta.entityName ? this[child.meta.entityName] : this.entity;
            child.prepareUpdateView(force, dirty);
            if ((!com && !this.entityId.startsWith("-") && child.meta.canCache) || (com && child.meta.refName != com.meta.tableName && child.meta.canCache)) {
                child.reloadData();
                if (child.parent && child.parent.parent && child.parent.parent.meta.displayBadge) {
                    child.parent.parent.countBadge();
                }
            }
        });
    }
    timeoutUpdateView2 = 0;
    updateView2(force = false, dirty = null, entityName = null, ...componentNames) {
        if (componentNames && componentNames.length > 0) {
            if (componentNames && componentNames.length > 0) {
                this.childCom.filter(x => x.meta.entityName == entityName && componentNames.includes(x.meta.fieldName)).forEach(child => {
                    child.entity = this[entityName];
                    child.prepareUpdateView(force, dirty);
                    child.updateView(force, dirty);
                });
                return;
            }
            this.filterChildren().filter(x => x.meta && x.meta.entityName == entityName && x.isSection && !x.isListViewItem).forEach(child => {
                child.entity = this[entityName];
                child.prepareUpdateView(force, dirty);
            });
            for (const child of this.childCom.filter(x => x.meta.entityName == entityName && !x.isListView)) {
                child.entity = this[entityName];
                child.prepareUpdateView(force, dirty);
                child.updateView(force, dirty, ...componentNames);
            }
            for (const child of this.childCom.filter(x => x.meta.entityName == entityName && x.isListView)) {
                child.entity = this[entityName];
                child.prepareUpdateView(force, dirty);
                if (child.meta.canCache) {
                    if (Utils.isNullOrWhiteSpace(child.meta.refName)) {
                        window.setTimeout(async () => {
                            await child.applyFilter();
                        });
                    }
                    else {
                        child.reloadData();
                        if (child.parent && child.parent.parent && child.parent.parent.meta.displayBadge) {
                            child.parent.parent.countBadge();
                        }
                    }
                }
            }
        }
        else {
            window.clearTimeout(this.timeoutUpdateView2);
            this.timeoutUpdateView2 = window.setTimeout(() => {
                if (componentNames && componentNames.length > 0) {
                    this.childCom.filter(x => x.meta.entityName == entityName && componentNames.includes(x.meta.fieldName)).forEach(child => {
                        child.entity = this[entityName];
                        child.prepareUpdateView(force, dirty);
                        child.updateView(force, dirty);
                    });
                    return;
                }
                this.filterChildren().filter(x => x.meta && x.meta.entityName == entityName && x.isSection && !x.isListViewItem).forEach(child => {
                    child.entity = this[entityName];
                    child.prepareUpdateView(force, dirty);
                });
                for (const child of this.childCom.filter(x => x.meta.entityName == entityName && !x.isListView)) {
                    child.entity = this[entityName];
                    child.prepareUpdateView(force, dirty);
                    child.updateView(force, dirty, ...componentNames);
                }
                for (const child of this.childCom.filter(x => x.meta.entityName == entityName && x.isListView)) {
                    child.entity = this[entityName];
                    child.prepareUpdateView(force, dirty);
                    if (child.meta.canCache) {
                        if (Utils.isNullOrWhiteSpace(child.meta.refName)) {
                            window.setTimeout(async () => {
                                await child.applyFilter();
                            });
                        }
                        else {
                            child.reloadData();
                            if (child.parent && child.parent.parent && child.parent.parent.meta.displayBadge) {
                                child.parent.parent.countBadge();
                            }
                        }
                    }
                }
            }, 100);
        }
    }

    sendEntity() {
        const confirm = new ConfirmDialog();
        confirm.title = "are you sure you want to submit this approval request?";
        confirm.pElement = this.element;
        confirm.editForm = this;
        confirm.yesConfirmed.add(async () => {
            const valid = await this.isFormValid();
            if (!valid) {
                Spinner.hide();
                return false;
            }
            if (this.entity["id"].startsWith("-")) {
                await this.savePatch();
            }
            await this.actSendEntity();
        });
        confirm.render();
    }

    openConfig(title, yesConfirmed, noConfirmed, needAnswer, com, ignoreNoButton, componentGroup, width, hasDispose) {
        if (!this.entity) {
            this.entity = {};
        }
        const confirmDialog = new ConfirmDialog();
        confirmDialog.title = title;
        confirmDialog.needAnswer = needAnswer;
        confirmDialog.hasDispose = hasDispose;
        confirmDialog.ignoreNoButton = ignoreNoButton;
        confirmDialog.Component = com;
        confirmDialog.componentGroup = componentGroup;
        confirmDialog.editForm = this;
        confirmDialog.width = width;
        confirmDialog.pElement = this.tabEditor.element;
        confirmDialog.entity = this.entity;
        confirmDialog.yesConfirmed.add(yesConfirmed);
        confirmDialog.noConfirmed.add(noConfirmed);
        confirmDialog.render();
        return confirmDialog;
    }

    async approvedEntity() {
        await this.loadEntity();
        const confirm = new ConfirmDialog();
        confirm.title = "are you sure you want to approved this approval request?";
        confirm.editForm = this;
        confirm.pElement = this.tabEditor.element;
        confirm.needAnswer = true;
        confirm.yesConfirmed.add(async () => {
            if (this.entity["id"].startsWith("-")) {
                await this.savePatch();
            }
            await this.actApprovedEntity(this.entity.reasonOfChange);
        });
        confirm.render();
    }

    async loadData() {
        var gridViews = this.editForm.childCom.filter(x => x.isListView);
        var entity = JSON.parse(JSON.stringify(this.entity));
        gridViews.forEach((grid, index) => {
            entity["t" + index] = grid.allListViewItem.filter(x => !x.groupRow).map(x => x.entity);
            entity["t" + index + "h"] = grid.header;
        })
        try {
            var res = await Client.instance.postAsync({ comId: this.entity.pdfPlanEmailId, data: entity }, "/api/createHtml2");
            return res;
        } catch (error) {
            return error.Message;
        }
    }

    updateEmailTemplate() {
        if (!this.entity.pdfPlanEmail) {
            this.entity.pdfTemplate = null;
            this.entity.pdfSubjectMail = null;
            this.updateView(false, false, "pdfTemplate", "pdfSubjectMail");
        }
        else {
            const matches = [...(this.entity.pdfPlanEmail.subjectMail || '').matchAll(/{(.*?)}/g)].map(m => m[1]);
            var subject = this.entity.pdfPlanEmail.subjectMail || '';
            this.loadData().then(template => {
                for (let index = 0; index < matches.length; index++) {
                    const element = matches[index];
                    var mapComponent = this.childCom.find(x => LangSelect.get(x.meta.label, this.meta.name) == element);
                    if (mapComponent) {
                        var text = mapComponent.getValueText();
                        subject = subject.replaceAll(`{${element}}`, text);
                    }
                }
                this.entity.pdfTemplate = template;
                this.entity.pdfSubjectMail = subject;
                this.updateView(false, false, "pdfTemplate", "pdfSubjectMail");
            });
        }
    }

    updateEmailTemplate2() {
        this.loadData().then(template => {
            this.entity.pdfTemplate = template;
            this.updateView(false, false, "pdfTemplate");
        });
    }

    updateEmailTo() {
        if (this.entity.pdfPartner) {
            this.entity.pdfToEmail = this.entity.pdfPartner.email;
            this.entity.pdfToName = this.entity.pdfPartner.contactName;
        }
        else {
            this.entity.pdfToEmail = null;
            this.entity.pdfToName = null;
        }
        this.updateView(false, false, "pdfToEmail", "pdfToName");

    }

    async declineEntity() {
        await this.loadEntity();
        var methodCheckDecline = this.checkDecline;
        if (methodCheckDecline) {
            let taskCheckDecline = await methodCheckDecline.apply(this, this);
            if (taskCheckDecline) {
                return;
            }
        }
        const confirm = new ConfirmDialog();
        confirm.title = "are you sure you want to decline this approval request?";
        confirm.pElement = this.tabEditor.element;
        confirm.editForm = this;
        confirm.needAnswer = true;
        confirm.yesConfirmed.add(async () => {
            if (this.entity["id"].startsWith("-")) {
                await this.savePatch();
            }
            await this.actDeclineEntity(this.entity.reasonOfChange);
        });
        confirm.render();
    }

    async unLockEntity() {
        await this.loadEntity();
        var methodCheckUnLock = this.checkUnLock;
        if (methodCheckUnLock) {
            let taskCheckUnLock = await methodCheckUnLock.apply(this, this);
            if (taskCheckUnLock) {
                return;
            }
        }
        await this.dispatchCustomEvent(this.meta.events, "beforeunlock", this);
        const confirm = new ConfirmDialog();
        confirm.title = "are you sure you want to unlock this record?";
        confirm.pElement = this.tabEditor.element;
        confirm.editForm = this;
        confirm.needAnswer = false;
        confirm.yesConfirmed.add(async () => {
            await this.actUnLockEntity(this.entity.reasonOfChange);
        });
        confirm.render();
    }

    firstCom(fieldName) {
        return this.childCom.find(x => x.meta.fieldName == fieldName);
    }

    GET(fieldName) {
        return this.childCom.find(x => x.meta.fieldName == fieldName);
    }

    sECTION(id) {
        return this.childSection.find(x => x.meta.id == id);
    }

    async actSendEntity() {
        this.entity.featureName = this.meta.label;
        this.entity.featureName2 = this.meta.name;
        this.entity.featureName3 = this.meta.name.includes("editor") ? this.meta.name.replace("-editor", "") : (this.openFrom ? this.tabEditor.meta.name : "");
        await this.dispatchCustomEvent(this.meta.events, "onsend", this);
        await this.dispatchCustomEvent(this.meta.events, "onsave", this);
        this.entity.statusId = 2;
        var code = this.entity.code;
        if (!Utils.isNullOrWhiteSpace(code) && this.entity.formatChat && !this.entity.formatChat.includes(code)) {
            this.entity.formatChat = this.entity.formatChat + " " + code;
        }
        var patchModel = this.getPatchVM();
        var res = await Client.instance.postAsync(patchModel, "/api/feature/sendEntity");
        if (res.status == 200) {
            this.entity = res.updatedItem[0];
            this.dirty = false;
            await this.loadMasterData(this.entity);
            this.updateView(true);
            var parent = this.openFrom.tabGroup.flatMap(x => x.children);
            if (parent.length > 0) {
                for (const element of parent) {
                    await element.countBadge();
                    var gridDetail = element.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                    if (gridDetail) {
                        await gridDetail.reloadData();
                    }
                }
            } else {
                var gridDetail = this.openFrom.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                if (gridDetail) {
                    await gridDetail.reloadData();
                }
            }
            await this.dispatchCustomEvent(this.meta.events, "sended", this);
            Toast.success("your submission was successful.")
        }
        else {
            Toast.warning("there was an error with your submission.")
        }
    }

    async actForwordEntity() {
        this.entity.featureName = this.meta.label;
        this.entity.featureName2 = this.meta.name;
        this.entity.featureName3 = this.meta.name.includes("editor") ? this.meta.name.replace("-editor", "") : (this.openFrom ? this.tabEditor.meta.name : "");
        var code = this.entity.code;
        if (!Utils.isNullOrWhiteSpace(code) && this.entity.formatChat && !this.entity.formatChat.includes(code)) {
            this.entity.formatChat = this.entity.formatChat + " " + code;
        }
        await this.dispatchCustomEvent(this.meta.events, "onforword", this);
        var patchModel = this.getPatchVM();
        var res = await Client.instance.postAsync(patchModel, "/api/feature/forwardEntity");
        if (res.status == 200) {
            this.entity = res.updatedItem[0];
            this.dirty = false;
            await this.loadMasterData(this.entity);
            this.updateView(true);
            var parent = this.openFrom.tabGroup.flatMap(x => x.children);
            if (parent.length > 0) {
                for (const element of parent) {
                    await element.countBadge();
                    var gridDetail = element.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                    if (gridDetail) {
                        await gridDetail.reloadData();
                    }
                }
            } else {
                var gridDetail = this.openFrom.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                if (gridDetail) {
                    await gridDetail.reloadData();
                }
            }
            await this.dispatchCustomEvent(this.meta.events, "forworded", this);
            Toast.success("your forward was successful.")
        }
        else {
            Toast.warning("there was an error with your forward.")
        }
    }

    async actApprovedEntity(change) {
        this.entity.featureName = this.meta.label;
        this.entity.featureName2 = this.meta.name;
        this.entity.featureName3 = this.meta.name.includes("editor") ? this.meta.name.replace("-editor", "") : (this.openFrom ? this.tabEditor.meta.name : "");
        var code = this.entity.code;
        if (!Utils.isNullOrWhiteSpace(code) && this.entity.formatChat && !this.entity.formatChat.includes(code)) {
            this.entity.formatChat = this.entity.formatChat + " " + code;
        }
        await this.dispatchCustomEvent(this.meta.events, "onapproved", this);
        var patchModel = this.getPatchVM();
        patchModel.reasonOfChange = change;
        var res = await Client.instance.postAsync(patchModel, "/api/feature/approvedEntity");
        if (res.status == 200) {
            this.entity = res.updatedItem[0];
            this.dirty = false;
            await this.loadMasterData(this.entity);
            this.updateView(true);
            var parent = this.openFrom.tabGroup.flatMap(x => x.children);
            if (parent.length > 0) {
                for (const element of parent) {
                    await element.countBadge();
                    var gridDetail = element.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                    if (gridDetail) {
                        await gridDetail.reloadData();
                    }
                }
            }
            else {
                var gridDetail = this.openFrom.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                if (gridDetail) {
                    await gridDetail.reloadData();
                }
            }
            await this.dispatchCustomEvent(this.meta.events, "approved", this);
            Toast.success("your approved was successful.")
        }
        else {
            Toast.warning(res.Message)
        }
    }

    async actDeclineEntity(change) {
        var code = this.entity.code;
        if (!Utils.isNullOrWhiteSpace(code) && this.entity.formatChat && !this.entity.formatChat.includes(code)) {
            this.entity.formatChat = this.entity.formatChat + " " + code;
        }
        this.entity.featureName = this.meta.label;
        this.entity.featureName2 = this.meta.name;
        this.entity.featureName3 = this.meta.name.includes("editor") ? this.meta.name.replace("-editor", "") : (this.openFrom ? this.tabEditor.meta.name : "");
        await this.dispatchCustomEvent(this.meta.events, "ondecline", this);
        this.entity.statusId = 4;
        this.entity.isSend = false;
        var patchModel = this.getPatchVM();
        patchModel.reasonOfChange = change;
        var res = await Client.instance.postAsync(patchModel, "/api/feature/declineEntity");
        if (res.status == 200) {
            this.entity = res.updatedItem[0];
            this.dirty = false;
            await this.loadMasterData(this.entity);
            this.updateView(true);
            var parent = this.openFrom.tabGroup.flatMap(x => x.children);
            if (parent.length > 0) {
                for (const element of parent) {
                    await element.countBadge();
                    var gridDetail = element.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                    if (gridDetail) {
                        await gridDetail.reloadData();
                    }
                }
            }
            else {
                var gridDetail = this.openFrom.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                if (gridDetail) {
                    await gridDetail.reloadData();
                }
            }
            await this.dispatchCustomEvent(this.meta.events, "declined", this);
            Toast.success("your decline was successful.")
        }
        else {
            Toast.warning(res.Message)
        }
    }

    async actUnLockEntity(change) {
        var code = this.entity.code;
        if (!Utils.isNullOrWhiteSpace(code) && this.entity.formatChat && !this.entity.formatChat.includes(code)) {
            this.entity.formatChat = this.entity.formatChat + " " + code;
        }
        this.entity.featureName = this.meta.label;
        this.entity.featureName2 = this.meta.name;
        this.entity.featureName3 = this.meta.name.includes("editor") ? this.meta.name.replace("-editor", "") : (this.openFrom ? this.tabEditor.meta.name : "");
        await this.dispatchCustomEvent(this.meta.events, "onunlock", this);
        this.entity.statusId = 4;
        this.entity.isSend = false;
        var patchModel = this.getPatchVM();
        patchModel.reasonOfChange = change;
        var res = await Client.instance.postAsync(patchModel, "/api/feature/unlockEntity");
        if (res.status == 200) {
            this.entity = res.updatedItem[0];
            this.dirty = false;
            await this.loadMasterData(this.entity);
            this.updateView(true);
            var parent = this.openFrom.tabGroup.flatMap(x => x.children);
            if (parent.length > 0) {
                for (const element of parent) {
                    await element.countBadge();
                    var gridDetail = element.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                    if (gridDetail) {
                        await gridDetail.reloadData();
                    }
                }
            }
            else {
                var gridDetail = this.openFrom.filterChildren(x => x.isListView).find(x => x.meta.refName == this.meta.entityId);
                if (gridDetail) {
                    await gridDetail.reloadData();
                }
            }
            await this.dispatchCustomEvent(this.meta.events, "unlocked", this);
            Toast.success("your unlock was successful.");
        }
        else {
            Toast.warning(res.Message);
        }
    }
}
