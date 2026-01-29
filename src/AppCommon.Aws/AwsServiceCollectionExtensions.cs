using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.SecurityToken;
using Amazon.SQS;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMember.Global

namespace AppCommon.Aws;

public class AwsOptions
{
    public string? ProfileName { get; set; }
    public TimeSpan MetadataTimeout { get; set; } = TimeSpan.FromSeconds(1);
    public string DefaultInstanceId { get; set; } = string.Empty;
    public string DefaultRegion { get; set; } = string.Empty;
    public string DefaultUserData { get; set; } = string.Empty;
}

public static class AwsServiceCollectionExtensions
{
    public static IServiceCollection AddAws(this IServiceCollection services, Action<AwsOptions>? configure = null)
    {
        var options = new AwsOptions();
        configure?.Invoke(options);

        services.AddSingleton<IInstanceMetadata>(_ =>
        {
            var service = new InstanceMetaService
            {
                DefaultInstanceId = options.DefaultInstanceId,
                DefaultRegion = options.DefaultRegion,
                DefaultUserData = options.DefaultUserData,
                Timeout = options.MetadataTimeout
            };

            return service;
        });

        if (!string.IsNullOrWhiteSpace(options.ProfileName))
        {
            var sharedFile = new SharedCredentialsFile();
            if (!(sharedFile.TryGetProfile(options.ProfileName, out var profile) &&
                  AWSCredentialsFactory.TryGetAWSCredentials(profile, sharedFile, out var credentials)))
                throw new InvalidOperationException($"Local profile '{options.ProfileName}' could not be loaded");

            AWSConfigs.AWSRegion = profile.Region.SystemName;

            services.AddSingleton(credentials);
        }

        return services;
    }

    public static IServiceCollection AddS3Client(this IServiceCollection services, string? regionName = null)
    {
        services.AddSingleton<IAmazonS3>(sp =>
        {
            RegionEndpoint? region = null;
            if (!string.IsNullOrWhiteSpace(regionName))
                region = RegionEndpoint.GetBySystemName(regionName);

            var credentials = sp.GetService<AWSCredentials>();

            if (credentials is not null && region is not null)
                return new AmazonS3Client(credentials, region);

            if (credentials is not null)
                return new AmazonS3Client(credentials);

            if (region is not null)
                return new AmazonS3Client(region);

            return new AmazonS3Client();
        });

        return services;
    }

    public static IServiceCollection AddSqsClient(this IServiceCollection services, string? regionName = null)
    {
        services.AddSingleton<IAmazonSQS>(sp =>
        {
            RegionEndpoint? region = null;
            if (!string.IsNullOrWhiteSpace(regionName))
                region = RegionEndpoint.GetBySystemName(regionName);

            var credentials = sp.GetService<AWSCredentials>();

            if (credentials is not null && region is not null)
                return new AmazonSQSClient(credentials, region);

            if (credentials is not null)
                return new AmazonSQSClient(credentials);

            if (region is not null)
                return new AmazonSQSClient(region);

            return new AmazonSQSClient();
        });

        return services;
    }

    public static IServiceCollection AddStsClient(this IServiceCollection services, string? regionName = null)
    {
        services.AddSingleton<IAmazonSecurityTokenService>(sp =>
        {
            RegionEndpoint? region = null;
            if (!string.IsNullOrWhiteSpace(regionName))
                region = RegionEndpoint.GetBySystemName(regionName);

            var credentials = sp.GetService<AWSCredentials>();

            if (credentials is not null && region is not null)
                return new AmazonSecurityTokenServiceClient(credentials, region);

            if (credentials is not null)
                return new AmazonSecurityTokenServiceClient(credentials);

            if (region is not null)
                return new AmazonSecurityTokenServiceClient(region);

            return new AmazonSecurityTokenServiceClient();
        });

        return services;
    }

    public static IServiceCollection AddStsClient(this IServiceCollection services, string accessKey, string secretKey, string? regionName = null)
    {
        Guard.IsNotNullOrWhiteSpace(accessKey);
        Guard.IsNotNullOrWhiteSpace(secretKey);

        services.AddSingleton<IAmazonSecurityTokenService>(_ =>
        {
            RegionEndpoint? region = null;
            if (!string.IsNullOrWhiteSpace(regionName))
                region = RegionEndpoint.GetBySystemName(regionName);

            var credentials = new BasicAWSCredentials(accessKey, secretKey);

            if (region is not null)
                return new AmazonSecurityTokenServiceClient(credentials, region);

            return new AmazonSecurityTokenServiceClient(credentials);
        });

        return services;
    }

    public static IServiceCollection AddDynamoDbClient(this IServiceCollection services, string tablePrefix = "")
    {
        services.AddSingleton<IAmazonDynamoDB>(sp =>
        {
            var credentials = sp.GetService<AWSCredentials>();

            if (credentials is not null)
                return new AmazonDynamoDBClient(credentials);

            return new AmazonDynamoDBClient();
        });

        services.AddScoped<IDynamoDBContext>(sp =>
        {
            var db = sp.GetRequiredService<IAmazonDynamoDB>();
            var context = new DynamoDBContextBuilder()
                .WithDynamoDBClient(() => db)
                .ConfigureContext(cfg =>
                {
                    cfg.TableNamePrefix = tablePrefix;
                    cfg.IsEmptyStringValueEnabled = true;
                })
                .Build();

            return context;
        });

        return services;
    }
}
