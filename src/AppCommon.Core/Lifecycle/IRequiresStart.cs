namespace AppCommon.Core.Lifecycle;

/// <summary>
/// Interface for services that require initialization at application startup.
/// Implementations are discovered and started by <see cref="StartupHostedService"/>.
/// </summary>
public interface IRequiresStart
{
    /// <summary>
    /// Called during application startup to initialize the service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the startup operation.</param>
    /// <returns>A task representing the initialization operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);
}
