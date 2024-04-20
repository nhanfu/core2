import EditableComponent from './editableComponent.js';
import { HtmlEvent, HTML, html } from './html.js';

class Button extends EditableComponent {
    constructor(ui, ele) {
        super(ui);
        this.ButtonEle = ele;
        this._textEle = null;
    }

    Render() {
        const htmlInstance = html.Instance;
        if (!this.ButtonEle) {
            if (!this.ParentElement) throw new Error('ParentElement cannot be null');
            html.Take(this.ParentElement).Button.ClassName(`btn${this.Meta.Id}`).Render();
            this.Element = this.ButtonEle = html.Context;
        } else {
            this.Element = this.ButtonEle;
        }
        html.Take(this.Element).ClassName(this.Meta.ClassName)
            .Event(HtmlEvent.click, this.DispatchClick).Style(this.Meta.Style);
        if (this.Meta.Icon) {
            htmlInstance.Icon(this.Meta.Icon).End.Text(' ').Render();
        }
        htmlInstance.Span.ClassName('caption').IText(this.Meta.Label ?? '');
        this._textEle = html.Context;
        this.Element.closest('td')?.addEventListener('keydown', this.ListViewItemTab);
        this.DOMContentLoaded?.();
    }

    DispatchClick() {
        if (this.Disabled || this.Element.hidden) {
            return;
        }
        this.Disabled = true;
        try {
            Spinner.AppendTo(this.Element);
            this.DispatchEvent(this.Meta.Events, HtmlEvent.click, this.Entity, this).Done(() => Spinner.Hide());
        } finally {
            this.Disabled = false;
        }
    }

    GetValueText() {
        if (!this.Entity || !this.Meta) {
            return this._textEle.textContent;
        }
        return this.Entity[this.FieldName]?.toString();
    }
}

window.Core2 = window.Core || {};
window.Core2.Button = Button;

export default Button;
