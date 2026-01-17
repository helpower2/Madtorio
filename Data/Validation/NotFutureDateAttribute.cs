using System.ComponentModel.DataAnnotations;

namespace Madtorio.Data.Validation;

public class NotFutureDateAttribute : ValidationAttribute
{
    public NotFutureDateAttribute()
        : base("The date cannot be in the future.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (value is DateTime dateValue)
        {
            var dateToCheck = dateValue.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dateValue, DateTimeKind.Local).ToUniversalTime()
                : dateValue.ToUniversalTime();

            if (dateToCheck.Date > DateTime.UtcNow.Date)
            {
                return new ValidationResult(ErrorMessage ?? "The date cannot be in the future.");
            }
        }

        return ValidationResult.Success;
    }
}
