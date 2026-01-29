using AppCommon.Aws;
using AppCommon.Core.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AppCommon.Aws.Tests;

public class AwsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAws_RegistersInstanceMetadata()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAws();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IInstanceMetadata));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddAws_RegistersIRequiresStart()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAws();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRequiresStart));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public async Task AddAws_WithOptions_ConfiguresMetadataDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAws(options =>
        {
            options.DefaultInstanceId = "test-instance";
            options.DefaultRegion = "us-west-2";
            options.DefaultUserData = "test-data";
            options.MetadataTimeout = TimeSpan.FromMilliseconds(100);
        });

        var provider = services.BuildServiceProvider();
        var metadata = provider.GetRequiredService<IInstanceMetadata>();

        // Trigger startup to set IsRunningOnEc2 = false (will timeout since not on EC2)
        var startable = provider.GetRequiredService<IRequiresStart>();
        await startable.StartAsync();

        // Assert - defaults should be used when not on EC2
        metadata.InstanceId.ShouldBe("test-instance");
        metadata.Region.ShouldBe("us-west-2");
        metadata.UserData.ShouldBe("test-data");
    }

    [Fact]
    public void AddAws_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddAws();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddS3Client_RegistersIAmazonS3()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddS3Client();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Amazon.S3.IAmazonS3));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddS3Client_WithRegion_RegistersIAmazonS3()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddS3Client("us-east-1");

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Amazon.S3.IAmazonS3));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddS3Client_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddS3Client();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddSqsClient_RegistersIAmazonSQS()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqsClient();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Amazon.SQS.IAmazonSQS));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddSqsClient_WithRegion_RegistersIAmazonSQS()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqsClient("eu-west-1");

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Amazon.SQS.IAmazonSQS));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddSqsClient_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddSqsClient();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddStsClient_RegistersIAmazonSecurityTokenService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStsClient();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Amazon.SecurityToken.IAmazonSecurityTokenService));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddStsClient_WithRegion_RegistersIAmazonSecurityTokenService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStsClient("ap-southeast-1");

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Amazon.SecurityToken.IAmazonSecurityTokenService));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddStsClient_WithCredentials_RegistersIAmazonSecurityTokenService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStsClient("accessKey", "secretKey");

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Amazon.SecurityToken.IAmazonSecurityTokenService));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddStsClient_WithCredentialsAndRegion_RegistersIAmazonSecurityTokenService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStsClient("accessKey", "secretKey", "us-west-2");

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Amazon.SecurityToken.IAmazonSecurityTokenService));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddStsClient_WithNullAccessKey_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() => services.AddStsClient(null!, "secretKey"));
    }

    [Fact]
    public void AddStsClient_WithNullSecretKey_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() => services.AddStsClient("accessKey", null!));
    }

    [Fact]
    public void AddStsClient_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddStsClient();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddDynamoDbClient_RegistersIAmazonDynamoDB()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDynamoDbClient();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Amazon.DynamoDBv2.IAmazonDynamoDB));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddDynamoDbClient_RegistersIDynamoDBContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDynamoDbClient();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Amazon.DynamoDBv2.DataModel.IDynamoDBContext));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddDynamoDbClient_WithTablePrefix_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDynamoDbClient("dev_");

        // Assert
        services.Any(d => d.ServiceType == typeof(Amazon.DynamoDBv2.IAmazonDynamoDB)).ShouldBeTrue();
        services.Any(d => d.ServiceType == typeof(Amazon.DynamoDBv2.DataModel.IDynamoDBContext)).ShouldBeTrue();
    }

    [Fact]
    public void AddDynamoDbClient_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddDynamoDbClient();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AllMethods_CanBeChained()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - should not throw
        services
            .AddAws()
            .AddS3Client()
            .AddSqsClient()
            .AddStsClient()
            .AddDynamoDbClient();

        services.Count.ShouldBeGreaterThan(0);
    }
}
