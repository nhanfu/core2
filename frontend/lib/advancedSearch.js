import { EditableComponent } from "./editableComponent.js";
import {
    AdvSearchVM, activeStateEnum, advSearchOperation, FieldCondition, logicOperation, operationToSql,
    OrderBy, orderbyDirection, Entity, ElementType, Component, ComponentType, EventType, keyCodeEnum,
    FeaturePolicy
} from "./models/";
import { Section } from "./section.js";
import { Uuid7 } from "./structs/uuidv7.js";
import { Str } from "./utils/ext.js";
import dayjs from 'dayjs';
import { Textbox } from "./textbox.js";
import { Numbox } from "./numbox.js";
import { Datepicker } from "./datepicker.js";

/** @typedef {import("./gridView.js").gridView} GridView */
/** @typedef {import("./listView.js").listView} ListView */

export class AdvancedSearch extends EditableComponent {
    /** @type {ListView} */
    // @ts-ignore
    parent;
    /** @type {AdvSearchVM} */
    // @ts-ignore
    entity;
    /**
     * @param {import("./listView.js").listView} parent
     */
    constructor(parent) {
        super(null);
        this.name = "AdvancedSearch";
        this.title = "Tìm kiếm nâng cao";
        this.icon = "fa fa-search-plus";
        this.parent = parent;
    }

    localRender() {
        this._headers = this.parent.header
            .filter(x => x.id != null && x.Label && x.active && !x.hidden);
        const fp = new FeaturePolicy();
        fp.canRead = true;
        fp.canWrite = true;
        fp.canDelete = true;
        this.entity = this.parent.advSearchVM;
        var orderby = this.parent.meta.orderBy;
        this.parent.orderBy = !orderby ? this.parent.orderBy :
            orderby.split(",").map(x => {
                if (!x) return null;
                var orderField = x.trim().replace(new regExp("\\s+", "g"), " ").replace("ds.", "").split(" ");
                if (orderField.length < 1) {
                    return null;
                }

                var field = this._headers.find(header => header.fieldName == orderField[0]);
                if (field == null) {
                    return null;
                }

                /** @type {OrderBy} */
                // @ts-ignore
                var result = {
                    comId: field.id,
                    fieldName: field.fieldName
                };
                if (orderField.length == 1) {
                    result.orderbyDirectionId = orderbyDirection.aSC;
                } else {
                    result.orderbyDirectionId = orderField[1].toLowerCase() === 'asc' ? orderbyDirection.aSC : orderbyDirection.dESC;
                }
                return result;
            }).filter(x => x != null);
        var section = this.addSection();
        this.addFilters(section);
        this.addOrderByGrid(section);
    }

    addSection() {
        var section = new Section(ElementType.div);
        // @ts-ignore
        section.meta = {
            column: 4,
            Label: "filter",
            active: true,
            className: "scroll-content"
        };
        this.addChild(section);
        var label = new hTMLLabelElement();
        section.element.appendChild(label);
        label.textContent = "status";
        section.className = "filter-warpper panel group wrapper";
        return section;
    }

