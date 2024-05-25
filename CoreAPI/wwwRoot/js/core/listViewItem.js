import { Section } from "./section.js";
import { Html } from "./utils/html.js";
import EventType from "./models/eventType.js";
import { Component } from "./models/component.js";
import { Utils } from "./utils/utils.js";
import { ComponentFactory } from "./utils/componentFactory.js";
import { Label } from "./label.js";
import ObservableArgs from "./models/observable.js";
import EditableComponent from "./editableComponent.js";
import { CustomEventType } from "./models/customEventType.js";
import { Client } from "./clients/client.js";
import { PatchDetail, PatchVM } from "./models/patch.js";
import { Toast } from "./toast.js";
import { Button } from "./button.js";
import { Textbox } from "./textbox.js";
import { ComponentType } from "./models/componentType.js";
import {ElementType} from './models/elementType.js';

/**
 * @typedef {import('./section.js').ListViewSection} ListViewSection
 * @typedef {import('./listView.js').ListView} ListView
 * Represents a list view item.
 * @extends Section
 */
export class ListViewItem extends Section {
    IsRow = true;
    /**
     * Creates an instance of ListViewItem.
     * @param {ElementType} [elementType=ElementType.tr] - The type of HTML element.
     */
    constructor(elementType = ElementType.tr) {
        super(elementType);
        // Initialize properties
        this.GroupSection = null;
        /** @type {ListViewSection} */
        this.ListViewSection = null;
        /** @type {ListView} */
        this.ListView = null;
        this.PreQueryFn = null;
        this._selected = false;
        this._focused = false;
        this._emptyRow = false;
        this.RowNo = 0;
        this.FocusEvent = null;
        this.GroupRow = false;
        this._focusAwaiter = 0;
        /** @type {PatchDetail[]} */
        this.PatchModel = [];
        this.ShowMessage = true;
    }

    /**
     * Gets or sets whether the item is selected.
     * @type {boolean}
     */
    get Selected() {
        return this._selected;
    }

    set Selected(value) {
        this._selected = value;
        this.SetSelected(value);
        const id = this.EntityId;
        if (value) {
            if (!this.ListView.SelectedIds.includes(id)) {
                this.ListView.SelectedIds.push(id);
            }
        } else {
            const index = this.ListView.SelectedIds.indexOf(id);
            if (index !== -1) {
                this.ListView.SelectedIds.splice(index, 1);
            }
        }
    }

    static NotCellText = ["Button", "Image", "Checkbox"];
    static EmptyRowClass = "empty-row";
    static SelectedClass = "__selected__";
    static FocusedClass = "focus";
    static HoveringClass = "hovering";
    static GroupRowClass = "group-row";

    /**
     * Handles focus event.
     * @param {boolean} [value=null] - The new focus state.
     * @param {boolean} [triggerEvent=true] - Whether to trigger the focus event.
     * @returns {boolean} The focus state.
     */
    Focused(value = null, triggerEvent = true) {
        if (value === null) return this._focused;
        this._focused = value;
        const id = this.EntityId;
        if (this._focused) {
            this.Element.classList.add(ListViewItem.FocusedClass);
            this.ListView.FocusId = id;
        } else {
            this.Element.classList.remove(ListViewItem.FocusedClass);
            this.ListView.FocusId = null;
        }
        if (triggerEvent) this.FocusEvent?.(this._focused);
        return this._focused;
    }

    /**
     * Sets the selected state of the element.
     * @param {boolean} value - The selected state.
     */
    SetSelected(value) {
        if (value) {
            this.Element.classList.add(ListViewItem.SelectedClass);
        } else {
            this.Element.classList.remove(ListViewItem.SelectedClass);
        }
    }

    /**
     * Gets or sets whether the item represents an empty row.
     * @type {boolean}
     */
    get EmptyRow() {
        return this._emptyRow;
    }

    set EmptyRow(value) {
        this._emptyRow = value;
        this.FilterChildren().forEach(x => x.EmptyRow = value);
        this.AlwaysValid = value;
        if (this.Element == null) return;
        if (value) {
            this.Element.classList.add(ListViewItem.EmptyRowClass);
        } else {
            this.Element.classList.remove(ListViewItem.EmptyRowClass);
        }
    }

    /**
     * Renders the item.
     */
    Render() {
        // @ts-ignore
        this.ListView = this.ListView ?? this.FindClosest(x => x.IsListView);
        this.Meta = this.Meta ?? this.ListView.Meta;
        super.Render();
        if (this._selected) {
            this.Element.classList.add(ListViewItem.SelectedClass);
        }
        this.SaveEvent();
    }

