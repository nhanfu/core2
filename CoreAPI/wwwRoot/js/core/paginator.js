import EditableComponent from "./editableComponent.js";
/**
 * @typedef {import('./listView.js').ListView} ListView
 */

/**
 * Class representing pagination options.
 */
export class PaginationOptions {
    /**
     * Create pagination options.
     * @param {number} Total - Total number of items.
     * @param {number} PageSize - Number of items per page.
     * @param {number} Selected - Currently selected page index.
     * @param {number} PageIndex - Index of the current page.
     * @param {number} PageNumber - Number representation of the current page.
     * @param {number} CurrentPageCount - Current count of pages.
     * @param {number} StartIndex - Start index of the pagination.
     * @param {number} EndIndex - End index of the pagination.
     * @param {Function} ClickHandler - Function to handle click events on page navigation.
     */
    constructor(Total, PageSize, Selected, PageIndex, PageNumber, CurrentPageCount, StartIndex, EndIndex, ClickHandler) {
        this.Total = Total;
        this.PageSize = PageSize;
        this.Selected = Selected;
        this.PageIndex = PageIndex;
        this.PageNumber = PageNumber;
        this.CurrentPageCount = CurrentPageCount;
        this.StartIndex = StartIndex;
        this.EndIndex = EndIndex;
        this.ClickHandler = ClickHandler;
    }
}

/**
 * Class representing a paginator.
 */
export class Paginator extends EditableComponent {
    /** @type {ListView} */
    Parent;
    /**
     * Create a paginator.
     * @param {PaginationOptions} paginationOptions - Options for pagination.
     */
    constructor(paginationOptions) {
        if (!paginationOptions) throw new Error("paginationOptions is required");
        this.Options = paginationOptions;
        this.Element = null; // This will be set when render is called
        this.PopulateDirty = false;
        this.AlwaysValid = true;
    }

    /**
     * Render the paginator into the DOM.
     */
    Render() {
        const container = document.createElement('div');
        container.className = 'grid-toolbar paging';

        const label = document.createElement('label');
        label.textContent = 'Page size';
        container.appendChild(label);

        // Create elements for page size, start index, end index, total, and page number
        const pageSizeElement = this.CreateNumberInput('PageSize');
        const startIndexLabel = this.CreateLabel('StartIndex');
        const endIndexLabel = this.CreateLabel('EndIndex');
        const totalLabel = this.CreateLabel('Total', "{0:n0}");
        const pageNumberElement = this.DreateNumberInput('PageNumber');

        container.appendChild(pageSizeElement);
        container.appendChild(startIndexLabel);
        container.appendChild(document.createTextNode('-'));
        container.appendChild(endIndexLabel);
        container.appendChild(document.createTextNode(' of '));
        container.appendChild(totalLabel);

        // Navigation buttons
        const ul = document.createElement('ul');
        ul.className = 'pagination';
        const prevLi = document.createElement('li');
        prevLi.textContent = '❮';
        prevLi.addEventListener('click', () => this.PrevPage());
        ul.appendChild(prevLi);

        const nextLi = document.createElement('li');
        nextLi.textContent = '❯';
        nextLi.addEventListener('click', () => this.NextPage());
        ul.appendChild(nextLi);

        container.appendChild(ul);
        this.Element = container;

        // Append the container to the parent element, assumed to be available
        document.body.appendChild(container); // or another parent element
    }

    /**
     * Create a number input linked to a specified property.
     * @param {string} propertyName - Name of the property that this input represents.
     * @returns {HTMLInputElement} - The created input element.
     */
    CreateNumberInput(propertyName) {
        const input = document.createElement('input');
        input.type = 'number';
        input.value = this.Options[propertyName];
        input.addEventListener('change', () => {
            this.Options[propertyName] = parseInt(input.value);
            this.ReloadListView();
        });
        return input;
    }

    /**
     * Create a label for displaying data.
     * @param {string} propertyName - Name of the property to display.
     * @param {string} [format] - Optional format string.
     * @returns {HTMLLabelElement} - The created label element.
     */
    CreateLabel(propertyName, format = "") {
        const label = document.createElement('label');
        label.textContent = format ? format.replace("{0:n0}", this.Options[propertyName].toLocaleString()) : this.Options[propertyName];
        return label;
    }

    /**
     * Handle the event for navigating to the next page.
     */
    NextPage() {
        const pages = Math.ceil(this.Options.Total / this.Options.PageSize);
        if (this.Options.PageNumber >= pages) return;

        this.Options.PageIndex++;
        if (this.Options.ClickHandler) this.Options.ClickHandler(this.Options.PageIndex, null);
        this.ReloadListView();
    }

    /**
     * Handle the event for navigating to the previous page.
     */
    PrevPage() {
        if (this.Options.PageIndex <= 0) return;

        this.Options.PageIndex--;
        if (this.Options.ClickHandler) this.Options.ClickHandler(this.Options.PageIndex, null);
        this.ReloadListView();
    }

    /**
     * Reload the list view. This is a placeholder for actual implementation.
     */
    ReloadListView() {
        // This method should trigger a refresh of the parent list view, dependent on specific implementation.
        this.Parent.ActionFilter();
    }
}
