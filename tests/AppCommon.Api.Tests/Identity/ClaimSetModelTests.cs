using AppCommon.Api.Identity;
using Shouldly;
using Xunit;

namespace AppCommon.Api.Tests.Identity;

public class ClaimSetModelTests
{
    [Fact]
    public void Roles_WhenRolesListIsNull_ReturnsEmptyEnumerable()
    {
        // Arrange
        var model = new ClaimSetModel { RolesList = null };

        // Act
        IClaimSet claimSet = model;
        var roles = claimSet.Roles.ToList();

        // Assert
        roles.ShouldBeEmpty();
    }

    [Fact]
    public void Roles_WhenRolesListHasItems_ReturnsRoles()
    {
        // Arrange
        var model = new ClaimSetModel
        {
            RolesList = ["admin", "user"]
        };

        // Act
        IClaimSet claimSet = model;
        var roles = claimSet.Roles.ToList();

        // Assert
        roles.ShouldBe(["admin", "user"]);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var model = new ClaimSetModel
        {
            Subject = "user-123",
            Name = "John Doe",
            Email = "john@example.com",
            Expiration = 1234567890
        };

        // Assert
        model.Subject.ShouldBe("user-123");
        model.Name.ShouldBe("John Doe");
        model.Email.ShouldBe("john@example.com");
        model.Expiration.ShouldBe(1234567890);
    }
}
