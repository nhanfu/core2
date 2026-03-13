import { SearchEntry } from "./searchEntry.js";
import { Client } from "./clients/client.js";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import EventType from "./models/eventType.js";

export class MultipleSearchEntry extends SearchEntry {
    static multipleClass = "multiple";
    _toggleButton = null;
    isMultiple = true;

    constructor(ui) {
        super(ui);
    }

    render() {
        this._listValues = [];
        this.setDefaultVal();
        this.tryParseData();
        this.renderInputAndEvents();
        this.findMatchText();
        this.searchResultEle = document.body;
        this.element.parentElement.classList.add(MultipleSearchEntry.multipleClass);
        this.element.parentElement.addEventListener("click", () => {
            this._input.focus();
        });
    }

    tryParseData() {
        if (!this.entity) {
            return;
        }
        let source = this.entity[this.Name];
        if (!source) {
            this.Matched = null;
            this._listValues = [];
            return;
        }
        this._listValues = source.toString().split(this.meta.groupFormat || ',').filter(x => x.trim().length > 0);
    }

    _listValues = [];

    get listValues() {
        return this._listValues;
    }

    set listValues(value) {
        if (!value) {
            this._listValues = [];
        } else {
            this._listValues = Array.from(new Set(value));
        }
        this.setEntityValue();
    }

    setEntityValue() {
        this.entity[this.Name] = this.listValues.length > 0 ? this.listValues.join(this.meta.groupFormat || ',') : null;
        this.entity[this.Name + "Text"] = this.matchedItems.length > 0 ? this.matchedItems.map(item => this.getMatchedText(item)).join(this.meta.groupFormat || ',') : this.entity[this.Name + "Text"];
    }

    matchedItems = [];

    findMatchText() {
        if (this.emptyRow) {
            return;
        }
        if (!this.processLocalMatch()) {
            this.setMatchedValue();
        }
    }

    processLocalMatch() {
        if (Utils.isNullOrWhiteSpace(this.meta.refName)) {
            var data = Utils.isFunction(this.meta.Query, false, this);
            this.matchedItems = data.filter(x => this.listValues.includes(x.Id.toString()));
            this.setMatchedValue();
            this.entity[this.Name + "Text"] = this.matchedItems.length > 0 ? this.matchedItems.map(item => this.getMatchedText(item)).join(this.meta.groupFormat || ',') : this.entity[this.Name + "Text"];
            return true;
        }
        else {
            this.Matched = this.entity[this.displayField] || null;
            if (this._listValues.length > 0 && this.matchedItems.filter(x => this._listValues.includes(x.Id)).length < this._listValues.length && (!this.parent.isListViewItem || this.meta.isMultiple)) {
                Client.instance.getByIdAsync(this.meta.refName, this._listValues).then(data => {
                    this.matchedItems = data.data ? data.data : [];
                    if (this.matchedItems.length != this._listValues.length) {
                        this.listValues = this.matchedItems.map(x => x[this.idField].toString());
                    }
                    this.setMatchedValue();
                    this.entity[this.Name + "Text"] = this.matchedItems.length > 0 ? this.matchedItems.map(item => this.getMatchedText(item)).join(this.meta.groupFormat || ',') : this.entity[this.Name + "Text"];
                })
                return true;
            }
        }
        return false;
    }

    setMatchedValue() {
        this._input.value = '';
        this.listValues.forEach(value => {
            let item = this.matchedItems.find(x => x[this.idField].toString() === value.toString());
            this.renderTag(item);
        });
    }

    clearTagIfNotExists() {
        Array.from(this.element.parentElement.querySelectorAll("span")).forEach(ta => {
            ta.remove();
        });
    }

    renderTag(item) {
        if (!item) {
            return;
        }
        let idAttr = item[this.idField];
        let exist = this.element.parentElement.querySelector(`span[data-id='${idAttr}']`);
        if (exist) {
            return;
        }
        Html.take(this.element.parentElement).span.attr("data-id", idAttr).i.className("fal fa-tag mr-1").end.text(this.getMatchedText(item));
        var tag = Html.context;
        this.element.parentElement.insertBefore(Html.context, this._input);
        if (this.disabled) {
            this._input.readOnly = true;
        }
        Html.instance.button.className("fa fa-times").event(EventType.Click, async () => {
            if (this.disabled) {
                return;
            }
            let oldMatch = this.matchedItems;
            this.matchedItems.splice(this.matchedItems.indexOf(item), 1);
            var id = item[this.idField];
            this.listValues = this.listValues.filter(x => x !== id.toString());
            this.setEntityValue();
            this.Dirty = true;
            if (this.userInput != null) {
                this.userInput?.invoke({ newData: this._value, oldData: oldMatch, evType: EventType.Change });
            }
            await this.dispatchEvent(this.meta.events, EventType.Change, this);
            tag.remove();
        }).end.render();
    }

    entrySelected(rowData) {
        window.clearTimeout(this._waitForDispose);
        this.emptyRow = false;
        if (rowData === null || this.disabled) {
            return;
        }

        let oldMatch = this.matchedItems;
        var id = rowData[this.idField];
        if (this.listValues.length == 0 || !this.listValues.includes(id)) {
            this.listValues.push(id.toString());
            this.matchedItems.push(rowData);
        }
        else {
            return;
        }
        this.setEntityValue();
        this.Dirty = true;
        this.findMatchText();
        this._gv.allListViewItem.forEach(item => {
            item.setChooseCell();
        });
        if (this.userInput != null) {
            this.userInput?.invoke({ newData: this._value, oldData: oldMatch, evType: EventType.Change });
        }
        this.dispatchEvent(this.meta.events, EventType.Change, this).then();
    }

    updateView(force = false, dirty = null, ...componentNames) {
        this.tryParseData();
        this.setEntityValue();
        this.clearTagIfNotExists();
        this.findMatchText();
    }
}