    /**
    * Handles save event.
    */
    SaveEvent() {
        this.AfterSaved.add(this.AfterSaveHandler.bind(this));
        this.EditForm.AfterSaved.add(this.AfterSaveHandler.bind(this));
        this.FocusEvent += (/** @type {Boolean} */ focus) => {
            window.clearTimeout(this._focusAwaiter);
            this._focusAwaiter = window.setTimeout(() => {
                if (!focus && this.Dirty && this.Meta.IsRealtime) this.PatchUpdateOrCreate().Done();
            }, 100);
        };
    }

    /**
     * @param {any} success
     */
    AfterSaveHandler(success) {
        if (!success) {
            this.EntityId = null;
        }
    }

    /**
     * Handles events binding.
     */
    BindingEvents() {
        if (!this.Element) return;
        Html.Take(this.Element)
            .Event(EventType.Click, this.RowItemClick)
            .Event(EventType.DblClick, this.RowDblClick)
            .Event(EventType.FocusIn, () => {
                this.ListView.AllListViewItem.forEach(x => {
                    if (x._focused) {
                        x._focused = false;
                    }
                });
                this._focused = true;
                this.FocusEvent?.(true);
            })
            .Event(EventType.FocusOut, this.RowFocusOut)
            .Event(EventType.MouseEnter, this.MouseEnter)
            .Event(EventType.MouseLeave, this.MouseLeave);
    }

    /**
 * Renders row data.
 * @param {Component[]} headers - The list of table headers.
 * @param {object} row - The row data.
 * @param {number} [index=null] - The index of the row.
 * @param {boolean} [emptyRow=false] - Whether the row is empty.
 */
    RenderRowData(headers, row, index = null, emptyRow = false) {
        if (index !== null) {
            if (index >= this.Element.parentElement.children.length || index < 0) {
                index = 0;
            }
            this.Element.parentElement.insertBefore(this.Element, this.Element.parentElement.children[index]);
        }
        const fn = Utils.IsFunction(this.Meta.Renderer);
        if (fn) {
            fn.call(this, this, headers);
        } else {
            headers.filter(header => !header.Hidden).forEach(header => {
                this.RenderTableCell(row, header, this.Element);
            });
        }
        this.BindingEvents();
    }

    /**
     * Renders a table cell.
     * @param {object} rowData - The row data.
     * @param {Component} header - The table header component.
     * @param {HTMLElement} [cellWrapper=null] - The wrapper element for the cell.
     */
    RenderTableCell(rowData, header, cellWrapper = null) {
        if (!header.FieldName) {
            return;
        }
        const isCustomCom = header.ComponentType.includes('.');
        const isEditable = header.Editable || ListViewItem.NotCellText.includes(header.ComponentType);
        const com = isCustomCom || isEditable ?
            ComponentFactory.GetComponent(header, this.EditForm) :
            new Label(header);
        if (!com) return;
        com.Id = header.Id;
        com.Name = header.FieldName;
        com.Entity = rowData;
        com.ParentElement = cellWrapper || Html.Context;
        this.AddChild(com);
        if (this.Disabled || header.Disabled) {
            com.SetDisabled(true);
        }
        if (com.Element && header.ChildStyle) {
            com.Element.style.cssText = header.ChildStyle;
        }
        com.UserInput.add(arg => this.UserInputHandler(arg, com));
    }

    /**
     * Handles user input event.
     * @param {ObservableArgs} arg - The observable arguments.
     * @param {EditableComponent} component - The editable component.
     */
    UserInputHandler(arg, component) {
        if (component.Disabled) {
            return;
        }
        this.ListView.RowChangeHandler(component.Entity, this, arg, component).then();
    }

    /**
     * Updates or creates a patch.
     * @param {boolean} [showMessage=true] - Whether to show a message.
     * @returns {Promise<boolean>} A promise that resolves to true if successful, otherwise false.
     */
    async PatchUpdateOrCreate(showMessage = true) {
        if (!this.Dirty) {
            return false;
        }
        return new Promise((resolve) => {
            const patchModel = this.GetPatchEntity();
            this.DispatchCustomEvent(this.Meta.Events, CustomEventType.BeforePatchUpdate, this.Entity, patchModel, this)
                .then(() => {
                    this.ShowMessage = showMessage;
                    this.ValidateAsync().then(isValid => {
                        if (!isValid) return;
                        Client.Instance.PatchAsync(patchModel).then(success => {
                            this.PatchUpdateCb(success > 0, patchModel);
                            resolve(success > 0);
                        });
                    });
                });
        });
    }

