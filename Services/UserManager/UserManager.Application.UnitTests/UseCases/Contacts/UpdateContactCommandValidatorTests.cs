using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Application.UseCases.Contacts.Validators;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class UpdateContactCommandValidatorTests
{
    private readonly UpdateContactCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidDetails_IsValid()
    {
        // Act
        var result = _validator.Validate(Command("Ana", "ana@example.com", null));

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithoutEmailAndPhone_FailsOnEmail()
    {
        // Act
        var result = _validator.Validate(Command("Ana", " ", null));

        // Assert
        Assert.Equal("Email", Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void Validate_WithInvalidPhone_FailsOnPhone()
    {
        // Act
        var result = _validator.Validate(Command("Ana", null, "123"));

        // Assert
        Assert.Equal("Phone", Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void Validate_WithoutName_FailsOnName()
    {
        // Act
        var result = _validator.Validate(Command("", "ana@example.com", null));

        // Assert
        Assert.Equal("Name", Assert.Single(result.Errors).PropertyName);
    }

    private static UpdateContactCommand Command(string name, string? email, string? phone) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, email, phone);
}
