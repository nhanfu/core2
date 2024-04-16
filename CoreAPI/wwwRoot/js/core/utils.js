export function GetPropValue(obj, path) {
    if (obj == null || path == null) return null;
    for (var i = 0, path = path.split('.'), len = path.length; i < len; i++) {
        obj = obj[path[i]];
    };
    return obj;
}

export function SetPropValue(obj, path, value) {
    if (obj == null || path == null) return;
    var a = path.split('.')
    var o = obj
    while (a.length - 1) {
        var n = a.shift()
        if (!(n in o)) o[n] = {}
        o = o[n]
    }
    o[a[0]] = value
}

export const Nothing = (arr) => arr == null || arr.length == 0;
export const isNoU = (o) => o === null || o === undefined;
export const HasNonSpaceChar = (o) => o !== null && o !== undefined && o.trim() !== '';
export const IsNullOrWhiteSpace = (o) => o === null || o === undefined || o.trim() === '';


export class Utils {
    static IsFunction(exp, obj, shouldAddReturn) {
        if (exp == null) {
            obj.v = null;
            return false;
        }
        try {
            var fn = new Function(shouldAddReturn ? "return " + exp : exp);
            const fnVal = fn.call(null);
            if (fnVal instanceof Function) {
                obj.v = fnVal;
                return true;
            } else {
                return false;
            }
        } catch ($e1) {
            obj.v = null;
            return false;
        }
    }
}

/**
 * @returns true if array contains at least one element
 */
export function HasElement() {
    return this != null && this.length > 0;
}

Array.prototype.Nothing = function () {
    return this.length === 0;
};

Array.prototype.Flattern = function (getChildren) {
    if (this.Nothing()) return this;
    var firstLevel = this.Select(x => getChildren(x)).Where(x => x != null).SelectMany(x => x);
    if (firstLevel.Nothing())
    {
        return this;
    }
    return this.concat(firstLevel.Flattern(getChildren));
};
Array.prototype.Where = Array.prototype.filter;
Array.prototype.SelectMany = Array.prototype.flatMap;
Array.prototype.Select = Array.prototype.map;
Array.prototype.HasElement = HasElement;
Array.prototype.ToArray = function() { return this; }
Array.prototype.Contains = function(item) {
    return this.indexOf(item) !== -1;
};
Array.prototype.Remove = function(item) {
    var index = this.indexOf(item);
    if (index !== -1) {
        this.splice(index, 1);
    }
};

String.prototype.HasElement = HasElement;
String.prototype.HasNonSpaceChar = HasNonSpaceChar;
String.prototype.IsNullOrWhiteSpace = IsNullOrWhiteSpace;