    /**
     * Callback after patch update.
     * @param {boolean} success - Whether the update was successful.
     * @param {PatchVM} patchModel - The patch model.
     */
    PatchUpdateCb(success, patchModel) {
        if (!success) {
            Toast.Warning("Save data was not successful");
        } else {
            Toast.Success("Save data success");
            this.EntityId = patchModel.EntityId;
            this.Dirty = false;
            this.EmptyRow = false;
        }
        this.AfterSaved?.invoke(success);
        this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterPatchUpdate, this.Entity, patchModel, this).then();
    }

    /**
     * Retrieves the patch entity.
     * @returns {PatchVM} The patch entity.
     */
    GetPatchEntity() {
        const shouldGetAll = this.EntityId === null;
        const dirtyPatch = this.FilterChildren(
            child => child instanceof EditableComponent && !(child instanceof Button) &&
                (shouldGetAll || child.Dirty) && child.Meta && child.Meta.FieldName.trim().length > 0
        ).flatMap(child => {
            let listDetail = [];
            if (typeof child['PatchDetail'] === 'function') {
                listDetail = child['PatchDetail'].call(child);
            } else {
                let actValue = child.FieldVal;
                if (this.EditForm.Feature.IgnoreEncode && child instanceof Textbox) {
                    actValue = actValue?.toString().trim().EncodeSpecialChar();
                }
                if (actValue.trim().length === 0) {
                    actValue = null;
                }
                const patch = {
                    Label: child.ComLabel,
                    Field: child.Name,
                    OldVal: child.OldValue !== null && child.ComponentType.includes(ComponentType.Datepicker)
                        ? child.OldValue?.toString().DateConverter()
                        : child.OldValue?.toString(),
                    Value: actValue,
                };
                listDetail = [patch];
            }
            return listDetail;
        }).filter((value, index, self) => self.findIndex(t => t.Field === value.Field) === index);

        this.AddIdToPatch(dirtyPatch);
        dirtyPatch.forEach(x => {
            if (x.Value.trim().length === 0) {
                x.Value = null;
            }
        });
        this.PatchModel.push(...dirtyPatch);
        // @ts-ignore
        return {
            CacheName: this.CacheName,
            QueueName: this.QueueName,
            Changes: dirtyPatch,
            Table: this.ListView.Meta.RefName,
            MetaConn: this.ListView.MetaConn,
            DataConn: this.ListView.DataConn,
        };
    }

    /**
     * Handles double click event on a row.
     * @param {Event} e - The event object.
     */
    RowDblClick(e) {
        e.stopPropagation();
        this.ListView.DblClick?.invoke(this.Entity);
        this.DispatchEvent(this.Meta.Events, EventType.DblClick, this.Entity).then();
    }

    /**
     * Handles row item click event.
     * @param {Event} e - The event object.
     */
    RowItemClick(e) {
        e.stopPropagation();
        const ctrl = e.CtrlOrMetaKey();
        const shift = e.ShiftKey();
        /** @type {HTMLElement} */
        // @ts-ignore
        const target = e.target;
        const focusing = this.FirstOrDefault(x => x.Element === target || x.ParentElement.contains(target)) !== null;
        this.HotKeySelectRow(ctrl, shift, focusing);
        if (!e.ShiftKey()) {
            this.ListView.RowClick?.invoke(this.Entity);
        }
        this.ListView.LastListViewItem = this;
        this.DispatchEvent(this.Meta.Events, EventType.Click, this.Entity).then();
    }

    /**
     * Handles hotkey selection of rows.
     * @param {boolean} ctrl - Whether the control key is pressed.
     * @param {boolean} shift - Whether the shift key is pressed.
     * @param {boolean} focusing - Whether the row is focusing.
     */
    HotKeySelectRow(ctrl, shift, focusing) {
        if (this.EmptyRow) {
            return;
        }
        if (this.ListView.VirtualScroll) {
            if (ctrl || shift) {
                this.Selected = !this._selected;
                if (this._selected) {
                    this.ListView.SelectedIndex = this.Children.indexOf(this);
                }
            }
            if (shift) {
                const allListView = this.ListView.AllListViewItem;
                if (this.ListView.LastShiftViewItem === null) {
                    this.ListView.LastShiftViewItem = this;
                    this.ListView.LastIndex = this.RowNo;
                }
                let _lastIndex = this.ListView.LastIndex;
                let currentIndex = this.RowNo;
                if (_lastIndex > currentIndex) {
                    let temp = _lastIndex;
                    _lastIndex = currentIndex;
                    currentIndex = temp;
                }
                if (this.ListView.VirtualScroll && currentIndex > _lastIndex) {
                    const sql = this.ListView.GetSql(_lastIndex - 1, currentIndex - _lastIndex + 1, true);
                    Client.Instance.GetIds(sql).then(selectedIds => {
                        if (this.Selected) {
                            selectedIds.filter(x => !this.ListView.SelectedIds.includes(x)).forEach(x => this.ListView.SelectedIds.push(x));
                        } else {
                            selectedIds.forEach(x => {
                                const index = this.ListView.SelectedIds.indexOf(x);
                                if (index !== -1) {
                                    this.ListView.SelectedIds.splice(index, 1);
                                }
                            });
                        }
                        this.SetSeletedListViewItem(allListView, _lastIndex, currentIndex);
                        this.ListView.LastShiftViewItem = null;
                    });
                } else {
                    this.SetSeletedListViewItem(allListView, _lastIndex, currentIndex);
                }
            }
        } else {
            if (!ctrl && !shift) {
                if (this.ListView.SelectedIds.length <= 1) {
                    this.ListView.ClearSelected();
                    this.Selected = !this._selected;
                    if (this._selected) {
                        this.ListView.SelectedIndex = this.ListViewSection.Children.indexOf(this);
                    }
                }
                return;
            }
            this.Selected = !this._selected;

            if (!shift && !ctrl && this._selected) {
                this.ListView.SelectedIndex = this.ListViewSection.Children.indexOf(this);
            }
            if (shift) {
                const allListView = this.ListView.AllListViewItem;
                const selected = allListView.find(x => x.Selected);
                let _lastIndex = allListView.indexOf(selected);
                var currentIndex = this.ListViewSection.Children.indexOf(this);
                if (currentIndex > _lastIndex) {
                    let temp = currentIndex;
                    currentIndex = _lastIndex;
                    _lastIndex = temp;
                }
                for (let i = _lastIndex; i <= currentIndex; i++) {
                    /** @type {ListViewItem} */
                    // @ts-ignore
                    let listViewItem = this.ListViewSection.Children[i];
                    if (listViewItem instanceof ListViewItem) {
                        listViewItem.Selected = true;
                    }
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
    SetSeletedListViewItem(allListView, _lastIndex, currentIndex) {
        const start = allListView[0].RowNo > _lastIndex ? allListView[0].RowNo : _lastIndex;
        const items = this.ListView.AllListViewItem.filter(x => x.RowNo >= start && x.RowNo <= currentIndex);
        if (!this.ListView.VirtualScroll) {
            this.ListView.SelectedIds = items.map(x => x.EntityId);
        }
        items.forEach(item => {
            const id = item.EntityId;
            if (this.ListView.SelectedIds.includes(id)) {
                item.Selected = this.Selected;
            } else {
                item.Selected = false;
            }
        });
    }
    /**
 * Handles row focus out event.
 */
    RowFocusOut() {
        this.Focused(false);
        return this.DispatchCustomEvent(this.Meta.Events, CustomEventType.RowFocusOut, this.Entity);
    }

    /**
     * Handles mouse enter event.
     */
    MouseEnter() {
        this.Element.classList.add(ListViewItem.HoveringClass);
        return this.DispatchCustomEvent(this.ListView.Meta.Events, CustomEventType.RowMouseEnter, this.Entity);
    }

    /**
     * Handles mouse leave event.
     */
    MouseLeave() {
        this.Element.classList.remove(ListViewItem.HoveringClass);
        return this.DispatchCustomEvent(this.ListView.Meta.Events, CustomEventType.RowMouseLeave, this.Entity);
    }

    /**
     * Gets or sets whether to show a message.
     * @type {boolean}
     */
    get ShowMessage() { return this._showMessage; }
    set ShowMessage(value) { this._showMessage = value; }

    /**
     * Validates asynchronously.
     * @returns {Promise<boolean>} A promise that resolves to true if all validations pass, otherwise false.
     */
    ValidateAsync() {
        return new Promise((ok, err) => {
            const allValid = this.FilterChildren(
                x => x.Children.length === 0,
                x => x.AlwaysValid
            ).ForEachAsync(x => x.ValidateAsync());
            allValid.then(res => {
                const allOk = res.every(x => x.IsValid);
                ok(allOk);
                if (!allOk && this.ShowMessage) {
                    const message = res.filter(x => !x.IsValid)
                        .map(x => Object.values(x.ValidationResult).Combine(null, Utils.BreakLine))
                        .Combine(null, Utils.BreakLine);
                    Toast.Warning(message);
                }
            }).catch(err);
        });
    }
}

