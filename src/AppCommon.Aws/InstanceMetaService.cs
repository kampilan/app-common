using Amazon.Util;

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


internal class InstanceMetaService: IInstanceMetadata
{

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(1);
    
    public string DefaultInstanceId { get; init; } = string.Empty;
    public string DefaultRegion { get; init; } = string.Empty;    
    public string DefaultUserData { get; init; } = string.Empty;    
    
    
    private bool _isRunningOnEc2 = true;
    
    public Task Start()
    {

        
        var tokenSource = new CancellationTokenSource();
        CancellationToken ct = tokenSource.Token;        
        
        var metaTask = Task.Run(() =>
        {
            try
            {

                var instanceId = EC2InstanceMetadata.InstanceId;
                if( string.IsNullOrWhiteSpace(instanceId) )
                    _isRunningOnEc2 = false;
                
            }
            catch
            {
                _isRunningOnEc2 = false;
            }
            
        }, ct);

        
        // *************************************************
        var delayTask = Task.Delay(Timeout, ct);

        var index = Task.WaitAny( metaTask, delayTask );
        if( index == 1)
            _isRunningOnEc2 = false;


        
        tokenSource.Cancel();        

        
        // *************************************************
        return Task.CompletedTask;
        
    }

    public bool IsRunningOnEc2 => _isRunningOnEc2;
    
    public string InstanceId => _isRunningOnEc2 ? EC2InstanceMetadata.InstanceId : DefaultInstanceId;
    public string Region     => _isRunningOnEc2 ? EC2InstanceMetadata.Region.SystemName : DefaultRegion;
    public string AvailabilityZone => _isRunningOnEc2 ? EC2InstanceMetadata.AvailabilityZone : string.Empty;   
    public string UserData   => _isRunningOnEc2 ? EC2InstanceMetadata.UserData: DefaultUserData;     

    bool IInstanceMetadata.IsRunningOnEc2 => IsRunningOnEc2;
    string IInstanceMetadata.InstanceId => InstanceId;
    string IInstanceMetadata.Region => Region;
    string IInstanceMetadata.AvailabilityZone => AvailabilityZone;    
    string IInstanceMetadata.UserData => UserData;
    
}