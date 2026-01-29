namespace AppCommon.Core.Mediator;

/// <summary>
/// Delegate representing the next step in the pipeline (another behavior or the handler).
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Pipeline behavior for cross-cutting concerns (logging, validation, etc.).
/// Behaviors wrap handler execution using a delegate chain pattern.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default);
}
