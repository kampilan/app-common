using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppCommon.Core.Lifecycle;

/// <summary>
/// Hosted service for Fabrica.One lifecycle management using flag files.
/// The orchestrator communicates through three flag files in the application directory:
/// - started.flag: Created by app when fully started
/// - muststop.flag: Created by Fabrica.One to signal graceful shutdown
/// - stopped.flag: Created by app when fully stopped
/// </summary>
public class AppLifecycleService : IHostedService, IDisposable
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<AppLifecycleService> _logger;
    private readonly string _flagDirectory;
    private FileSystemWatcher? _watcher;

    private const string StartedFlag = "started.flag";
    private const string MustStopFlag = "muststop.flag";
    private const string StoppedFlag = "stopped.flag";

    public AppLifecycleService(
        IHostApplicationLifetime lifetime,
        ILogger<AppLifecycleService> logger,
        string? flagDirectory = null)
    {
        _lifetime = lifetime;
        _logger = logger;
        _flagDirectory = flagDirectory ?? AppContext.BaseDirectory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Clean up stale flags from previous runs
        CleanupStaleFlags();

        // Register lifecycle callbacks
        _lifetime.ApplicationStarted.Register(OnStarted);
        _lifetime.ApplicationStopped.Register(OnStopped);

        // Start watching for muststop.flag
        StartWatching();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopWatching();
        return Task.CompletedTask;
    }

    private void CleanupStaleFlags()
    {
        // Clean up all stale flags from previous runs
        DeleteFlag(StartedFlag);
        DeleteFlag(StoppedFlag);
        DeleteFlag(MustStopFlag);
    }

    private void OnStarted()
    {
        CreateFlag(StartedFlag);
        _logger.LogInformation("Application started - created {Flag}", StartedFlag);
    }

    private void OnStopped()
    {
        CreateFlag(StoppedFlag);
        _logger.LogInformation("Application stopped - created {Flag}", StoppedFlag);
    }

    private void StartWatching()
    {
        // Dispose existing watcher if StartWatching called multiple times
        _watcher?.Dispose();

        _watcher = new FileSystemWatcher(_flagDirectory, MustStopFlag)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
        };
        _watcher.Created += OnMustStopCreated;
        _watcher.EnableRaisingEvents = true;
    }

    private void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
    }

    private void OnMustStopCreated(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Detected {Flag} - initiating graceful shutdown", MustStopFlag);
        _lifetime.StopApplication();
    }

    private void CreateFlag(string flagName)
    {
        var path = Path.Combine(_flagDirectory, flagName);
        File.WriteAllText(path, DateTime.UtcNow.ToString("O"));
    }

    private void DeleteFlag(string flagName)
    {
        var path = Path.Combine(_flagDirectory, flagName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
