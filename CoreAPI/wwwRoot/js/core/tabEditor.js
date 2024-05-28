import { Client } from "./clients/client.js";
import { EditForm } from "./editForm.js";
import { ElementType } from "./models/elementType.js";
import EventType from "./models/eventType.js";
import { KeyCodeEnum } from "./models/enum.js";
import { Html } from "./utils/html.js";
import { LangSelect } from "./utils/langSelect.js";
import { ComponentExt } from "./utils/componentExt.js";
import { GridView } from "./gridView.js";
import { ListView } from "./listView.js";
import { Button } from "./button.js";
import EditableComponent from "./editableComponent.js";

/**
 * Represents a tab editor component, which can manage multiple tabs and their content.
 * @extends {EditForm}
 */
export class TabEditor extends EditForm {
    static TabWrapper = document.getElementById("tabs");
    static TabContainer = document.getElementById("tab-content");
    /** @type {TabEditor[]} */
    static Tabs = [];
    static ActiveTab = () => TabEditor.Tabs.find(x => x.Show);
    static FindTab = (id) => TabEditor.Tabs.find(x => x.Id === id);
    /** @type {boolean} */
    static ShowTabText;
    static ActiveClass = "active";
    Popup = false;

    constructor(entity = null) {
        super(entity);
        this.PopulateDirty = false;
        this.ShouldLoadEntity = true;
        this._hotKeyComponents = [];
        this.DataSearchEntry = {};
    }

    /**
     * Gets or sets the visibility of the tab editor.
     * @property
     */
    get Show() {
        return super.Show;
    }

    set Show(value) {
        super.Show = value;
        if (!this._li) {
            return;
        }

        if (value) {
            this._li.classList.add(TabEditor.ActiveClass);
            this._li.querySelector(ElementType.a).classList.add(TabEditor.ActiveClass);
        } else {
            this._li.classList.remove(TabEditor.ActiveClass);
            this._li.querySelector(ElementType.a).classList.remove(TabEditor.ActiveClass);
        }
    }

    /**
     * Renders the component to the DOM.
     */
    Render() {
        if (!this.ParentElement) {
            this.ParentElement = TabEditor.TabContainer;
        }
        if (this.Popup) {
            this.RenderPopup();
        } else {
            this.RenderTab();
        }
        this.Focus();
    }

    get TabTitle() {
        return this.Meta?.Label ?? this.Title;
    }

    /**
     * Renders the tab part of the editor.
     */
    RenderTab() {
        const html = Html.Take(TabEditor.TabWrapper);
        if (Html.Context) {
            html.Li.ClassName("nav-item").Title(this.TabTitle)
                .A.ClassName("nav-link pl-lg-2 pr-lg-2 pl-xl-3 pr-xl-3")
                .Event(EventType.Click, () => this.Focus()).Event(EventType.MouseUp, (e) => this.Close(e));
            html.Icon("fa fal fa-compress-wide").Event(EventType.Click, (e) => {
                ComponentExt.FullScreen(this.Element);
            }).End.Render();
            html.Icon("fa fa-times").Event(EventType.Click, (e) => {
                e.stopPropagation();
                this.DirtyCheckAndCancel();
            }).End.Span.ClassName(this.Meta?.Icon ?? "").End.Span.ClassName("title").IText(this.TabTitle).End.Render();
            this._li = Html.Context.parentElement;
            // @ts-ignore
            this.IconElement = this._li.firstElementChild;
        }
        Html.Take(TabEditor.TabContainer).TabIndex(-1).Trigger(EventType.Focus).Div.Event(EventType.KeyDown, (e) => this.HotKeyHandler(e)).Render();
        this.Element = Html.Context;
        this.ParentElement = TabEditor.TabContainer;
        TabEditor.Tabs.push(this);
        super.Render();
    }

