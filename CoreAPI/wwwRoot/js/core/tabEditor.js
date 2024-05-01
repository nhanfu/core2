import { Document, HTMLElement, Event } from "Bridge.Html5";
import { EditForm, EditableComponent, GridView, ListView, Button } from "Core.Components";
import { EventType, KeyCodeEnum, ElementTypes } from "Core.Enums";
import { Client, App, LangSelect } from "Core.Clients";
import { Feature } from "Core.ViewModels";

/**
 * Represents a tab editor component, which can manage multiple tabs and their content.
 * @extends {EditForm}
 */
export class TabEditor extends EditForm {
    static TabWrapper = Document.getElementById("tabs");
    static TabContainer = Document.getElementById("tab-content");
    static Tabs = [];
    static ActiveTab = () => TabEditor.Tabs.find(x => x.Show);
    static FindTab = (id) => TabEditor.Tabs.find(x => x.Id === id);
    static ShowTabText;
    static ActiveClass = "active";

    constructor(entity = null) {
        super(entity);
        this.PopulateDirty = false;
        this.ShouldLoadEntity = true;
        this._hotKeyComponents = [];
        this.DataSearchEntry = {};
    }

    /**
     * Renders the component to the DOM.
     */
    render() {
        if (!this.ParentElement) {
            this.ParentElement = TabEditor.TabContainer;
        }
        if (this.Popup) {
            this.renderPopup();
        } else {
            this.renderTab();
        }
        this.focus();
    }

    get TabTitle() {
        return this.Feature?.Label ?? this.Title;
    }

    /**
     * Renders the tab part of the editor.
     */
    renderTab() {
        const html = Html.Take(TabEditor.TabWrapper);
        if (Html.Context) {
            html.Li.className("nav-item").title(this.TabTitle)
            .A.className("nav-link pl-lg-2 pr-lg-2 pl-xl-3 pr-xl-3")
            .event(EventType.Click, () => this.focus()).event(EventType.MouseUp, (e) => this.close(e));
            html.Icon("fa fal fa-compress-wide").event(EventType.Click, (e) => {
                ComponentExt.fullScreen(this.Element);
            }).end().render();
            html.Icon("fa fa-times").event(EventType.Click, (e) => {
                e.stopPropagation();
                this.dirtyCheckAndCancel();
            }).end().span.className(this.Feature?.Icon ?? "").end().span.className("title").iText(this.TabTitle).end().render();
            this._li = Html.Context.parentElement;
            this.IconElement = this._li.firstElementChild;
        }
        Html.Take(TabEditor.TabContainer).tabIndex(-1).trigger(EventType.Focus).div.event(EventType.KeyDown, (e) => this.hotKeyHandler(e)).render();
        this.Element = Html.Context;
        this.ParentElement = TabEditor.TabContainer;
        TabEditor.Tabs.push(this);
        super.render();
    }

    /**
     * Renders the popup part of the editor.
     */
    renderPopup() {
        if (this.Parent instanceof GridView) {
            Html.Take(this.ParentElement ?? this.Parent?.Element ?? TabEditor.TabContainer)
            .div.className("backdrop-gridview").tabIndex(-1).trigger(EventType.Focus).event(EventType.KeyDown, (e) => this.hotKeyHandler(e));
            this._backdropGridView = Html.Context;
            Html.Instance
                .div.className("popup-content").div.className("popup-title").span.iconForSpan(this.Icon);
            this.IconElement = Html.Context;
            Html.Instance.end().span.iText(this.Title);
            this.TitleElement = Html.Context;
            Html.Instance.end().div.className("icon-box").span.className("fa fa-times")
                .event(EventType.Click, () => this.dispose())
                .endOf(".popup-title")
                .div.className("popup-body");
            this.Element = Html.Context;
            super.render();
            if (this._backdropGridView.outOfViewport().top) {
                this._backdropGridView.scrollIntoView(true);
            }
        } else {
            Html.Take(this.ParentElement ?? this.Parent?.Element ?? TabEditor.TabContainer)
            .div.className("backdrop").tabIndex(-1).trigger(EventType.Focus).event(EventType.KeyDown, (e) => this.hotKeyHandler(e));
            this._backdrop = Html.Context;
            Html.Instance
                .div.className("popup-content").div.className("popup-title").span.iconForSpan(this.Icon);
            this.IconElement = Html.Context;
            Html.Instance.end().span.iText(this.Title);
            this.TitleElement = Html.Context;
            Html.Instance.end().div.className("icon-box").span.className("fa fa-times")
                .event(EventType.Click, () => this.dispose())
                .endOf(".popup-title")
                .div.className("popup-body");
            this.Element = Html.Context;
            super.render();
            if (this._backdrop.outOfViewport().top) {
                this._backdrop.scrollIntoView(true);
            }
        }
    }

