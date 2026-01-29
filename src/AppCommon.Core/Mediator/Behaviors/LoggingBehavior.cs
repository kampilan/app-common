using System.Diagnostics;
using AppCommon.Core.Lifecycle;
using Microsoft.Extensions.Logging;

namespace AppCommon.Core.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that logs request start, completion, and errors.
/// Replaces the separate pre/post/error handlers.
/// Child commands within a batch context are logged at Debug level to reduce verbosity.
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUser) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();

        // Use Debug level for child commands in a batch to reduce log noise
        var isChildCommand = BatchExecutionContext.IsInBatch;
        var batchContext = isChildCommand ? $" | BatchId: {BatchExecutionContext.BatchId}" : "";

        if (isChildCommand)
        {
            logger.LogDebug(
                "Request {RequestType} started | User: {UserId} | CorrelationId: {CorrelationId}{BatchContext} | Payload: {@Payload}",
                requestName, currentUser.UserId ?? "anonymous", correlationId, batchContext, request);
        }
        else
        {
            logger.LogInformation(
                "Request {RequestType} started | User: {UserId} | CorrelationId: {CorrelationId} | Payload: {@Payload}",
                requestName, currentUser.UserId ?? "anonymous", correlationId, request);
        }

        try
        {
            var response = await next();
            stopwatch.Stop();

            if (isChildCommand)
            {
                logger.LogDebug(
                    "Request {RequestType} completed | Duration: {ElapsedMs}ms | CorrelationId: {CorrelationId}{BatchContext}",
                    requestName, stopwatch.ElapsedMilliseconds, correlationId, batchContext);
            }
            else
            {
                logger.LogInformation(
                    "Request {RequestType} completed | Duration: {ElapsedMs}ms | CorrelationId: {CorrelationId}",
                    requestName, stopwatch.ElapsedMilliseconds, correlationId);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Request {RequestType} failed | Duration: {ElapsedMs}ms | CorrelationId: {CorrelationId}{BatchContext} | Error: {ErrorMessage}",
                requestName, stopwatch.ElapsedMilliseconds, correlationId, batchContext, ex.Message);
            throw;
        }
    }
}
