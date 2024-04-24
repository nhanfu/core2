import { Utils } from "./utils.js";

export function HasNonSpaceChar() { return this.trim() !== ''; }

export class string {
    static Empty = '';
    static Type = 'string';
    static Format(template, ...args) {
        return template.replace(/{(\d+)}/g, (match, index) => {
            return typeof args[index] != 'undefined' ? args[index] : match;
        });
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
    return path.split(".").reduce((obj, key) => obj && obj[key], this);
};
Object.prototype.SetComplexPropValue = function (path, value) {
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
Promise.prototype.Done = Promise.prototype.then;
Promise.prototype.done = Promise.prototype.then;