    /**
     * Renders the popup part of the editor.
     */
    RenderPopup() {
        if (this.Parent instanceof GridView) {
            Html.Take(this.ParentElement ?? this.Parent?.Element ?? TabEditor.TabContainer)
                .Div.ClassName("backdrop-gridview").TabIndex(-1).Trigger(EventType.Focus).Event(EventType.KeyDown, (e) => this.HotKeyHandler(e));
            this._backdropGridView = Html.Context;
            Html.Instance
                .Div.ClassName("popup-content").Div.ClassName("popup-title").Span.IconForSpan(this.Icon);
            this.IconElement = Html.Context;
            Html.Instance.End.Span.IText(this.Title);
            this.TitleElement = Html.Context;
            Html.Instance.End.Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                .Event(EventType.Click, () => this.Dispose())
                .EndOf(".popup-title")
                .Div.ClassName("popup-body");
            this.Element = Html.Context;
            super.Render();
            if (this._backdropGridView.OutOfViewport().Top) {
                this._backdropGridView.scrollIntoView(true);
            }
        } else {
            Html.Take(this.ParentElement ?? this.Parent?.Element ?? TabEditor.TabContainer)
                .Div.ClassName("backdrop").TabIndex(-1).Trigger(EventType.Focus).Event(EventType.KeyDown, (e) => this.HotKeyHandler(e));
            this._backdrop = Html.Context;
            Html
                .Div.ClassName("popup-content").Div.ClassName("popup-title").Span.IconForSpan(this.Icon);
            this.IconElement = Html.Context;
            Html.Instance.End.Span.IText(this.Title);
            this.TitleElement = Html.Context;
            Html.Instance.End.Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                .Event(EventType.Click, () => this.Dispose())
                .EndOf(".popup-title")
                .Div.ClassName("popup-body");
            this.Element = Html.Context;
            super.Render();
            if (this._backdrop.OutOfViewport().Top) {
                this._backdrop.scrollIntoView(true);
            }
        }
    }

    TriggerMatchHotKey(e, keyCode, shiftKey, ctrlKey, altKey) {
        if (keyCode == null) {
            return;
        }
        let patternList = [];
        if (shiftKey) {
            patternList.push(KeyCodeEnum.Shift);
        }
    
        if (ctrlKey) {
            patternList.push(KeyCodeEnum.Ctrl);
        }
    
        if (altKey) {
            patternList.push(KeyCodeEnum.Alt);
        }
    
        if (keyCode < KeyCodeEnum.Shift) {
            patternList.unshift(keyCode);
        } else if (keyCode > KeyCodeEnum.Alt) {
            patternList.push(keyCode);
        }
    
        this._hotKeyComponents = this._hotKeyComponents
            || this.FilterChildren(x => x instanceof Button && x.Meta.HotKey.trim() !== '').map(x => x);
        this._hotKeyComponents.forEach(com => {
            let parts = com.Meta.HotKey.split(",");
            if (parts.length === 0) {
                return;
            }
    
            let lastPart = parts[parts.length - 1];
            let configKeys = lastPart.split("-").map(x => {
                let key = KeyCodeEnum[x.trim()];
                return key ? key : null;
            }).filter(x => x != null).sort((a, b) => a - b);
            let isMatch = JSON.stringify(patternList) === JSON.stringify(configKeys);
            if (!isMatch) {
                return;
            }
    
            e.preventDefault();
            e.stopPropagation();
            com.Element?.click();
            return;
        });
    }

    /**
     * Handles hotkey events for the editor.
     * @param {Event} e - The event object.
     */
    HotKeyHandler(e) {
        const keyCode = e.KeyCodeEnum();
        if (keyCode === KeyCodeEnum.F6) {
            let gridView = this.FindActiveComponent(x => x instanceof GridView).FirstOrDefault();
            if(gridView instanceof GridView){
                if (gridView && !gridView.AllListViewItem.some(x => x.Selected)) {
                    if (gridView.AllListViewItem.length) {
                        gridView.AllListViewItem[0].Focus();
                    } else {
                        gridView.ListViewSearch.Focus();
                    }
                }
                return;
            }
        }
        if (e.AltKey() && keyCode === KeyCodeEnum.GraveAccent) {
            if (TabEditor.Tabs.length <= 1) {
                return;
            }
            let index = TabEditor.Tabs.indexOf(this);
            index = index >= TabEditor.Tabs.length - 1 ? 0 : index + 1;
            if (index < 0 || index > TabEditor.Tabs.length) {
                return;
            }

            TabEditor.Tabs[index]?.Focus();
        }
        const shiftKey = e.ShiftKey();
        const ctrlKey = e.CtrlOrMetaKey();
        const altKey = e.AltKey();
        const defaultKeys = this.DefaultHotKeys(keyCode, shiftKey, ctrlKey, altKey);
        if (defaultKeys) {
            e.preventDefault();
            e.stopPropagation();
            return;
        }
        if (keyCode >= KeyCodeEnum.Shift && keyCode <= KeyCodeEnum.Alt) {
            return;
        }

        this.TriggerMatchHotKey(e, keyCode, shiftKey, ctrlKey, altKey);
    }

    

