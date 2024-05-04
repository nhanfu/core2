/**
 * Represents a list view item.
 * @extends Section
 */
export class ListViewItem extends Section {
    /**
     * Creates an instance of ListViewItem.
     * @param {ElementType} [elementType=ElementType.tr] - The type of HTML element.
     */
    constructor(elementType = ElementType.tr) {
        super(elementType);
        // Initialize properties
        this.GroupSection = null;
        this.ListViewSection = null;
        this.ListView = null;
        this.PreQueryFn = null;
        this._selected = false;
        this._focused = false;
        this._emptyRow = false;
        this.RowNo = 0;
        this.FocusEvent = null;
        this.GroupRow = false;
        this._focusAwaiter = 0;
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
        // Set the selected state
        this._selected = value;
        this.SetSelected(value);
        // Manage selected ids
        const id = this.Entity[IdField]?.toString();
        if (value) {
            if (!this.ListViewSection.ListView.SelectedIds.includes(id)) {
                this.ListViewSection.ListView.SelectedIds.push(id);
            }
        } else {
            const index = this.ListViewSection.ListView.SelectedIds.indexOf(id);
            if (index !== -1) {
                this.ListViewSection.ListView.SelectedIds.splice(index, 1);
            }
        }
    }

    /**
     * Handles focus event.
     * @param {boolean} [value=null] - The new focus state.
     * @param {boolean} [triggerEvent=true] - Whether to trigger the focus event.
     * @returns {boolean} The focus state.
     */
    Focused(value = null, triggerEvent = true) {
        if (value === null) return this._focused;
        this._focused = value;
        const id = this.Entity[IdField];
        if (this._focused) {
            this.Element.classList.add(FocusedClass);
            this.ListViewSection.ListView.FocusId = id;
        } else {
            this.Element.classList.remove(FocusedClass);
            this.ListViewSection.ListView.FocusId = null;
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
            this.Element.classList.add(SelectedClass);
        } else {
            this.Element.classList.remove(SelectedClass);
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
        if (value) {
            this.Element.classList.add(EmptyRowClass);
        } else {
            this.Element.classList.remove(EmptyRowClass);
        }
        this.FilterChildren(EditableComponent).forEach(x => x.EmptyRow = value);
        this.AlwaysValid = value;
    }

    /**
     * Renders the item.
     */
    Render() {
        this.ListViewSection = this.ListViewSection ?? this.FindClosest(ListViewSection);
        this.ListView = this.ListView ?? this.FindClosest(ListView);
        this.Meta = this.ListView.Meta;
        super.Render();
        if (this._selected) {
            this.Element.classList.add(SelectedClass);
        }
        this.SaveEvent();
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
        if (Utils.IsFunction(Meta.Renderer, Function)) {
            Meta.Renderer.call(this, this, headers);
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
        const isEditable = header.Editable || NotCellText.includes(header.ComponentType);
        const com = isCustomCom || isEditable ?
            ComponentFactory.GetComponent(header, EditForm) :
            new Label(header);
        if (!com) return;
        const component = com instanceof EditableComponent ? com : com;
        component.Id = header.Id;
        component.Name = header.FieldName;
        component.Entity = rowData;
        component.ParentElement = cellWrapper || Html.Context;
        this.AddChild(component);
        if (this.Disabled || header.Disabled) {
            component.SetDisabled(true);
        }
        if (component.Element && !header.ChildStyle.trim().length === 0) {
            component.Element.style.cssText = header.ChildStyle;
        }
        component.UserInput = arg => this.UserInputHandler(arg, component);
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
        ListView.RowChangeHandler(component.Entity, this, arg, component).then();
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
            this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforePatchUpdate, Entity, patchModel, this)
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
        this.AfterSaved?.(success);
        this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterPatchUpdate, Entity, patchModel, this).then();
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
            if (typeof child.PatchDetail === 'function') {
                listDetail = child.PatchDetail.call(child);
            } else {
                const value = Utils.GetPropValue(child.Entity, child.FieldName);
                let actValue = '';
                switch (child.ComponentType) {
                    case 'Datepicker':
                        actValue = value.toString().DateConverter();
                        break;
                    case 'Checkbox':
                        actValue = Convert.ToBoolean(value) ? '1' : '0';
                        break;
                    default:
                        actValue = !EditForm.Feature.IgnoreEncode ? value?.toString().trim().EncodeSpecialChar() : value?.toString().trim();
                        break;
                }
                if (actValue.trim().length === 0) {
                    actValue = null;
                }
                const patch = {
                    Label: child.Label,
                    Field: child.FieldName,
                    OldVal: child.OldValue !== null && typeof child.OldValue === 'object' && Utils.IsDate(child.OldValue) ?
                        child.OldValue.toString().DateConverter() : child.OldValue?.toString(),
                    Value: actValue,
                };
                listDetail = [patch];
            }
            return listDetail;
        }).filter((value, index, self) => self.findIndex(t => t.Field === value.Field) === index);
        if (!ListView.Meta.DefaultVal.trim().length === 0 && Utils.IsFunction(ListView.Meta.DefaultVal)) {
            const dfObj = ListView.Meta.DefaultVal.call(this, EditForm);
            const patchDetail = JSON.parse(dfObj.toString());
            const defaultValue = dirtyPatch.find(x => x.Field === patchDetail.Field);
            if (defaultValue !== undefined) {
                defaultValue.Value = patchDetail.Value;
            } else {
                dirtyPatch.push(patchDetail);
            }
        }
        this.AddIdToPatch(dirtyPatch);
        dirtyPatch.forEach(x => {
            if (x.Value.trim().length === 0) {
                x.Value = null;
            }
        });
        this.PatchModel.push(...dirtyPatch);
        return {
            CacheName: CacheName,
            QueueName: QueueName,
            Changes: dirtyPatch,
            Table: ListView.Meta.RefName,
            MetaConn: ListView.MetaConn,
            DataConn: ListView.DataConn,
        };
    }

    /**
     * Handles double click event on a row.
     * @param {Event} e - The event object.
     */
    RowDblClick(e) {
        e.stopPropagation();
        ListViewSection.ListView.DblClick?.(Entity);
        this.DispatchEvent(Meta.Events, EventType.DblClick, Entity).then();
    }

    /**
     * Handles row item click event.
     * @param {Event} e - The event object.
     */
    RowItemClick(e) {
        e.stopPropagation();
        const ctrl = e.CtrlOrMetaKey();
        const shift = e.shiftKey;
        const target = e.target;
        const focusing = this.FirstOrDefault(x => x.Element === target || x.ParentElement.contains(target)) !== null;
        this.HotKeySelectRow(ctrl, shift, focusing);
        if (!e.shiftKey) {
            ListViewSection.ListView.RowClick?.(Entity);
        }
        ListViewSection.ListView.LastListViewItem = this;
        this.DispatchEvent(Meta.Events, EventType.Click, Entity).then();
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
        if (ListViewSection.ListView.VirtualScroll) {
            if (ctrl || shift) {
                this.Selected = !this._selected;
                if (this._selected) {
                    ListViewSection.ListView.SelectedIndex = ListViewSection.Children.indexOf(this);
                }
            }
            if (shift) {
                const allListView = ListViewSection.ListView.AllListViewItem;
                if (ListViewSection.ListView.LastShiftViewItem === null) {
                    ListViewSection.ListView.LastShiftViewItem = this;
                    ListViewSection.ListView.LastIndex = RowNo;
                }
                let _lastIndex = ListViewSection.ListView.LastIndex;
                const currentIndex = RowNo;
                if (_lastIndex > currentIndex) {
                    [_lastIndex, currentIndex] = [currentIndex, _lastIndex];
                }
                if (ListViewSection.ListView.VirtualScroll && currentIndex > _lastIndex) {
                    const sql = ListView.GetSql(_lastIndex - 1, currentIndex - _lastIndex + 1, true);
                    Client.Instance.GetIds(sql).then(selectedIds => {
                        if (this.Selected) {
                            selectedIds.filter(x => !ListViewSection.ListView.SelectedIds.includes(x)).forEach(x => ListViewSection.ListView.SelectedIds.push(x));
                        } else {
                            selectedIds.forEach(x => {
                                const index = ListViewSection.ListView.SelectedIds.indexOf(x);
                                if (index !== -1) {
                                    ListViewSection.ListView.SelectedIds.splice(index, 1);
                                }
                            });
                        }
                        this.SetSeletedListViewItem(allListView, _lastIndex, currentIndex);
                        ListViewSection.ListView.LastShiftViewItem = null;
                    });
                } else {
                    this.SetSeletedListViewItem(allListView, _lastIndex, currentIndex);
                }
            }
        } else {
            if (!ctrl && !shift) {
                if (ListViewSection.ListView.SelectedIds.length <= 1) {
                    ListViewSection.ListView.ClearSelected();
                    this.Selected = !this._selected;
                    if (this._selected) {
                        ListViewSection.ListView.SelectedIndex = ListViewSection.Children.indexOf(this);
                    }
                }
                return;
            }
            this.Selected = !this._selected;

            if (!shift && !ctrl && this._selected) {
                ListViewSection.ListView.SelectedIndex = ListViewSection.Children.indexOf(this);
            }
            if (shift) {
                const allListView = ListViewSection.ListView.AllListViewItem;
                const selected = allListView.find(x => x.Selected);
                let _lastIndex = allListView.indexOf(selected);
                const currentIndex = ListViewSection.Children.indexOf(this);
                if (_lastIndex > currentIndex) {
                    [_lastIndex, currentIndex] = [currentIndex, _lastIndex];
                }
                for (let i = _lastIndex; i <= currentIndex; i++) {
                    if (ListViewSection.Children[i] instanceof ListViewItem) {
                        ListViewSection.Children[i].Selected = true;
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
        const items = ListViewSection.ListView.AllListViewItem.filter(x => x.RowNo >= start && x.RowNo <= currentIndex);
        if (!ListViewSection.ListView.VirtualScroll) {
            ListViewSection.ListView.SelectedIds = items.map(x => x.EntityId);
        }
        items.forEach(item => {
            const id = item.EntityId;
            if (ListViewSection.ListView.SelectedIds.includes(id)) {
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
        Task.Run(async () => await this.DispatchCustomEvent(Meta.Events, CustomEventType.RowFocusOut, Entity));
    }

    /**
     * Handles mouse enter event.
     */
    MouseEnter() {
        this.Element.classList.add(HoveringClass);
        Task.Run(async () => await this.DispatchCustomEvent(ListViewSection.ListView.Meta.Events, CustomEventType.RowMouseEnter, Entity));
    }

    /**
     * Handles mouse leave event.
     */
    MouseLeave() {
        this.Element.classList.remove(HoveringClass);
        Task.Run(async () => await this.DispatchCustomEvent(ListViewSection.ListView.Meta.Events, CustomEventType.RowMouseLeave, Entity));
    }

    /**
     * Gets or sets the visibility of the component.
     * @type {boolean}
     */
    get Show() { return super.Show; }
    set Show(value) { this.Toggle(value); }

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
    async ValidateAsync() {
        const tcs = new TaskCompletionSource();
        const allValid = this.FilterChildren(
            x => x.Children.length === 0,
            x => x.AlwaysValid
        ).forEachAsync(x => x.ValidateAsync());
        allValid.then(validities => {
            const res = validities.toArray();
            const allOk = res.every(x => x.IsValid);
            tcs.TrySetResult(allOk);
            if (!allOk && this.ShowMessage) {
                const message = validities.filter(x => !x.IsValid)
                    .map(x => x.ValidationResult.Values.combine(Utils.BreakLine))
                    .combine(Utils.BreakLine);
                Toast.Warning(message);
            }
        });
        return tcs.Task;
    }
}

export class GroupViewItem extends ListViewItem {
    static #ChevronDown = "fa-chevron-down";
    static #ChevronRight = "fa-chevron-right";

    #showChildren = false;
    #parentItem;
    #childrenItems = [];
    #groupText;
    #chevron;

    constructor(elementType) {
        super(elementType);
        this.GroupRow = true;
        this.#childrenItems = [];
    }

    Render() {
        super.Render();
        this.Element.classList.add(GroupGridView.GroupRowClass);
    }

    get Selected() { return false; }
    set Selected(value) { this._selected = false; }

    get ParentItem() { return this.#parentItem; }
    set ParentItem(value) { this.#parentItem = value; }

    get ChildrenItems() { return this.#childrenItems; }
    set ChildrenItems(value) { this.#childrenItems = value; }

    get GroupText() { return this.#groupText; }
    set GroupText(value) { this.#groupText = value; }

    get Chevron() { return this.#chevron; }
    set Chevron(value) { this.#chevron = value; }

    AppendGroupText(text) {
        if (!this.#groupText) return;
        this.#groupText.innerHTML = this.#groupText.firstElementChild.outerHTML + text;
    }

    SetGroupText(text) {
        if (!this.#groupText) return;
        this.#groupText.innerHTML = text;
    }

    get ShowChildren() { return this.#showChildren; }
    set ShowChildren(value) {
        this.#showChildren = value;
        this.#childrenItems.forEach(x => x.Show = value);
        if (value) {
            this.#chevron.classList.replace(GroupViewItem.#ChevronRight, GroupViewItem.#ChevronDown);
        } else {
            this.#chevron.classList.replace(GroupViewItem.#ChevronDown, GroupViewItem.#ChevronRight);
        }
    }
}