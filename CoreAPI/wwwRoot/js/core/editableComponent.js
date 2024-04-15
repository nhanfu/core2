import { GetPropValue, SetPropValue, IsFunction, isNoU } from "./utils.js";

export default class EditableComponent {
    Parent;
    ParentElement;
    Element;
    Entity;
    Meta;
    DefaultValue;
    Classes = [];
    #emptyRow;
    constructor(meta, ele) {
        this.Meta = meta;
        this.Element = ele;
    }
    get EntityId() {
        return this.Entity?.Id;
    }
    get FieldName() {
        return this.Meta?.FieldName;
    }
    get EmptyRow() {
        if (isNoU(this.#emptyRow)) {
            this.#emptyRow = this.FindClosest('ListViewItem')?.EmptyRow;
        }
        return this.#emptyRow;
    }
    set EmptyRow(val) {
        this.#emptyRow = val;
        this.FindClosest('ListViewItem').EmptyRow = val;
    }
    SetDefaultVal() {
        if (this.Entity == null || this.EntityId == null) return;
        var fn = {};
        if (IsFunction(this.Meta.DefaultVal, fn)) {
            fn.call(this, this);
        }
        else if (GetPropValue(this.Entity, this.FieldName) == null) {
            SetPropValue(this.Entity, this.FieldName, this.DefaultValue);
        }
    }
    FindClosest(type) {
        let found = this;
        while (found != null) {
            if (found.Classes?.includes(type) || found.Meta?.ComponentType === type) return found;
            found = found.Parent;
        }
    }
    ToggleShow() {}
    ToggleDisabled() {}
}