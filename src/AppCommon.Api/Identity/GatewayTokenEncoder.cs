using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AppCommon.Api.Identity;

public class GatewayTokenEncoder : IGatewayTokenEncoder
{
    private readonly GatewayTokenOptions _options;
    private readonly JsonWebTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;

    public GatewayTokenEncoder(IOptions<GatewayTokenOptions> options)
    {
        _options = options.Value;
        _tokenHandler = new JsonWebTokenHandler();

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = _options.ValidateExpiration,
            ValidateIssuerSigningKey = _options.ValidateSignature,
            ClockSkew = _options.ClockSkew,
            RequireSignedTokens = _options.ValidateSignature
        };

        if (_options.ValidateSignature)
        {
            if (string.IsNullOrWhiteSpace(_options.SigningKey))
            {
                throw new InvalidOperationException(
                    "SigningKey must be configured when ValidateSignature is true.");
            }

            var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
            _validationParameters.IssuerSigningKey = new SymmetricSecurityKey(keyBytes);
        }
    }

    public async Task<IClaimSet> DecodeAsync(string token)
    {
        var jsonToken = _tokenHandler.ReadJsonWebToken(token)
            ?? throw new SecurityTokenException("Failed to parse token as JWT.");

        if (_options.ValidateSignature)
        {
            var result = await _tokenHandler.ValidateTokenAsync(token, _validationParameters);

            if (!result.IsValid)
            {
                throw new SecurityTokenException("Token validation failed.", result.Exception);
            }
        }
        else if (_options.ValidateExpiration)
        {
            // Tokens without an expiration claim (ValidTo == MinValue) are rejected
            // when expiration validation is enabled
            if (jsonToken.ValidTo == DateTime.MinValue)
            {
                throw new SecurityTokenException("Token does not contain an expiration claim.");
            }

            var expiration = jsonToken.ValidTo.Add(_options.ClockSkew);
            if (expiration < DateTime.UtcNow)
            {
                throw new SecurityTokenExpiredException("Token has expired.");
            }
        }

        return ExtractClaimsFromToken(jsonToken);
    }

    private static ClaimSetModel ExtractClaimsFromToken(JsonWebToken token)
    {
        return new ClaimSetModel
        {
            Subject = token.Subject,
            Name = token.GetClaimValue("name"),
            Email = token.GetClaimValue("email"),
            RolesList = GetRoles(token),
            Expiration = token.ValidTo != DateTime.MinValue
                ? new DateTimeOffset(token.ValidTo).ToUnixTimeSeconds()
                : null
        };
    }

    private static List<string>? GetRoles(JsonWebToken token)
    {
        var rolesClaim = token.Claims.FirstOrDefault(c => c.Type == "roles");
        if (rolesClaim == null)
            return null;

        // Handle both single role and array of roles
        var roles = token.Claims
            .Where(c => c.Type == "roles")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        return roles.Count > 0 ? roles : null;
    }
}

internal static class JsonWebTokenExtensions
{
    public static string? GetClaimValue(this JsonWebToken token, string claimType)
    {
        return token.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }
}
