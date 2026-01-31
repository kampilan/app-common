using AppCommon.Core.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppCommon.Api.Context;

/// <summary>
/// Extension methods for registering <see cref="IRequestContext"/>.
/// </summary>
public static class RequestContextExtensions
{
    /// <summary>
    /// Registers <see cref="IRequestContext"/> with the dependency injection container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This registers <see cref="RequestContext"/> as the implementation, which reads user
    /// information from <c>HttpContext.User</c>. Any authentication handler that populates
    /// the <c>ClaimsPrincipal</c> (JWT, cookies, gateway tokens, etc.) will automatically
    /// be reflected in <see cref="IRequestContext"/>.
    /// </para>
    /// <para>
    /// <b>Components that use IRequestContext:</b>
    /// <list type="bullet">
    /// <item><description><c>LoggingBehavior</c> - includes Subject in all mediator request logs</description></item>
    /// <item><description><c>AuditSaveChangesInterceptor</c> - records who made changes and correlates entries</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method also registers <c>IHttpContextAccessor</c> if not already registered.
    /// Uses <c>TryAddScoped</c>, so it's safe to call multiple times.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRequestContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddScoped<IRequestContext, RequestContext>();
        return services;
    }
}
