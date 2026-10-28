using UserManager.Domain.Exceptions;
using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.UnitTests.ValueObjects;

public sealed class EmailTests
{
    [Fact]
    public void Create_NormalizesCaseAndSurroundingSpaces()
    {
        // Act
        var email = Email.Create("  Ana.Silva@Example.COM ");

        // Assert
        Assert.Equal("ana.silva@example.com", email.Value);
    }

    [Fact]
    public void Create_WithSameAddressInDifferentCase_IsEqual()
    {
        // Act
        var first = Email.Create("ana@example.com");
        var second = Email.Create("ANA@Example.com");

        // Assert
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("ana@example.com")]
    [InlineData("  ANA@Example.COM ")]
    public void IsValid_WithWellFormedAddress_ReturnsTrue(string value)
    {
        // Act
        var isValid = Email.IsValid(value);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_WhenExactlyMaxLength_ReturnsTrue()
    {
        // Arrange
        var value = new string('a', Email.MaxLength - "@example.com".Length) + "@example.com";

        // Act
        var isValid = Email.IsValid(value);

        // Assert
        Assert.True(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ana")]
    [InlineData("ana@")]
    [InlineData("@example.com")]
    [InlineData("ana@example")]
    [InlineData("ana silva@example.com")]
    [InlineData("ana@@example.com")]
    public void IsValid_WithMalformedAddress_ReturnsFalse(string? value)
    {
        // Act
        var isValid = Email.IsValid(value);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WhenLongerThanMaxLength_ReturnsFalse()
    {
        // Arrange
        var value = new string('a', Email.MaxLength - "@example.com".Length + 1) + "@example.com";

        // Act
        var isValid = Email.IsValid(value);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Create_WithMalformedAddress_ThrowsDomainException()
    {
        // Act
        var create = () => Email.Create("ana@");

        // Assert
        Assert.Throws<DomainException>(create);
    }
}
