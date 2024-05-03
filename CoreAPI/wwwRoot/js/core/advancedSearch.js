import EditableComponent from "./editableComponent.js";
import { Component } from "./models/component.js";
import { ComponentType } from "./models/componentType.js";
import { ElementType } from "./models/elementType.js";
import { ActiveStateEnum, AdvSearchOperation, FieldCondition, LogicOperation, OperationToSql, OrderbyDirection } from "./models/enum.js";
import EventType from "./models/eventType.js";
import { KeyCodeEnum } from "./models/keycode.js";
import { Section } from "./section.js";
import { Uuid7 } from "./structs/uuidv7.js";
import { TabEditor } from "./tabEditor.js";
import { string } from "./utils/ext.js";
import * as dayjs from './structs/dayjs.min.js';
import Textbox from "./textbox.js";

/** @typedef {import("./editForm.js").ListView} ListView */
/** @typedef {import("./models/enum.js").AdvSearchVM} AdvSearchVM */

export class AdvancedSearch extends TabEditor {
    /** @type {ListView} */
    Parent;
    /** @type {AdvSearchVM} */
    Entity;
    constructor(parent) {
        super("Component");
        this.Name = "AdvancedSearch";
        this.Title = "Tìm kiếm nâng cao";
        this.Icon = "fa fa-search-plus";
        this.Parent = parent;
    }

    LoadFeatureAndRender(callback) {
        super.LoadFeatureAndRender(this.LocalRender.bind(this));
    }

    LocalRender() {
        this._headers = this.Parent.Header
            .filter(x => x.Id != null && x.Label && x.Active && !x.Hidden);
        this.Feature.FeaturePolicy.push({
            CanRead: true,
            CanWrite: true,
            CanDelete: true
        });
        this.Entity = this.Parent.AdvSearchVM;
        var fieldMap = this.HeaderForAdvSearch();
        var orderby = this.Parent.Meta.OrderBy;
        this.Parent.OrderBy = !orderby ? this.Parent.OrderBy :
            orderby.split(",").map(x => {
                if (x.IsNullOrWhiteSpace()) return null;
                var orderField = x.trim().replace(new RegExp("\\s+", "g"), " ").replace("ds.", "").split(" ");
                if (orderField.length < 1) {
                    return null;
                }

                var field = this._headers.find(header => header.FieldName == orderField[0]);
                if (field == null) {
                    return null;
                }

                var result = {
                    ComId: field.Id,
                    FieldName: field.FieldName
                };
                if (orderField.length == 1) {
                    result.OrderbyDirectionId = OrderbyDirection.ASC;
                } else {
                    result.OrderbyDirectionId = orderField[1].toLowerCase() === 'asc' ? OrderbyDirection.ASC : OrderbyDirection.DESC;
                }
                return result;
            }).filter(x => x != null);
        var section = this.AddSection();
        this.AddFilters(section);
        this.AddOrderByGrid(section);
    }

    AddSection() {
        var section = new Section(ElementType.div);
        section.Meta = {
            Column: 4,
            Label: "Filter",
            Active: true,
            ClassName: "scroll-content"
        };
        this.AddChild(section);
        var label = new HTMLLabelElement();
        section.Element.appendChild(label);
        label.textContent = "Status";
        section.ClassName = "filter-warpper panel group wrapper";
        return section;
    }

