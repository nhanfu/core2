export class ValidationRule {
    static Required = "required";
    static MinLength = "minLength";
    static CheckLength = "checkLength";
    static MaxLength = "maxLength";
    static GreaterThanOrEqual = "min";
    static LessThanOrEqual = "max";
    static GreaterThan = "gt";
    static LessThan = "lt";
    static Equal = "eq";
    static NotEqual = "ne";
    static RegEx = "regEx";
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
