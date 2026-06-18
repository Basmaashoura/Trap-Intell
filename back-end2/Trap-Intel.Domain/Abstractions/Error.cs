namespace Trap_Intel.Domain.Abstractions
{
    /// <summary>
    /// Error representation with code and message.
    /// </summary>
    public sealed class Error
    {
        public string Code { get; }
        public string Message { get; }
        public IReadOnlyDictionary<string, object>? Data { get; private init; }

        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString() => $"{Code}: {Message}";

        /// <summary>
        /// Creates a new Error with additional data attached.
        /// </summary>
        public Error WithData(IDictionary<string, object> data) => new(Code, Message)
        {
            Data = new Dictionary<string, object>(data)
        };

        // Pre-defined common errors
        public static readonly Error None = new("None", string.Empty);
        public static readonly Error NullValue = new("Error.NullValue", "The value cannot be null.");

        public static Error Custom(string code, string message) => new(code, message);
    }
}
