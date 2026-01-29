using AppCommon.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Shouldly;
using Xunit;

namespace AppCommon.Api.Tests.Configuration;

public class AppConfigurationExtensionsTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _originalBaseDirectory;

    public AppConfigurationExtensionsTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ConfigTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        // Store original and set test directory as base
        _originalBaseDirectory = AppContext.BaseDirectory;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    [Fact]
    public void AddFabricaConfiguration_ReturnsBuilder()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        var result = builder.AddFabricaConfiguration();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void AddFabricaConfiguration_ClearsDefaultSources()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var initialSourceCount = builder.Configuration.Sources.Count;
        initialSourceCount.ShouldBeGreaterThan(0); // Should have default sources

        // Act
        builder.AddFabricaConfiguration();

        // Assert - should have exactly 4 sources (3 JSON + 1 env vars)
        // Note: JSON sources are added even if files don't exist (optional: true)
        builder.Configuration.Sources.Count.ShouldBe(4);
    }

    [Fact]
    public void AddFabricaConfiguration_AddsSourcesInCorrectOrder()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddFabricaConfiguration();

        // Assert - verify source order
        var sources = builder.Configuration.Sources.ToList();
        sources.Count.ShouldBe(4);

        sources[0].ShouldBeOfType<JsonConfigurationSource>();
        sources[1].ShouldBeOfType<JsonConfigurationSource>();
        sources[2].ShouldBeOfType<EnvironmentVariablesConfigurationSource>();
        sources[3].ShouldBeOfType<JsonConfigurationSource>();
    }

    [Fact]
    public void AddFabricaConfiguration_JsonSourcesAreOptional()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act - should not throw even though files don't exist
        builder.AddFabricaConfiguration();
        var config = builder.Build().Configuration;

        // Assert - configuration should be accessible
        config.ShouldNotBeNull();
    }

    [Fact]
    public void AddFabricaConfiguration_LoadsConfigurationJson()
    {
        // Arrange
        var configPath = Path.Combine(AppContext.BaseDirectory, "configuration.json");
        var configCreated = false;

        try
        {
            File.WriteAllText(configPath, """{"TestKey": "from-configuration"}""");
            configCreated = true;

            var builder = WebApplication.CreateBuilder();

            // Act
            builder.AddFabricaConfiguration();
            var config = builder.Build().Configuration;

            // Assert
            config["TestKey"].ShouldBe("from-configuration");
        }
        finally
        {
            if (configCreated && File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    [Fact]
    public void AddFabricaConfiguration_EnvironmentJsonOverridesConfigurationJson()
    {
        // Arrange
        var configPath = Path.Combine(AppContext.BaseDirectory, "configuration.json");
        var envPath = Path.Combine(AppContext.BaseDirectory, "environment.json");
        var configCreated = false;
        var envCreated = false;

        try
        {
            File.WriteAllText(configPath, """{"TestKey": "from-configuration", "ConfigOnly": "config-value"}""");
            configCreated = true;
            File.WriteAllText(envPath, """{"TestKey": "from-environment", "EnvOnly": "env-value"}""");
            envCreated = true;

            var builder = WebApplication.CreateBuilder();

            // Act
            builder.AddFabricaConfiguration();
            var config = builder.Build().Configuration;

            // Assert
            config["TestKey"].ShouldBe("from-environment"); // Overridden
            config["ConfigOnly"].ShouldBe("config-value");   // From configuration.json
            config["EnvOnly"].ShouldBe("env-value");         // From environment.json
        }
        finally
        {
            if (configCreated && File.Exists(configPath))
                File.Delete(configPath);
            if (envCreated && File.Exists(envPath))
                File.Delete(envPath);
        }
    }

    [Fact]
    public void AddFabricaConfiguration_MissionJsonOverridesAll()
    {
        // Arrange
        var configPath = Path.Combine(AppContext.BaseDirectory, "configuration.json");
        var envPath = Path.Combine(AppContext.BaseDirectory, "environment.json");
        var missionPath = Path.Combine(AppContext.BaseDirectory, "mission.json");
        var configCreated = false;
        var envCreated = false;
        var missionCreated = false;

        try
        {
            File.WriteAllText(configPath, """{"TestKey": "from-configuration"}""");
            configCreated = true;
            File.WriteAllText(envPath, """{"TestKey": "from-environment"}""");
            envCreated = true;
            File.WriteAllText(missionPath, """{"TestKey": "from-mission", "MissionOnly": "mission-value"}""");
            missionCreated = true;

            var builder = WebApplication.CreateBuilder();

            // Act
            builder.AddFabricaConfiguration();
            var config = builder.Build().Configuration;

            // Assert
            config["TestKey"].ShouldBe("from-mission");
            config["MissionOnly"].ShouldBe("mission-value");
        }
        finally
        {
            if (configCreated && File.Exists(configPath))
                File.Delete(configPath);
            if (envCreated && File.Exists(envPath))
                File.Delete(envPath);
            if (missionCreated && File.Exists(missionPath))
                File.Delete(missionPath);
        }
    }

    [Fact]
    public void AddFabricaConfiguration_EnvironmentVariablesOverrideJsonFiles()
    {
        // Arrange
        var configPath = Path.Combine(AppContext.BaseDirectory, "configuration.json");
        var envPath = Path.Combine(AppContext.BaseDirectory, "environment.json");
        var configCreated = false;
        var envCreated = false;
        var envVarKey = $"TEST_CONFIG_KEY_{Guid.NewGuid():N}";

        try
        {
            File.WriteAllText(configPath, "{\"" + envVarKey + "\": \"from-configuration\"}");
            configCreated = true;
            File.WriteAllText(envPath, "{\"" + envVarKey + "\": \"from-environment\"}");
            envCreated = true;

            Environment.SetEnvironmentVariable(envVarKey, "from-env-var");

            var builder = WebApplication.CreateBuilder();

            // Act
            builder.AddFabricaConfiguration();
            var config = builder.Build().Configuration;

            // Assert - env var should override environment.json but be overridden by mission.json
            // Since no mission.json exists, env var wins
            config[envVarKey].ShouldBe("from-env-var");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarKey, null);
            if (configCreated && File.Exists(configPath))
                File.Delete(configPath);
            if (envCreated && File.Exists(envPath))
                File.Delete(envPath);
        }
    }

    [Fact]
    public void AddFabricaConfiguration_MissionJsonOverridesEnvironmentVariables()
    {
        // Arrange
        var missionPath = Path.Combine(AppContext.BaseDirectory, "mission.json");
        var missionCreated = false;
        var envVarKey = $"TEST_MISSION_KEY_{Guid.NewGuid():N}";

        try
        {
            Environment.SetEnvironmentVariable(envVarKey, "from-env-var");
            File.WriteAllText(missionPath, "{\"" + envVarKey + "\": \"from-mission\"}");
            missionCreated = true;

            var builder = WebApplication.CreateBuilder();

            // Act
            builder.AddFabricaConfiguration();
            var config = builder.Build().Configuration;

            // Assert - mission.json should override env var
            config[envVarKey].ShouldBe("from-mission");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarKey, null);
            if (missionCreated && File.Exists(missionPath))
                File.Delete(missionPath);
        }
    }

    [Fact]
    public void AddFabricaConfiguration_SupportsNestedConfiguration()
    {
        // Arrange
        var configPath = Path.Combine(AppContext.BaseDirectory, "configuration.json");
        var configCreated = false;

        try
        {
            File.WriteAllText(configPath, """
            {
                "Database": {
                    "ConnectionString": "Server=localhost",
                    "Timeout": 30
                }
            }
            """);
            configCreated = true;

            var builder = WebApplication.CreateBuilder();

            // Act
            builder.AddFabricaConfiguration();
            var config = builder.Build().Configuration;

            // Assert
            config["Database:ConnectionString"].ShouldBe("Server=localhost");
            config["Database:Timeout"].ShouldBe("30");
        }
        finally
        {
            if (configCreated && File.Exists(configPath))
                File.Delete(configPath);
        }
    }
}
