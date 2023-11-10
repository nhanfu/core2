using System.ComponentModel.DataAnnotations;

namespace Core.Attributes
{
    public class ValidateModelAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return base.IsValid(value, validationContext);
        }
    }
}
