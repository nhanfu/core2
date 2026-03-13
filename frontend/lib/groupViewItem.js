import { ListViewItem } from "./listViewItem.js";


export class GroupViewItem extends ListViewItem {
    static #chevronDown = "fa-square";
    static #chevronRight = "fa-check-square";

    #showChildren = true;
    #showChildren1 = true;
    #parentItem;
    #childrenItems = [];
    #groupText;

    /**
     * @param {import("./models/elementType.js").elementType} elementType
     */
    constructor(elementType) {
        super(elementType);
        this.groupRow = true;
        this.#childrenItems = [];
    }
    Key;
    render() {
        super.render();
        this.element.classList.add(ListViewItem.groupRowClass);
    }

    get selected() { return false; }
    set selected(value) { this._selected = false; }

    get parentItem() { return this.#parentItem; }
    set parentItem(value) { this.#parentItem = value; }

    get childrenItems() { return this.#childrenItems; }
    set childrenItems(value) { this.#childrenItems = value; }

    get groupText() { return this.#groupText; }
    set groupText(value) { this.#groupText = value; }

    /**
     * @param {string} text
     */
    appendGroupText(text) {
        if (!this.#groupText) return;
        this.#groupText.innerHTML = this.#groupText.firstElementChild.outerHTML + text;
    }

    setGroupText(text) {
        if (!this.#groupText) return;
        this.#groupText.innerHTML = text;
    }

    get showChildren() { return this.#showChildren; }
    set showChildren(value) {
        this.#showChildren = value;
        this.#childrenItems.forEach(x => x.Show = value);
    }

    get showChildren1() { return this.#showChildren1; }
    set showChildren1(value) {
        this.#showChildren1 = value;
        this.#childrenItems.forEach(x => x.selected = value);
        if (!value) {
            this._chevron.replaceClass(GroupViewItem.#chevronRight, GroupViewItem.#chevronDown);
        } else {
            this._chevron.replaceClass(GroupViewItem.#chevronDown, GroupViewItem.#chevronRight);
        }
    }
}
