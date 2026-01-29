using Microsoft.AspNetCore.Authentication;

namespace AppCommon.Api.Identity;

public class GatewayTokenOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "GatewayToken";
    public const string DefaultHeaderName = "X-Gateway-Token";

    /// <summary>
    /// The header name to read the token from.
    /// </summary>
    public string HeaderName { get; set; } = DefaultHeaderName;

    /// <summary>
    /// Whether to validate the JWT signature. Set to false when behind a trusted proxy
    /// that has already validated the token.
    /// </summary>
    public bool ValidateSignature { get; set; } = false;

    /// <summary>
    /// The signing key for signature validation. Required when ValidateSignature is true.
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>
    /// Whether to validate token expiration.
    /// </summary>
    public bool ValidateExpiration { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance for expiration validation.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Optional development token to use when no token is provided in the header.
    /// This allows local development to behave the same as running behind a gateway.
    /// Should only be configured in development environments.
    /// </summary>
    public string? DevelopmentToken { get; set; }
}
