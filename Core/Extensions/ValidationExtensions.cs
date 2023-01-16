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

    public static class ValidationExtensions
    {
        public static async Task<bool> IsUnique<T>(T entity, string fieldName, string filter = null)
        {
            const string id = "Id";
            if (filter is null)
            {
                filter = $"Active eq true and {fieldName} eq '{entity.GetPropValue(fieldName)}' and Id ne {entity["Id"].As<int>()}";
            }
            var isExists = await new Client(typeof(T).Name).GetAsync<OdataResult<T>>(
                $"?$select={id},{fieldName}&$filter={filter}");
            return isExists?.Value?.Count == 0 || isExists?.Value?.Count == 1 && entity[id] == isExists.Value.First()[id];
        }
    }
}
