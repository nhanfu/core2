import { Component } from "./component";

/**
 * Represents system roles.
 * @enum {number}
 */
export const RoleEnum = {
    /** @type {number} System role. */
    System: 8,
};

/**
 * Enum for task states.
 * @enum {number}
 */
export const TaskStateEnum = {
    /** @type {number} Unread status. */
    UnreadStatus: 339,
    /** @type {number} Read status. */
    Read: 340,
    /** @type {number} Processing status. */
    Processing: 341,
    /** @type {number} Proceeded status. */
    Proceeded: 342,
};

/**
 * Types of UI components.
 * @enum {number}
 */
export const ComponentTypeTypeEnum = {
    /** @type {number} Dropdown component. */
    Dropdown: 1,
    /** @type {number} Search entry component. */
    SearchEntry: 1,
    /** @type {number} Multiple search entry component. */
    MultipleSearchEntry: 1,
    /** @type {number} Datepicker component. */
    Datepicker: 2,
    /** @type {number} Number input component. */
    Number: 3,
    /** @type {number} Textbox component. */
    Textbox: 4,
    /** @type {number} Checkbox component. */
    Checkbox: 5,
};

/**
 * Enum for active states.
 * @enum {number}
 */
export const ActiveStateEnum = {
    /** @type {number} Represents all statuses. */
    All: 2,
    /** @type {number} Active status. */
    Yes: 1,
    /** @type {number} Inactive status. */
    No: 0,
};

/**
 * Enum for advanced search operations.
 * @enum {number}
 */
export const AdvSearchOperation = {
    /** @type {number} Equals. */
    Equal: 1,
    /** @type {number} Not equal. */
    NotEqual: 2,
    /** @type {number} Greater than. */
    GreaterThan: 3,
    /** @type {number} Greater than or equal. */
    GreaterThanOrEqual: 4,
    /** @type {number} Less than. */
    LessThan: 5,
    /** @type {number} Less than or equal. */
    LessThanOrEqual: 6,
    /** @type {number} Contains. */
    Contains: 7,
    /** @type {number} Does not contain. */
    NotContains: 8,
    /** @type {number} Starts with. */
    StartWith: 9,
    /** @type {number} Does not start with. */
    NotStartWith: 10,
    /** @type {number} Ends with. */
    EndWidth: 11,
    /** @type {number} Does not end with. */
    NotEndWidth: 12,
    /** @type {number} In a set. */
    In: 13,
    /** @type {number} Not in a set. */
    NotIn: 14,
    /** @type {number} Equals date. */
    EqualDatime: 15,
    /** @type {number} Greater than date. */
    GreaterThanDatime: 21,
    /** @type {number} Less than date. */
    LessThanDatime: 22,
    /** @type {number} Not equal date. */
    NotEqualDatime: 16,
    /** @type {number} Equals null. */
    EqualNull: 17,
    /** @type {number} Not equal null. */
    NotEqualNull: 18,
    /** @type {number} Like. */
    Like: 19,
    /** @type {number} Not like. */
    NotLike: 20,
    /** @type {number} Greater than or equal date. */
    GreaterEqualDatime: 23,
    /** @type {number} Less than or equal date. */
    LessEqualDatime: 24,
};

