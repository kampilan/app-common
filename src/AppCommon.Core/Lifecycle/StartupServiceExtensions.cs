using Microsoft.Extensions.DependencyInjection;

namespace AppCommon.Core.Lifecycle;

/// <summary>
/// Extension methods for registering startup services.
/// </summary>
public static class StartupServiceExtensions
{
    /// <summary>
    /// Adds the <see cref="StartupHostedService"/> which discovers and initializes
    /// all registered <see cref="IRequiresStart"/> implementations at application startup.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStartupServices(this IServiceCollection services)
    {
        services.AddHostedService<StartupHostedService>();
        return services;
    }

    /// <summary>
    /// Registers a service that implements <see cref="IRequiresStart"/> as both its
    /// concrete type and as an <see cref="IRequiresStart"/> for automatic startup.
    /// </summary>
    /// <typeparam name="TService">The service type (must implement IRequiresStart).</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStartable<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService, IRequiresStart
    {
        services.AddSingleton<TImplementation>();
        services.AddSingleton<TService>(sp => sp.GetRequiredService<TImplementation>());
        services.AddSingleton<IRequiresStart>(sp => sp.GetRequiredService<TImplementation>());
        return services;
    }

    /// <summary>
    /// Registers a service that implements <see cref="IRequiresStart"/> for automatic startup.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStartable<TImplementation>(this IServiceCollection services)
        where TImplementation : class, IRequiresStart
    {
        services.AddSingleton<TImplementation>();
        services.AddSingleton<IRequiresStart>(sp => sp.GetRequiredService<TImplementation>());
        return services;
    }
}
