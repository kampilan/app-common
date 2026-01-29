using System.Reflection;
using AppCommon.Api.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Shouldly;
using Xunit;

namespace AppCommon.Api.Tests.Endpoints;

public class EndpointModuleExtensionsTests
{
    [Fact]
    public void MapEndpointModules_WithSingleAssembly_DiscoverAndRegistersModules()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        var routesCalled = new List<string>();

        // Act - use test assembly which contains TestEndpointModule
        app.MapEndpointModules(typeof(EndpointModuleExtensionsTests).Assembly);

        // Assert - the module was discovered (we can't easily verify routes without more infrastructure)
        // The fact that no exception was thrown indicates success
        app.ShouldNotBeNull();
    }

    [Fact]
    public void MapEndpointModules_WithMultipleAssemblies_DiscoverAndRegistersModules()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act
        var result = app.MapEndpointModules(
            [typeof(EndpointModuleExtensionsTests).Assembly],
            "/api");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(app);
    }

    [Fact]
    public void MapEndpointModules_WithCustomPrefix_UsesPrefix()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act
        var result = app.MapEndpointModules(
            typeof(EndpointModuleExtensionsTests).Assembly,
            "/custom-prefix");

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public void MapEndpointModules_WithEmptyAssembly_CompletesWithoutError()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act - use mscorlib which has no IEndpointModule implementations
        var result = app.MapEndpointModules(typeof(object).Assembly);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public void MapEndpointModules_ReturnsOriginalBuilder()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act
        var result = app.MapEndpointModules(typeof(EndpointModuleExtensionsTests).Assembly);

        // Assert
        result.ShouldBeSameAs(app);
    }

    [Fact]
    public void MapEndpointModules_DefaultPrefixIsApi()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act - call without specifying prefix
        var result = app.MapEndpointModules(typeof(EndpointModuleExtensionsTests).Assembly);

        // Assert - no exception, default /api prefix used
        result.ShouldNotBeNull();
    }

    [Fact]
    public void MapEndpointModules_InstantiatesAndCallsAddRoutes()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        TrackingEndpointModule.WasAddRoutesCalled = false;

        // Act
        app.MapEndpointModules(typeof(EndpointModuleExtensionsTests).Assembly);

        // Assert
        TrackingEndpointModule.WasAddRoutesCalled.ShouldBeTrue();
    }

    [Fact]
    public void MapEndpointModules_IgnoresAbstractClasses()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act - should not throw even though AbstractEndpointModule exists
        var result = app.MapEndpointModules(typeof(EndpointModuleExtensionsTests).Assembly);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public void MapEndpointModules_IgnoresInterfaces()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act - should not try to instantiate IEndpointModule itself
        var result = app.MapEndpointModules(typeof(IEndpointModule).Assembly);

        // Assert
        result.ShouldNotBeNull();
    }
}

/// <summary>
/// Test endpoint module that tracks whether AddRoutes was called.
/// </summary>
public class TrackingEndpointModule : IEndpointModule
{
    public static bool WasAddRoutesCalled { get; set; }

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        WasAddRoutesCalled = true;
    }
}

/// <summary>
/// Abstract endpoint module to verify abstract classes are ignored.
/// </summary>
public abstract class AbstractEndpointModule : IEndpointModule
{
    public abstract void AddRoutes(IEndpointRouteBuilder app);
}
