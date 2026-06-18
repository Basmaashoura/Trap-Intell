using System.ComponentModel.DataAnnotations;

namespace Trap_Intel.Api.Filters;

/// <summary>
/// Endpoint filter that validates arguments using DataAnnotations.
/// Required since Minimal APIs do not run DataAnnotations validation by default.
/// </summary>
public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null) continue;

            var validationContext = new ValidationContext(argument);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(argument, validationContext, validationResults, validateAllProperties: true))
            {
                var errors = validationResults
                    .Where(r => r.ErrorMessage != null)
                    .ToDictionary(
                        r => r.MemberNames.FirstOrDefault() ?? "Unknown",
                        r => new[] { r.ErrorMessage! });

                return Results.ValidationProblem(errors);
            }
        }

        return await next(context);
    }
}
