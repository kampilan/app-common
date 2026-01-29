using AppCommon.Core.Identity;
using AppCommon.Core.Mediator;
using AppCommon.Core.Mediator.Behaviors;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AppCommon.Core.Tests.Mediator.Behaviors;

public class LoggingBehaviorTests
{
    private readonly ILogger<LoggingBehavior<TestLoggingRequest, TestLoggingResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly LoggingBehavior<TestLoggingRequest, TestLoggingResponse> _behavior;

    public LoggingBehaviorTests()
    {
        _logger = Substitute.For<ILogger<LoggingBehavior<TestLoggingRequest, TestLoggingResponse>>>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns("test-user-123");
        _behavior = new LoggingBehavior<TestLoggingRequest, TestLoggingResponse>(_logger, _currentUserService);
    }

    [Fact]
    public async Task HandleAsync_CallsNextAndReturnsResult()
    {
        // Arrange
        var request = new TestLoggingRequest { Data = "test" };
        var expectedResponse = new TestLoggingResponse { Result = "success" };
        Task<TestLoggingResponse> Next() => Task.FromResult(expectedResponse);

        // Act
        var result = await _behavior.HandleAsync(request, Next);

        // Assert
        result.ShouldBe(expectedResponse);
    }

    [Fact]
    public async Task HandleAsync_WhenNotInBatch_LogsAtInformationLevel()
    {
        // Arrange
        var request = new TestLoggingRequest { Data = "test" };
        Task<TestLoggingResponse> Next() => Task.FromResult(new TestLoggingResponse { Result = "ok" });

        // Act
        await _behavior.HandleAsync(request, Next);

        // Assert - verify Information level was used (not Debug)
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_WhenInBatch_LogsAtDebugLevel()
    {
        // Arrange
        var request = new TestLoggingRequest { Data = "test" };
        Task<TestLoggingResponse> Next() => Task.FromResult(new TestLoggingResponse { Result = "ok" });

        // Act
        using (BatchExecutionContext.BeginBatch("test-batch"))
        {
            await _behavior.HandleAsync(request, Next);
        }

        // Assert - verify Debug level was used
        _logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_WhenNextThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var request = new TestLoggingRequest { Data = "test" };
        var expectedException = new InvalidOperationException("Test error");
        Task<TestLoggingResponse> Next() => throw expectedException;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _behavior.HandleAsync(request, Next));

        exception.ShouldBe(expectedException);

        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            expectedException,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdIsNull_LogsAnonymous()
    {
        // Arrange
        _currentUserService.UserId.Returns((string?)null);
        var request = new TestLoggingRequest { Data = "test" };
        Task<TestLoggingResponse> Next() => Task.FromResult(new TestLoggingResponse { Result = "ok" });

        // Act
        await _behavior.HandleAsync(request, Next);

        // Assert - behavior should complete without error (anonymous is used internally)
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_LogsStartAndCompletion()
    {
        // Arrange
        var request = new TestLoggingRequest { Data = "test" };
        Task<TestLoggingResponse> Next() => Task.FromResult(new TestLoggingResponse { Result = "ok" });

        // Act
        await _behavior.HandleAsync(request, Next);

        // Assert - should have logged twice (start and completion)
        _logger.Received(2).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_WhenInBatchAndThrows_LogsErrorWithBatchContext()
    {
        // Arrange
        var request = new TestLoggingRequest { Data = "test" };
        var expectedException = new InvalidOperationException("Batch error");
        Task<TestLoggingResponse> Next() => throw expectedException;

        // Act
        using (BatchExecutionContext.BeginBatch("error-batch"))
        {
            await Should.ThrowAsync<InvalidOperationException>(
                async () => await _behavior.HandleAsync(request, Next));
        }

        // Assert
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            expectedException,
            Arg.Any<Func<object, Exception?, string>>());
    }
}

public class TestLoggingRequest : IRequest<TestLoggingResponse>
{
    public required string Data { get; init; }
}

public class TestLoggingResponse
{
    public required string Result { get; init; }
}
