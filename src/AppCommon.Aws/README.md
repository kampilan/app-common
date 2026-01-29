# AppCommon.Aws

AWS SDK integrations and service utilities for .NET applications.

## Features

- **DI Extensions** - Register AWS clients with Microsoft.Extensions.DependencyInjection
- **EC2 Metadata** - Detect if running on EC2, access instance metadata with fallback defaults
- **Credential Support** - Local profile credentials or EC2 instance roles

## Quick Start

```csharp
// Basic AWS setup with EC2 metadata detection
services.AddStartupServices(); // From AppCommon.Core
services.AddAws(options =>
{
    options.DefaultRegion = "us-west-2";
    options.DefaultInstanceId = "local-dev";
});

// Add AWS service clients
services.AddS3Client();
services.AddSqsClient();
services.AddDynamoDbClient("myapp_");

// Access EC2 metadata
public class MyService(IInstanceMetadata metadata)
{
    public void DoWork()
    {
        if (metadata.IsRunningOnEc2)
            Console.WriteLine($"Running on {metadata.InstanceId} in {metadata.Region}");
    }
}
```

## Key Types

| Type | Purpose |
|------|---------|
| `AddAws()` | Core AWS setup with metadata service |
| `AddS3Client()` | Register `IAmazonS3` |
| `AddSqsClient()` | Register `IAmazonSQS` |
| `AddStsClient()` | Register `IAmazonSecurityTokenService` |
| `AddDynamoDbClient()` | Register `IAmazonDynamoDB` and `IDynamoDBContext` |
| `IInstanceMetadata` | EC2 instance metadata access |

## Local Development

Use AWS profile credentials:

```csharp
services.AddAws(options =>
{
    options.ProfileName = "my-dev-profile";
    options.DefaultRegion = "us-west-2";
});
```

## Documentation

See [full documentation](https://github.com/kampilan/app-common/blob/main/PRD.md) for detailed usage.