    /**
     * 
     * @param {Section} section 
     */
    AddFilters(section) {
        /** @type {ListView} */
        this._filterGrid = new GridView({
            Id: Uuid7.Id25(),
            FieldName: "Conditions",
            Column: 4,
            RefName: "FieldCondition",
            LocalRender: true,
            IgnoreConfirmHardDelete: true,
            CanAdd: true,
            Events: "{'DOMContentLoaded': 'FilterDomLoaded'}"
        });
        this._filterGrid.OnDeleteConfirmed = () => {
            this._filterGrid.GetSelectedRows().forEach(row => {
                this._filterGrid.RowData.remove(row);
            });
        };
        this._filterGrid.Header = this._filterGrid.Meta.LocalHeader = [
            {
                Id: "1",
                FieldName: "FieldId",
                Events: "{'change': 'FieldId_Changed'}",
                Label: "Tên cột",
                ReferenceId: this._ComponentId,
                RefName: "Component",
                FormatData: "ShortDesc",
                Active: true,
                Editable: true,
                ComponentType: "SearchEntry",
                MinWidth: "100px",
                MaxWidth: "200px",
                LocalRender: true,
                LocalData: this._headers,
                LocalHeader: [
                    {
                        EntityId: this._ComponentId,
                        FieldName: "ShortDesc",
                        Label: "Column",
                        Active: true
                    }
                ],
                Validation: "[{\"Rule\": \"required\", \"Message\": \"{0} is required\"}]"
            },
            {
                Id: "2",
                FieldName: "CompareOperatorId",
                Label: "Toán tử",
                ReferenceId: this._entityId,
                RefName: "Entity",
                ComponentType: "SearchEntry",
                FormatData: "Description",
                Active: true,
                Editable: true,
                MinWidth: "150px",
                LocalRender: true,
                LocalData: IEnumerableExtensions.ToEntity(AdvSearchOperation),
                LocalHeader: [
                    {
                        EntityId: this._entityId,
                        FieldName: "Name",
                        Label: "Operator",
                        Active: true
                    },
                    {
                        EntityId: this._entityId,
                        FieldName: "Description",
                        Label: "Allias",
                        Active: true
                    }
                ],
                Validation: "[{\"Rule\": \"required\", \"Message\": \"{0} is required\"}]"
            },
            {
                Id: "3",
                FieldName: "Value",
                Label: "Value",
                ReferenceId: this._entityId,
                RefName: "Entity",
                ComponentType: "Input",
                Active: true,
                Editable: true,
                MinWidth: "450px",
                Validation: "[{\"Rule\": \"required\", \"Message\": \"{0} is required\"}]"
            },
            {
                Id: "2",
                FieldName: "LogicOperatorId",
                Label: "logic",
                ReferenceId: this._entityId,
                RefName: "Entity",
                ComponentType: "SearchEntry",
                FormatData: "Description",
                Active: true,
                Editable: true,
                DefaultVal: "0",
                LocalRender: true,
                LocalData: LogicOperation.ToEntity(),
                LocalHeader: [
                    {
                        EntityId: this._entityId,
                        FieldName: "Name",
                        Label: "Logic",
                        Active: true
                    },
                    {
                        EntityId: this._entityId,
                        FieldName: "Value",
                        Label: "Value",
                        Active: true
                    }
                ]
            }
        ];
        this._filterGrid.RowData.Data = this._filterGrid.Meta.LocalData = this.Entity.Conditions;
        this._filterGrid.ParentElement = section.Element;
        section.AddChild(this._filterGrid);
        this._filterGrid.Element.addEventListener(EventType.KeyDown, this.ToggleIndent.bind(this));
    }

    FilterDomLoaded() {
        this._filterGrid.MainSection.Children.forEach(x => {
            var condition = x.Entity;
            this.FieldId_Changed(condition, condition.Field);
        });
    }

    HeaderForAdvSearch() {
        return this.Parent.Header
            .filter(x => x.Id != null && !x.Label.IsNullOrWhiteSpace() && x.Active && !x.Hidden);
    }

    /**
     * 
     * @param {Section} section 
     */
    AddOrderByGrid(section) {
        /** @type {ListView} */
        this._orderByGrid = new GridView({
            FieldName: "OrderBy",
            Column: 4,
            ReferenceId: this._orderById,
            RefName: "Entity",
            CanAdd: true,
            IgnoreConfirmHardDelete: true,
            LocalRender: true
        });
        this._orderByGrid.OnDeleteConfirmed = () => {
            this._orderByGrid.GetSelectedRows().forEach(row => {
                this._orderByGrid.RowData.Remove(row);
            });
        };
        this._orderByGrid.Meta.LocalHeader = [
            {
                Id: "1",
                FieldName: "FieldId",
                Events: "{'change': 'FieldId_Changed'}",
                Label: "Tên cột",
                ReferenceId: this._ComponentId,
                RefName: "Component",
                FormatData: "ShortDesc",
                Active: true,
                Editable: true,
                ComponentType: "SearchEntry",
                MinWidth: "100px",
                MaxWidth: "200px",
                LocalData: this._headers,
                LocalRender: true,
                LocalHeader: [
                    {
                        EntityId: this._ComponentId,
                        FieldName: "ShortDesc",
                        Label: "Tên cột",
                        Active: true
                    }
                ]
            },
            {
                Id: "2",
                EntityId: this._orderById,
                FieldName: "OrderbyDirectionId",
                Label: "Thứ tự",
                ReferenceId: this._entityId,
                RefName: "Entity",
                ComponentType: "SearchEntry",
                FormatData: "Description",
                Active: true,
                Editable: true,
                MinWidth: "100px",
                MaxWidth: "120px",
                LocalData: OrderbyDirection.ToEntity(),
                LocalHeader: [
                    {
                        EntityId: this._entityId,
                        FieldName: "Name",
                        Label: "Thứ tự",
                        Active: true
                    }
                ],
                LocalRender: true
            }
        ];
        this._orderByGrid.Meta.LocalData = this.Entity.OrderBy;
        this._orderByGrid.ParentElement = section.Element;
        section.AddChild(this._orderByGrid);
    }

