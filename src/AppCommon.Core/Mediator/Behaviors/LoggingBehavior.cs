using System.Diagnostics;
using AppCommon.Core.Context;
using Microsoft.Extensions.Logging;

namespace AppCommon.Core.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that logs request start, completion, and errors with user context.
/// </summary>
/// <remarks>
/// <para>
/// <b>What it logs:</b>
/// <list type="bullet">
/// <item><description>Request start with payload, user subject, and correlation ID</description></item>
/// <item><description>Request completion with duration</description></item>
/// <item><description>Request errors with exception details</description></item>
/// </list>
/// </para>
/// <para>
/// <b>User context:</b> This behavior reads from <see cref="IRequestContext"/> to include
/// the current user's subject in all log entries. If <see cref="IRequestContext.Subject"/> is null,
/// logs will show "anonymous".
/// </para>
/// <para>
/// <b>Correlation:</b> Uses <see cref="IRequestContext.CorrelationUid"/> which is consistent
/// with audit log entries, enabling end-to-end tracing.
/// </para>
/// <para>
/// <b>Integration:</b> User info is automatically available when any ASP.NET Core authentication
/// handler populates <c>HttpContext.User</c>. No manual setup required beyond calling
/// <c>app.UseAuthentication()</c> before endpoints.
/// </para>
/// <para>
/// <b>Batch context:</b> Child commands within a <see cref="BatchExecutionContext"/> are logged
/// at Debug level to reduce verbosity.
/// </para>
/// <para>
/// <b>Registration:</b>
/// <code>
/// services.AddRequestContext();  // Required - from AppCommon.Api
/// services.AddPipelineBehavior(typeof(LoggingBehavior&lt;,&gt;));
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="IRequestContext"/>
/// <seealso cref="BatchExecutionContext"/>
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    IRequestContext requestContext) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        var correlationUid = requestContext.CorrelationUid;
        var subject = requestContext.Subject ?? "anonymous";
        var stopwatch = Stopwatch.StartNew();

        // Use Debug level for child commands in a batch to reduce log noise
        var isChildCommand = BatchExecutionContext.IsInBatch;
        var batchContext = isChildCommand ? $" | BatchId: {BatchExecutionContext.BatchId}" : "";

        if (isChildCommand)
        {
            logger.LogDebug(
                "Request {RequestType} started | User: {Subject} | CorrelationUid: {CorrelationUid}{BatchContext} | Payload: {@Payload}",
                requestName, subject, correlationUid, batchContext, request);
        }
        else
        {
            logger.LogInformation(
                "Request {RequestType} started | User: {Subject} | CorrelationUid: {CorrelationUid} | Payload: {@Payload}",
                requestName, subject, correlationUid, request);
        }

        try
        {
            var response = await next();
            stopwatch.Stop();

            if (isChildCommand)
            {
                logger.LogDebug(
                    "Request {RequestType} completed | Duration: {ElapsedMs}ms | CorrelationUid: {CorrelationUid}{BatchContext}",
                    requestName, stopwatch.ElapsedMilliseconds, correlationUid, batchContext);
            }
            else
            {
                logger.LogInformation(
                    "Request {RequestType} completed | Duration: {ElapsedMs}ms | CorrelationUid: {CorrelationUid}",
                    requestName, stopwatch.ElapsedMilliseconds, correlationUid);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Request {RequestType} failed | Duration: {ElapsedMs}ms | CorrelationUid: {CorrelationUid}{BatchContext} | Error: {ErrorMessage}",
                requestName, stopwatch.ElapsedMilliseconds, correlationUid, batchContext, ex.Message);
            throw;
        }
    }
}
