import { KeyCodeEnum } from "../models/keycode.js";
import { Utils } from "./utils.js";
import { OutOfViewPort } from "./outOfViewPort.js";

export function HasNonSpaceChar() { return this.trim() !== ''; }

export class string {
    static Empty = '';
    static Comma = ',';
    static Type = 'string';
    static Format(template, ...args) {
        return template.replace(/{(\d+)}/g, (match, index) => {
            return typeof args[index] != 'undefined' ? args[index] : match;
        });
    }
    static Join(separator, ...str) {
        str.join(separator)
    }
}

/**
 * @returns true if array contains at least one element
 */
function HasElement() {
    return this != null && this.length > 0;
}

Array.prototype.Nothing = function () {
    return this.length === 0;
};

Array.prototype.Flattern = function (getChildren) {
    if (this.Nothing()) return this;
    var firstLevel = this.Select(x => getChildren(x)).Where(x => x != null).SelectMany(x => x);
    if (firstLevel.Nothing()) {
        return this;
    }
    return this.concat(firstLevel.Flattern(getChildren));
};
Array.prototype.Any = function (predicate) {
    if (!predicate) {
        return this.length > 0;
    }
    for (let i = 0; i < this.length; i++) {
        if (predicate(this[i])) {
            return true;
        }
    }
};
Array.prototype.Where = Array.prototype.filter;
Array.prototype.SelectMany = Array.prototype.flatMap;
Array.prototype.SelectForEach = Array.prototype.map;
Array.prototype.Select = Array.prototype.map;
Array.prototype.HasElement = HasElement;
Array.prototype.ToArray = function () { return this; }
Array.prototype.Contains = function (item) {
    return this.indexOf(item) !== -1;
};
Array.prototype.Remove = function (item) {
    var index = this.indexOf(item);
    if (index !== -1) {
        this.splice(index, 1);
    }
};
Array.prototype.ToDictionary = function (keySelector, valueSelector) {
    if (valueSelector == null) valueSelector = x => x;
    return this.reduce((acc, curr) => {
        acc[keySelector(curr)] = valueSelector(curr);
        return acc;
    }, {});
};
Array.prototype.FirstOrDefault = function (predicate) {
    if (!predicate) return this.length > 0 ? this[0] : null;
    for (let i = 0; i < this.length; i++) {
        if (predicate(this[i])) return this[i];
    }
}
Array.prototype.GroupBy = function (keyFunction) {
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
        items.Key = map[key].keyObj;
        return items;
    });
};
Array.prototype.ForEach = Array.prototype.forEach;
Array.prototype.DistinctBy = function(keySelector) {
    return this.GroupBy(keySelector).FirstOrDefault();
};
Array.prototype.ForEachAsync = async function (map2Promise){
    var promises = this.map(map2Promise);
    await Promise.all(promises);
    return this;
};
Array.prototype.Combine = function (mapper, separator) {
    return this.map(mapper).join(separator);
};