    ToggleIndent(e) {
        var keyCode = e.KeyCodeEnum();
        if (keyCode != KeyCodeEnum.Tab) {
            return;
        }

        e.PreventDefault();
        var reducing = e.ShiftKey();
        var selectedRows = this._filterGrid.GetSelectedRows();
        var idMap = selectedRows.reduce((map, row) => {
            map[row.Id] = row;
            return map;
        }, {});
        this._filterGrid.RowAction(row => idMap.hasOwnProperty(row.Entity.Id), row => {
            var fieldCondition = row.Entity;
            fieldCondition.Level += reducing ? -1 : 1;
            Array.from(row.Element.querySelectorAll("td")).forEach(td => {
                td.style.paddingLeft = fieldCondition.Level + "rem";
            });
        });
    }

    DirtyCheckAndCancel() {
        super.Dispose();
    }

    ApplyAdvSearch() {
        this.IsFormValid().Done(isValid => {
            if (!isValid) return;
            this.CalcAdvSearchQuery();
            this.Parent.ReloadData(false, 0).Done();
        });
    }

    CalcAdvSearchQuery() {
        this.Parent.Wheres = this.Entity.Conditions.map((x, index) => {
            return {
                Condition: this.GetSearchValue(x)
            };
        }).filter(x => x.Condition);
    }

    /**
     * 
     * @param {FieldCondition} condition 
     * @returns 
     */
    GetSearchValue(condition) {
        var ignoreSearch = false;
        var value = condition.Value;
        if (value == null && condition.CompareOperatorId != AdvSearchOperation.EqualNull && condition.CompareOperatorId != AdvSearchOperation.NotEqualNull) {
            return null;
        }
        if (condition.Field.ComponentType.includes(ComponentType.Datepicker) && value) {
            value = value;
        } else if (condition.Field.ComponentType == nameof(Number)) {
            value = value + "";
        } else {
            value = value.EncodeSpecialChar() + "";
        }
        var func = OperationToSql[condition.CompareOperatorId];
        var formattedFunc = ignoreSearch ? string.Empty : string.Format(func, condition.OriginFieldName, value);
        return formattedFunc;
    }

    /**
     * 
     * @param {FieldCondition} condition 
     * @param {Component} field 
     * @returns 
     */
    FieldId_Changed(condition, field) {
        if (condition == null || field == null) {
            return;
        }
        condition.OriginFieldName = field.FieldName;
        condition.Field = field;

        var cell = this._filterGrid.FirstOrDefault(x => x.Entity == condition && x.FieldName == "Value");
        /** @type {EditableComponent} */
        var compareCell = this._filterGrid.find(x => x.Entity == condition
            && x.FieldName == "CompareOperatorId");
        if (cell == null) {
            return;
        }

        var parentCellElement = cell.ParentElement;
        var parentCell = cell.Parent;
        cell.Dispose();
        /** @type {EditableComponent} */
        var component = null;
        if (field.ComponentType.includes(ComponentType.Datepicker)) {
            component = this.SetSearchDateTime(compareCell, field);
            condition.Value = new dayjs().format('YYYY/MM/DD');
        } else if (field.ComponentType.includes(ComponentType.SearchEntry) || field.ComponentType.includes(ComponentType.MultipleSearchEntry)) {
            component = this.SetSearchId(compareCell, field);
            condition.Value = "";
        } else if (field.ComponentType.includes(ComponentType.Checkbox)) {
            component = this.SetSearchBool(compareCell, field);
            condition.Value = ActiveStateEnum.All;
            condition.Display.ValueText = 'All';
        } else if (field.ComponentType.includes(ComponentType.Numbox)) {
            component = this.SetSearchDecimal(compareCell, field);
            condition.Value = "0";
        } else {
            component = AdvancedSearch.SetSearchString(compareCell, field);
        }
        // Binding data manually because of field name confliction
        component.UserInput += (e) => {
            component.Entity.Value = e.NewData;
        };
        condition.LogicOperatorId = condition.LogicOperatorId || LogicOperation.And;
        this._filterGrid.FirstOrDefault(x => x.Meta != null && x.Entity == condition
            && x.FieldName == "LogicOperatorId")?.UpdateView();
        condition.CompareOperatorId = compareCell.Meta.LocalData.find(x => x.Id == condition.CompareOperatorId)?.Id;
        compareCell.Value = condition.CompareOperatorId;
        compareCell.Display.ValueText = Object.keys(AdvSearchOperation).find(key => AdvSearchOperation[key] === condition.CompareOperatorId);
        compareCell.UpdateView();
        component.Entity = condition;
        component.Value = condition.Value;
        component.Parent = parentCell;
        parentCell.Children.splice(2, 0, component);
        component.ParentElement = parentCellElement;
        component.Render();
    }

