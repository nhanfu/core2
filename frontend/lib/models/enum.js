import { Component } from "./component.js";

/**
 * Represents system roles.
 * @enum {number}
 */
export const roleEnum = {
    /** @type {string} System role. */
    System: "8",
};

/**
 * Enum for task states.
 * @enum {number}
 */
export const taskStateEnum = {
    /** @type {number} Unread status. */
    unreadStatus: 339,
    /** @type {number} Read status. */
    Read: 340,
    /** @type {number} Processing status. */
    Processing: 341,
    /** @type {number} Proceeded status. */
    Proceeded: 342,
};

export const positionEnum = {
    absolute: "absolute",
    fixed: "fixed", // '@' không được sử dụng trong tên biến javaScript
    inherit: "inherit",
    initial: "initial",
    relative: "relative",
    static: "static", // 'static' là từ khóa trong javaScript, cần trích dẫn nó nếu dùng như tên thuộc tính không bị trích dẫn
    sticky: "sticky",
    unset: "unset"
};

export const keyCodeEnum = {
    backspace: 8,
    tab: 9,
    enter: 13,
    shift: 16,
    ctrl: 17,
    alt: 18,
    pauseBreak: 19,
    capsLock: 20,
    escape: 27,
    pageUp: 33,
    space: 32,
    pageDown: 34,
    end: 35,
    home: 36,
    leftArrow: 37,
    upArrow: 38,
    rightArrow: 39,
    downArrow: 40,
    insert: 45,
    delete: 46,
    zero: 48,
    one: 49,
    two: 50,
    three: 51,
    four: 52,
    five: 53,
    six: 54,
    seven: 55,
    eight: 56,
    nine: 57,
    a: 65,
    b: 66,
    c: 67,
    d: 68,
    e: 69,
    f: 70,
    g: 71,
    h: 72,
    i: 73,
    j: 74,
    k: 75,
    l: 76,
    m: 77,
    n: 78,
    o: 79,
    p: 80,
    q: 81,
    r: 82,
    s: 83,
    t: 84,
    u: 85,
    v: 86,
    w: 87,
    x: 88,
    y: 89,
    z: 90,
    leftWindowKey: 91,
    rightWindowKey: 92,
    selectKey: 93,
    numpad0: 96,
    numpad1: 97,
    numpad2: 98,
    numpad3: 99,
    numpad4: 100,
    numpad5: 101,
    numpad6: 102,
    numpad7: 103,
    numpad8: 104,
    numpad9: 105,
    multiply: 106,
    add: 107,
    subtract: 109,
    decimalPoint: 110,
    Divide: 111,
    F1: 112,
    F2: 113,
    F3: 114,
    F4: 115,
    F5: 116,
    F6: 117,
    F7: 118,
    F8: 119,
    F9: 120,
    F10: 121,
    F11: 122,
    F12: 123,
    numLock: 144,
    scrollLock: 145,
    semiColon: 186,
    equalSign: 187,
    Comma: 188,
    Dash: 189,
    Period: 190,
    forwardSlash: 191,
    graveAccent: 192,
    openBracket: 219,
    backSlash: 220,
    closeBraket: 221,
    singleQuote: 222
};

export const operatorEnum = {
    In: 1,
    notIn: 2,
    Gt: 3,
    Ge: 4,
    Lt: 5,
    Le: 6,
    Lr: 7,
    Rl: 8
};

export const searchMethodEnum = {
    empty: 1,
    filled: 2,
    equal: 3,
    notEqual: 4,
    contain: 5,
    notContain: 6,
    startWith: 7,
    endWith: 8,
    smaller: 9,
    smallerEqual: 10,
    greater: 11,
    greaterEqual: 12,
    range: 13
};

/**
 * Types of UI components.
 * @enum {number}
 */
export const componentTypeTypeEnum = {
    
};

/**
 * Enum for active states.
 * @enum {number}
 */
export const activeStateEnum = {
    /** @type {number} Represents all statuses. */
    all: 2,
    /** @type {number} Active status. */
    yes: 1,
    /** @type {number} Inactive status. */
    no: 0,
};

/**
 * Enum for advanced search operations.
 * @enum {number}
 */
