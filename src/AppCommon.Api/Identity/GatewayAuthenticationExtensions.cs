using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AppCommon.Api.Identity;

public static class GatewayAuthenticationExtensions
{
    /// <summary>
    /// Adds gateway token authentication to the service collection.
    /// </summary>
    public static AuthenticationBuilder AddGatewayTokenAuthentication(
        this IServiceCollection services,
        Action<GatewayTokenOptions>? configureOptions = null)
    {
        return services.AddGatewayTokenAuthentication(
            GatewayTokenOptions.DefaultScheme,
            configureOptions);
    }

    /// <summary>
    /// Adds gateway token authentication with a custom scheme name.
    /// </summary>
    public static AuthenticationBuilder AddGatewayTokenAuthentication(
        this IServiceCollection services,
        string authenticationScheme,
        Action<GatewayTokenOptions>? configureOptions = null)
    {
        services.AddSingleton<IGatewayTokenEncoder, GatewayTokenEncoder>();

        var builder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = authenticationScheme;
            options.DefaultChallengeScheme = authenticationScheme;
        });

        if (configureOptions != null)
        {
            builder.AddScheme<GatewayTokenOptions, GatewayTokenAuthenticationHandler>(
                authenticationScheme, configureOptions);
        }
        else
        {
            builder.AddScheme<GatewayTokenOptions, GatewayTokenAuthenticationHandler>(
                authenticationScheme, _ => { });
        }

        return builder;
    }

    /// <summary>
    /// Adds gateway token authentication to an existing authentication builder.
    /// </summary>
    public static AuthenticationBuilder AddGatewayToken(
        this AuthenticationBuilder builder,
        Action<GatewayTokenOptions>? configureOptions = null)
    {
        return builder.AddGatewayToken(
            GatewayTokenOptions.DefaultScheme,
            configureOptions);
    }

    /// <summary>
    /// Adds gateway token authentication to an existing authentication builder with a custom scheme name.
    /// </summary>
    public static AuthenticationBuilder AddGatewayToken(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        Action<GatewayTokenOptions>? configureOptions = null)
    {
        builder.Services.AddSingleton<IGatewayTokenEncoder, GatewayTokenEncoder>();

        if (configureOptions != null)
        {
            builder.AddScheme<GatewayTokenOptions, GatewayTokenAuthenticationHandler>(
                authenticationScheme, configureOptions);
        }
        else
        {
            builder.AddScheme<GatewayTokenOptions, GatewayTokenAuthenticationHandler>(
                authenticationScheme, _ => { });
        }

        return builder;
    }
}
