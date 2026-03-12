export class ValidationRule {
    static Required = "required";
    static minLength = "minLength";
    static checkLength = "checkLength";
    static maxLength = "maxLength";
    static greaterThanOrEqual = "min";
    static lessThanOrEqual = "max";
    static greaterThan = "gt";
    static lessThan = "lt";
    static Equal = "eq";
    static notEqual = "ne";
    static regEx = "regEx";
    static Replace = "replace";
    static Unique = "unique";

    constructor(rule, message, value1, value2, condition, rejectInvalid) {
        this.rule = rule;
        this.message = message;
        this.value1 = value1;
        this.value2 = value2;
        this.condition = condition;
        this.rejectInvalid = rejectInvalid;
    }
}
