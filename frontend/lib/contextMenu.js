import { html } from "./utils/html.js";
import { EditableComponent } from "./editableComponent.js";
import EventType from "./models/eventType.js";

/**
 * represents a context menu item.
 */
export class ContextMenuItem {
    ele = null;
    icon = '';
    line = false;
    style = '';
    text = '';
    shortcut = '';
    /**
     * the click event handler of the item.
     * @type {function}
     */
    click = null;
    disabled = false;
    parameter = null;
    /**
     * the sub-menu items of the item.
     * @type {ContextMenuItem[]}
     */
    menuItems = [];
}
/**
 * represents a context menu.
 */
export class ContextMenu extends EditableComponent {
    pElement = null;
    top = 0;
    left = 0;
    _selectedContextMenuItem = null;
    _selectedItem = null;
    isRoot = false;
    _selectedIndex = -1;
    /**
     * the menu items of the menu.
     * @type {ContextMenuItem[]}
     */
    menuItems = [];
    _active = 'active';
    static _instance = null;
    isSingleton = true;
    /**
     * gets the singleton instance of the menu.
     * @type {ContextMenu}
     */
    static get instance() {
        if (!this._instance) {
            // @ts-ignore
            this._instance = new ContextMenu();
            this._instance.menuItems = [];
        }
        this._instance.pElement = null;
        return this._instance;
    }

    /**
     * renders the menu.
     */
    render() {
        if (this.menuItems.length == 0) {
            return;
        }
        if (this.element == null) {
            html.take(this.pElement ?? document.body).div.className("context-menu");
            this.element = html.instance.context;
            this.element.addEventListener("focusout", () => this.dispose());
            this.element.addEventListener("keydown", (e) => this.hotKeyHandler(e));
        }
        if (this.pElement == null && this.element != null) {
            document.body.appendChild(this.element);
        }
        if (this.pElement != null && this.element != null) {
            this.pElement.appendChild(this.element);
        }
        html.take(this.element).clear().tabIndex(-1).floating(this.top, this.left);
        this.parentElement = this.element.parentElement;
        this.renderMenuItems(this.menuItems);
        window.setTimeout(() => {
            if (this.element != null) {
                this.element.style.display = "block";
                this.alterPosition();
                this.element.focus();
            }
        }, 50);
    }

    /**
     * renders the menu items.
     * @param {ContextMenuItem[]} items - the menu items to render.
     * @param {number} level - the level of the menu items.
     */
    renderMenuItems(items, level = 0) {
        for (let i = 0; i < items.length; i++) {
            const item = items[i];
            if (!item) {
                continue;
            }
            html.instance.div.className("menu-item");
            item.ele = html.context;
            if (i == 0 && level == 0 && (items[i].menuItems == null || items[i].menuItems.length == 0)) {
                this._selectedIndex = i;
                this.setSelectedItem(html.context);
            }
            if (item.disabled) {
                html.instance.attr("disabled", "disabled");
            } else {
                html.instance.event("click", (e) => this.menuItemClickHandler(e, item));
            }
            html.instance.div.className("menu-left").span.className("icon").icon(item.icon).end.end.iText(item.text, this.editForm.meta.Label).end.span.className("shortcut").iText(item.shortcut).end.render();
            if (item.menuItems != null && item.menuItems.length > 0) {
                html.instance.div.className("submenu context-menu").render();
                this.renderMenuItems(item.menuItems, level + 1);
                html.instance.end.render();
            }
            html.instance.end.render();
            if (item.line) {
                html.instance.hr.render();
            }
        }
    }

    /**
     * sets the selected item.
     * @param {HTMLElement} ele - the HTML element to set as selected.
     */
    setSelectedItem(ele) {
        this._selectedItem = ele;
        this._selectedItem.classList.add(this._active);
    }

    /**
     * handles the click event of a menu item.
     * @param {event} e - the click event.
     * @param {ContextMenuItem} item - the clicked menu item.
     */
    menuItemClickHandler(e, item) {
        e.stopPropagation();
        if (!item || !item.click) {
            return;
        }
        item.click(item.parameter);
        this.element.dispatchEvent(new event('focusout'));
    }

    /**
     * handles the hotkey event.
     * @param {event} e - the hotkey event.
     */
    hotKeyHandler(e) {
        e.preventDefault();
        if (!this.element || !this.element.children || this.element.children.length === 0) {
            return;
        }
        const children = this._selectedItem ? this._selectedItem.parentElement.children : this.element.children;
        const code = e.keyCode();
        switch (code) {
            case 27:
                this.dispose();
                break;
            case 37:
                if (this.isRoot || !this._selectedItem || !this._selectedItem.parentElement) {
                    return;
                }
                array.from(this._selectedItem.parentElement.children).forEach(x => x.classList.remove(this._active));
                this._selectedItem = this._selectedItem.parentElement;
                break;
            case 38:
                e.preventDefault();
                e.stopPropagation();
                array.from(children).forEach(x => x.classList.remove(this._active));
                this._selectedIndex = this._selectedIndex > 0 ? this._selectedIndex - 1 : children.length - 1;
                this.setSelectedItem(children[this._selectedIndex]);
                break;
            case 39:
                const ul = this._selectedItem ? this._selectedItem.lastElementChild : null;
                if (!ul || !ul.children || ul.children.length === 0) {
                    return;
                }
                array.from(ul.children).forEach(x => x.classList.remove(this._active));
                this.setSelectedItem(ul.firstElementChild);
                break;
            case 40:
                e.preventDefault();
                e.stopPropagation();
                array.from(children).forEach(x => x.classList.remove(this._active));
                this._selectedIndex = this._selectedIndex < children.length - 1 ? this._selectedIndex + 1 : 0;
                this.setSelectedItem(children[this._selectedIndex]);
                break;
            case 13:
                if (!this._selectedItem && this.element.firstElementChild instanceof HTMLElement) {
                    this.setSelectedItem(this.element.firstElementChild);
                }
                this.menuItemClickHandler(e, this.menuItems.find(x => x.ele === this._selectedItem));
                break;
        }
    }

    /**
     * alters the position of the menu.
     */
    alterPosition() {
        this.floating(this.top, this.left);
        const clientRect = this.element.getBoundingClientRect();
        const outOfViewPort = this.element.outOfViewport();
        if (outOfViewPort.bottom) {
            this.element.style.top = `${this.top - clientRect.height}px`;
        }
        if (outOfViewPort.right) {
            this.element.style.left = `${this.left - clientRect.width}px`;
            this.element.style.top = `${this.top}px`;
        }
        const updatedOutOfViewPort = this.element.outOfViewport();
        if (updatedOutOfViewPort.bottom) {
            this.element.style.top = `${this.top - clientRect.height}px`;
            this.element.style.top = `${this.top - clientRect.height - this.element.clientHeight}px`;
        }
    }
    time;
    dispose() {
        window.clearTimeout(this.time);
        this.time = window.setTimeout(() => {
            if (this.element != null) {
                this.element.remove();
                this.element = null;
            }
        }, 100);
    }

    floating(top, left) {
        this.element.style.position = 'fixed';
        this.element.style.top = `${top}px`;
        this.element.style.left = `${left}px`;
    }
}