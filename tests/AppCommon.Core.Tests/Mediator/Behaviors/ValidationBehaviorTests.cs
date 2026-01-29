using AppCommon.Core.Mediator;
using AppCommon.Core.Mediator.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AppCommon.Core.Tests.Mediator.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task HandleAsync_WhenNoValidators_CallsNextDirectly()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestValidationRequest>>();
        var behavior = new ValidationBehavior<TestValidationRequest, TestValidationResponse>(validators);
        var request = new TestValidationRequest { Name = "test" };
        var expectedResponse = new TestValidationResponse { Id = 123 };
        var nextCalled = false;

        Task<TestValidationResponse> Next()
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        }

        // Act
        var result = await behavior.HandleAsync(request, Next);

        // Assert
        nextCalled.ShouldBeTrue();
        result.ShouldBe(expectedResponse);
    }

    [Fact]
    public async Task HandleAsync_WhenValidationPasses_CallsNext()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestValidationRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestValidationRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestValidationRequest, TestValidationResponse>([validator]);
        var request = new TestValidationRequest { Name = "valid" };
        var expectedResponse = new TestValidationResponse { Id = 456 };

        Task<TestValidationResponse> Next() => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.HandleAsync(request, Next);

        // Assert
        result.ShouldBe(expectedResponse);
        await validator.Received(1).ValidateAsync(
            Arg.Any<ValidationContext<TestValidationRequest>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestValidationRequest>>();
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required")
        };
        validator.ValidateAsync(Arg.Any<ValidationContext<TestValidationRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestValidationRequest, TestValidationResponse>([validator]);
        var request = new TestValidationRequest { Name = "" };

        Task<TestValidationResponse> Next() => Task.FromResult(new TestValidationResponse { Id = 1 });

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(
            async () => await behavior.HandleAsync(request, Next));

        exception.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_DoesNotCallNext()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestValidationRequest>>();
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required")
        };
        validator.ValidateAsync(Arg.Any<ValidationContext<TestValidationRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestValidationRequest, TestValidationResponse>([validator]);
        var request = new TestValidationRequest { Name = "" };
        var nextCalled = false;

        Task<TestValidationResponse> Next()
        {
            nextCalled = true;
            return Task.FromResult(new TestValidationResponse { Id = 1 });
        }

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(
            async () => await behavior.HandleAsync(request, Next));

        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_WithMultipleValidators_RunsAllValidators()
    {
        // Arrange
        var validator1 = Substitute.For<IValidator<TestValidationRequest>>();
        validator1.ValidateAsync(Arg.Any<ValidationContext<TestValidationRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var validator2 = Substitute.For<IValidator<TestValidationRequest>>();
        validator2.ValidateAsync(Arg.Any<ValidationContext<TestValidationRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestValidationRequest, TestValidationResponse>([validator1, validator2]);
        var request = new TestValidationRequest { Name = "test" };

        Task<TestValidationResponse> Next() => Task.FromResult(new TestValidationResponse { Id = 1 });

        // Act
        await behavior.HandleAsync(request, Next);

        // Assert
        await validator1.Received(1).ValidateAsync(
            Arg.Any<ValidationContext<TestValidationRequest>>(),
            Arg.Any<CancellationToken>());
        await validator2.Received(1).ValidateAsync(
            Arg.Any<ValidationContext<TestValidationRequest>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithMultipleValidators_AggregatesFailures()
    {
        // Arrange
        var validator1 = Substitute.For<IValidator<TestValidationRequest>>();
        validator1.ValidateAsync(Arg.Any<ValidationContext<TestValidationRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "Name error")]));

        var validator2 = Substitute.For<IValidator<TestValidationRequest>>();
        validator2.ValidateAsync(Arg.Any<ValidationContext<TestValidationRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Email", "Email error")]));

        var behavior = new ValidationBehavior<TestValidationRequest, TestValidationResponse>([validator1, validator2]);
        var request = new TestValidationRequest { Name = "" };

        Task<TestValidationResponse> Next() => Task.FromResult(new TestValidationResponse { Id = 1 });

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(
            async () => await behavior.HandleAsync(request, Next));

        exception.Errors.Count().ShouldBe(2);
        exception.Errors.ShouldContain(e => e.PropertyName == "Name");
        exception.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task HandleAsync_PassesCancellationToken_ToValidators()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestValidationRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestValidationRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestValidationRequest, TestValidationResponse>([validator]);
        var request = new TestValidationRequest { Name = "test" };
        var cancellationToken = new CancellationToken();

        Task<TestValidationResponse> Next() => Task.FromResult(new TestValidationResponse { Id = 1 });

        // Act
        await behavior.HandleAsync(request, Next, cancellationToken);

        // Assert
        await validator.Received(1).ValidateAsync(
            Arg.Any<ValidationContext<TestValidationRequest>>(),
            cancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WithRealValidator_ValidatesCorrectly()
    {
        // Arrange
        var validator = new TestValidationRequestValidator();
        var behavior = new ValidationBehavior<TestValidationRequest, TestValidationResponse>([validator]);
        var invalidRequest = new TestValidationRequest { Name = "" };

        Task<TestValidationResponse> Next() => Task.FromResult(new TestValidationResponse { Id = 1 });

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(
            async () => await behavior.HandleAsync(invalidRequest, Next));

        exception.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task HandleAsync_WithRealValidator_PassesValidRequest()
    {
        // Arrange
        var validator = new TestValidationRequestValidator();
        var behavior = new ValidationBehavior<TestValidationRequest, TestValidationResponse>([validator]);
        var validRequest = new TestValidationRequest { Name = "Valid Name" };
        var expectedResponse = new TestValidationResponse { Id = 789 };

        Task<TestValidationResponse> Next() => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.HandleAsync(validRequest, Next);

        // Assert
        result.ShouldBe(expectedResponse);
    }
}

public class TestValidationRequest : IRequest<TestValidationResponse>
{
    public required string Name { get; init; }
}

public class TestValidationResponse
{
    public required int Id { get; init; }
}

public class TestValidationRequestValidator : AbstractValidator<TestValidationRequest>
{
    public TestValidationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
    }
}
