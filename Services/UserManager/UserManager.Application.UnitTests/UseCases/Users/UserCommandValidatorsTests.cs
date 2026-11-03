using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Validators;
using UserManager.Domain.Entities;

namespace UserManager.Application.UnitTests.UseCases.Users;

public sealed class UserCommandValidatorsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RenameValidator_WithoutName_FailsOnName(string name)
    {
        // Act
        var result = new RenameUserCommandValidator().Validate(new RenameUserCommand(Guid.NewGuid(), name));

        // Assert
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void RenameValidator_WithNameTooLong_FailsOnName()
    {
        // Act
        var result = new RenameUserCommandValidator().Validate(new RenameUserCommand(Guid.NewGuid(), new string('a', User.MAX_NAME_LENGTH + 1)));

        // Assert
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void RenameValidator_WithoutId_FailsOnId()
    {
        // Act
        var result = new RenameUserCommandValidator().Validate(new RenameUserCommand(Guid.Empty, "Ana"));

        // Assert
        Assert.Contains(result.Errors, e => e.PropertyName == "Id");
    }

    [Fact]
    public void RenameValidator_WithValidData_IsValid()
    {
        // Act
        var result = new RenameUserCommandValidator().Validate(new RenameUserCommand(Guid.NewGuid(), "Ana Souza"));

        // Assert
        Assert.True(result.IsValid);
    }
}
