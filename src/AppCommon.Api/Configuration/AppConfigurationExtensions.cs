using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace AppCommon.Api.Configuration;

/// <summary>
/// Extension methods for Fabrica.One configuration injection.
/// Files are loaded from AppContext.BaseDirectory (where the executable resides).
///
/// Loading order (lowest to highest priority):
/// 1. configuration.json - App defaults (shipped with app)
/// 2. environment.json - Environment settings (orchestrator injected)
/// 3. Environment variables
/// 4. mission.json - Deployment context (orchestrator injected)
/// </summary>
public static class AppConfigurationExtensions
{
    private const string ConfigurationJson = "configuration.json";
    private const string EnvironmentJson = "environment.json";
    private const string MissionJson = "mission.json";

    public static WebApplicationBuilder AddFabricaConfiguration(this WebApplicationBuilder builder)
    {
        var basePath = AppContext.BaseDirectory;

        // Clear default configuration sources
        builder.Configuration.Sources.Clear();

        // 1. configuration.json - App defaults (shipped with application)
        builder.Configuration.AddJsonFile(
            Path.Combine(basePath, ConfigurationJson),
            optional: true,
            reloadOnChange: false);

        // 2. environment.json - Environment settings (orchestrator injected)
        builder.Configuration.AddJsonFile(
            Path.Combine(basePath, EnvironmentJson),
            optional: true,
            reloadOnChange: false);

        // 3. Environment variables (includes DotNetEnv loaded values)
        builder.Configuration.AddEnvironmentVariables();

        // 4. mission.json - Deployment context (highest file priority)
        builder.Configuration.AddJsonFile(
            Path.Combine(basePath, MissionJson),
            optional: true,
            reloadOnChange: false);

        return builder;
    }
}
