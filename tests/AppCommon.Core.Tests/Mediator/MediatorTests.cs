using AppCommon.Core.Mediator;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AppCommon.Core.Tests.Mediator;

public class MediatorTests
{
    [Fact]
    public async Task SendAsync_WithValidRequest_CallsHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = Substitute.For<IRequestHandler<TestRequest, TestResponse>>();
        handler.HandleAsync(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TestResponse { Value = "handled" });

        services.AddScoped(_ => handler);
        services.AddScoped<IMediator, AppCommon.Core.Mediator.Mediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var request = new TestRequest { Input = "test" };

        // Act
        var response = await mediator.SendAsync(request);

        // Assert
        response.ShouldNotBeNull();
        response.Value.ShouldBe("handled");
        await handler.Received(1).HandleAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IMediator, AppCommon.Core.Mediator.Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await mediator.SendAsync<TestResponse>(null!));
    }

    [Fact]
    public async Task SendAsync_WithNoHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IMediator, AppCommon.Core.Mediator.Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var request = new TestRequest { Input = "test" };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await mediator.SendAsync(request));
    }

    [Fact]
    public async Task SendAsync_WithPipelineBehavior_ExecutesBehaviorBeforeHandler()
    {
        // Arrange
        var executionOrder = new List<string>();

        var services = new ServiceCollection();

        var handler = Substitute.For<IRequestHandler<TestRequest, TestResponse>>();
        handler.HandleAsync(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                executionOrder.Add("handler");
                return Task.FromResult(new TestResponse { Value = "handled" });
            });

        var behavior = Substitute.For<IPipelineBehavior<TestRequest, TestResponse>>();
        behavior.HandleAsync(Arg.Any<TestRequest>(), Arg.Any<RequestHandlerDelegate<TestResponse>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                executionOrder.Add("behavior-before");
                var next = callInfo.ArgAt<RequestHandlerDelegate<TestResponse>>(1);
                var result = await next();
                executionOrder.Add("behavior-after");
                return result;
            });

        services.AddScoped(_ => handler);
        services.AddScoped<IPipelineBehavior<TestRequest, TestResponse>>(_ => behavior);
        services.AddScoped<IMediator, AppCommon.Core.Mediator.Mediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestRequest { Input = "test" });

        // Assert
        executionOrder.ShouldBe(["behavior-before", "handler", "behavior-after"]);
    }

    [Fact]
    public async Task SendAsync_WithMultipleBehaviors_ExecutesInCorrectOrder()
    {
        // Arrange
        var executionOrder = new List<string>();

        var services = new ServiceCollection();

        var handler = Substitute.For<IRequestHandler<TestRequest, TestResponse>>();
        handler.HandleAsync(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                executionOrder.Add("handler");
                return Task.FromResult(new TestResponse { Value = "handled" });
            });

        services.AddScoped(_ => handler);
        services.AddScoped<IPipelineBehavior<TestRequest, TestResponse>>(_ =>
            new TestBehavior("first", executionOrder));
        services.AddScoped<IPipelineBehavior<TestRequest, TestResponse>>(_ =>
            new TestBehavior("second", executionOrder));
        services.AddScoped<IMediator, AppCommon.Core.Mediator.Mediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestRequest { Input = "test" });

        // Assert
        // First registered = outermost, executes first
        executionOrder.ShouldBe(["first-before", "second-before", "handler", "second-after", "first-after"]);
    }

    [Fact]
    public async Task SendAsync_CachesHandlerWrapper_ForSameRequestType()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = Substitute.For<IRequestHandler<TestRequest, TestResponse>>();
        handler.HandleAsync(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TestResponse { Value = "handled" });

        services.AddScoped(_ => handler);
        services.AddScoped<IMediator, AppCommon.Core.Mediator.Mediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - send multiple requests of the same type
        await mediator.SendAsync(new TestRequest { Input = "test1" });
        await mediator.SendAsync(new TestRequest { Input = "test2" });
        await mediator.SendAsync(new TestRequest { Input = "test3" });

        // Assert - handler should be called 3 times (caching is internal, we just verify it works)
        await handler.Received(3).HandleAsync(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>());
    }

    private class TestBehavior(string name, List<string> executionOrder) : IPipelineBehavior<TestRequest, TestResponse>
    {
        public async Task<TestResponse> HandleAsync(
            TestRequest request,
            RequestHandlerDelegate<TestResponse> next,
            CancellationToken cancellationToken = default)
        {
            executionOrder.Add($"{name}-before");
            var result = await next();
            executionOrder.Add($"{name}-after");
            return result;
        }
    }
}

public class TestRequest : IRequest<TestResponse>
{
    public required string Input { get; init; }
}

public class TestResponse
{
    public required string Value { get; init; }
}
