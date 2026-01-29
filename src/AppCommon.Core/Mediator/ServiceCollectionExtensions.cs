using System.Reflection;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AppCommon.Core.Mediator;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator and all handlers from the specified assemblies.
    /// </summary>
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        Guard.IsNotNull(services);
        Guard.IsNotNull(assemblies);
        Guard.HasSizeGreaterThan(assemblies, 0);

        // Register mediator
        services.AddScoped<IMediator, Mediator>();

        // Register all handlers from assemblies
        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));

            foreach (var handlerType in handlerTypes)
            {
                var handlerInterface = handlerType.GetInterfaces()
                    .First(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
                services.AddScoped(handlerInterface, handlerType);
            }
        }

        return services;
    }

    /// <summary>
    /// Registers an open generic pipeline behavior for all request types.
    /// Order matters: first registered = outermost (executes first).
    /// </summary>
    public static IServiceCollection AddPipelineBehavior(
        this IServiceCollection services,
        Type behaviorType)
    {
        Guard.IsNotNull(services);
        Guard.IsNotNull(behaviorType);

        services.AddScoped(typeof(IPipelineBehavior<,>), behaviorType);
        return services;
    }
}
