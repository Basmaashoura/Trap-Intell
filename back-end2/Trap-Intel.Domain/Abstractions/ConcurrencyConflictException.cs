namespace Trap_Intel.Domain.Abstractions
{
    /// <summary>
    /// Thrown when the persistence layer detects that a resource was updated concurrently.
    /// </summary>
    public sealed class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
