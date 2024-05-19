import { Html } from "./utils/html.js";
import EditableComponent from "./editableComponent.js";

/**
 * Represents a context menu item.
 */
export class ContextMenuItem {
    /**
     * The HTML element associated with the item.
     */
    Ele = null;
    /**
     * The icon of the item.
     */
    Icon = '';
    /**
     * The style of the item.
     */
    Style = '';
    /**
     * The text of the item.
     */
    Text = '';
    /**
     * The click event handler of the item.
     * @type {Function}
     */
    Click = null;
    /**
     * Indicates if the item is disabled.
     */
    Disabled = false;
    /**
     * The parameter of the item.
     */
    Parameter = null;
    /**
     * The sub-menu items of the item.
     * @type {ContextMenuItem[]}
     */
    MenuItems = [];
}

/**
 * Represents a context menu.
 */
export class ContextMenu extends EditableComponent {
    /**
     * The root HTML element of the menu.
     */
    _root = null;
    /**
     * The parent HTML element of the menu.
     */
    PElement = null;
    /**
     * The top position of the menu.
     */
    Top = 0;
    /**
     * The left position of the menu.
     */
    Left = 0;
    /**
     * The selected context menu item.
     */
    _selectedContextMenuItem = null;
    /**
     * The selected HTML element.
     */
    _selectedItem = null;
    /**
     * Indicates if the menu is the root menu.
     */
    IsRoot = false;
    /**
     * The index of the selected item.
     */
    _selectedIndex = -1;
    /**
     * The menu items of the menu.
     * @type {ContextMenuItem[]}
     */
    MenuItems = [];
    /**
     * The active CSS class name.
     */
    _active = 'active';
    /**
     * The singleton instance of the menu.
     */
    static _instance = null;
    /**
     * Indicates if the menu is a singleton.
     */
    IsSingleton = true;

    /**
     * Gets the singleton instance of the menu.
     * @type {ContextMenu}
     */
    static get Instance() {
        if (!this._instance) {
            // @ts-ignore
            this._instance = new ContextMenu();
            this._instance.MenuItems = [];
        }
        this._instance.PElement = null;
        return this._instance;
    }

    /**
     * Renders the menu.
     */
    Render() {
        if (!this._root) {
            this._root = document.createElement('ul');
            this._root.className = 'context-menu';
            this._root.addEventListener('focusout', this.Dispose.bind(this));
            this._root.addEventListener('keydown', this.HotKeyHandler.bind(this));
        }
        if (!this.PElement && this._root) {
            document.body.appendChild(this._root);
        }
        if (this.PElement && this._root) {
            this.PElement.appendChild(this._root);
        }
        this.Element = this._root;
        this._root.innerHTML = '';
        this._root.tabIndex = -1;
        this.Floating(this.Top, this.Left);
        this.ParentElement = this.Element.parentElement;
        this.RenderMenuItems(this.MenuItems);
        setTimeout(() => {
            this.Element.style.display = 'block';
            this.Element.focus();
            this.AlterPosition();
        });
    }

    /**
     * Renders the menu items.
     * @param {ContextMenuItem[]} items - The menu items to render.
     * @param {number} level - The level of the menu items.
     */
    RenderMenuItems(items, level = 0) {
        for (let i = 0; i < items.length; i++) {
            const item = items[i];
            if (!item) {
                continue;
            }
            const li = document.createElement('li');

            Html.Instance.Li.Style(item.Style).Render();
                item.Ele = Html.Context;
                if (i == 0 && level == 0 && (items[i].MenuItems == null || items[i].MenuItems.Nothing()))
                {
                    this._selectedIndex = i;
                    this.SetSelectedItem(Html.Context);
                }
            if (item.Disabled) {
                li.classList.add('disabled');
            } else {
                li.addEventListener('click', (e) => this.MenuItemClickHandler(e, item));
            }
            const iconSpan = document.createElement('span');
            const textSpan = document.createElement('span');
            iconSpan.className = item.Icon;
            textSpan.innerText = item.Text;
            li.appendChild(iconSpan);
            li.appendChild(textSpan);
            if (item.MenuItems && item.MenuItems.length > 0) {
                const ul = document.createElement('ul');
                li.appendChild(ul);
                this.RenderMenuItems(item.MenuItems, level + 1);
            }
            this._root.appendChild(li);
        }
    }