    /**
     * Checks if the default hotkeys are triggered.
     * @param {boolean} shiftKey - Indicates if the Shift key is pressed.
     * @param {boolean} ctrlKey - Indicates if the Control key is pressed.
     * @param {boolean} altKey - Indicates if the Alt key is pressed.
     * @returns {boolean} - True if a default hotkey is triggered, otherwise false.
     */
    DefaultHotKeys(keyCode, shiftKey, ctrlKey, altKey) {
        if (!keyCode) {
            return false;
        }
        // @ts-ignore
        if (keyCode === KeyCodeEnum.Escape && !shiftKey && !ctrlKey && !altKey) {
            this.DirtyCheckAndCancel();
            return true;
        }
        if (ctrlKey && altKey && (keyCode === KeyCodeEnum.LeftArrow || keyCode === KeyCodeEnum.RightArrow)) {
            let index = TabEditor.Tabs.indexOf(this);
            index = keyCode === KeyCodeEnum.LeftArrow ? (index === 0 ? TabEditor.Tabs.length - 1 : index - 1) : (index >= TabEditor.Tabs.length - 1 ? 0 : index + 1);
            if (index < 0 || index > TabEditor.Tabs.length) {
                return false;
            }

            TabEditor.Tabs[index]?.Focus();
            return true;
        }
        if (ctrlKey && shiftKey && keyCode === KeyCodeEnum.F) {
            // Trigger search in the grid view
            let listView = this.FindActiveComponent(x => x instanceof ListView).FirstOrDefault();
            if(listView instanceof ListView) {
                if (!listView || !listView.Meta.CanSearch) {
                    return true;
                }
                listView.ListViewSearch.AdvancedSearch(null);
                return true;
            }
        }
        return false;
    }

    /**
     * Focuses the tab editor component, updating the document title and potentially the URL.
     */
    Focus() {
        if (!this.Popup) {
            TabEditor.Tabs.forEach(x => x.Show = false);
            if (this.FeatureName) {
                this.Href = Client.BaseUri + '/' + this.FeatureName + (this.EntityId ? `?Id=${this.EntityId}` : '');
                window.history.pushState(null, LangSelect.Get(this.TabTitle), this.Href);
            }
        }
        this.Show = true;
        document.title = LangSelect.Get(this.TabTitle);
        this.FindActiveComponent(x => x instanceof EditableComponent && x?.Meta?.Focus == true && x.Meta.Focus).FirstOrDefault()?.Focus();
    }

    Close(event) {
        const intWhich = parseInt(event["which"]?.toString());
        const intButton = parseInt(event["button"]?.toString());
        if (intWhich === 2 || intButton === 1) {
            event.preventDefault();
            this.DirtyCheckAndCancel();
        }
    }

    /**
     * Disposes of the tab editor, removing it from the DOM and focusing on the parent form.
     */
    Dispose() {
        if (this.ParentForm) {
            this.ParentForm.Focus();
        } else if (this.Parent) {
            this.Parent.Focus();
        } else {
            this.ParentElement?.focus();
        }

        if (!this.Popup && this._li) {
            this._li.remove();
            this.DisposeTab();
        } else {
            this.OpenFrom?.Focus();
        }
        super.Dispose();
    }

    /**
     * Disposes of the tab, removing its association from the list of tabs.
     */
    DisposeTab() {
        if (!this.ParentForm) {
            const lastTab = TabEditor.Tabs.filter(x => x !== this).pop();
            if (lastTab) {
                lastTab.Focus();
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
    RemoveDOM() {
        this.Element?.remove();
        this._li?.remove();
        this._backdrop?.remove();
        this._backdropGridView?.remove();
    }

}
