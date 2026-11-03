using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Application.UseCases.Contacts.Validators;
using UserManager.Domain.Entities;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class CreateContactCommandValidatorTests
{
    private readonly CreateContactCommandValidator _validator = new();

    [Theory]
    [InlineData("ana@example.com", null)]
    [InlineData(null, "(11) 98765-4321")]
    [InlineData("", "11987654321")]
    [InlineData(" ANA@Example.com ", "")]
    public void Validate_WithEmailOrPhone_IsValid(string? email, string? phone)
    {
        // Act
        var result = _validator.Validate(new CreateContactCommand("Ana", email, phone));

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", " ")]
    public void Validate_WithoutEmailAndPhone_FailsOnEmail(string? email, string? phone)
    {
        // Act
        var result = _validator.Validate(new CreateContactCommand("Ana", email, phone));

        // Assert
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithInvalidEmail_FailsOnEmail()
    {
        // Act
        var result = _validator.Validate(new CreateContactCommand("Ana", "ana@", "11987654321"));

        // Assert
        Assert.Equal("Email", Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void Validate_WithInvalidPhone_FailsOnPhone()
    {
        // Act
        var result = _validator.Validate(new CreateContactCommand("Ana", "ana@example.com", "123"));

        // Assert
        Assert.Equal("Phone", Assert.Single(result.Errors).PropertyName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithoutName_FailsOnName(string name)
    {
        // Act
        var result = _validator.Validate(new CreateContactCommand(name, "ana@example.com", null));

        // Assert
        Assert.Equal("Name", Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void Validate_WithNameTooLong_FailsOnName()
    {
        // Arrange
        var name = new string('a', Contact.MaxNameLength + 1);

        // Act
        var result = _validator.Validate(new CreateContactCommand(name, "ana@example.com", null));

        // Assert
        Assert.Equal("Name", Assert.Single(result.Errors).PropertyName);
    }
}
