export class ValidationRule {
    static required = "required";
    static minLength = "minLength";
    static checkLength = "checkLength";
    static maxLength = "maxLength";
    static greaterThanOrEqual = "min";
    static lessThanOrEqual = "max";
    static greaterThan = "gt";
    static lessThan = "lt";
    static equal = "eq";
    static notEqual = "ne";
    static regEx = "regEx";
    static replace = "replace";
    static unique = "unique";

    constructor(rule, message, value1, value2, condition, rejectInvalid) {
        this.rule = rule;
        this.message = message;
        this.value1 = value1;
        this.value2 = value2;
        this.condition = condition;
        this.rejectInvalid = rejectInvalid;
    }
}
