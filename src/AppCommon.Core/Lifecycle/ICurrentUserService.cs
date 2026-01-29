namespace AppCommon.Core.Lifecycle;

/// <summary>
/// Provides access to the current user's identity information.
/// Implementations should be scoped to the current request/operation context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's unique identifier, or null if not authenticated.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