export const advSearchOperation = {
    /** @type {number} Equals. */
    equal: 1,
    /** @type {number} Not equal. */
    notEqual: 2,
    /** @type {number} Greater than. */
    greaterThan: 3,
    /** @type {number} Greater than or equal. */
    greaterThanOrEqual: 4,
    /** @type {number} Less than. */
    lessThan: 5,
    /** @type {number} Less than or equal. */
    lessThanOrEqual: 6,
    /** @type {number} Contains. */
    contains: 7,
    /** @type {number} Does not contain. */
    notContains: 8,
    /** @type {number} Starts with. */
    startWith: 9,
    /** @type {number} Does not start with. */
    notStartWith: 10,
    /** @type {number} Ends with. */
    endWidth: 11,
    /** @type {number} Does not end with. */
    notEndWidth: 12,
    /** @type {number} In a set. */
    In: 13,
    /** @type {number} Not in a set. */
    notIn: 14,
    /** @type {number} Equals date. */
    equalDatime: 15,
    /** @type {number} Greater than date. */
    greaterThanDatime: 21,
    /** @type {number} Less than date. */
    lessThanDatime: 22,
    /** @type {number} Not equal date. */
    notEqualDatime: 16,
    /** @type {number} Equals null. */
    equalNull: 17,
    /** @type {number} Not equal null. */
    notEqualNull: 18,
    /** @type {number} Like. */
    Like: 19,
    /** @type {number} Not like. */
    notLike: 20,
    /** @type {number} Greater than or equal date. */
    greaterEqualDatime: 23,
    /** @type {number} Less than or equal date. */
    lessEqualDatime: 24,
};

export const operationToSql = {
    [advSearchOperation.equal]: "{0} = N'{1}'",
    [advSearchOperation.notEqual]: "{0} != N'{1}'",
    [advSearchOperation.greaterThan]: "{0} > N'{1}'",
    [advSearchOperation.greaterThanOrEqual]: "{0} >= N'{1}'",
    [advSearchOperation.lessThan]: "{0} < N'{1}'",
    [advSearchOperation.lessThanOrEqual]: "{0} <= N'{1}'",
    [advSearchOperation.contains]: "charindex(N'{1}', {0}) >= 1",
    [advSearchOperation.notContains]: "contains({0}, N'{1}') eq false",
    [advSearchOperation.startWith]: "charindex(N'{1}', {0}) = 1",
    [advSearchOperation.notStartWith]: "charindex(N'{1}', {0}) > 1",
    [advSearchOperation.endWidth]: "{0} like N'%{1}')",
    [advSearchOperation.notEndWidth]: "{0} not like N'%{1}'",
    [advSearchOperation.In]: "{0} in ({1})",
    [advSearchOperation.Like]: "{0} like N'%{1}%'",
    [advSearchOperation.notLike]: "{0} not like N'{1}'",
    [advSearchOperation.notIn]: "{0} not in ({1})",
    [advSearchOperation.equalDatime]: "cast(date, {0}) = N'{1}'",
    [advSearchOperation.notEqualDatime]: "cast(date, {0}) != N'{1}'",
    [advSearchOperation.equalNull]: "{0} is null",
    [advSearchOperation.notEqualNull]: "{0} is not null",
    [advSearchOperation.greaterThanDatime]: "cast(date, {0}) > N'{1}'",
    [advSearchOperation.greaterEqualDatime]: "cast(date, {0}) >= N'{1}'",
    [advSearchOperation.lessThanDatime]: "cast(date, {0}) < N'{1}'",
    [advSearchOperation.lessEqualDatime]: "cast(date, {0}) <= N'{1}'",
};

/**
 * Logical operations for combining conditions.
 * @enum {number}
 */
export const logicOperation = {
    /** @type {number} Logical AND. */
    And: 0,
    /** @type {number} Logical OR. */
    Or: 1,
};

/**
 * Directions for sorting.
 * @enum {number}
 */
export const orderbyDirection = {
    /** @type {number} Ascending order. */
    ASC: 1,
    /** @type {number} Descending order. */
    DESC: 2,
};

/**
 * Role selection options.
 * @enum {number}
 */
export const roleSelection = {
    /** @type {number} Top first selection. */
    topFirst: 1,
    /** @type {number} Bottom first selection. */
    bottomFirst: 2,
};

/**
 * Represents advanced search configurations.
 */