String.prototype.ToString = String.prototype.toString;
String.prototype.HasElement = HasElement;
String.prototype.HasAnyChar = HasElement;
String.prototype.HasNonSpaceChar = HasNonSpaceChar;
String.prototype.IsNullOrWhiteSpace = function () {
    return this.trim() === '';
};
String.prototype.DecodeSpecialChar = function () {
    return Utils.DecodeSpecialChar(this);
};
Object.prototype.GetComplexProp = function (path) {
    if (path == null) return null;
    return path.split(".").reduce((obj, key) => obj && obj[key], this);
};
Object.prototype.SetComplexPropValue = function (path, value) {
    if (path == null) return;
    const keys = path.split('.');
    let obj = this;
    for (let i = 0; i < keys.length - 1; i++) {
        const key = keys[i];
        if (!obj[key]) {
            obj[key] = {};
        }
        obj = obj[key];
    }
    obj[keys[keys.length - 1]] = value;
};
Object.prototype.SetPropValue = Object.prototype.SetComplexPropValue;
Object.prototype.Nothing = function () {
    return Object.keys(this).length === 0;
};
Object.prototype.CopyPropFrom = function (source) {
    if (source && typeof source === 'object') {
        for (let key in source) {
            if (source.hasOwnProperty(key)) {
                this[key] = source[key];
            }
        }
    }
};
Object.prototype.Clear = function () {
    if (this.count > 0) {
        for (let i = 0; i < this.buckets.length; i++) {
            this.buckets[i] = -1;
        }

        if (this.isSimpleKey) {
            this.simpleBuckets = {};
        }

        this.entries.fill(null, 0, this.count);
        this.freeList = -1;
        this.count = 0;
        this.freeCount = 0;
        this.version++;
    }
};
Promise.prototype.Done = Promise.prototype.then;
Promise.prototype.done = Promise.prototype.then;
Date.prototype.addSeconds = function (seconds) {
    var date = new Date(this.valueOf());
    date.setSeconds(date.getSeconds() + seconds);
    return date;
};
Date.prototype.addMinutes = function (minutes) {
    var date = new Date(this.valueOf());
    date.setMinutes(date.getMinutes() + minutes);
    return date;
};
Date.prototype.addHours = function (hours) {
    var date = new Date(this.valueOf());
    date.setHours(date.getHours() + hours);
    return date;
};
Date.prototype.addDays = function (days) {
    var date = new Date(this.valueOf());
    date.setDate(date.getDate() + days);
    return date;
};
Date.prototype.addMonths = function (months) {
    var date = new Date(this.valueOf());
    date.setMonth(date.getMonth() + months);
    return date;
};
Date.prototype.addYears = function (years) {
    var date = new Date(this.valueOf());
    date.setFullYear(date.getFullYear() + years);
    return date;
};
HTMLElement.prototype.HasClass = function(str) {
    return this.classList.contains(str);
};
HTMLElement.prototype.ReplaceClass = function(cls, byCls) {
    this.classList.remove(cls);
    this.classList.add(byCls);
};
Number.prototype.leadingDigit = function() {
    return this < 10 ? '0' + this : '' + this;
}
/**
 * Extends the Event prototype with custom methods for handling event properties and behaviors.
 */

/**
 * Gets the top position (Y-coordinate) of the event.
 * @returns {number} The Y-coordinate.
 */
Event.prototype.Top = function() {
    return parseFloat(this.clientY);
};

/**
 * Gets the left position (X-coordinate) of the event.
 * @returns {number} The X-coordinate.
 */
Event.prototype.Left = function() {
    return parseFloat(this.clientX);
};

/**
 * Gets the keyCode from the event.
 * @returns {number} The keyCode or -1 if undefined.
 */
Event.prototype.KeyCode = function() {
    return this.keyCode ?? -1;
};

/**
 * Attempts to parse keyCode to an enum value.
 * @returns {KeyCodeEnum|null} Parsed KeyCodeEnum or null if unable to parse.
 */
Event.prototype.KeyCodeEnum = function() {
    if (this.keyCode == null) {
        return null;
    }
    const keyCodeStr = this.keyCode.toString();
    // Assuming KeyCodeEnum is defined globally
    const res = KeyCodeEnum[keyCodeStr];
    return res ? res : null;
};

/**
 * Checks if the Shift key was pressed during the event.
 * @returns {boolean} True if Shift key was pressed.
 */
Event.prototype.ShiftKey = function() {
    return this.shiftKey;
};

/**
 * Detects if the user pressed Ctrl or Command key while the event occurs.
 * @returns {boolean} True if Ctrl or Meta key was pressed.
 */
Event.prototype.CtrlOrMetaKey = function() {
    return this.ctrlKey || this.metaKey;
};

/**
 * Checks if the Alt key was pressed during the event.
 * @returns {boolean} True if Alt key was pressed.
 */
