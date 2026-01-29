using AppCommon.Core.Mediator;
using Shouldly;
using Xunit;

namespace AppCommon.Core.Tests.Mediator;

public class BatchCommandResultTests
{
    [Fact]
    public void Succeeded_CreatesSuccessResult()
    {
        // Arrange
        var response = new SampleResponse { Id = 123, Name = "Test" };

        // Act
        var result = BatchCommandResult.Succeeded(response, "entity-uid");

        // Assert
        result.Success.ShouldBeTrue();
        result.Response.ShouldBe(response);
        result.EntityUid.ShouldBe("entity-uid");
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Succeeded_InfersCommandTypeFromResponse()
    {
        // Arrange
        var response = new CreateUserResponse { UserId = "user-123" };

        // Act
        var result = BatchCommandResult.Succeeded(response);

        // Assert
        result.CommandType.ShouldBe("CreateUser");
    }

    [Fact]
    public void Succeeded_WithoutEntityUid_SetsEntityUidToNull()
    {
        // Arrange
        var response = new SampleResponse { Id = 1, Name = "Test" };

        // Act
        var result = BatchCommandResult.Succeeded(response);

        // Assert
        result.EntityUid.ShouldBeNull();
    }

    [Fact]
    public void Failed_CreatesFailureResult()
    {
        // Arrange
        const string commandType = "CreateUser";
        const string entityUid = "user-123";
        const string errorMessage = "User already exists";

        // Act
        var result = BatchCommandResult.Failed(commandType, entityUid, errorMessage);

        // Assert
        result.Success.ShouldBeFalse();
        result.CommandType.ShouldBe(commandType);
        result.EntityUid.ShouldBe(entityUid);
        result.ErrorMessage.ShouldBe(errorMessage);
        result.Response.ShouldBeNull();
    }

    [Fact]
    public void Failed_WithNullEntityUid_AllowsNullEntityUid()
    {
        // Act
        var result = BatchCommandResult.Failed("DeleteUser", null, "Not found");

        // Assert
        result.EntityUid.ShouldBeNull();
    }

    [Fact]
    public void Failed_WithNullCommandType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            BatchCommandResult.Failed(null!, "entity", "error"));
    }

    [Fact]
    public void Failed_WithEmptyCommandType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            BatchCommandResult.Failed("", "entity", "error"));
    }

    [Fact]
    public void Failed_WithNullError_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            BatchCommandResult.Failed("CreateUser", "entity", null!));
    }

    [Fact]
    public void Failed_WithEmptyError_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            BatchCommandResult.Failed("CreateUser", "entity", ""));
    }

    [Fact]
    public void GetResponse_WithCorrectType_ReturnsTypedResponse()
    {
        // Arrange
        var response = new SampleResponse { Id = 42, Name = "Test" };
        var result = BatchCommandResult.Succeeded(response);

        // Act
        var typedResponse = result.GetResponse<SampleResponse>();

        // Assert
        typedResponse.ShouldNotBeNull();
        typedResponse.Id.ShouldBe(42);
        typedResponse.Name.ShouldBe("Test");
    }

    [Fact]
    public void GetResponse_WithIncorrectType_ReturnsNull()
    {
        // Arrange
        var response = new SampleResponse { Id = 42, Name = "Test" };
        var result = BatchCommandResult.Succeeded(response);

        // Act
        var typedResponse = result.GetResponse<CreateUserResponse>();

        // Assert
        typedResponse.ShouldBeNull();
    }

    [Fact]
    public void GetResponse_WhenResponseIsNull_ReturnsNull()
    {
        // Arrange
        var result = BatchCommandResult.Failed("Test", null, "error");

        // Act
        var typedResponse = result.GetResponse<SampleResponse>();

        // Assert
        typedResponse.ShouldBeNull();
    }

    [Fact]
    public void Record_Equality_WorksCorrectly()
    {
        // Arrange
        var result1 = new BatchCommandResult
        {
            Success = true,
            CommandType = "Test",
            Response = null,
            EntityUid = "123"
        };

        var result2 = new BatchCommandResult
        {
            Success = true,
            CommandType = "Test",
            Response = null,
            EntityUid = "123"
        };

        // Assert
        result1.ShouldBe(result2);
    }

    private class SampleResponse
    {
        public int Id { get; init; }
        public required string Name { get; init; }
    }

    private class CreateUserResponse
    {
        public required string UserId { get; init; }
    }
}
