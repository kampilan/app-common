using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AppCommon.Api.Middleware;

/// <summary>
/// Exception handler specifically for FluentValidation ValidationException.
/// Registered before GlobalExceptionHandler to handle validation errors with specific formatting.
/// </summary>
public class ValidationExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ValidationExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public ValidationExceptionHandler(
        ILogger<ValidationExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            // Not a validation exception - let the next handler deal with it
            return false;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var request = httpContext.Request;
        var user = httpContext.User?.Identity?.Name ?? "anonymous";

        // Group validation errors by property name
        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        // Log validation failure with structured data
        _logger.LogWarning(
            "Validation failed for {Method} {Path} | User: {User} | TraceId: {TraceId} | Errors: {@ValidationErrors}",
            request.Method,
            request.Path,
            user,
            traceId,
            errors);

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "See the errors property for details."
        };

        problemDetails.Extensions["TraceId"] = traceId;
        problemDetails.Extensions["Errors"] = errors;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
