using FluentValidation.Results;
using SharedKernel;

namespace Application.Extensions;

/// <summary>
/// Extension methods for validation.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Converts FluentValidation ValidationResult to ValidationException.
    /// </summary>
    public static ValidationException ToValidationException(this ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationException(errors);
    }
}
