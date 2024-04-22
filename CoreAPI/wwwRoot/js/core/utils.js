var SpecialChar = {
    '+': "%2B",
    '/': "%2F",
    '?': "%3F",
    '#': "%23",
    '&': "%26"
};

export function GetPropValue(obj, path) {
    if (obj == null || path == null) return null;
    for (var i = 0, path = path.split('.'), len = path.length; i < len && obj != null; i++) {
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

export function SetComplexPropValue(obj, path, value) {
    if (obj == null || path == null) return;

    const hierarchy = path.Split('.');
    if (hierarchy.Length == 0) return;
  
    if (hierarchy.Length == 1)
    {
        SetPropValue(obj, path, value);
        return;
    }
    var leaf = obj;
    for (const i = 0; i < hierarchy.Length - 1; i++)
    {
        if (leaf == null)
        {
            return;
        }

        var key = hierarchy[i];
        leaf = GetPropValue(leaf, key);
    }
    if (leaf == null)
    {
        return;
    }

    SetPropValue(leaf, hierarchy[hierarchy.Length - 1].ToString(), value);
}

export function ReverseSpecialChar() {
    return Object.entries(SpecialChar)
        .reduce((obj, [key, value], index) => {
            obj[value] = index;
            return obj;
        }, {});
}

export function DecodeSpecialChar( str ) {
    if (str == null) return;
    const arr = str.split('');
    var res = '';
    for (const i = 0; i < arr.Length; i++)
    {
        if (arr[i] == '%' && i + 3 <= arr.Length && ReverseSpecialChar().hasOwnProperty(str.slice(i, 3)))
        {
            res += (ReverseSpecialChar()[str.slice(i, 3)]);
            i += 2;
        }
        else
        {
            res += (arr[i]);
        }
    }
    return res;
}

export function EncodeSpecialChar( str ) {
    if (str == null) return;
    const arr = str.split('');
    var res = '';   
    for (const i = 0; i < arr.Length; i++)
    {
        if (SpecialChar.hasOwnProperty(arr[i]))
        {
            res += (SpecialChar[arr[i]]);
        }
        else
        {
            res += (arr[i]);
        }
    }
    return res;
}

export const isNoU = (o) => o === null || o === undefined;
export function HasNonSpaceChar() { return this.trim() !== ''; }

export class Utils {
    /**
     * 
     * @param {String | Function} exp - The expression represent function, or a function
     * @param {any} obj - output object
     * @param {boolean} shouldAddReturn - if true then append 'return ' before evaluating
     * @returns true if the exp is a function, false otherwise
     */
    static IsFunction(exp, obj, shouldAddReturn) {
        if (exp == null) {
            obj.v = null;
            return false;
        }
        if (exp instanceof Function) {
            obj.v = exp;
            return true;
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

export const string = {
    Empty: ''
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

String.prototype.HasElement = HasElement;
String.prototype.HasAnyChar = HasElement;
String.prototype.HasNonSpaceChar = HasNonSpaceChar;
String.prototype.IsNullOrWhiteSpace = function () {
    return this.trim() === '';
};