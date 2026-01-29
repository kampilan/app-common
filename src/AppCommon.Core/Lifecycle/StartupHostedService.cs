using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppCommon.Core.Lifecycle;

/// <summary>
/// Hosted service that discovers and starts all <see cref="IRequiresStart"/> implementations
/// during application startup.
/// </summary>
/// <remarks>
/// Fail-fast by design: if any startup service fails, the application will not start.
/// This ensures the app never runs in a degraded state with missing dependencies.
/// </remarks>
public class StartupHostedService : IHostedService
{
    private readonly IEnumerable<IRequiresStart> _startables;
    private readonly ILogger<StartupHostedService> _logger;

    public StartupHostedService(
        IEnumerable<IRequiresStart> startables,
        ILogger<StartupHostedService> logger)
    {
        _startables = startables;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var startables = _startables.ToList();

        if (startables.Count == 0)
        {
            _logger.LogDebug("No IRequiresStart services registered");
            return;
        }

        _logger.LogInformation("Starting {Count} services that require initialization", startables.Count);

        foreach (var startable in startables)
        {
            var typeName = startable.GetType().Name;

            try
            {
                _logger.LogDebug("Starting {ServiceName}", typeName);
                await startable.StartAsync(cancellationToken);
                _logger.LogDebug("Started {ServiceName}", typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start {ServiceName}", typeName);
                throw;
            }
        }

        _logger.LogInformation("All startup services initialized");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
