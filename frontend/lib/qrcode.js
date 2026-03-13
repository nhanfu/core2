import { EditableComponent } from "./editableComponent.js";
import { QRCode as QR } from "./structs/qrcode.js";
import { Html } from "./utils/html.js";

/**
 * Represents a barcode component that can be rendered and updated.
 */
export class QRCode extends EditableComponent {
    /**
     * Creates an instance of barCode.
     * @param {Object} meta - The UI component associated with the barcode.
     * @param {HTMLElement} ele - The parent HTML element for the barcode.
     */
    constructor(meta, ele) {
        super(meta);
        if (!meta) {
            throw new Error("UI is required");
        }
        this.parentElement = ele;
        this.defaultValue = '';
        this.Value = '';
    }

    /**
     * Renders the barcode into the parent element.
     */
    Render() {
        const ctx = Html.take(this.parentElement)
            .clear()
            .Div.Style(`width:${this.meta.Width}px;margin:auto`)
            .Id("barcode" + this.meta.Id);
        // @ts-ignore
        this.element = ctx;
        this.Value = this.fieldVal;
        new QR("barcode" + this.meta.Id, {
            text: this.Value,
            width: this.meta.Width,
            height: this.meta.Width,
            colorDark: "#000000",
            colorLight: "#ffffff",
        });
    }

    /**
     * Updates the view of the barcode, re-rendering if the value has changed.
     * @param {boolean} [Force=false] - Forces the update regardless of changes.
     * @param {boolean|null} [Dirty=null] - Marks the component as dirty, not used in this method.
     * @param {...string} componentNames - Additional components to consider in the update.
     */
    updateView(Force = false, Dirty = null, ...componentNames) {
        if (this.fieldVal === this.Value) {
            return;
        }
        this.render();
    }
}
