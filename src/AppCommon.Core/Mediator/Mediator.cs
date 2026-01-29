using System.Collections.Concurrent;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AppCommon.Core.Mediator;

/// <summary>
/// Default mediator implementation that routes requests through pipeline behaviors to handlers.
/// Uses cached handler wrappers to avoid reflection on every request.
/// </summary>
public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> HandlerWrappers = new();

    public Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(request);

        var requestType = request.GetType();

        var wrapper = HandlerWrappers.GetOrAdd(
            requestType,
            static type =>
            {
                var requestInterface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                    ?? throw new InvalidOperationException($"Type {type.Name} does not implement IRequest<TResponse>");

                var responseType = requestInterface.GetGenericArguments()[0];

                var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(type, responseType);
                return (RequestHandlerBase)Activator.CreateInstance(wrapperType)!;
            });

        return wrapper.HandleAsync<TResponse>(request, serviceProvider, cancellationToken);
    }
}

/// <summary>
/// Base class for handler wrappers - enables caching without knowing generic types.
/// </summary>
internal abstract class RequestHandlerBase
{
    public abstract Task<TResponse> HandleAsync<TResponse>(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

/// <summary>
/// Typed wrapper that handles pipeline construction and execution without reflection.
/// One instance is cached per request type.
/// </summary>
internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerBase
    where TRequest : IRequest<TResponse>
{
    public override Task<TResult> HandleAsync<TResult>(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        return (Task<TResult>)(object)HandleAsync((TRequest)request, serviceProvider, cancellationToken);
    }

    private static Task<TResponse> HandleAsync(
        TRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>()
            ?? throw new InvalidOperationException($"No handler registered for {typeof(TRequest).Name}");

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();

        // Build pipeline: behaviors wrap handler
        RequestHandlerDelegate<TResponse> pipeline = () => handler.HandleAsync(request, cancellationToken);

        // Wrap behaviors from innermost to outermost
        foreach (var behavior in behaviors.Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(request, next, cancellationToken);
        }

        return pipeline();
    }
}
