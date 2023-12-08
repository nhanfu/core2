using Core.Models;
using Core.Clients;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Extensions
{
    public class ValidationRule
    {
        public const string Required = "required";
        public const string MinLength = "minLength";
        public const string CheckLength = "checkLength";
        public const string MaxLength = "maxLength";
        public const string GreaterThanOrEqual = "min";
        public const string LessThanOrEqual = "max";
        public const string GreaterThan = "gt";
        public const string LessThan = "lt";
        public const string Equal = "eq";
        public const string NotEqual = "ne";
        public const string RegEx = "regEx";
        public const string Unique = "unique";

        public string Rule { get; set; }
        public string Message { get; set; }
        public object Value1 { get; set; }
        public object Value2 { get; set; }
        public string Condition { get; set; }
        public bool RejectInvalid { get; set; }
    }
}