Event.prototype.AltKey = function() {
    return this.altKey;
};

/**
 * Gets the checked status from the target element of the event, assuming the target is an input element.
 * @returns {boolean} Checked status.
 */
Event.prototype.GetChecked = function() {
    if (this.target && this.target.type === "checkbox") {
        return this.target.checked;
    }
    return false;
};

/**
 * Gets the input text from the target element of the event, assuming the target is an input element.
 * @returns {string} Input text value.
 */
Event.prototype.GetInputText = function() {
    if (this.target && typeof this.target.value === "string") {
        return this.target.value;
    }
    return "";
};

/**
 * Calculates the full height of an element, including margins.
 * @returns {number} The total height in pixels.
 */
HTMLElement.prototype.GetFullHeight = function() {
    if (!this) {
        return 0;
    }
    const style = window.getComputedStyle(this);
    const marginTop = parseFloat(style.marginTop) || 0;
    const marginBottom = parseFloat(style.marginBottom) || 0;
    return this.scrollHeight + marginTop + marginBottom;
};

/**
 * Removes a class from the element.
 * @param {string} className - The class name to remove.
 */
Node.prototype.RemoveClass = function(className) {
    if (!this || !className) {
        return;
    }
    this.classList.remove(className);
};

/**
 * Toggles a class on the element based on its presence.
 * @param {string} className - The class to toggle.
 */
Node.prototype.ToggleClass = function(className) {
    if (!this || !className) {
        return;
    }
    this.classList.toggle(className);
};

/**
 * Sets the display style to empty, effectively showing the element.
 */
HTMLElement.prototype.Show = function() {
    if (!this) {
        return;
    }
    this.style.display = '';
};

/**
 * Gets the computed style of the element.
 * @returns {CSSStyleDeclaration} The computed style of the element.
 */
HTMLElement.prototype.GetComputedStyle = function() {
    return window.getComputedStyle(this);
};

/**
 * Sets the display style to 'none', hiding the element.
 */
HTMLElement.prototype.Hide = function() {
    if (!this) {
        return;
    }
    this.style.display = 'none';
};

/**
 * Checks if the element is hidden.
 * @returns {boolean} True if the element is hidden; otherwise, false.
 */
HTMLElement.prototype.Hidden = function() {
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
HTMLElement.prototype.OutOfViewport = function() {
    const bounding = this.getBoundingClientRect();
    const outOfViewPort = new OutOfViewPort();
    outOfViewPort.Top = bounding.top < 0;
    outOfViewPort.Left = bounding.left < 0;
    outOfViewPort.Bottom = bounding.bottom > window.innerHeight;
    outOfViewPort.Right = bounding.right > window.innerWidth;
    outOfViewPort.Any = outOfViewPort.Top || outOfViewPort.Left || outOfViewPort.Bottom || outOfViewPort.Right;
    outOfViewPort.All = outOfViewPort.Top && outOfViewPort.Left && outOfViewPort.Bottom && outOfViewPort.Right;
    return outOfViewPort;
};

/**
 * 
 * @param {HTMLElement} ele 
 * @param {Set<HTMLElement>} visited 
 * @param {(ele: HTMLElement) => boolean} predicate
 * @returns 
 */
const search = (ele, visited, predicate, results) => {
    if (!ele || visited.has(ele)) {
        return;
    }
    visited.add(ele);
    if (predicate(ele)) {
        results.push(ele);
    }
    if (ele.children.length == 0) return;
    ele.children.forEach(child => search(child));
};
/**
 * Filters child elements based on a predicate.
 * @param {function(HTMLElement): boolean} predicate - A function to test each element.
 * @returns {HTMLElement[]} An array of HTMLElements that match the predicate.
 */
HTMLElement.prototype.FilterElement = function(predicate) {
    const results = [];
    const visited = new Set();
    search(this, visited, predicate, results);
    return results;
};