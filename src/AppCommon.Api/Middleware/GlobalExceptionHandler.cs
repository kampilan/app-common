using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppCommon.Api.Middleware;

/// <summary>
/// Global exception handler that catches all unhandled exceptions,
/// logs them with full context, and returns RFC 7807 Problem Details responses.
/// Registered after specific handlers (like ValidationExceptionHandler) to act as a fallback.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService,
        IHostEnvironment environment)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        // Log the exception with full context
        LogException(httpContext, exception, traceId);

        // Determine status code and problem details based on exception type
        var (statusCode, problemDetails) = CreateProblemDetails(exception, traceId);

        httpContext.Response.StatusCode = statusCode;

        // Use IProblemDetailsService to write the response
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private void LogException(HttpContext httpContext, Exception exception, string traceId)
    {
        var request = httpContext.Request;
        var user = httpContext.User?.Identity?.Name ?? "anonymous";

        // Create a structured log scope with request context
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["User"] = user,
            ["RequestMethod"] = request.Method,
            ["RequestPath"] = request.Path.ToString(),
            ["QueryString"] = request.QueryString.ToString(),
            ["UserAgent"] = request.Headers.UserAgent.ToString()
        }))
        {
            switch (exception)
            {
                case KeyNotFoundException:
                case FileNotFoundException:
                    _logger.LogWarning(exception, "Resource not found");
                    break;

                case UnauthorizedAccessException:
                    _logger.LogWarning(exception, "Unauthorized access attempt");
                    break;

                case InvalidOperationException:
                case ArgumentException:
                    _logger.LogWarning(exception, "Bad request: {Message}", exception.Message);
                    break;

                case OperationCanceledException:
                    _logger.LogInformation("Request was cancelled");
                    break;

                case TimeoutException:
                    _logger.LogError(exception, "Operation timed out");
                    break;

                default:
                    _logger.LogError(
                        exception,
                        "Unhandled exception of type {ExceptionType}: {Message}",
                        exception.GetType().Name,
                        exception.Message);
                    break;
            }
        }
    }

    private (int StatusCode, ProblemDetails ProblemDetails) CreateProblemDetails(
        Exception exception,
        string traceId)
    {
        var (statusCode, type, title, detail) = exception switch
        {
            KeyNotFoundException or FileNotFoundException => (
                StatusCodes.Status404NotFound,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                "Resource not found",
                exception.Message),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                "Unauthorized",
                "You are not authorized to access this resource."),

            InvalidOperationException or ArgumentException => (
                StatusCodes.Status400BadRequest,
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                "Bad request",
                exception.Message),

            OperationCanceledException => (
                StatusCodes.Status499ClientClosedRequest,
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                "Request cancelled",
                "The request was cancelled by the client."),

            TimeoutException => (
                StatusCodes.Status504GatewayTimeout,
                "https://tools.ietf.org/html/rfc9110#section-15.6.5",
                "Gateway timeout",
                "The operation timed out."),

            _ => (
                StatusCodes.Status500InternalServerError,
                "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                "An error occurred while processing your request",
                _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.")
        };

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail
        };

        // Always include traceId for correlation
        problemDetails.Extensions["traceId"] = traceId;

        // Include additional debug info in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;

            if (exception is not (KeyNotFoundException or FileNotFoundException or OperationCanceledException))
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            }

            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    type = exception.InnerException.GetType().FullName,
                    message = exception.InnerException.Message
                };
            }
        }

        return (statusCode, problemDetails);
    }
}