    static OperatorFactory(componentType) {
        var entities = IEnumerableExtensions.ToEntity(AdvSearchOperation);
        switch (componentType) {
            case ComponentType.Textbox:
                return entities.filter(x => (x.Id.TryParseInt() >= AdvSearchOperation.Contains && x.Id.TryParseInt() < AdvSearchOperation.In)
                    || x.Id.TryParseInt() == AdvSearchOperation.Equal || x.Id.TryParseInt() == AdvSearchOperation.NotEqual)
                    .sort((x, y) => x.Id.TryParseInt() == AdvSearchOperation.Contains ? -1 : 1);
            case ComponentType.Datepicker:
                return entities.filter(x => x.Id.TryParseInt() < AdvSearchOperation.Contains);
            case ComponentType.Number:
                return entities.filter(x => x.Id.TryParseInt() < AdvSearchOperation.Contains);
            case ComponentType.Checkbox:
                return entities.filter(x => x.Id.TryParseInt() == AdvSearchOperation.Equal);
            case ComponentType.SearchEntry:
                return entities.filter(x => x.Id.TryParseInt() == AdvSearchOperation.In || x.Id.TryParseInt() == AdvSearchOperation.NotIn);
        }
        return null;
    }

    static SetSearchString(compareCell, comInfo) {
        var component;
        var com = new Component();
        com.CopyPropFrom(comInfo);
        com.ComponentType = nameof(Textbox);
        component = new Textbox(comInfo);
        compareCell.Meta.LocalData = AdvancedSearch.OperatorFactory(ComponentType.Textbox);
        return component;
    }

    SetSearchDecimal(compareCell, comInfo) {
        var component;
        var com = new Component();
        com.CopyPropFrom(comInfo);
        com.ComponentType = ComponentType.Numbox;
        component = new Numbox(comInfo);
        compareCell.Meta.LocalData = AdvancedSearch.OperatorFactory(ComponentType.Number);
        return component;
    }

    SetSearchBool(compareCell, com) {
        var comInfo = new Component();
        comInfo.CopyPropFrom(com);
        var component;
        comInfo.FormatData = '{Description}';
        comInfo.ComponentType = ComponentType.MultipleSearchEntry;
        comInfo.LocalRender = true;
        comInfo.LocalData = ActiveStateEnum.ToEntity();
        comInfo.LocalHeader = AdvancedSearch.GetBooleanSearchHeader();
        component = new MultipleSearchEntry(comInfo);
        compareCell.Meta.LocalData = AdvancedSearch.OperatorFactory(ComponentType.SearchEntry);
        return component;
    }

    static GetBooleanSearchHeader() {
        return [
            {
                FieldName: nameof(Models.Entity.Name),
                Label: "Trạng thái",
                Active: true
            },
            {
                FieldName: nameof(Models.Entity.Description),
                Label: "Miêu tả",
                Active: true
            }
        ];
    }

    SetSearchId(compareCell, field) {
        compareCell.Meta.LocalData = AdvancedSearch.OperatorFactory(ComponentType.SearchEntry);
        compareCell.Value = AdvSearchOperation.In.toString();

        var comInfo = new Component();
        comInfo.CopyPropFrom(field);
        comInfo.ComponentType = ComponentType.MultipleSearchEntry;
        var component = new MultipleSearchEntry(comInfo);
        return component;
    }

    SetSearchDateTime(compareCell, comInfo) {
        var component;
        var com = new Component();
        com.CopyPropFrom(comInfo);
        com.ComponentType = nameof(Datepicker);
        com.Precision = 7; // add time picker
        component = new Datepicker(com);
        compareCell.Meta.LocalData =
            AdvSearchOperation.ToEntity().filter(x => x.Id < AdvSearchOperation.Contains);
        return component;
    }
}
