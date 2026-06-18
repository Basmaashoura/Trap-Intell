namespace SharedKernel;

/// <summary>
/// Base exception for domain-specific business rule violations.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// The error representing the business rule violation.
    /// </summary>
    public Error Error { get; }

    protected DomainException(Error error) : base(error.Description)
    {
        Error = error;
    }

    protected DomainException(Error error, Exception innerException)
        : base(error.Description, innerException)
    {
        Error = error;
    }
}