    /**
     * Handles hotkey events for the editor.
     * @param {Event} e - The event object.
     */
    hotKeyHandler(e) {
        const keyCode = e.KeyCodeEnum();
        if (keyCode === KeyCodeEnum.F6) {
            let gridView = this.findActiveComponent(GridView).first();
            if (gridView && !gridView.allListViewItem.some(x => x.selected)) {
                if (gridView.allListViewItem.length) {
                    gridView.allListViewItem[0].focus();
                } else {
                    gridView.listViewSearch.focus();
                }
            }
            return;
        }
        if (e.altKey() && keyCode === KeyCodeEnum.GraveAccent) {
            if (TabEditor.Tabs.length <= 1) {
                return;
            }
            let index = TabEditor.Tabs.indexOf(this);
            index = index >= TabEditor.Tabs.length - 1 ? 0 : index + 1;
            if (index < 0 || index > TabEditor.Tabs.length) {
                return;
            }

            TabEditor.Tabs[index]?.focus();
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
        if (keyCode >= KeyCodeEnum.Shift && keyCode <= KeyCodeEnum.Alt) {
            return;
        }

        this.triggerMatchHotKey(e, keyCode, shiftKey, ctrlKey, altKey);
    }

    /**
     * Checks if the default hotkeys are triggered.
     * @param {KeyCodeEnum} keyCode - The key code of the event.
     * @param {boolean} shiftKey - Indicates if the Shift key is pressed.
     * @param {boolean} ctrlKey - Indicates if the Control key is pressed.
     * @param {boolean} altKey - Indicates if the Alt key is pressed.
     * @returns {boolean} - True if a default hotkey is triggered, otherwise false.
     */
    defaultHotKeys(keyCode, shiftKey, ctrlKey, altKey) {
        if (!keyCode) {
            return false;
        }
        if (keyCode === KeyCodeEnum.Escape && !shiftKey && !ctrlKey && !altKey) {
            this.dirtyCheckAndCancel();
            return true;
        }
        if (ctrlKey && altKey && (keyCode === KeyCodeEnum.LeftArrow || keyCode === KeyCodeEnum.RightArrow)) {
            let index = TabEditor.Tabs.indexOf(this);
            index = keyCode === KeyCodeEnum.LeftArrow ? (index === 0 ? TabEditor.Tabs.length - 1 : index - 1) : (index >= TabEditor.Tabs.length - 1 ? 0 : index + 1);
            if (index < 0 || index > TabEditor.Tabs.length) {
                return false;
            }

            TabEditor.Tabs[index]?.focus();
            return true;
        }
        if (ctrlKey && shiftKey && keyCode === KeyCodeEnum.F) {
            // Trigger search in the grid view
            let listView = this.findActiveComponent(ListView).first();
            if (!listView || !listView.meta.canSearch) {
                return true;
            }

            listView.listViewSearch.advancedSearch(null);
            return true;
        }
        return false;
    }

    /**
     * Focuses the tab editor component, updating the document title and potentially the URL.
     */
    focus() {
        if (!this.Popup) {
            TabEditor.Tabs.forEach(x => x.Show = false);
            if (this.FeatureName && App.featureLoaded) {
                this.Href = Client.baseUri + '/' + this.FeatureName + (this.EntityId ? `?Id=${this.EntityId}` : '');
                window.history.pushState(null, LangSelect.get(this.TabTitle), this.Href);
            }
        }
        this.Show = true;
        document.title = LangSelect.get(this.TabTitle);
        this.findActiveComponent(EditableComponent, x => x?.Meta?.Focus).first()?.focus();
    }

    /**
     * Closes the tab editor on specific mouse events.
     * @param {Event} e - The event object.
     */
    close(e) {
        const which = parseInt(e["which"]?.toString());
        const button = parseInt(e["button"]?.toString());
        if (which === 2 || button === 1) {
            e.preventDefault();
            this.dirtyCheckAndCancel();
        }
    }

    /**
     * Disposes of the tab editor, removing it from the DOM and focusing on the parent form.
     */
    dispose() {
        if (this.ParentForm) {
            this.ParentForm.focus();
        } else if (this.Parent) {
            this.Parent.focus();
        } else {
            this.ParentElement?.focus();
        }

        if (!this.Popup && this._li) {
            this._li.remove();
            this.disposeTab();
        } else {
            this.OpenFrom?.focus();
        }
        super.dispose();
    }

    /**
     * Disposes of the tab, removing its association from the list of tabs.
     */
    disposeTab() {
        if (!this.ParentForm) {
            const lastTab = TabEditor.Tabs.filter(x => x !== this).pop();
            if (lastTab) {
                lastTab.focus();
            }
        } else {
            this.ParentForm.focus();
            this.ParentForm = null;
        }
        TabEditor.Tabs = TabEditor.Tabs.filter(x => x !== this);
    }

    /**
     * Removes DOM elements associated with the tab editor.
     */
    removeDOM() {
        this.Element?.remove();
        this._li?.remove();
        this._backdrop?.remove();
        this._backdropGridView?.remove();
    }
}