    /**
     * add basic filter to the top of GridView or ListView
     * @param {Section} section 
     */
    addFilters(section) {
        /** @type {GridView} */
        // @ts-ignore
        this._filterGrid = new GridView({
            id: Uuid7.id25(),
            fieldName: "conditions",
            column: 4,
            refName: "FieldCondition",
            localRender: true,
            ignoreConfirmHardDelete: true,
            canAdd: true,
            events: "{'dOMContentLoaded': 'filterDomLoaded'}"
        });
        // @ts-ignore
        this._filterGrid.onDeleteConfirmed = () => {
            // @ts-ignore
            this._filterGrid.getSelectedRows().forEach(row => {
                // @ts-ignore
                this._filterGrid.rowData.remove(row);
            });
        };
        this._filterGrid.header = this._filterGrid.meta.localHeader = [
            {
                id: "1",
                fieldName: "fieldId",
                events: "{'change': 'fieldId_Changed'}",
                Label: "Tên cột",
                refName: "Component",
                formatData: "shortDesc",
                active: true,
                editable: true,
                componentType: "SearchEntry",
                minWidth: "100px",
                maxWidth: "200px",
                localRender: true,
                localData: this._headers,
                // @ts-ignore
                localHeader: [
                    // @ts-ignore
                    {
                        fieldName: "shortDesc",
                        Label: "column",
                        active: true
                    }
                ],
                validation: "[{\"rule\": \"required\", \"Message\": \"{0} is required\"}]"
            },
            {
                id: "2",
                fieldName: "compareOperatorId",
                Label: "Toán tử",
                // @ts-ignore
                referenceId: this._entityId,
                refName: "Entity",
                componentType: "SearchEntry",
                formatData: "description",
                active: true,
                editable: true,
                minWidth: "150px",
                localRender: true,
                // @ts-ignore
                localData: iEnumerableExtensions.toEntity(advSearchOperation),
                localHeader: [
                    // @ts-ignore
                    {
                        // @ts-ignore
                        entityId: this._entityId,
                        fieldName: "name",
                        Label: "operator",
                        active: true
                    },
                    // @ts-ignore
                    {
                        // @ts-ignore
                        entityId: this._entityId,
                        fieldName: "description",
                        Label: "allias",
                        active: true
                    }
                ],
                validation: "[{\"rule\": \"required\", \"Message\": \"{0} is required\"}]"
            },
            // @ts-ignore
            {
                id: "3",
                fieldName: "value",
                Label: "value",
                // @ts-ignore
                referenceId: this._entityId,
                refName: "Entity",
                componentType: "input",
                active: true,
                editable: true,
                minWidth: "450px",
                validation: "[{\"rule\": \"required\", \"Message\": \"{0} is required\"}]"
            },
            {
                id: "2",
                fieldName: "logicOperatorId",
                Label: "logic",
                // @ts-ignore
                referenceId: this._entityId,
                refName: "Entity",
                componentType: "SearchEntry",
                formatData: "description",
                active: true,
                editable: true,
                defaultVal: "0",
                localRender: true,
                localData: logicOperation.toEntity(),
                localHeader: [
                    // @ts-ignore
                    {
                        // @ts-ignore
                        entityId: this._entityId,
                        fieldName: "name",
                        Label: "logic",
                        active: true
                    },
                    // @ts-ignore
                    {
                        // @ts-ignore
                        entityId: this._entityId,
                        fieldName: "value",
                        Label: "value",
                        active: true
                    }
                ]
            }
        ];
        this._filterGrid.rowData.data = this._filterGrid.meta.localData = this.entity.conditions;
        this._filterGrid.parentElement = section.element;
        section.addChild(this._filterGrid);
        this._filterGrid.element.addEventListener(EventType.keyDown, this.toggleIndent.bind(this));
    }

    filterDomLoaded() {
        this._filterGrid.mainSection.children.forEach(x => {
            var condition = x.Entity;
            this.fieldId_Changed(condition, condition.field);
        });
    }

    headerForAdvSearch() {
        return this.parent.header
            .filter(x => x.id != null && x.Label && x.active && !x.hidden);
    }

    /**
     * 
     * @param {Section} section 
     */
    addOrderByGrid(section) {
        /** @type {ListView} */
        // @ts-ignore
        this._orderByGrid = new GridView({
            fieldName: "OrderBy",
            column: 4,
            // @ts-ignore
            referenceId: this._orderById,
            refName: "Entity",
            canAdd: true,
            ignoreConfirmHardDelete: true,
            localRender: true
        });
        // @ts-ignore
        this._orderByGrid.onDeleteConfirmed = () => {
            // @ts-ignore
            this._orderByGrid.getSelectedRows().forEach(row => {
                this._orderByGrid.rowData.remove(row);
            });
        };
        this._orderByGrid.meta.localHeader = [
            {
                id: "1",
                fieldName: "fieldId",
                events: "{'change': 'fieldId_Changed'}",
                Label: "Tên cột",
                // @ts-ignore
                referenceId: this._ComponentId,
                refName: "Component",
                formatData: "shortDesc",
                active: true,
                editable: true,
                componentType: "SearchEntry",
                minWidth: "100px",
                maxWidth: "200px",
                localData: this._headers,
                localRender: true,
                localHeader: [
                    // @ts-ignore
                    {
                        // @ts-ignore
                        entityId: this._ComponentId,
                        fieldName: "shortDesc",
                        Label: "Tên cột",
                        active: true
                    }
                ]
            },
            {
                id: "2",
                // @ts-ignore
                entityId: this._orderById,
                fieldName: "orderbyDirectionId",
                Label: "Thứ tự",
                // @ts-ignore
                referenceId: this._entityId,
                refName: "Entity",
                componentType: "SearchEntry",
                formatData: "description",
                active: true,
                editable: true,
                minWidth: "100px",
                maxWidth: "120px",
                localData: orderbyDirection.toEntity(),
                localHeader: [
                    // @ts-ignore
                    {
                        // @ts-ignore
                        entityId: this._entityId,
                        fieldName: "name",
                        Label: "Thứ tự",
                        active: true
                    }
                ],
                localRender: true
            }
        ];
        this._orderByGrid.meta.localData = this.entity.orderBy;
        this._orderByGrid.parentElement = section.element;
        section.addChild(this._orderByGrid);
    }

