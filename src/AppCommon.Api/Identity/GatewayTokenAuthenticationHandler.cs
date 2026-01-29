using System.Text.Encodings.Web;
using AppCommon.Core.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AppCommon.Api.Identity;

public class GatewayTokenAuthenticationHandler : AuthenticationHandler<GatewayTokenOptions>
{
    private readonly IGatewayTokenEncoder _tokenEncoder;
    private readonly ICurrentUserService _currentUserService;

    public GatewayTokenAuthenticationHandler(
        IGatewayTokenEncoder tokenEncoder,
        ICurrentUserService currentUserService,
        IOptionsMonitor<GatewayTokenOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _tokenEncoder = tokenEncoder;
        _currentUserService = currentUserService;
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

            // Populate ICurrentUserService
            _currentUserService.SetUser(
                claims.Subject,
                claims.Name,
                claims.Email,
                claims.Roles);

            // Build ClaimsPrincipal
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
