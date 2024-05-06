import EditableComponent from "./editableComponent";
import { Html } from "./utils/html.js";
import EventType from './models/eventType.js';
import EditableComponent from './editableComponent.js';
import ObservableArgs from './models/observable.js';
import "./utils/fix.js";
import { Uuid7 } from "./structs/uuidv7.js";


class RichTextBox extends EditableComponent {
     /**
     * @param {Component} ui
     * @param {HTMLElement} [ele=null] 
     */
    constructor(ui, ele = null) {
        super(ui,ele);
        this.defaultValue = "";
        if (this.Meta.Row <= 0) {
                this.Meta.Row = 1;
        }
        if (ele != null)
            {
                this.ParentElement = ele ; 
                this.BindingWebComponent();
            }
            else
            {
                this.ParentElement = this.ParentElement ?? Html.Context;
                this.BindingWebComponent();
            }
        
            this.ParentElement.AppendChild(this.Element);
    }

    BindingWebComponent() {
        this.Element = Html.Take(this.parentElement).Div.Id(Uuid7.Id25()).GetContext();
    }

    render() {
        initCkEditor(this);
    }

    updateView(force = false, dirty = null, ...componentNames) {
        const handler = this._events["UpdateView"];
        if (handler) {
            handler(new ObservableArgs({ Com: this, EvType: 'Change' }));
        }
    }
}
window.Core2 = window.Core2 || {};
window.Core2.RichTextBox = RichTextBox;

export default RichTextBox;