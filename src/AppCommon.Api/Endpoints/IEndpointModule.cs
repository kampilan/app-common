using Microsoft.AspNetCore.Routing;

namespace AppCommon.Api.Endpoints;

/// <summary>
/// Interface for endpoint modules that can be auto-discovered and registered.
/// Each feature's Endpoint class implements this to colocate routing with handlers.
/// </summary>
public interface IEndpointModule
{
    /// <summary>
    /// Adds routes to the endpoint route builder.
    /// The builder is already scoped to /api prefix.
    /// </summary>
    void AddRoutes(IEndpointRouteBuilder app);
}
