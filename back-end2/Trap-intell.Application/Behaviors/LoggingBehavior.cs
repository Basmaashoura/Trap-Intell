using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Behaviors;

/// <summary>
/// Pipeline behavior for unified logging of requests and their outcomes.
/// Logs request start, completion, and failures (if any).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Processing request {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            if (response is Result result && result.IsFailure)
            {
                var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Message}"));
                _logger.LogWarning(
                    "Request {RequestName} failed after {ElapsedMilliseconds} ms. Errors: {Errors}",
                    requestName, stopwatch.ElapsedMilliseconds, errors);
            }
            else
            {
                _logger.LogInformation(
                    "Completed request {RequestName} successfully in {ElapsedMilliseconds} ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Request {RequestName} threw an exception after {ElapsedMilliseconds} ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
