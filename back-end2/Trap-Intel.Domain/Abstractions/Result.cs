using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Trap_Intel.Domain.Abstractions
{
    /// <summary>
    /// Represents the outcome of an operation (success/failure) with multiple errors support.
    /// </summary>
    public class Result
    {
        protected internal Result(bool isSuccess, List<Error> errors)
        {
            if (isSuccess && errors.Count > 0)
            {
                throw new InvalidOperationException("Success result cannot have errors.");
            }

            if (!isSuccess && errors.Count == 0)
            {
                throw new InvalidOperationException("Failure result must have at least one error.");
            }

            IsSuccess = isSuccess;
            Errors = errors;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public List<Error> Errors { get; }

        public static Result Success() => new(true, new());
        public static Result Failure(Error error) => new(false, new() { error });
        public static Result Failure(List<Error> errors) => new(false, errors);

        public static Result<TValue> Success<TValue>(TValue value) => new(value, true, new());
        public static Result<TValue> Failure<TValue>(Error error) => new(default, false, new() { error });
        public static Result<TValue> Failure<TValue>(List<Error> errors) => new(default, false, errors);

        public static Result<TValue> Create<TValue>(TValue? value) =>
            value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

        public static Result<TValue> Try<TValue>(Func<TValue> func, Func<Exception, Error>? errorHandler = null)
        {
            try
            {
                return Success(func());
            }
            catch (Exception ex)
            {
                var error = errorHandler?.Invoke(ex) ?? Error.Custom("Error.Exception", ex.Message);
                return Failure<TValue>(error);
            }
        }
    }

    /// <summary>
    /// Generic result with value.
    /// </summary>
    public class Result<TValue> : Result
    {
        private readonly TValue? _value;

        protected internal Result(TValue? value, bool isSuccess, List<Error> errors)
            : base(isSuccess, errors)
        {
            _value = value;
        }

        [NotNull]
        public TValue Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed result.");

        public static implicit operator Result<TValue>(TValue? value) => Create(value);
    }
}
