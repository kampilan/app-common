using System.Diagnostics;
using AppCommon.Api.Middleware;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AppCommon.Api.Tests.Middleware;

public class ValidationExceptionHandlerTests
{
    private readonly ILogger<ValidationExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ValidationExceptionHandler _handler;

    public ValidationExceptionHandlerTests()
    {
        _logger = Substitute.For<ILogger<ValidationExceptionHandler>>();
        _problemDetailsService = Substitute.For<IProblemDetailsService>();
        _problemDetailsService.TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(true);
        _handler = new ValidationExceptionHandler(_logger, _problemDetailsService);
    }

    [Fact]
    public async Task TryHandleAsync_WithValidationException_ReturnsTrue()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = CreateValidationException("Name", "Name is required");

        // Act
        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_WithNonValidationException_ReturnsFalse()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("Some error");

        // Act
        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_SetsStatusCode400()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = CreateValidationException("Name", "Name is required");

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task TryHandleAsync_GroupsErrorsByProperty()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var errors = new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Name", "Name must be at least 3 characters"),
            new ValidationFailure("Email", "Email is invalid")
        };
        var exception = new ValidationException(errors);

        ProblemDetailsContext? capturedContext = null;
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(ctx => capturedContext = ctx))
            .Returns(true);

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedContext.ShouldNotBeNull();
        var problemErrors = capturedContext.ProblemDetails.Extensions["Errors"] as Dictionary<string, string[]>;
        problemErrors.ShouldNotBeNull();
        problemErrors["Name"].Length.ShouldBe(2);
        problemErrors["Email"].Length.ShouldBe(1);
    }

    [Fact]
    public async Task TryHandleAsync_IncludesTraceId()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        var exception = CreateValidationException("Name", "Name is required");

        ProblemDetailsContext? capturedContext = null;
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(ctx => capturedContext = ctx))
            .Returns(true);

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.ProblemDetails.Extensions["TraceId"].ShouldBe("test-trace-id");
    }

    [Fact]
    public async Task TryHandleAsync_SetsProblemDetailsType()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = CreateValidationException("Name", "Name is required");

        ProblemDetailsContext? capturedContext = null;
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(ctx => capturedContext = ctx))
            .Returns(true);

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.ProblemDetails.Type.ShouldBe("https://tools.ietf.org/html/rfc9110#section-15.5.1");
        capturedContext.ProblemDetails.Title.ShouldBe("One or more validation errors occurred.");
    }

    [Fact]
    public async Task TryHandleAsync_LogsValidationFailure()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = CreateValidationException("Name", "Name is required");

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert - verify logging occurred
        _logger.ReceivedWithAnyArgs().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/test";
        return context;
    }

    private static ValidationException CreateValidationException(string propertyName, string errorMessage)
    {
        var failures = new[] { new ValidationFailure(propertyName, errorMessage) };
        return new ValidationException(failures);
    }
}
