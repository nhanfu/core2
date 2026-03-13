import { keyCodeEnum, Entity } from "../models/enum.js";
import { Utils } from "./utils.js";
import { OutOfViewPort } from "./outOfViewPort.js";

export function hasNonSpaceChar() { return this.trim() !== ''; }

export class Str {
    static Empty = '';
    static Comma = ',';
    static Type = 'string';
    /**
     * @param {string} template
     * @param {(string | any[])[]} args
     */
    static format(template, ...args) {
        return template.replace(/{(\d+)}/g, (/** @type {any} */ match, /** @type {string | number} */ index) => {
            return typeof args[index] != 'undefined' ? args[index] : match;
        });
    }
    /**
     * @param {string} separator
     * @param {any[]} str
     */
    static Join(separator, ...str) {
        str.join(separator)
    }
}

/**
 * @returns true if array contains at least one element
 */
function hasElement() {
    return this != null && this.length > 0;
}

Array.prototype.nothing = function () {
    return this.length === 0;
};

/**
 * @template T, K
 * @param {(value: T) => K[]} getChildren
 * @returns {K[]}
 */
Array.prototype.flattern = function (getChildren) {
    if (this.nothing()) return this;
    var firstLevel = this.select(x => getChildren(x)).where(x => x != null).selectMany((/** @type {any} */ x) => x);
    if (firstLevel.nothing()) {
        return this;
    }
    return this.concat(firstLevel.Flattern(getChildren));
};
Array.prototype.any = function (/** @type {(arg0: any) => any} */ predicate) {
    if (!predicate) {
        return this.length > 0;
    }
    for (let i = 0; i < this.length; i++) {
        if (predicate(this[i])) {
            return true;
        }
    }
};
Array.prototype.where = Array.prototype.filter;
Array.prototype.selectMany = Array.prototype.flatMap;
Array.prototype.selectForEach = Array.prototype.map;
Array.prototype.select = Array.prototype.map;
Array.prototype.hasElement = hasElement;
Array.prototype.toArray = function () { return this; }
Array.prototype.contains = function (/** @type {any} */ item) {
    return this.indexOf(item) !== -1;
};
Array.prototype.remove = function (/** @type {any} */ item) {
    var index = this.indexOf(item);
    if (index !== -1) {
        this.splice(index, 1);
    }
};
Array.prototype.toDictionary = function (/** @type {(arg0: any) => string | number} */ keySelector, /** @type {(x: any) => any} */ valueSelector) {
    if (valueSelector == null) valueSelector = (/** @type {any} */ x) => x;
    return this.reduce((acc, curr) => {
        acc[keySelector(curr)] = valueSelector(curr);
        return acc;
    }, {});
};
Array.prototype.firstOrDefault = function (predicate = null) {
    if (!predicate) return this.length > 0 ? this[0] : null;
    for (let i = 0; i < this.length; i++) {
        if (predicate(this[i])) return this[i];
    }
}
Array.prototype.groupBy = function (/** @type {(arg0: any) => any} */ keyFunction) {
    const map = this.reduce((accumulator, item) => {
        const keyObj = keyFunction(item);
        const key = JSON.stringify(keyObj);

        if (!accumulator[key]) {
            accumulator[key] = [];
            accumulator[key].keyObj = keyObj;
        }
        accumulator[key].push(item);
        return accumulator;
    }, {});
    return Object.keys(map).map(key => {
        const items = map[key];
        items.key = map[key].keyObj;
        return items;
    });
};
Array.prototype.forEach = Array.prototype.forEach;
/**
 * @template T, K
 * @param {(item: T) => K} keySelector 
 * @returns 
 */
Array.prototype.distinctBy = function (/** @type {(item: T) => K} */ keySelector) {
    return this.groupBy(keySelector).firstOrDefault();
};
/**
 * @template T, K
 * @returns 
 */
