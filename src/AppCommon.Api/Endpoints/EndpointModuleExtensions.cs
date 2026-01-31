using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AppCommon.Api.Endpoints;

public static class EndpointModuleExtensions
{
    /// <summary>
    /// Maps all IEndpointModule implementations from the specified assembly.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="assembly">The assembly to scan for endpoint modules.</param>
    /// <param name="prefix">Optional route prefix (e.g., "/api"). Defaults to no prefix.</param>
    public static IEndpointRouteBuilder MapEndpointModules(
        this IEndpointRouteBuilder app,
        Assembly assembly,
        string prefix = "")
    {
        return app.MapEndpointModules([assembly], prefix);
    }

    /// <summary>
    /// Maps all IEndpointModule implementations from the specified assemblies.
    /// Modules can have constructor dependencies resolved from DI.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="assemblies">The assemblies to scan for endpoint modules.</param>
    /// <param name="prefix">Optional route prefix (e.g., "/api"). Defaults to no prefix.</param>
    public static IEndpointRouteBuilder MapEndpointModules(
        this IEndpointRouteBuilder app,
        Assembly[] assemblies,
        string prefix = "")
    {
        var group = app.MapGroup(prefix);

        // Find and instantiate all endpoint modules
        var moduleTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IEndpointModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in moduleTypes)
        {
            var module = (IEndpointModule)ActivatorUtilities.CreateInstance(app.ServiceProvider, type);
            module.AddRoutes(group);
        }

        return app;
    }
}