export class AdvSearchVM {
    /**
     * Constructs an instance of AdvSearchVM.
     */
    constructor() {
        this.activeState = null;
        /** @type {FieldCondition[]} */
        this.conditions = [];
        this.advSearchConditions = [];
        /** @type {OrderBy[]} */
        this.orderBy = [];
    }
}

/**
 * Represents a selected cell in the UI.
 */
export class CellSelected {
    /**
     * Constructs an instance of CellSelected.
     */
    constructor() {
        this.fieldName = '';
        this.fieldText = '';
        this.componentType = '';
        this.value = '';
        this.valueText = '';
        this.operator = null;
        this.operatorText = '';
        this.logic = null;
        this.isSearch = false;
        this.group = false;
        this.shift = false;
    }
}

/**
 * Represents a conditional expression in a query.
 */
export class Where {
    /**
     * Constructs an instance of Where.
     */
    constructor() {
        this.condition = '';
        this.group = false;
    }
}

/**
 * Represents a condition in a field used for advanced searches.
 */
export class FieldCondition {
    /**
     * Constructs an instance of FieldCondition.
     */
    constructor() {
        this.id = '';
        this.originFieldName = '';
        this.fieldId = '';
        /** @type {Component} */
        this.field = null;
        /** @type {advSearchOperation} */
        this.compareOperatorId = null;
        this.value = '';
        this.display = {};
        /** @type {logicOperation} */
        this.logicOperatorId = null;
        this.logicOperator = null;
        this.level = '';
        this.group = false;
    }
}

/**
 * Represents the ordering of a field in a query.
 */
export class OrderBy {
    /** @type {String | null | undefined} */
    id = '';
    comId = '';
    fieldName = '';
    /** @type {orderbyDirection | null | undefined} */
    orderbyDirectionId = null;
}

/**
 * Represents an event message in a queue.
 */
export class MQEvent {
    /**
     * Constructs an instance of MQEvent.
     */
    constructor() {
        this.deviceKey = '';
        this.queueName = '';
        this.action = '';
        this.id = '';
        this.prevId = '';
        this.time = null; // javaScript does not have a direct equivalent to dateTimeOffset, using Date instead
        this.message = null;
    }
}

/**
 * @class Entity
 * @property {string} name
 * @property {string} description
 * @property {boolean} active
 */
export class Entity {
    /** @type {number} id */
    id;
    /** @type {string} */
    name;
    /** @type {any} */
    value;
    /** @type {any} */
    display;
    /** @type {boolean} */
    active;
}

/**
 * HTTP methods used in web requests.
 * @enum {string}
 */
export const httpMethod = {
    GET: 'GET',
    POST: 'POST',
    PUT: 'PUT',
    PATCH: 'PATCH',
    DELETE: 'DELETE'
};

/**
 * HTTP status codes as per the HTTP specification.
 * @enum {number}
 */
export const httpStatusCode = {
    // Informational 1xx
    Continue: 100,
    switchingProtocols: 101,

    // Successful 2xx
    OK: 200,
    Created: 201,
    Accepted: 202,
    nonAuthoritativeInformation: 203,
    noContent: 204,
    resetContent: 205,
    partialContent: 206,

    // Redirection 3xx
    multipleChoices: 300,
    Ambiguous: 300,
    movedPermanently: 301,
    Moved: 301,
    Found: 302,
    Redirect: 302,
    seeOther: 303,
    redirectmethod: 303,
    notModified: 304,
    useProxy: 305,
    Unused: 306,
    temporaryRedirect: 307,
    redirectKeepVerb: 307,

    // Client Error 4xx
    badRequest: 400,
    Unauthorized: 401,
    paymentRequired: 402,
    Forbidden: 403,
    notFound: 404,
    methodNotAllowed: 405,
    notAcceptable: 406,
    proxyAuthenticationRequired: 407,
    requestTimeout: 408,
    Conflict: 409,
    Gone: 410,
    lengthRequired: 411,
    preconditionFailed: 412,
    requestEntityTooLarge: 413,
    requestUriTooLong: 414,
    unsupportedMediaType: 415,
    requestedRangeNotSatisfiable: 416,
    expectationFailed: 417,
    upgradeRequired: 426,

    // Server Error 5xx
    internalServerError: 500,
    notImplemented: 501,
    badGateway: 502,
    serviceUnavailable: 503,
    gatewayTimeout: 504,
    httpVersionNotSupported: 505
};
