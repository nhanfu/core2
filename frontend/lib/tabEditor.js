import { EditForm } from "./editForm.js";
import { EventType, keyCodeEnum } from "./models/";
import { Html } from "./utils/html.js";
import { GridView } from "./gridView.js";
import { ListView } from "./listView.js";
import { ChromeTabs } from "./chrometab.js";
import { Utils } from "./index.js";

/**
 * Represents a tab editor component, which can manage multiple tabs and their content.
 * @extends {EditForm}
 */
export class TabEditor extends EditForm {
    static get tabContainer() {
        var container = document.getElementById("tab-content");
        if (container == null) {
            container = document.createElement("div").id("tab-content");
            document.body.appendChild(container);
        }
        return container;
    }
    static activeTab = () => ChromeTabs.tabs.find(x => x.content.Show);
    static findTab = (id) => ChromeTabs.tabs.find(x => x.Id === id);
    /** @type {boolean} */
    static showTabText;
    static activeClass = "active";
    constructor(entity = null) {
        super(entity);
        this.isTab = true;
        this.populateDirty = false;
        this._hotKeyComponents = [];
        this.dataSearchEntry = {};
    }

    /**
     * Gets or sets the visibility of the tab editor.
     * @property
     */
    get show() {
        return super.show;
    }

    set Show(value) {
        super.show = value;
        if (value && !this.Popup && this.isLargeUp) {
            ChromeTabs.setCurrentTab(this._li, this.Pop);
        }
    }

    /**
     * Renders the component to the DOM.
     */
    render() {
        if (!this.parentElement) {
            this.parentElement = TabEditor.tabContainer;
        }
        if (!this.Popup) {
            this.renderTab();
        }
        else {
            super.render();
        }
    }

    get tabTitle() {
        return this.meta?.Label ?? this.Title;
    }

    /**
     * Renders the tab part of the editor.
     */
    renderTab() {
        Html.take(TabEditor.tabContainer).tabIndex(-1).trigger(EventType.Focus).div.event(EventType.keyDown, (e) => this.hotKeyHandler(e)).render();
        this.element = Html.context;
        this.parentElement = TabEditor.tabContainer;
        super.render();
    }

    triggerMatchHotKey(e, keyCode, shiftKey, ctrlKey, altKey) {
        if (keyCode == null) {
            return;
        }
        let patternList = [];
        if (shiftKey) {
            patternList.push(keyCodeEnum.shift);
        }

        if (ctrlKey) {
            patternList.push(keyCodeEnum.ctrl);
        }

        if (altKey) {
            patternList.push(keyCodeEnum.alt);
        }

        if (keyCode < keyCodeEnum.shift) {
            patternList.unshift(keyCode);
        } else if (keyCode > keyCodeEnum.alt) {
            patternList.push(keyCode);
        }

        this._hotKeyComponents = this.childCom.filter(x => x.isButton && !Utils.isNullOrWhiteSpace(x.meta.hotKey));
        this._hotKeyComponents.forEach(com => {
            let parts = com.Meta.hotKey.split(",");
            if (parts.length === 0) {
                return;
            }

            let lastPart = parts[parts.length - 1];
            let configKeys = lastPart.split("-").map(x => {
                let key = keyCodeEnum[x.trim()];
                return key ? key : null;
            }).filter(x => x != null).sort((a, b) => a - b);
            let isMatch = JSON.stringify(patternList) === JSON.stringify(configKeys);
            if (!isMatch) {
                return;
            }

            e.preventDefault();
            e.stopPropagation();
            com.element.click();
            return;
        });
    }

    /**
     * Handles hotkey events for the editor.
     * @param {Event} e - The event object.
     */
    hotKeyHandler(e) {
        const keyCode = e.keyCodeEnum();
        if (keyCode === keyCodeEnum.F6) {
            let gridView = this.findActiveComponent(x => x instanceof GridView).firstOrDefault();
            if (gridView instanceof GridView) {
                if (gridView && !gridView.allListViewItem.some(x => x.Selected)) {
                    if (gridView.allListViewItem.length) {
                        gridView.allListViewItem[0].Focus();
                    } else {
                        gridView.listViewSearch.Focus();
                    }
                }
                return;
            }
        }
        const shiftKey = e.shiftKey();
        const ctrlKey = e.ctrlOrMetaKey();
        const altKey = e.altKey();
        const defaultKeys = this.defaultHotKeys(keyCode, shiftKey, ctrlKey, altKey);
        if (defaultKeys) {
            e.preventDefault();
            e.stopPropagation();
            return;
        }
        if (keyCode >= keyCodeEnum.shift && keyCode <= keyCodeEnum.alt) {
            return;
        }

        this.triggerMatchHotKey(e, keyCode, shiftKey, ctrlKey, altKey);
    }
    /**
     * Checks if the default hotkeys are triggered.
     * @param {boolean} shiftKey - Indicates if the Shift key is pressed.
     * @param {boolean} ctrlKey - Indicates if the Control key is pressed.
     * @param {boolean} altKey - Indicates if the Alt key is pressed.
     * @returns {boolean} - True if a default hotkey is triggered, otherwise false.
     */
    defaultHotKeys(keyCode, shiftKey, ctrlKey, altKey) {
        if (!keyCode) {
            return false;
        }
        if (keyCode === keyCodeEnum.escape && !shiftKey && !ctrlKey && !altKey) {
            this.dirtyCheckAndCancel();
            return true;
        }
        if (ctrlKey && shiftKey && keyCode === keyCodeEnum.f) {
            // Trigger search in the grid view
            let listView = this.findActiveComponent(x => x instanceof ListView).firstOrDefault();
            if (listView instanceof ListView) {
                if (!listView || !listView.meta.canSearch) {
                    return true;
                }
                listView.listViewSearch.advancedSearch(null);
                return true;
            }
        }
        return false;
    }

    Close(event) {
        const intWhich = parseInt(event["which"]?.toString());
        const intButton = parseInt(event["button"]?.toString());
        if (intWhich === 2 || intButton === 1) {
            event.preventDefault();
            this.dirtyCheckAndCancel();
        }
    }

    /**
     * Disposes of the tab editor, removing it from the DOM and focusing on the parent form.
     */
    Dispose() {
        if (!this.Popup && this._li) {
            this.disposeTab();
        }
        this.childCom.forEach(c => c.Dispose());
        super.Dispose();
    }

    /**
     * Disposes of the tab editor, removing it from the DOM and focusing on the parent form.
     */
    forceDispose() {
        this.Dirty = false;
        this.Dispose();
        let existingTabIndex = ChromeTabs.tabs.findIndex(tab => tab.ul === this._li);
        if (existingTabIndex !== -1) {
            ChromeTabs.tabs.splice(existingTabIndex, 1);
        }
    }

    /**
     * Disposes of the tab, removing its association from the list of tabs.
     */
    disposeTab() {
        if (this.parentForm) {
            this.parentForm.focus();
            this.parentForm = null;
        }
        if (ChromeTabs.tabs.length == 0) {
            window.history.pushState({ page: null }, "/#/home", (window.location.origin || ""));
        }
    }

    /**
     * Removes DOM elements associated with the tab editor.
     */
    removeDOM() {
        this.element?.remove();
        this._backdrop?.remove();
        this._backdropGridView?.remove();
    }

}
