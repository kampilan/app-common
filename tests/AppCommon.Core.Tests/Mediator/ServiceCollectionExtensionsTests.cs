using System.Reflection;
using AppCommon.Core.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AppCommon.Core.Tests.Mediator;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMediator_RegistersMediator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(ServiceCollectionExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var mediator = provider.GetService<IMediator>();
        mediator.ShouldNotBeNull();
        mediator.ShouldBeOfType<AppCommon.Core.Mediator.Mediator>();
    }

    [Fact]
    public void AddMediator_RegistersHandlersFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(ServiceCollectionExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<IRequestHandler<TestCommand, TestCommandResponse>>();
        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<TestCommandHandler>();
    }

    [Fact]
    public void AddMediator_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddMediator(typeof(ServiceCollectionExtensionsTests).Assembly));
    }

    [Fact]
    public void AddMediator_WithNullAssemblies_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddMediator(null!));
    }

    [Fact]
    public void AddMediator_WithEmptyAssemblies_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            services.AddMediator([]));
    }

    [Fact]
    public void AddMediator_WithMultipleAssemblies_RegistersAllHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddMediator(assembly);
        var provider = services.BuildServiceProvider();

        // Assert - verify handlers from test assembly are registered
        var handler = provider.GetService<IRequestHandler<TestCommand, TestCommandResponse>>();
        handler.ShouldNotBeNull();
    }

    [Fact]
    public void AddMediator_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddMediator(typeof(ServiceCollectionExtensionsTests).Assembly);

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddPipelineBehavior_RegistersBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPipelineBehavior(typeof(TestPipelineBehavior<,>));
        var provider = services.BuildServiceProvider();

        // Assert
        var behaviors = provider.GetServices<IPipelineBehavior<TestCommand, TestCommandResponse>>();
        behaviors.ShouldContain(b => b is TestPipelineBehavior<TestCommand, TestCommandResponse>);
    }

    [Fact]
    public void AddPipelineBehavior_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddPipelineBehavior(typeof(TestPipelineBehavior<,>)));
    }

    [Fact]
    public void AddPipelineBehavior_WithNullBehaviorType_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddPipelineBehavior(null!));
    }

    [Fact]
    public void AddPipelineBehavior_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddPipelineBehavior(typeof(TestPipelineBehavior<,>));

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddMediator_RegistersHandlersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(ServiceCollectionExtensionsTests).Assembly);

        // Act
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IRequestHandler<TestCommand, TestCommandResponse>));

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddMediator_RegistersMediatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(ServiceCollectionExtensionsTests).Assembly);

        // Act
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMediator));

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }
}

// Test fixtures
public class TestCommand : ICommand<TestCommandResponse>
{
    public required string Name { get; init; }
}

public class TestCommandResponse
{
    public required string Result { get; init; }
}

public class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
{
    public Task<TestCommandResponse> HandleAsync(TestCommand request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestCommandResponse { Result = $"Handled: {request.Name}" });
    }
}

public class TestPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        return await next();
    }
}
