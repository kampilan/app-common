using AppCommon.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AppCommon.Core.Tests.Configuration;

public class ServiceModuleTests
{
    #region Test Module Classes

    private class DatabaseModule : IServiceModule
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int MaxRetries { get; set; }

        public void Build(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(new DatabaseSettings(ConnectionString, MaxRetries));
        }
    }

    private record DatabaseSettings(string ConnectionString, int MaxRetries);

    private class MultiPropertyModule : IServiceModule
    {
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public int Timeout { get; set; }
        public string[] Tags { get; set; } = [];

        public void Build(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(new ModuleSnapshot(Name, Enabled, Timeout, Tags));
        }
    }

    private record ModuleSnapshot(string Name, bool Enabled, int Timeout, string[] Tags);

    private class NestedConfigModule : IServiceModule
    {
        public string ServiceName { get; set; } = string.Empty;
        public RetryOptions Retry { get; set; } = new();

        public void Build(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(new NestedSnapshot(ServiceName, Retry.MaxAttempts, Retry.DelayMs));
        }
    }

    private class RetryOptions
    {
        public int MaxAttempts { get; set; }
        public int DelayMs { get; set; }
    }

    private record NestedSnapshot(string ServiceName, int MaxAttempts, int DelayMs);

    private class ConfigPassthroughModule : IServiceModule
    {
        public string SectionName { get; set; } = string.Empty;

        public void Build(IServiceCollection services, IConfiguration configuration)
        {
            var value = configuration.GetSection(SectionName)["Key"];
            services.AddSingleton(new PassthroughSnapshot(value));
        }
    }

    private record PassthroughSnapshot(string? Value);

    #endregion

    #region Configuration Binding Tests

    [Fact]
    public void Bind_PopulatesStringAndIntProperties()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Server=localhost;Database=test",
                ["MaxRetries"] = "5"
            })
            .Build();

        var module = configuration.Get<DatabaseModule>()!;

        module.ConnectionString.ShouldBe("Server=localhost;Database=test");
        module.MaxRetries.ShouldBe(5);
    }

    [Fact]
    public void Bind_PopulatesBoolAndArrayProperties()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Name"] = "TestService",
                ["Enabled"] = "true",
                ["Timeout"] = "30",
                ["Tags:0"] = "api",
                ["Tags:1"] = "production"
            })
            .Build();

        var module = configuration.Get<MultiPropertyModule>()!;

        module.Name.ShouldBe("TestService");
        module.Enabled.ShouldBeTrue();
        module.Timeout.ShouldBe(30);
        module.Tags.ShouldBe(new[] { "api", "production" });
    }

    [Fact]
    public void Bind_PopulatesNestedObjects()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceName"] = "OrderService",
                ["Retry:MaxAttempts"] = "3",
                ["Retry:DelayMs"] = "500"
            })
            .Build();

        var module = configuration.Get<NestedConfigModule>()!;

        module.ServiceName.ShouldBe("OrderService");
        module.Retry.MaxAttempts.ShouldBe(3);
        module.Retry.DelayMs.ShouldBe(500);
    }

    [Fact]
    public void Bind_MissingProperties_UsesDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = ""
            })
            .Build();

        var module = configuration.Get<DatabaseModule>()!;

        module.ConnectionString.ShouldBe(string.Empty);
        module.MaxRetries.ShouldBe(0);
    }

    #endregion

    #region Build Tests

    [Fact]
    public void Build_RegistersServicesUsingBoundProperties()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Server=prod;Database=app",
                ["MaxRetries"] = "3"
            })
            .Build();

        var module = configuration.Get<DatabaseModule>()!;
        var services = new ServiceCollection();

        module.Build(services, configuration);

        var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<DatabaseSettings>();
        settings.ConnectionString.ShouldBe("Server=prod;Database=app");
        settings.MaxRetries.ShouldBe(3);
    }

    [Fact]
    public void Build_CanUseConfigurationParameterForAdditionalLookups()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SectionName"] = "MySection",
                ["MySection:Key"] = "the-value"
            })
            .Build();

        var module = configuration.Get<ConfigPassthroughModule>()!;
        var services = new ServiceCollection();

        module.Build(services, configuration);

        var provider = services.BuildServiceProvider();
        var snapshot = provider.GetRequiredService<PassthroughSnapshot>();
        snapshot.Value.ShouldBe("the-value");
    }

    [Fact]
    public void Build_WithNestedConfig_RegistersCorrectValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceName"] = "PaymentService",
                ["Retry:MaxAttempts"] = "5",
                ["Retry:DelayMs"] = "1000"
            })
            .Build();

        var module = configuration.Get<NestedConfigModule>()!;
        var services = new ServiceCollection();

        module.Build(services, configuration);

        var provider = services.BuildServiceProvider();
        var snapshot = provider.GetRequiredService<NestedSnapshot>();
        snapshot.ServiceName.ShouldBe("PaymentService");
        snapshot.MaxAttempts.ShouldBe(5);
        snapshot.DelayMs.ShouldBe(1000);
    }

    #endregion

    #region AddServiceModule Extension Tests

    [Fact]
    public void AddServiceModule_BindsAndBuilds()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Server=prod;Database=app",
                ["MaxRetries"] = "3"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddServiceModule<DatabaseModule>(configuration);

        var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<DatabaseSettings>();
        settings.ConnectionString.ShouldBe("Server=prod;Database=app");
        settings.MaxRetries.ShouldBe(3);
    }

    [Fact]
    public void AddServiceModule_WithEmptyConfig_UsesDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();

        services.AddServiceModule<DatabaseModule>(configuration);

        var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<DatabaseSettings>();
        settings.ConnectionString.ShouldBe(string.Empty);
        settings.MaxRetries.ShouldBe(0);
    }

    [Fact]
    public void AddServiceModule_ReturnsServiceCollectionForChaining()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Server=localhost"
            })
            .Build();

        var services = new ServiceCollection();

        var result = services.AddServiceModule<DatabaseModule>(configuration);

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddServiceModule_WithConfigure_AppliesOverrides()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Server=from-config",
                ["MaxRetries"] = "1"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddServiceModule<DatabaseModule>(configuration, module =>
        {
            module.ConnectionString = "Server=from-override";
        });

        var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<DatabaseSettings>();
        settings.ConnectionString.ShouldBe("Server=from-override");
        settings.MaxRetries.ShouldBe(1);
    }

    [Fact]
    public void AddServiceModule_WithConfigure_OverridesOnlySpecifiedProperties()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Server=from-config",
                ["MaxRetries"] = "7"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddServiceModule<DatabaseModule>(configuration, module =>
        {
            module.MaxRetries = 99;
        });

        var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<DatabaseSettings>();
        settings.ConnectionString.ShouldBe("Server=from-config");
        settings.MaxRetries.ShouldBe(99);
    }

    [Fact]
    public void AddServiceModule_WithConfigure_ReturnsServiceCollectionForChaining()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Server=localhost"
            })
            .Build();

        var services = new ServiceCollection();

        var result = services.AddServiceModule<DatabaseModule>(configuration, _ => { });

        result.ShouldBeSameAs(services);
    }

    #endregion
}
