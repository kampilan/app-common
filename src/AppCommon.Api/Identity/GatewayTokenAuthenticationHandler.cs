using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AppCommon.Api.Identity;

/// <summary>
/// ASP.NET Core authentication handler that processes JWT tokens forwarded by a gateway/proxy.
/// </summary>
/// <remarks>
/// <para>
/// <b>Request flow:</b>
/// <list type="number">
/// <item><description>Reads JWT from configured header (default: X-Gateway-Token)</description></item>
/// <item><description>Falls back to <see cref="GatewayTokenOptions.DevelopmentToken"/> if no header and configured</description></item>
/// <item><description>Decodes token via <see cref="IGatewayTokenEncoder"/></description></item>
/// <item><description>Creates <see cref="System.Security.Claims.ClaimsPrincipal"/> and assigns to <c>HttpContext.User</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Integration with IRequestContext:</b> Once this handler populates <c>HttpContext.User</c>,
/// <see cref="Core.Context.IRequestContext"/> automatically reflects the authenticated user's claims.
/// Downstream components like <c>LoggingBehavior</c> and <c>AuditSaveChangesInterceptor</c> read from
/// <c>IRequestContext</c> to get user context - no manual wiring required.
/// </para>
/// <para>
/// <b>Troubleshooting:</b> If <see cref="Core.Context.IRequestContext.Subject"/> is null:
/// <list type="bullet">
/// <item><description>Ensure <c>app.UseAuthentication()</c> is called before endpoint handlers</description></item>
/// <item><description>Check the JWT token is present in the request header</description></item>
/// <item><description>Verify the header name matches <see cref="GatewayTokenOptions.HeaderName"/></description></item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="Core.Context.IRequestContext"/>
/// <seealso cref="GatewayAuthenticationExtensions"/>
public class GatewayTokenAuthenticationHandler : AuthenticationHandler<GatewayTokenOptions>
{
    private readonly IGatewayTokenEncoder _tokenEncoder;

    /// <summary>
    /// Initializes a new instance of the authentication handler.
    /// </summary>
    public GatewayTokenAuthenticationHandler(
        IGatewayTokenEncoder tokenEncoder,
        IOptionsMonitor<GatewayTokenOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _tokenEncoder = tokenEncoder;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headerName = Options.HeaderName;
        string? token = null;

        // Try to get token from header first
        if (Request.Headers.TryGetValue(headerName, out var tokenValue))
        {
            token = tokenValue.ToString();
        }

        // Fall back to development token if no header and dev token is configured
        if (string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(Options.DevelopmentToken))
        {
            token = Options.DevelopmentToken;
            Logger.LogDebug("Using development fallback token");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            Logger.LogDebug("Gateway token header '{HeaderName}' not present and no development token configured", headerName);
            return AuthenticateResult.NoResult();
        }

        try
        {
            var claims = await _tokenEncoder.DecodeAsync(token);

            // Build ClaimsPrincipal - this populates HttpContext.User
            // IRequestContext reads from HttpContext.User automatically
            var identity = new GatewayIdentity(claims, Scheme.Name);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogDebug(
                "Successfully authenticated user '{Subject}' via gateway token",
                claims.Subject);

            return AuthenticateResult.Success(ticket);
        }
        catch (SecurityTokenExpiredException ex)
        {
            Logger.LogWarning(ex, "Gateway token has expired");
            return AuthenticateResult.Fail("Token has expired.");
        }
        catch (SecurityTokenException ex)
        {
            Logger.LogWarning(ex, "Gateway token validation failed");
            return AuthenticateResult.Fail("Invalid token.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during gateway token authentication");
            return AuthenticateResult.Fail("Authentication failed.");
        }
    }
}
