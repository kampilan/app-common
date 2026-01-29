namespace AppCommon.Core.Mediator;

/// <summary>
/// Mediator interface for sending requests through the pipeline to handlers.
/// </summary>
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