    /**
     * Sets the selected item.
     * @param {HTMLElement} ele - The HTML element to set as selected.
     */
    SetSelectedItem(ele) {
        this._selectedItem = ele;
        this._selectedItem.classList.add(this._active);
    }

    /**
     * Handles the click event of a menu item.
     * @param {Event} e - The click event.
     * @param {ContextMenuItem} item - The clicked menu item.
     */
    MenuItemClickHandler(e, item) {
        e.stopPropagation();
        if (!item || !item.Click) {
            return;
        }
        item.Click(item.Parameter);
        this.Element.dispatchEvent(new Event('focusout'));
    }

    /**
     * Handles the hotkey event.
     * @param {Event} e - The hotkey event.
     */
    HotKeyHandler(e) {
        e.preventDefault();
        if (!this.Element || !this.Element.children || this.Element.children.length === 0) {
            return;
        }
        const children = this._selectedItem ? this._selectedItem.parentElement.children : this.Element.children;
        const code = e.KeyCode();
        switch (code) {
            case 27:
                this.Dispose();
                break;
            case 37:
                if (this.IsRoot || !this._selectedItem || !this._selectedItem.parentElement) {
                    return;
                }
                Array.from(this._selectedItem.parentElement.children).forEach(x => x.classList.remove(this._active));
                this._selectedItem = this._selectedItem.parentElement;
                break;
            case 38:
                e.preventDefault();
                e.stopPropagation();
                Array.from(children).forEach(x => x.classList.remove(this._active));
                this._selectedIndex = this._selectedIndex > 0 ? this._selectedIndex - 1 : children.length - 1;
                this.SetSelectedItem(children[this._selectedIndex]);
                break;
            case 39:
                const ul = this._selectedItem ? this._selectedItem.lastElementChild : null;
                if (!ul || !ul.children || ul.children.length === 0) {
                    return;
                }
                Array.from(ul.children).forEach(x => x.classList.remove(this._active));
                this.SetSelectedItem(ul.firstElementChild);
                break;
            case 40:
                e.preventDefault();
                e.stopPropagation();
                Array.from(children).forEach(x => x.classList.remove(this._active));
                this._selectedIndex = this._selectedIndex < children.length - 1 ? this._selectedIndex + 1 : 0;
                this.SetSelectedItem(children[this._selectedIndex]);
                break;
            case 13:
                if (!this._selectedItem && this.Element.firstElementChild instanceof HTMLElement) {
                    this.SetSelectedItem(this.Element.firstElementChild);
                }
                this.MenuItemClickHandler(e, this.MenuItems.find(x => x.Ele === this._selectedItem));
                break;
        }
    }

    /**
     * Alters the position of the menu.
     */
    AlterPosition() {
        this.Floating(this.Top, this.Left);
        const clientRect = this.Element.getBoundingClientRect();
        const outOfViewPort = this.Element.OutOfViewport();
        if (outOfViewPort.Bottom) {
            this.Element.style.top = `${this.Top - clientRect.height}px`;
        }
        if (outOfViewPort.Right) {
            this.Element.style.left = `${this.Left - clientRect.width}px`;
            this.Element.style.top = `${this.Top}px`;
        }
        const updatedOutOfViewPort = this.Element.OutOfViewport();
        if (updatedOutOfViewPort.Bottom) {
            this.Element.style.top = `${this.Top - clientRect.height}px`;
            this.Element.style.top = `${this.Top - clientRect.height - this.Element.clientHeight}px`;
        }
    }

    /**
     * Removes the menu from the DOM.
     */
    RemoveDOM() {
        this._root.style.display = 'none';
    }

    /**
     * Disposes the menu.
     */
    Dispose() {
        this.RemoveDOM();
    }

    /**
     * Sets the menu to a floating position.
     * @param {number} top - The top position.
     * @param {number} left - The left position.
     */
    Floating(top, left) {
        this.Element.style.position = 'fixed';
        this.Element.style.top = `${top}px`;
        this.Element.style.left = `${left}px`;
    }
}