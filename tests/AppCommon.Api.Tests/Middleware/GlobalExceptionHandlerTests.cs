using AppCommon.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AppCommon.Api.Tests.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IHostEnvironment _environment;
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        _problemDetailsService = Substitute.For<IProblemDetailsService>();
        _problemDetailsService.TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(true);
        _environment = Substitute.For<IHostEnvironment>();
        _environment.EnvironmentName.Returns("Production");
        _handler = new GlobalExceptionHandler(_logger, _problemDetailsService, _environment);
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsTrue()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new Exception("Test error");

        // Act
        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_WithKeyNotFoundException_Returns404()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new KeyNotFoundException("Resource not found");

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task TryHandleAsync_WithFileNotFoundException_Returns404()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new FileNotFoundException("File not found");

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task TryHandleAsync_WithUnauthorizedAccessException_Returns401()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new UnauthorizedAccessException("Not authorized");

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task TryHandleAsync_WithInvalidOperationException_Returns400()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("Invalid operation");

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task TryHandleAsync_WithArgumentException_Returns400()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new ArgumentException("Invalid argument");

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task TryHandleAsync_WithOperationCanceledException_Returns499()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new OperationCanceledException();

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(499); // Client Closed Request
    }

    [Fact]
    public async Task TryHandleAsync_WithTimeoutException_Returns504()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new TimeoutException("Timed out");

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status504GatewayTimeout);
    }

    [Fact]
    public async Task TryHandleAsync_WithGenericException_Returns500()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new Exception("Unknown error");

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task TryHandleAsync_IncludesTraceId()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        var exception = new Exception("Test error");

        ProblemDetailsContext? capturedContext = null;
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(ctx => capturedContext = ctx))
            .Returns(true);

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.ProblemDetails.Extensions["traceId"].ShouldBe("test-trace-id");
    }

    [Fact]
    public async Task TryHandleAsync_InDevelopment_IncludesExceptionDetails()
    {
        // Arrange
        var devEnvironment = Substitute.For<IHostEnvironment>();
        devEnvironment.EnvironmentName.Returns("Development");
        var handler = new GlobalExceptionHandler(_logger, _problemDetailsService, devEnvironment);

        var httpContext = CreateHttpContext();
        var exception = new Exception("Test error");

        ProblemDetailsContext? capturedContext = null;
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(ctx => capturedContext = ctx))
            .Returns(true);

        // Act
        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.ProblemDetails.Extensions.ContainsKey("exceptionType").ShouldBeTrue();
        capturedContext.ProblemDetails.Extensions.ContainsKey("stackTrace").ShouldBeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_InProduction_HidesExceptionDetails()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new Exception("Sensitive error details");

        ProblemDetailsContext? capturedContext = null;
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(ctx => capturedContext = ctx))
            .Returns(true);

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.ProblemDetails.Detail.ShouldBe("An unexpected error occurred.");
        capturedContext.ProblemDetails.Extensions.ContainsKey("stackTrace").ShouldBeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_InDevelopment_IncludesInnerException()
    {
        // Arrange
        var devEnvironment = Substitute.For<IHostEnvironment>();
        devEnvironment.EnvironmentName.Returns("Development");
        var handler = new GlobalExceptionHandler(_logger, _problemDetailsService, devEnvironment);

        var httpContext = CreateHttpContext();
        var innerException = new InvalidOperationException("Inner error");
        var exception = new Exception("Outer error", innerException);

        ProblemDetailsContext? capturedContext = null;
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(ctx => capturedContext = ctx))
            .Returns(true);

        // Act
        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.ProblemDetails.Extensions.ContainsKey("innerException").ShouldBeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_SetsProblemDetailsType()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new KeyNotFoundException("Not found");

        ProblemDetailsContext? capturedContext = null;
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(ctx => capturedContext = ctx))
            .Returns(true);

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.ProblemDetails.Type.ShouldBe("https://tools.ietf.org/html/rfc9110#section-15.5.5");
        capturedContext.ProblemDetails.Title.ShouldBe("Resource not found");
    }

    [Fact]
    public async Task TryHandleAsync_LogsExceptions()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new Exception("Test error");

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
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        return context;
    }
}