    /**
     * @param {event} e
     */
    toggleIndent(e) {
        var keyCode = e.keyCodeEnum();
        if (keyCode != keyCodeEnum.tab) {
            return;
        }

        e.preventDefault();
        var reducing = e.shiftKey();
        // @ts-ignore
        var selectedRows = this._filterGrid.getSelectedRows();
        var idMap = selectedRows.reduce((/** @type {{ [x: string]: any; }} */ map, /** @type {{ id: string | number; }} */ row) => {
            map[row.id] = row;
            return map;
        }, {});
        this._filterGrid.rowAction(row => {
            var fieldCondition = row.entity;
            fieldCondition.level += reducing ? -1 : 1;
            array.from(row.element.querySelectorAll("td")).forEach(td => {
                td.style.paddingLeft = fieldCondition.level + "rem";
            });
        }, row => idMap.hasOwnProperty(row.entity.id));
    }

    dirtyCheckAndCancel() {
        super.dispose();
    }

    async applyAdvSearch() {
        const isValid = await this.validateAsync();
        if (!isValid) return;
        this.calcAdvSearchQuery();
        this.parent.reloadData(false, 0).done();
    }

    calcAdvSearchQuery() {
        // @ts-ignore
        this.parent.wheres = this.entity.conditions.map((x, index) => {
            return {
                condition: this.getSearchValue(x)
            };
        }).filter(x => x.condition);
    }

    /**
     * 
     * @param {FieldCondition} condition 
     * @returns 
     */
    getSearchValue(condition) {
        var ignoreSearch = false;
        var value = condition.value;
        if (value == null && condition.compareOperatorId != advSearchOperation.equalNull && condition.compareOperatorId != advSearchOperation.notEqualNull) {
            return null;
        }
        if (condition.field.componentType.includes(ComponentType.Datepicker) && value) {
            value = value;
            // @ts-ignore
        } else if (condition.field.componentType == nameof(number)) {
            value = value + "";
        } else {
            // @ts-ignore
            value = value + "";
        }
        var func = operationToSql[condition.compareOperatorId];
        var formattedFunc = ignoreSearch ? Str.empty : Str.format(func, condition.originFieldName, value);
        return formattedFunc;
    }

    /**
     * 
     * @param {FieldCondition} condition 
     * @param {Component} field 
     * @returns 
     */
    fieldId_Changed(condition, field) {
        if (condition == null || field == null) {
            return;
        }
        condition.originFieldName = field.fieldName;
        condition.field = field;

        var cell = this._filterGrid.firstOrDefault(x => x.entity == condition && x.name == "value");
        /** @type {EditableComponent} */
        // @ts-ignore
        var compareCell = this._filterGrid.find(x => x.Entity == condition
            && x.fieldName == "compareOperatorId");
        if (cell == null) {
            return;
        }

        var parentCellElement = cell.parentElement;
        var parentCell = cell.parent;
        cell.dispose();
        /** @type {EditableComponent} */
        var component = null;
        if (field.componentType.includes(ComponentType.Datepicker)) {
            component = this.setSearchDateTime(compareCell, field);
            // @ts-ignore
            condition.value = new dayjs().format('yYYY/mM/dD');
        } else if (field.componentType.includes(ComponentType.searchEntry) || field.componentType.includes(ComponentType.multipleSearchEntry)) {
            component = this.setSearchId(compareCell, field);
            condition.value = "";
        } else if (field.componentType.includes(ComponentType.Checkbox)) {
            component = this.setSearchBool(compareCell, field);
            // @ts-ignore
            condition.value = activeStateEnum.all;
            condition.display.valueText = 'all';
        } else if (field.componentType.includes(ComponentType.Numbox)) {
            component = this.setSearchDecimal(compareCell, field);
            condition.value = "0";
        } else {
            // @ts-ignore
            component = AdvancedSearch.setSearchString(compareCell, field);
        }
        // binding data manually because of field name confliction
        // @ts-ignore
        component.userInput += (e) => {
            component.entity.value = e.newData;
        };
        condition.logicOperatorId = condition.logicOperatorId || logicOperation.and;
        this._filterGrid.firstOrDefault(x => x.meta != null && x.entity == condition
            && x.name == "logicOperatorId")?.updateView();
        condition.compareOperatorId = compareCell.meta.localData.find(x => x.id == condition.compareOperatorId)?.id;
        // @ts-ignore
        compareCell.value = condition.compareOperatorId;
        // @ts-ignore
        compareCell.display.valueText = object.keys(advSearchOperation).find(key => advSearchOperation[key] === condition.compareOperatorId);
        compareCell.updateView();
        component.entity = condition;
        // @ts-ignore
        component.value = condition.value;
        component.parent = parentCell;
        parentCell.children.splice(2, 0, component);
        component.parentElement = parentCellElement;
        // @ts-ignore
        component.render();
    }

