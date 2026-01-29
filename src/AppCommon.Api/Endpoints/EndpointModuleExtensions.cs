using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AppCommon.Api.Endpoints;

public static class EndpointModuleExtensions
{
    /// <summary>
    /// Maps all IEndpointModule implementations from the specified assemblies.
    /// Routes are automatically prefixed with /api.
    /// </summary>
    public static IEndpointRouteBuilder MapEndpointModules(
        this IEndpointRouteBuilder app,
        Assembly assembly,
        string prefix = "/api")
    {
        return app.MapEndpointModules([assembly], prefix);
    }

    /// <summary>
    /// Maps all IEndpointModule implementations from the specified assemblies.
    /// Routes are automatically prefixed with /api.
    /// </summary>
    public static IEndpointRouteBuilder MapEndpointModules(
        this IEndpointRouteBuilder app,
        Assembly[] assemblies,
        string prefix = "/api")
    {
        // Create route group with /api prefix
        var group = app.MapGroup(prefix);

        // Find and instantiate all endpoint modules
        var moduleTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IEndpointModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in moduleTypes)
        {
            var module = (IEndpointModule)Activator.CreateInstance(type)!;
            module.AddRoutes(group);
        }

        return app;
    }
}
