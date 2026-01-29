using AppCommon.Api.Lifecycle;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AppCommon.Api.Tests.Lifecycle;

public class AppLifecycleServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ILogger<AppLifecycleService> _logger;
    private readonly TestHostApplicationLifetime _lifetime;

    public AppLifecycleServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"AppLifecycleTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);

        _lifetime = new TestHostApplicationLifetime();
        _logger = Substitute.For<ILogger<AppLifecycleService>>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    [Fact]
    public async Task StartAsync_CleansUpStaleFlags()
    {
        // Arrange - create stale flags
        File.WriteAllText(Path.Combine(_tempDirectory, "started.flag"), "stale");
        File.WriteAllText(Path.Combine(_tempDirectory, "stopped.flag"), "stale");
        File.WriteAllText(Path.Combine(_tempDirectory, "muststop.flag"), "stale");

        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        File.Exists(Path.Combine(_tempDirectory, "started.flag")).ShouldBeFalse();
        File.Exists(Path.Combine(_tempDirectory, "stopped.flag")).ShouldBeFalse();
        File.Exists(Path.Combine(_tempDirectory, "muststop.flag")).ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_ReturnsCompletedTask()
    {
        // Arrange
        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);

        // Act
        var task = service.StartAsync(CancellationToken.None);

        // Assert
        task.IsCompleted.ShouldBeTrue();
        await task; // Should not throw
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        // Arrange
        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);
        await service.StartAsync(CancellationToken.None);

        // Act
        var task = service.StopAsync(CancellationToken.None);

        // Assert
        task.IsCompleted.ShouldBeTrue();
        await task; // Should not throw
    }

    [Fact]
    public async Task OnApplicationStarted_CreatesStartedFlag()
    {
        // Arrange
        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);
        await service.StartAsync(CancellationToken.None);

        // Act - trigger ApplicationStarted
        _lifetime.TriggerStarted();

        // Assert
        var flagPath = Path.Combine(_tempDirectory, "started.flag");
        File.Exists(flagPath).ShouldBeTrue();
    }

    [Fact]
    public async Task OnApplicationStarted_LogsInformation()
    {
        // Arrange
        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);
        await service.StartAsync(CancellationToken.None);

        // Act
        _lifetime.TriggerStarted();

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task OnApplicationStopped_CreatesStoppedFlag()
    {
        // Arrange
        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);
        await service.StartAsync(CancellationToken.None);

        // Act - trigger ApplicationStopped
        _lifetime.TriggerStopped();

        // Assert
        var flagPath = Path.Combine(_tempDirectory, "stopped.flag");
        File.Exists(flagPath).ShouldBeTrue();
    }

    [Fact]
    public async Task OnApplicationStopped_LogsInformation()
    {
        // Arrange
        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);
        await service.StartAsync(CancellationToken.None);

        // Act
        _lifetime.TriggerStopped();

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task MustStopFlagCreated_InitiatesGracefulShutdown()
    {
        // Arrange
        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);
        await service.StartAsync(CancellationToken.None);

        // Act - create muststop.flag
        var flagPath = Path.Combine(_tempDirectory, "muststop.flag");
        await File.WriteAllTextAsync(flagPath, DateTime.UtcNow.ToString("O"));

        // Wait for FileSystemWatcher to detect the change
        await Task.Delay(500);

        // Assert
        _lifetime.StopApplicationCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task MustStopFlagCreated_LogsInformation()
    {
        // Arrange
        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);
        await service.StartAsync(CancellationToken.None);

        // Act - create muststop.flag
        var flagPath = Path.Combine(_tempDirectory, "muststop.flag");
        await File.WriteAllTextAsync(flagPath, DateTime.UtcNow.ToString("O"));

        // Wait for FileSystemWatcher to detect the change
        await Task.Delay(500);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Dispose_StopsWatchingForMustStopFlag()
    {
        // Arrange
        var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);
        await service.StartAsync(CancellationToken.None);

        // Act
        service.Dispose();

        // Create muststop.flag after dispose - should NOT trigger shutdown
        var flagPath = Path.Combine(_tempDirectory, "muststop.flag");
        await File.WriteAllTextAsync(flagPath, DateTime.UtcNow.ToString("O"));
        await Task.Delay(200);

        // Assert - StopApplication should not have been called
        _lifetime.StopApplicationCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task StopAsync_StopsWatchingForMustStopFlag()
    {
        // Arrange
        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);
        await service.StartAsync(CancellationToken.None);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Create muststop.flag after stop - should NOT trigger shutdown
        var flagPath = Path.Combine(_tempDirectory, "muststop.flag");
        await File.WriteAllTextAsync(flagPath, DateTime.UtcNow.ToString("O"));
        await Task.Delay(200);

        // Assert - StopApplication should not have been called
        _lifetime.StopApplicationCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task StartedFlag_ContainsIso8601Timestamp()
    {
        // Arrange
        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);
        await service.StartAsync(CancellationToken.None);

        var beforeCreate = DateTime.UtcNow;

        // Act
        _lifetime.TriggerStarted();

        // Assert
        var flagPath = Path.Combine(_tempDirectory, "started.flag");
        var content = await File.ReadAllTextAsync(flagPath);
        var timestamp = DateTime.Parse(content, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();
        timestamp.ShouldBeGreaterThanOrEqualTo(beforeCreate.AddSeconds(-1));
        timestamp.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Constructor_WithNullFlagDirectory_UsesAppContextBaseDirectory()
    {
        // Act
        using var service = new AppLifecycleService(_lifetime, _logger, null);

        // Assert - service should be created without throwing
        service.ShouldNotBeNull();
    }

    [Fact]
    public async Task CleanupStaleFlags_DoesNotThrow_WhenFlagsDoNotExist()
    {
        // Arrange - no flags exist
        using var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);

        // Act & Assert - should not throw
        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task MultipleDisposes_DoNotThrow()
    {
        // Arrange
        var service = new AppLifecycleService(_lifetime, _logger, _tempDirectory);
        await service.StartAsync(CancellationToken.None);

        // Act & Assert - should not throw
        service.Dispose();
        service.Dispose();
    }

    /// <summary>
    /// Test implementation of IHostApplicationLifetime that allows triggering lifecycle events.
    /// </summary>
    private class TestHostApplicationLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource _startedSource = new();
        private readonly CancellationTokenSource _stoppingSource = new();
        private readonly CancellationTokenSource _stoppedSource = new();

        public CancellationToken ApplicationStarted => _startedSource.Token;
        public CancellationToken ApplicationStopping => _stoppingSource.Token;
        public CancellationToken ApplicationStopped => _stoppedSource.Token;

        public bool StopApplicationCalled { get; private set; }

        public void StopApplication()
        {
            StopApplicationCalled = true;
        }

        public void TriggerStarted() => _startedSource.Cancel();
        public void TriggerStopping() => _stoppingSource.Cancel();
        public void TriggerStopped() => _stoppedSource.Cancel();
    }
}
