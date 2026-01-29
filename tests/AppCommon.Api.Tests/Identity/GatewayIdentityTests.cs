using System.Security.Claims;
using AppCommon.Api.Identity;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AppCommon.Api.Tests.Identity;

public class GatewayIdentityTests
{
    [Fact]
    public void Constructor_SetsAuthenticationType()
    {
        // Arrange
        var claimSet = CreateClaimSet();

        // Act
        var identity = new GatewayIdentity(claimSet);

        // Assert
        identity.AuthenticationType.ShouldBe(GatewayTokenOptions.DefaultScheme);
    }

    [Fact]
    public void Constructor_WithCustomAuthenticationType_SetsAuthenticationType()
    {
        // Arrange
        var claimSet = CreateClaimSet();

        // Act
        var identity = new GatewayIdentity(claimSet, "CustomScheme");

        // Assert
        identity.AuthenticationType.ShouldBe("CustomScheme");
    }

    [Fact]
    public void Constructor_AddsSubjectAsNameIdentifierClaim()
    {
        // Arrange
        var claimSet = CreateClaimSet(subject: "user-123");

        // Act
        var identity = new GatewayIdentity(claimSet);

        // Assert
        identity.FindFirst(ClaimTypes.NameIdentifier)?.Value.ShouldBe("user-123");
    }

    [Fact]
    public void Constructor_AddsNameClaim()
    {
        // Arrange
        var claimSet = CreateClaimSet(name: "John Doe");

        // Act
        var identity = new GatewayIdentity(claimSet);

        // Assert
        identity.FindFirst(ClaimTypes.Name)?.Value.ShouldBe("John Doe");
    }

    [Fact]
    public void Constructor_AddsEmailClaim()
    {
        // Arrange
        var claimSet = CreateClaimSet(email: "john@example.com");

        // Act
        var identity = new GatewayIdentity(claimSet);

        // Assert
        identity.FindFirst(ClaimTypes.Email)?.Value.ShouldBe("john@example.com");
    }

    [Fact]
    public void Constructor_AddsRoleClaims()
    {
        // Arrange
        var claimSet = CreateClaimSet(roles: ["admin", "user"]);

        // Act
        var identity = new GatewayIdentity(claimSet);

        // Assert
        var roleClaims = identity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.ShouldContain("admin");
        roleClaims.ShouldContain("user");
    }

    [Fact]
    public void Constructor_SkipsNullSubject()
    {
        // Arrange
        var claimSet = CreateClaimSet(subject: null);

        // Act
        var identity = new GatewayIdentity(claimSet);

        // Assert
        identity.FindFirst(ClaimTypes.NameIdentifier).ShouldBeNull();
    }

    [Fact]
    public void Constructor_SkipsWhitespaceSubject()
    {
        // Arrange
        var claimSet = CreateClaimSet(subject: "   ");

        // Act
        var identity = new GatewayIdentity(claimSet);

        // Assert
        identity.FindFirst(ClaimTypes.NameIdentifier).ShouldBeNull();
    }

    [Fact]
    public void Constructor_SkipsWhitespaceRoles()
    {
        // Arrange
        var claimSet = CreateClaimSet(roles: ["admin", "   ", "user"]);

        // Act
        var identity = new GatewayIdentity(claimSet);

        // Assert
        var roleClaims = identity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Count.ShouldBe(2);
        roleClaims.ShouldContain("admin");
        roleClaims.ShouldContain("user");
    }

    [Fact]
    public void Constructor_HandlesEmptyRoles()
    {
        // Arrange
        var claimSet = CreateClaimSet(roles: []);

        // Act
        var identity = new GatewayIdentity(claimSet);

        // Assert
        identity.FindAll(ClaimTypes.Role).ShouldBeEmpty();
    }

    private static IClaimSet CreateClaimSet(
        string? subject = "sub",
        string? name = "name",
        string? email = "email@test.com",
        IEnumerable<string>? roles = null)
    {
        var claimSet = Substitute.For<IClaimSet>();
        claimSet.Subject.Returns(subject);
        claimSet.Name.Returns(name);
        claimSet.Email.Returns(email);
        claimSet.Roles.Returns(roles ?? []);
        return claimSet;
    }
}
