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
Object.prototype.Nothing = function () {
    return Object.keys(this).length === 0;
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
Date.prototype.addSeconds = function(seconds) {
    var date = new Date(this.valueOf());
    date.setSeconds(date.getSeconds() + seconds);
    return date;
};
Date.prototype.addMinutes = function(minutes) {
    var date = new Date(this.valueOf());
    date.setMinutes(date.getMinutes() + minutes);
    return date;
};
Date.prototype.addHours = function(hours) {
    var date = new Date(this.valueOf());
    date.setHours(date.getHours() + hours);
    return date;
};
Date.prototype.addDays = function(days) {
    var date = new Date(this.valueOf());
    date.setDate(date.getDate() + days);
    return date;
};
Date.prototype.addMonths = function(months) {
    var date = new Date(this.valueOf());
    date.setMonth(date.getMonth() + months);
    return date;
};
Date.prototype.addYears = function(years) {
    var date = new Date(this.valueOf());
    date.setFullYear(date.getFullYear() + years);
    return date;
};