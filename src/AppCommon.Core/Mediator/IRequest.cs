namespace AppCommon.Core.Mediator;

/// <summary>
/// Unified marker interface for requests with a response type.
/// Works for both commands and queries.
/// </summary>
public interface IRequest<TResponse> { }

/// <summary>
/// Convenience alias for commands to preserve semantic intent.
/// </summary>
public interface ICommand<TResponse> : IRequest<TResponse> { }

/// <summary>
/// Convenience alias for queries to preserve semantic intent.
/// </summary>
public interface IQuery<TResponse> : IRequest<TResponse> { }
