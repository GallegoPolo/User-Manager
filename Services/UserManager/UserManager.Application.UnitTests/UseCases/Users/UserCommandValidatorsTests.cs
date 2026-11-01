using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Validators;

namespace UserManager.Application.UnitTests.UseCases.Users;

public sealed class UserCommandValidatorsTests
{
    [Theory]
    [InlineData("ana@localhost")]
    [InlineData("ana@")]
    [InlineData("")]
    public void CreateValidator_WithInvalidEmail_FailsOnEmail(string email)
    {
        // Act
        var result = new CreateUserCommandValidator().Validate(new CreateUserCommand("Ana", email));

        // Assert
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void CreateValidator_WithEmailNeedingNormalization_IsValid()
    {
        // Act
        var result = new CreateUserCommandValidator().Validate(new CreateUserCommand("Ana", " ANA@Example.com "));

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("ana@localhost")]
    [InlineData("ana@")]
    [InlineData("")]
    public void UpdateValidator_WithInvalidEmail_FailsOnEmail(string email)
    {
        // Act
        var result = new UpdateUserCommandValidator().Validate(new UpdateUserCommand(Guid.NewGuid(), "Ana", email));

        // Assert
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void UpdateValidator_WithEmailNeedingNormalization_IsValid()
    {
        // Act
        var result = new UpdateUserCommandValidator().Validate(new UpdateUserCommand(Guid.NewGuid(), "Ana", " ANA@Example.com "));

        // Assert
        Assert.True(result.IsValid);
    }
}