Array.prototype.distinct = function () {
    return this.groupBy(x => x).firstOrDefault();
};
Array.prototype.forEachAsync = async function (/** @type {(value: any, index: number, array: any[]) => any} */ map2Promise) {
    var promises = this.map(map2Promise);
    await Promise.all(promises);
    return this;
};
Array.prototype.Clear = function () {
    while (this.length) this.pop();
};
Array.prototype.addRange = Array.prototype.push;
Array.prototype.Combine = function (/** @type {(value: any, index: number, array: any[]) => any} */ mapper = null, /** @type {string} */ separator = ',') {
    if (mapper) {
        return this.map(mapper).join(separator);
    } else {
        return this.join(separator);
    }
};
/**
 * @template T
 * @param {(item: T) => any} keySelector 
 * @param {(item: T) => any} keySelector2 
 * @param {boolean} asc1 
 * @param {boolean} asc2 
 * @returns {T[]}
 */
Array.prototype.orderBy = function (keySelector, keySelector2, asc1 = true, asc2 = true) {
    return this.slice().sort((a, b) => {
        const ra = keySelector(a);
        const rb = keySelector(b);
        if (ra != rb) return ra > rb ? (asc1 ? 1 : -1) : (asc1 ? -1 : 1);
        const ra2 = keySelector2(a);
        const rb2 = keySelector2(b);
        return ra2 > rb2 ? (asc2 ? 1 : -1) : (asc2 ? -1 : 1);
    });
};
Array.prototype.lastOrDefault = function (predicate = null) {
    if (predicate) return this.findLast(predicate);
    return this.length > 0 ? this[this.length - 1] : null;
};
Date.prototype.addSeconds = function (/** @type {number} */ seconds) {
    var date = new Date(this.valueOf());
    date.setSeconds(date.getSeconds() + seconds);
    return date;
};
Date.prototype.addMinutes = function (/** @type {number} */ minutes) {
    var date = new Date(this.valueOf());
    date.setMinutes(date.getMinutes() + minutes);
    return date;
};
Date.prototype.addHours = function (/** @type {number} */ hours) {
    var date = new Date(this.valueOf());
    date.setHours(date.getHours() + hours);
    return date;
};
Date.prototype.addDays = function (/** @type {number} */ days) {
    var date = new Date(this.valueOf());
    date.setDate(date.getDate() + days);
    return date;
};
Date.prototype.addMonths = function (/** @type {number} */ months) {
    var date = new Date(this.valueOf());
    date.setMonth(date.getMonth() + months);
    return date;
};
Date.prototype.addYears = function (/** @type {number} */ years) {
    var date = new Date(this.valueOf());
    date.setFullYear(date.getFullYear() + years);
    return date;
};
HTMLElement.prototype.hasClass = function (/** @type {string} */ str) {
    return this.classList.contains(str);
};
HTMLElement.prototype.replaceClass = function (/** @type {string} */ cls, /** @type {string} */ byCls) {
    this.classList.remove(cls);
    this.classList.add(byCls);
};
Number.prototype.leadingDigit = function () {
    // @ts-ignore
    return this < 10 ? '0' + this : '' + this;
}
/**
 * Extends the Event prototype with custom methods for handling event properties and behaviors.
 */

/**
 * Gets the top position (Y-coordinate) of the event.
 * @returns {number} The Y-coordinate.
 */
Event.prototype.Top = function () {
    // @ts-ignore
    return this.clientY;
};

/**
 * Gets the left position (X-coordinate) of the event.
 * @returns {number} The X-coordinate.
 */
Event.prototype.Left = function () {
    // @ts-ignore
    return parseFloat(this.clientX);
};

/**
 * Gets the keyCode from the event.
 * @returns {number} The keyCode or -1 if undefined.
 */
Event.prototype.keyCode = function () {
    // @ts-ignore
    return this.keyCode ?? -1;
};

/**
 * Attempts to parse keyCode to an enum value.
 * @returns {keyCodeEnum|null} Parsed keyCodeEnum or null if unable to parse.
 */
Event.prototype.keyCodeEnum = function () {
    // @ts-ignore
    return this.keyCode ?? -1;
};

/**
 * Checks if the Shift key was pressed during the event.
 * @returns {boolean} True if Shift key was pressed.
 */
Event.prototype.shiftKey = function () {
    // @ts-ignore
    return this.shiftKey;
};

/**
 * Detects if the user pressed Ctrl or Command key while the event occurs.
 * @returns {boolean} True if Ctrl or Meta key was pressed.
 */
