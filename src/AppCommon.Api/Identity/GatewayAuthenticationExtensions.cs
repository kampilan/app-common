using AppCommon.Core.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AppCommon.Api.Identity;

/// <summary>
/// Extension methods for registering gateway token authentication.
/// </summary>
/// <remarks>
/// <para>
/// These extensions register the authentication pipeline for applications
/// running behind a gateway/proxy that forwards JWT tokens.
/// </para>
/// <para>
/// <b>What gets registered:</b>
/// <list type="bullet">
/// <item><description><see cref="IGatewayTokenEncoder"/> and <see cref="GatewayTokenEncoder"/> (singleton)</description></item>
/// <item><description><see cref="GatewayTokenAuthenticationHandler"/> as the authentication handler</description></item>
/// </list>
/// </para>
/// <para>
/// <b>How it works with IRequestContext:</b> When authentication succeeds, the handler populates
/// <c>HttpContext.User</c> with a <see cref="System.Security.Claims.ClaimsPrincipal"/>.
/// <see cref="IRequestContext"/> reads from <c>HttpContext.User</c> automatically, so downstream
/// components like <c>LoggingBehavior</c> and <c>AuditSaveChangesInterceptor</c> get user context
/// without any manual wiring.
/// </para>
/// <para>
/// <b>Prerequisite:</b> Call <c>services.AddRequestContext()</c> to register <see cref="IRequestContext"/>.
/// </para>
/// </remarks>
/// <seealso cref="IRequestContext"/>
/// <seealso cref="GatewayTokenAuthenticationHandler"/>
public static class GatewayAuthenticationExtensions
{
    /// <summary>
    /// Adds gateway token authentication to the service collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Prerequisite:</b> Call <c>services.AddRequestContext()</c> before this method to register
    /// <see cref="IRequestContext"/>, which provides user info to <c>LoggingBehavior</c> and
    /// <c>AuditSaveChangesInterceptor</c>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for authentication options.</param>
    /// <returns>The authentication builder for further configuration.</returns>
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
    /// <remarks>
    /// <para>
    /// <b>Prerequisite:</b> Call <c>services.AddRequestContext()</c> before this method to register
    /// <see cref="IRequestContext"/>, which provides user info to <c>LoggingBehavior</c> and
    /// <c>AuditSaveChangesInterceptor</c>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="authenticationScheme">The authentication scheme name.</param>
    /// <param name="configureOptions">Optional configuration for authentication options.</param>
    /// <returns>The authentication builder for further configuration.</returns>
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
    /// Use this when you need multiple authentication schemes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Prerequisite:</b> Call <c>services.AddRequestContext()</c> to register
    /// <see cref="IRequestContext"/>, which provides user info to <c>LoggingBehavior</c> and
    /// <c>AuditSaveChangesInterceptor</c>.
    /// </para>
    /// </remarks>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="configureOptions">Optional configuration for authentication options.</param>
    /// <returns>The authentication builder for further configuration.</returns>
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
    /// <remarks>
    /// <para>
    /// <b>Prerequisite:</b> Call <c>services.AddRequestContext()</c> to register
    /// <see cref="IRequestContext"/>, which provides user info to <c>LoggingBehavior</c> and
    /// <c>AuditSaveChangesInterceptor</c>.
    /// </para>
    /// </remarks>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="authenticationScheme">The authentication scheme name.</param>
    /// <param name="configureOptions">Optional configuration for authentication options.</param>
    /// <returns>The authentication builder for further configuration.</returns>
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
