using System.ComponentModel.DataAnnotations;

namespace TMS.API.Attributes
{
    public class ValidateModelAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return base.IsValid(value, validationContext);
        }
    }
}
