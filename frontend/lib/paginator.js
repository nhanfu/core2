import { EditableComponent } from "./editableComponent.js";
import { Label } from "./label.js";
import { ListView } from "./listView.js";
import { Numbox } from "./numbox.js";
import { Html } from "./utils/html.js";
/**
 * @typedef {import('./listView.js').listView} ListView
 */

/**
 * Class representing pagination options.
 */
export class PaginationOptions {
    /**
     * Create pagination options.
     * @param {number} total - Total number of items.
     * @param {number} pageSize - Number of items per page.
     * @param {number} selected - Currently selected page index.
     * @param {number} pageIndex - Index of the current page.
     * @param {number} pageNumber - Number representation of the current page.
     * @param {number} currentPageCount - Current count of pages.
     * @param {number} startIndex - Start index of the pagination.
     * @param {number} endIndex - End index of the pagination.
     * @param {Function} clickHandler - Function to handle click events on page navigation.
     */
    constructor(total, pageSize, selected, pageIndex, pageNumber, currentPageCount, startIndex, endIndex, clickHandler) {
        this.total = total;
        this.pageSize = pageSize;
        this.selected = selected;
        this.pageIndex = pageIndex;
        this.pageNumber = pageNumber;
        this.currentPageCount = currentPageCount;
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.clickHandler = clickHandler;
        this.disabled = false;
    }
}

/**
 * Class representing a paginator.
 */
export class Paginator extends EditableComponent {
    /** @type {ListView} */
    // @ts-ignore
    parent;
    /**
     * Create a paginator.
     * @param {PaginationOptions} paginationOptions - Options for pagination.
     */
    constructor(paginationOptions) {
        super(null, null);
        if (!paginationOptions) throw new Error("paginationOptions is required");
        this.entity = paginationOptions;
        this.options = paginationOptions;
        this.options.startIndex = this.options.startIndex || 1;
        this.element = null;
        this.populateDirty = false;
        this.alwaysValid = true;
    }

    /**
     * Render the paginator into the DOM.
     */
    render() {
        Html.take(this.parent.element).div.className("grid-toolbar paging").label.iText("Pagination").end.render();
        this.element = Html.context;
        var startIndex = new Label({ fieldName: "startIndex" });
        var endIndex = new Label({ fieldName: "endIndex" });
        var total = new Label({ fieldName: "Total" });
        var pageNum = new Numbox({ fieldName: "pageNumber" });
        pageNum.alwaysValid = true;
        pageNum.setSeclection = false;
        var pageSize = new Numbox({ fieldName: "pageSize" })
        pageSize.setSeclection = false;
        this.addChild(pageSize);
        pageSize.element.addEventListener("change", this.reloadListView);
        Html.instance.end.render();
        Html.instance.div.style("display: flex;").render();
        this.addChild(startIndex);
        Html.instance.text("-");
        this.addChild(endIndex);
        Html.instance.iText(" of ");
        this.addChild(total);
        Html.take(this.element).ul.className("pagination").li.text("❮").event("click", this.prevPage.bind(this)).end.render();
        this.addChild(pageNum);
        pageNum.element.addEventListener("change", () => {
            this.options.pageIndex = this.options.pageNumber - 1;
            this.reloadListView();
        });
        Html.instance.end.li.text("❯").event("click", this.nextPage.bind(this)).end.render();
    }

    /**
     * Create a number input linked to a specified property.
     * @param {string} propertyName - Name of the property that this input represents.
     * @returns {hTMLInputElement} - The created input element.
     */
    createNumberInput(propertyName) {
        const input = document.createElement('input');
        input.type = 'number';
        input.value = this.options[propertyName];
        input.addEventListener('change', () => {
            this.options[propertyName] = parseInt(input.value);
            this.reloadListView();
        });
        return input;
    }

    /**
     * Create a label for displaying data.
     * @param {string} propertyName - Name of the property to display.
     * @param {string} [format] - Optional format string.
     * @returns {hTMLLabelElement} - The created label element.
     */
    createLabel(propertyName, format = "") {
        const label = document.createElement('label');
        label.textContent = format ? format.replace("{0:n0}", this.options[propertyName].toLocaleString()) : this.options[propertyName];
        return label;
    }

    /**
     * Handle the event for navigating to the next page.
     */
    nextPage() {
        const pages = Math.ceil(this.options.Total / this.options.pageSize);
        if (this.options.pageNumber >= pages) return;

        this.options.pageIndex++;
        if (this.options.clickHandler) this.options.clickHandler(this.options.pageIndex, null);
        this.reloadListView();
    }

    /**
     * Handle the event for navigating to the previous page.
     */
    prevPage() {
        if (this.options.pageIndex <= 0) return;

        this.options.pageIndex--;
        if (this.options.clickHandler) this.options.clickHandler(this.options.pageIndex, null);
        this.reloadListView();
    }

    /**
     * Reload the list view. This is a placeholder for actual implementation.
     */
    reloadListView() {
        // This method should trigger a refresh of the parent list view, dependent on specific implementation.
        if (this.parent instanceof ListView) {
            this.parent.actionFilter();
        }
    }
}
