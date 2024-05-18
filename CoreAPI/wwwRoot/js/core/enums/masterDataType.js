const RoleEnum = {
    System: 8,
};

const TaskStateEnum = {
    UnreadStatus: 339,
    Read: 340,
    Processing: 341,
    Proceeded: 342,
};

const ComponentTypeTypeEnum = {
    Dropdown: 1,
    SearchEntry: 1,
    MultipleSearchEntry: 1,
    Datepicker: 2,
    Number: 3,
    Textbox: 4,
    Checkbox: 5,
};

const ActiveStateEnum = {
    All: 2,
    Yes: 1,
    No: 0,
};

const AdvSearchOperation = {
    Equal: 1,
    NotEqual: 2,
    GreaterThan: 3,
    GreaterThanOrEqual: 4,
    LessThan: 5,
    LessThanOrEqual: 6,
    Contains: 7,
    NotContains: 8,
    StartWith: 9,
    NotStartWith: 10,
    EndWidth: 11,
    NotEndWidth: 12,
    In: 13,
    NotIn: 14,
    EqualDatime: 15,
    GreaterThanDatime: 21,
    LessThanDatime: 22,
    NotEqualDatime: 16,
    EqualNull: 17,
    NotEqualNull: 18,
    Like: 19,
    NotLike: 20,
    GreaterEqualDatime: 23,
    LessEqualDatime: 24,
};

const LogicOperation = {
    And: 0,
    Or: 1,
};

const OrderbyDirection = {
    ASC: 1,
    DESC: 2,
};

const RoleSelection = {
    TopFirst: 1,
    BottomFirst: 2,
};
