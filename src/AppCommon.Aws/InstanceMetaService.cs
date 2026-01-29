using Amazon.Util;
using AppCommon.Core.Lifecycle;

// ReSharper disable MemberCanBePrivate.Global

namespace AppCommon.Aws;

public interface IInstanceMetadata
{
    bool IsRunningOnEc2 { get; }

    string InstanceId { get; }
    string Region { get; }
    string AvailabilityZone { get; }
    string UserData { get; }
}


/// <summary>
/// Detects EC2 environment and provides instance metadata.
/// Falls back to default values when not running on EC2.
/// </summary>
/// <remarks>
/// Detection uses a timeout to avoid blocking on non-EC2 environments where the
/// metadata endpoint is unreachable. Any failure (timeout, exception, empty response)
/// indicates non-EC2 environment - this is by design, not an error condition.
/// </remarks>
internal class InstanceMetaService : IInstanceMetadata, IRequiresStart
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(1);

    public string DefaultInstanceId { get; init; } = string.Empty;
    public string DefaultRegion { get; init; } = string.Empty;
    public string DefaultUserData { get; init; } = string.Empty;

    private volatile bool _isRunningOnEc2 = true;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = tokenSource.Token;

        var metaTask = Task.Run(() =>
        {
            try
            {
                var instanceId = EC2InstanceMetadata.InstanceId;
                if (string.IsNullOrWhiteSpace(instanceId))
                    _isRunningOnEc2 = false;
            }
            catch
            {
                // Any exception means we're not on EC2 - this is expected behavior
                _isRunningOnEc2 = false;
            }
        }, ct);

        var delayTask = Task.Delay(Timeout, ct);

        var completedTask = await Task.WhenAny(metaTask, delayTask);
        if (completedTask == delayTask)
            _isRunningOnEc2 = false;

        await tokenSource.CancelAsync();
    }

    public bool IsRunningOnEc2 => _isRunningOnEc2;

    public string InstanceId => _isRunningOnEc2 ? EC2InstanceMetadata.InstanceId : DefaultInstanceId;
    public string Region => _isRunningOnEc2 ? EC2InstanceMetadata.Region.SystemName : DefaultRegion;
    public string AvailabilityZone => _isRunningOnEc2 ? EC2InstanceMetadata.AvailabilityZone : string.Empty;
    public string UserData => _isRunningOnEc2 ? EC2InstanceMetadata.UserData : DefaultUserData;
}
