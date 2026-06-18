using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Behaviors;

/// <summary>
/// Pipeline behavior to handle validation using FluentValidation.
/// It intercepts the request, runs all validators, and if any fail, it returns a failed Result.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var errors = _validators
            .Select(validator => validator.Validate(context))
            .SelectMany(validationResult => validationResult.Errors)
            .Where(validationFailure => validationFailure != null)
            .Select(failure => Error.Custom(
                $"Validation.{failure.PropertyName}", 
                failure.ErrorMessage))
            .Distinct()
            .ToList();

        if (errors.Count != 0)
        {
            return CreateValidationResult<TResponse>(errors);
        }

        return await next();
    }

    /// <summary>
    /// Helper method to create a generic Result with validation errors using reflection.
    /// </summary>
    private static TResponse CreateValidationResult<TResult>(List<Error> errors)
        where TResult : Result
    {
        // If the result is just Result (not Result<T>)
        if (typeof(TResult) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errors);
        }

        // If it's a generic Result<T>
        var validationResult = typeof(Result).GetMethods()
            .First(m => m.Name == "Failure" && m.IsGenericMethod && m.GetParameters().First().ParameterType == typeof(List<Error>))
            .MakeGenericMethod(typeof(TResult).GenericTypeArguments[0])
            .Invoke(null, [errors]);

        return (TResponse)validationResult!;
    }
}
