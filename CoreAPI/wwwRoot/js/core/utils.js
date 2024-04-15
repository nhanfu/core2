export const IsFunction = (exp, obj, shouldAddReturn) => {
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
};

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

export const isNoU = (o) => o === null || o === undefined;
export const hasNonSpaceChar = (o) => o !== null && o !== undefined && o.trim() !== '';
