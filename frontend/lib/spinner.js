import { Html } from "./utils/html";

export class Spinner {
    /** @type {Spinner} */
    static _instance = null;
    /** @type {HTMLElement} */
    static _span = null;
    /** @type {HTMLElement} */
    static _backdrop = null;
    static _hiddenAwaiter = null;
    constructor() {

    }

    static Init() {
        if (this._instance !== null) {
            return;
        }

        this._instance = new Spinner();
        Html.take(document.body).div.className("backdrop-spinner").style("background: transparent !important;");
        this._backdrop = Html.getContext();
        Html.div.className("loader");
        this._span = Html.getContext();
        this._span.style.display = "none";
        this._backdrop.style.display = "none";
    }

    static appendTo() {
        if (!this._span) {
            return;
        }
        this._span.style.display = "";
        this._backdrop.style.display = "";
    }

    static hide() {
        if (this._span) {
            this._span.style.display = "none";
            this._backdrop.style.display = "none";
        }
    }
}