export const OperationToSql = {
    [AdvSearchOperation.Equal]: "{0} = N'{1}'",
    [AdvSearchOperation.NotEqual]: "{0} != N'{1}'",
    [AdvSearchOperation.GreaterThan]: "{0} > N'{1}'",
    [AdvSearchOperation.GreaterThanOrEqual]: "{0} >= N'{1}'",
    [AdvSearchOperation.LessThan]: "{0} < N'{1}'",
    [AdvSearchOperation.LessThanOrEqual]: "{0} <= N'{1}'",
    [AdvSearchOperation.Contains]: "charindex(N'{1}', {0}) >= 1",
    [AdvSearchOperation.NotContains]: "contains({0}, N'{1}') eq false",
    [AdvSearchOperation.StartWith]: "charindex(N'{1}', {0}) = 1",
    [AdvSearchOperation.NotStartWith]: "charindex(N'{1}', {0}) > 1",
    [AdvSearchOperation.EndWidth]: "{0} like N'%{1}')",
    [AdvSearchOperation.NotEndWidth]: "{0} not like N'%{1}'",
    [AdvSearchOperation.In]: "{0} in ({1})",
    [AdvSearchOperation.Like]: "{0} like N'%{1}%'",
    [AdvSearchOperation.NotLike]: "{0} not like N'{1}'",
    [AdvSearchOperation.NotIn]: "{0} not in ({1})",
    [AdvSearchOperation.EqualDatime]: "cast(date, {0}) = N'{1}'",
    [AdvSearchOperation.NotEqualDatime]: "cast(date, {0}) != N'{1}'",
    [AdvSearchOperation.EqualNull]: "{0} is null",
    [AdvSearchOperation.NotEqualNull]: "{0} is not null",
    [AdvSearchOperation.GreaterThanDatime]: "cast(date, {0}) > N'{1}'",
    [AdvSearchOperation.GreaterEqualDatime]: "cast(date, {0}) >= N'{1}'",
    [AdvSearchOperation.LessThanDatime]: "cast(date, {0}) < N'{1}'",
    [AdvSearchOperation.LessEqualDatime]: "cast(date, {0}) <= N'{1}'",
};

/**
 * Logical operations for combining conditions.
 * @enum {number}
 */
export const LogicOperation = {
    /** @type {number} Logical AND. */
    And: 0,
    /** @type {number} Logical OR. */
    Or: 1,
};

/**
 * Directions for sorting.
 * @enum {number}
 */
export const OrderbyDirection = {
    /** @type {number} Ascending order. */
    ASC: 1,
    /** @type {number} Descending order. */
    DESC: 2,
};

/**
 * Role selection options.
 * @enum {number}
 */
export const RoleSelection = {
    /** @type {number} Top first selection. */
    TopFirst: 1,
    /** @type {number} Bottom first selection. */
    BottomFirst: 2,
};

/**
 * Represents advanced search configurations.
 */
export class AdvSearchVM {
    /**
     * Constructs an instance of AdvSearchVM.
     */
    constructor() {
        this.ActiveState = null;
        /** @type {FieldCondition[]} */
        this.Conditions = [];
        /** @type {OrderBy[]} */
        this.OrderBy = [];
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
        this.FieldName = '';
        this.FieldText = '';
        this.ComponentType = '';
        this.Value = '';
        this.ValueText = '';
        this.Operator = null;
        this.OperatorText = '';
        this.Logic = null;
        this.IsSearch = false;
        this.Group = false;
        this.Shift = false;
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
        this.Condition = '';
        this.Group = false;
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
        this.Id = '';
        this.OriginFieldName = '';
        this.FieldId = '';
        /** @type {Component} */
        this.Field = null;
        /** @type {AdvSearchOperation} */
        this.CompareOperatorId = null;
        this.Value = '';
        this.Display = {};
        /** @type {LogicOperation} */
        this.LogicOperatorId = null;
        this.LogicOperator = null;
        this.Level = '';
        this.Group = false;
    }
}

/**
 * Represents the ordering of a field in a query.
 */
export class OrderBy {
    /**
     * Constructs an instance of OrderBy.
     */
    constructor() {
        this.Id = '';
        this.ComId = '';
        this.FieldName = '';
        this.OrderbyDirectionId = null;
    }
}

/**
 * Represents an event message in a queue.
 */
export class MQEvent {
    /**
     * Constructs an instance of MQEvent.
     */
    constructor() {
        this.DeviceKey = '';
        this.QueueName = '';
        this.Action = '';
        this.Id = '';
        this.PrevId = '';
        this.Time = null; // JavaScript does not have a direct equivalent to DateTimeOffset, using Date instead
        this.Message = null;
    }
}
