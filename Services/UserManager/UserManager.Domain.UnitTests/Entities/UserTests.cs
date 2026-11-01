using UserManager.Domain.Entities;
using UserManager.Domain.Events.Entities;
using UserManager.Domain.Exceptions;
using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.UnitTests.Entities;

public sealed class UserTests
{
    private static readonly Email AnaEmail = Email.Create(" ANA@Example.com ");

    [Fact]
    public void Create_RaisesUserCreatedWithNormalizedEmail()
    {
        // Act
        var user = User.Create("Ana", AnaEmail);

        // Assert
        Assert.Equal(Email.Create("ana@example.com"), user.Email);
        var created = Assert.IsType<UserCreatedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal(user.Id, created.UserId);
        Assert.Equal("ana@example.com", created.Email);
    }

    [Fact]
    public void Create_WithoutEmail_ThrowsDomainException()
    {
        // Act
        var create = () => User.Create("Ana", null!);

        // Assert
        Assert.Throws<DomainException>(create);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutName_ThrowsDomainException(string? name)
    {
        // Act
        var create = () => User.Create(name!, AnaEmail);

        // Assert
        Assert.Throws<DomainException>(create);
    }

    [Fact]
    public void Create_WithNameTooLong_ThrowsDomainException()
    {
        // Act
        var create = () => User.Create(new string('a', User.MAX_NAME_LENGTH + 1), AnaEmail);

        // Assert
        Assert.Throws<DomainException>(create);
    }

    [Fact]
    public void Update_ReplacesDataAndRaisesUserUpdated()
    {
        // Arrange
        var user = User.Create("Ana", AnaEmail);
        user.ClearDomainEvents();

        // Act
        user.Update("Ana Souza", Email.Create("Ana.Souza@Example.com"));

        // Assert
        Assert.Equal("Ana Souza", user.Name);
        Assert.Equal(Email.Create("ana.souza@example.com"), user.Email);
        var updated = Assert.IsType<UserUpdatedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal("ana.souza@example.com", updated.Email);
    }

    [Fact]
    public void Update_WithoutEmail_ThrowsAndKeepsCurrentData()
    {
        // Arrange
        var user = User.Create("Ana", AnaEmail);

        // Act
        var update = () => user.Update("Ana Souza", null!);

        // Assert
        Assert.Throws<DomainException>(update);
        Assert.Equal("Ana", user.Name);
        Assert.Equal(AnaEmail, user.Email);
    }

    [Fact]
    public void Delete_RaisesUserDeletedWithEmail()
    {
        // Arrange
        var user = User.Create("Ana", AnaEmail);
        user.ClearDomainEvents();

        // Act
        user.Delete();

        // Assert
        var deleted = Assert.IsType<UserDeletedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal("ana@example.com", deleted.Email);
    }
}