Event.prototype.ctrlOrMetaKey = function () {
    // @ts-ignore
    return this.ctrlKey || this.metaKey;
};

/**
 * Checks if the Alt key was pressed during the event.
 * @returns {boolean} True if Alt key was pressed.
 */
Event.prototype.altKey = function () {
    // @ts-ignore
    return this.altKey;
};

/**
 * Gets the checked status from the target element of the event, assuming the target is an input element.
 * @returns {boolean} Checked status.
 */
Event.prototype.getChecked = function () {
    // @ts-ignore
    if (this.target && this.target.type === "checkbox") {
        // @ts-ignore
        return this.target.checked;
    }
    return false;
};

/**
 * Gets the input text from the target element of the event, assuming the target is an input element.
 * @returns {string} Input text value.
 */
Event.prototype.getInputText = function () {
    // @ts-ignore
    if (this.target && typeof this.target.value === "string") {
        // @ts-ignore
        return this.target.value;
    }
    return "";
};

/**
 * Calculates the full height of an element, including margins.
 * @returns {number} The total height in pixels.
 */
HTMLElement.prototype.getFullHeight = function () {
    if (!this) {
        return 0;
    }
    const style = window.getComputedStyle(this);
    const marginTop = parseFloat(style.marginTop) || 0;
    const marginBottom = parseFloat(style.marginBottom) || 0;
    return this.scrollHeight + marginTop + marginBottom;
};

/**
 * Add a class to the element.
 * @param {string} className - The class name to add.
 */
HTMLElement.prototype.addClass = function (className) {
    if (!this || !className) {
        return;
    }
    this.classList.add(className);
};

/**
 * Removes a class from the element.
 * @param {string} className - The class name to remove.
 */
HTMLElement.prototype.removeClass = function (className) {
    if (!this || !className) {
        return;
    }
    this.classList.remove(className);
};

/**
 * Toggles a class on the element based on its presence.
 * @param {string} className - The class to toggle.
 */
HTMLElement.prototype.toggleClass = function (className) {
    if (!this || !className) {
        return;
    }
    this.classList.toggle(className);
};

/**
 * Sets the display style to empty, effectively showing the element.
 */
HTMLElement.prototype.Show = function () {
    if (!this) {
        return;
    }
    this.style.display = '';
};

/**
 * Gets the computed style of the element.
 * @returns {cSSStyleDeclaration} The computed style of the element.
 */
HTMLElement.prototype.getComputedStyle = function () {
    return window.getComputedStyle(this);
};

/**
 * Sets the display style to 'none', hiding the element.
 */
HTMLElement.prototype.Hide = function () {
    if (!this) {
        return;
    }
    this.style.display = 'none';
};

/**
 * Checks if the element is hidden.
 * @returns {boolean} True if the element is hidden; otherwise, false.
 */
HTMLElement.prototype.Hidden = function () {
    if (!this) {
        return true;
    }
    const rect = this.getBoundingClientRect();
    const style = window.getComputedStyle(this);
    return style.display === "none" || (rect.bottom === 0 && rect.top === 0 && rect.width === 0 && rect.height === 0);
};

/**
 * Determines if the element is outside the viewport.
 * @returns {OutOfViewPort} An object indicating which sides are out of the viewport.
 */
HTMLElement.prototype.outOfViewport = function () {
    const bounding = this.getBoundingClientRect();
    const outOfViewPort = new OutOfViewPort();
    outOfViewPort.top = bounding.top < 0;
    outOfViewPort.left = bounding.left < 0;
    outOfViewPort.bottom = bounding.bottom > window.innerHeight;
    outOfViewPort.right = bounding.right > window.innerWidth;
    outOfViewPort.any = outOfViewPort.top || outOfViewPort.left || outOfViewPort.bottom || outOfViewPort.right;
    outOfViewPort.all = outOfViewPort.top && outOfViewPort.left && outOfViewPort.bottom && outOfViewPort.right;
    return outOfViewPort;
};

/** 
* @param {(value: Element, index: number, array: Element[]) => void} callback 
*/
HTMLCollection.prototype.forEach = function (callback) {
    Array.from(this).forEach(callback);
};