    /**
     * 
     * @param {string} componentType 
     * @returns {Entity[]}
     */
    static operatorFactory(componentType) {
        // @ts-ignore
        var entities = advSearchOperation;
        switch (componentType) {
            case ComponentType.dropdown:
                return entities.In;
        }
        return null;
    }

    static setSearchString(compareCell, comInfo) {
        var component;
        var com = new Component();
        com.copyPropFrom(comInfo);
        com.componentType = ComponentType.Textbox;
        component = new Textbox(comInfo);
        compareCell.meta.localData = AdvancedSearch.operatorFactory(ComponentType.Textbox);
        return component;
    }

    /**
     * create component for search decimal
     * @param {EditableComponent} compareCell 
     * @param {Component} comInfo 
     * @returns {EditableComponent}
     */
    setSearchDecimal(compareCell, comInfo) {
        var component;
        var com = new Component();
        com.copyPropFrom(comInfo);
        com.componentType = ComponentType.Numbox;
        // @ts-ignore
        component = new Numbox(comInfo);
        compareCell.meta.localData = AdvancedSearch.operatorFactory(ComponentType.number);
        return component;
    }

    /**
     * create component for search boolean
     * @param {EditableComponent} compareCell 
     * @param {Component} com 
     * @returns {EditableComponent}
     */
    setSearchBool(compareCell, com) {
        var comInfo = new Component();
        comInfo.copyPropFrom(com);
        var component;
        comInfo.formatData = '{description}';
        comInfo.componentType = ComponentType.multipleSearchEntry;
        comInfo.localRender = true;
        comInfo.localData = activeStateEnum.toEntity();
        // @ts-ignore
        comInfo.localHeader = AdvancedSearch.getBooleanSearchHeader();
        // @ts-ignore
        component = new MultipleSearchEntry(comInfo);
        compareCell.meta.localData = AdvancedSearch.operatorFactory(ComponentType.searchEntry);
        return component;
    }

    static getBooleanSearchHeader() {
        return [
            {
                // @ts-ignore
                fieldName: nameof(models.Entity.name),
                Label: "Trạng thái",
                active: true
            },
            {
                // @ts-ignore
                fieldName: nameof(models.Entity.description),
                Label: "Miêu tả",
                active: true
            }
        ];
    }

    /**
     * create component for search dropdown
     * @param {EditableComponent} compareCell 
     * @param {Component} field 
     * @returns {EditableComponent}
     */
    setSearchId(compareCell, field) {
        compareCell.meta.localData = AdvancedSearch.operatorFactory(ComponentType.searchEntry);
        compareCell.fieldVal = advSearchOperation.In;
        compareCell.entity.display = compareCell.entity.display
            ?? { operationText: advSearchOperation.getFieldNameByVal(advSearchOperation.In) };

        var comInfo = new Component();
        comInfo.copyPropFrom(field);
        comInfo.componentType = ComponentType.multipleSearchEntry;
        // @ts-ignore
        var component = new MultipleSearchEntry(comInfo);
        return component;
    }

    /**
     * create component for search dropdown
     * @param {EditableComponent} compareCell 
     * @param {Component} comInfo 
     * @returns {EditableComponent}
     */
    setSearchDateTime(compareCell, comInfo) {
        var component;
        var com = new Component();
        com.copyPropFrom(comInfo);
        // @ts-ignore
        com.componentType = nameof(Datepicker);
        com.precision = 7; // add time picker
        // @ts-ignore
        component = new Datepicker(com);
        compareCell.meta.localData =
            advSearchOperation.toEntity().filter(x => x.id < advSearchOperation.contains);
        return component;
    }
}
