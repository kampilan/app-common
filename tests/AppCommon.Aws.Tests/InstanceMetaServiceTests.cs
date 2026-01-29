using AppCommon.Aws;
using AppCommon.Core.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AppCommon.Aws.Tests;

public class InstanceMetaServiceTests
{
    [Fact]
    public void IsRunningOnEc2_BeforeStart_IsTrue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAws();
        var provider = services.BuildServiceProvider();

        // Act
        var metadata = provider.GetRequiredService<IInstanceMetadata>();

        // Assert - before StartAsync, assumes running on EC2
        metadata.IsRunningOnEc2.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_WhenNotOnEc2_SetsIsRunningOnEc2ToFalse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAws(options =>
        {
            options.MetadataTimeout = TimeSpan.FromMilliseconds(100); // Short timeout
        });
        var provider = services.BuildServiceProvider();

        var startable = provider.GetRequiredService<IRequiresStart>();
        var metadata = provider.GetRequiredService<IInstanceMetadata>();

        // Act
        await startable.StartAsync();

        // Assert - should timeout and set to false since we're not on EC2
        metadata.IsRunningOnEc2.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_WhenNotOnEc2_ReturnsDefaultInstanceId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAws(options =>
        {
            options.MetadataTimeout = TimeSpan.FromMilliseconds(100);
            options.DefaultInstanceId = "local-dev-instance";
        });
        var provider = services.BuildServiceProvider();

        var startable = provider.GetRequiredService<IRequiresStart>();
        var metadata = provider.GetRequiredService<IInstanceMetadata>();

        // Act
        await startable.StartAsync();

        // Assert
        metadata.InstanceId.ShouldBe("local-dev-instance");
    }

    [Fact]
    public async Task StartAsync_WhenNotOnEc2_ReturnsDefaultRegion()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAws(options =>
        {
            options.MetadataTimeout = TimeSpan.FromMilliseconds(100);
            options.DefaultRegion = "us-west-2";
        });
        var provider = services.BuildServiceProvider();

        var startable = provider.GetRequiredService<IRequiresStart>();
        var metadata = provider.GetRequiredService<IInstanceMetadata>();

        // Act
        await startable.StartAsync();

        // Assert
        metadata.Region.ShouldBe("us-west-2");
    }

    [Fact]
    public async Task StartAsync_WhenNotOnEc2_ReturnsDefaultUserData()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAws(options =>
        {
            options.MetadataTimeout = TimeSpan.FromMilliseconds(100);
            options.DefaultUserData = "my-user-data";
        });
        var provider = services.BuildServiceProvider();

        var startable = provider.GetRequiredService<IRequiresStart>();
        var metadata = provider.GetRequiredService<IInstanceMetadata>();

        // Act
        await startable.StartAsync();

        // Assert
        metadata.UserData.ShouldBe("my-user-data");
    }

    [Fact]
    public async Task StartAsync_WhenNotOnEc2_ReturnsEmptyAvailabilityZone()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAws(options =>
        {
            options.MetadataTimeout = TimeSpan.FromMilliseconds(100);
        });
        var provider = services.BuildServiceProvider();

        var startable = provider.GetRequiredService<IRequiresStart>();
        var metadata = provider.GetRequiredService<IInstanceMetadata>();

        // Act
        await startable.StartAsync();

        // Assert
        metadata.AvailabilityZone.ShouldBeEmpty();
    }

    [Fact]
    public async Task StartAsync_CompletesWithinTimeout()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAws(options =>
        {
            options.MetadataTimeout = TimeSpan.FromMilliseconds(500);
        });
        var provider = services.BuildServiceProvider();

        var startable = provider.GetRequiredService<IRequiresStart>();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await startable.StartAsync();
        stopwatch.Stop();

        // Assert - should complete within reasonable time (timeout + some buffer)
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(2000);
    }

    [Fact]
    public void IInstanceMetadata_And_IRequiresStart_ResolveSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAws();
        var provider = services.BuildServiceProvider();

        // Act
        var metadata = provider.GetRequiredService<IInstanceMetadata>();
        var startable = provider.GetRequiredService<IRequiresStart>();

        // Assert - both should resolve to the same singleton instance
        ReferenceEquals(metadata, startable).ShouldBeTrue();
    }
}
