using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AppCommon.Api.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

namespace AppCommon.Api.Tests.Identity;

public class GatewayTokenEncoderTests
{
    private const string TestSigningKey = "this-is-a-test-signing-key-that-is-long-enough-for-hmac-sha256";

    [Fact]
    public async Task DecodeAsync_WithValidToken_ReturnsClaimSet()
    {
        // Arrange
        var token = CreateToken(subject: "user-123", name: "John Doe", email: "john@example.com");
        var encoder = CreateEncoder(validateSignature: false, validateExpiration: false);

        // Act
        var claims = await encoder.DecodeAsync(token);

        // Assert
        claims.Subject.ShouldBe("user-123");
        claims.Name.ShouldBe("John Doe");
        claims.Email.ShouldBe("john@example.com");
    }

    [Fact]
    public async Task DecodeAsync_WithRoles_ReturnsRolesInClaimSet()
    {
        // Arrange
        var token = CreateToken(roles: ["admin", "user"]);
        var encoder = CreateEncoder(validateSignature: false, validateExpiration: false);

        // Act
        var claims = await encoder.DecodeAsync(token);

        // Assert
        claims.Roles.ShouldContain("admin");
        claims.Roles.ShouldContain("user");
    }

    [Fact]
    public async Task DecodeAsync_WithExpiration_ReturnsExpirationInClaimSet()
    {
        // Arrange
        var expiration = DateTime.UtcNow.AddHours(1);
        var token = CreateToken(expiration: expiration);
        var encoder = CreateEncoder(validateSignature: false, validateExpiration: false);

        // Act
        var claims = await encoder.DecodeAsync(token);

        // Assert
        claims.Expiration.ShouldNotBeNull();
    }

    [Fact]
    public async Task DecodeAsync_WithValidSignature_Succeeds()
    {
        // Arrange
        var token = CreateToken(subject: "user-123", signingKey: TestSigningKey);
        var encoder = CreateEncoder(validateSignature: true, signingKey: TestSigningKey);

        // Act
        var claims = await encoder.DecodeAsync(token);

        // Assert
        claims.Subject.ShouldBe("user-123");
    }

    [Fact]
    public async Task DecodeAsync_WithInvalidSignature_ThrowsSecurityTokenException()
    {
        // Arrange
        var token = CreateToken(subject: "user-123", signingKey: "different-key-that-is-long-enough");
        var encoder = CreateEncoder(validateSignature: true, signingKey: TestSigningKey);

        // Act & Assert
        await Should.ThrowAsync<SecurityTokenException>(async () =>
            await encoder.DecodeAsync(token));
    }

    [Fact]
    public async Task DecodeAsync_WithExpiredToken_WhenValidationEnabled_ThrowsSecurityTokenExpiredException()
    {
        // Arrange
        var notBefore = DateTime.UtcNow.AddHours(-2);
        var expiration = DateTime.UtcNow.AddHours(-1);
        var token = CreateToken(expiration: expiration, notBefore: notBefore);
        var encoder = CreateEncoder(validateSignature: false, validateExpiration: true, clockSkew: TimeSpan.Zero);

        // Act & Assert
        await Should.ThrowAsync<SecurityTokenExpiredException>(async () =>
            await encoder.DecodeAsync(token));
    }

    [Fact]
    public async Task DecodeAsync_WithExpiredToken_WithinClockSkew_Succeeds()
    {
        // Arrange
        var notBefore = DateTime.UtcNow.AddMinutes(-10);
        var expiration = DateTime.UtcNow.AddMinutes(-2);
        var token = CreateToken(expiration: expiration, notBefore: notBefore);
        var encoder = CreateEncoder(validateSignature: false, validateExpiration: true, clockSkew: TimeSpan.FromMinutes(5));

        // Act
        var claims = await encoder.DecodeAsync(token);

        // Assert
        claims.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithValidateSignatureAndNoKey_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            CreateEncoder(validateSignature: true, signingKey: null));
    }

    [Fact]
    public void Constructor_WithValidateSignatureAndWhitespaceKey_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            CreateEncoder(validateSignature: true, signingKey: "   "));
    }

    [Fact]
    public async Task DecodeAsync_WithFarFutureExpiration_ReturnsExpiration()
    {
        // Arrange
        var futureExpiration = DateTime.UtcNow.AddYears(10);
        var token = CreateToken(expiration: futureExpiration);
        var encoder = CreateEncoder(validateSignature: false, validateExpiration: false);

        // Act
        var claims = await encoder.DecodeAsync(token);

        // Assert
        claims.Expiration.ShouldNotBeNull();
    }

    private static GatewayTokenEncoder CreateEncoder(
        bool validateSignature = false,
        bool validateExpiration = true,
        string? signingKey = null,
        TimeSpan? clockSkew = null)
    {
        var options = new GatewayTokenOptions
        {
            ValidateSignature = validateSignature,
            ValidateExpiration = validateExpiration,
            SigningKey = signingKey,
            ClockSkew = clockSkew ?? TimeSpan.FromMinutes(5)
        };

        return new GatewayTokenEncoder(Options.Create(options));
    }

    private static string CreateToken(
        string? subject = "sub",
        string? name = null,
        string? email = null,
        IEnumerable<string>? roles = null,
        DateTime? expiration = null,
        DateTime? notBefore = null,
        string? signingKey = null)
    {
        var claims = new List<Claim>();

        if (subject != null)
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, subject));
        if (name != null)
            claims.Add(new Claim("name", name));
        if (email != null)
            claims.Add(new Claim("email", email));

        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim("roles", role));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            NotBefore = notBefore
        };

        if (signingKey != null)
        {
            var keyBytes = Encoding.UTF8.GetBytes(signingKey);
            tokenDescriptor.SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256Signature);
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
