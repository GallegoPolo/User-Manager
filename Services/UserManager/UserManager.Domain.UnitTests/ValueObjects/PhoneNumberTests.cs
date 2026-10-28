using UserManager.Domain.Exceptions;
using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.UnitTests.ValueObjects;

public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("+55 11 98765-4321", "+5511987654321")]
    [InlineData("11.98765.4321", "11987654321")]
    [InlineData("12345678", "12345678")]
    public void Create_RemovesFormattingCharacters(string value, string expected)
    {
        // Act
        var phone = PhoneNumber.Create(value);

        // Assert
        Assert.Equal(expected, phone.Value);
    }

    [Fact]
    public void Create_WithSameDigitsInDifferentFormat_IsEqual()
    {
        // Act
        var first = PhoneNumber.Create("(11) 98765-4321");
        var second = PhoneNumber.Create("11 987654321");

        // Assert
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("(11) 98765-4321")]
    [InlineData("123456789012345")]
    [InlineData("+123456789012345")]
    public void IsValid_WithWellFormedNumber_ReturnsTrue(string value)
    {
        // Act
        var isValid = PhoneNumber.IsValid(value);

        // Assert
        Assert.True(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("1234567890123456")]
    [InlineData("11 98765-43a1")]
    [InlineData("++5511987654321")]
    [InlineData("55+11987654321")]
    public void IsValid_WithInvalidNumber_ReturnsFalse(string? value)
    {
        // Act
        var isValid = PhoneNumber.IsValid(value);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Create_WithInvalidNumber_ThrowsDomainException()
    {
        // Act
        var create = () => PhoneNumber.Create("123");

        // Assert
        Assert.Throws<DomainException>(create);
    }
}
