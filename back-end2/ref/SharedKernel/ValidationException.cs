namespace SharedKernel;

/// <summary>
/// Exception thrown when validation fails.
/// Contains detailed validation error information per property.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>
    /// The validation errors grouped by property name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]>
        {
            [propertyName] = new[] { errorMessage }
        };
    }
}
