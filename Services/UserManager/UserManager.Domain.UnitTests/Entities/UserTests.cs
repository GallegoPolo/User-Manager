using UserManager.Domain.Entities;
using UserManager.Domain.Events.Entities;
using UserManager.Domain.Exceptions;
using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.UnitTests.Entities;

public sealed class UserTests
{
    private const string AnaSub = "kc-ana";
    private static readonly Email AnaEmail = Email.Create(" ANA@Example.com ");

    [Fact]
    public void Provision_WithValidData_SetsIdentityAndRaisesUserCreated()
    {
        // Act
        var user = User.Provision(AnaSub, "Ana", AnaEmail);

        // Assert
        Assert.Equal(AnaSub, user.IdentityProviderId);
        Assert.Equal(Email.Create("ana@example.com"), user.Email);
        var created = Assert.IsType<UserCreatedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal(user.Id, created.UserId);
        Assert.Equal("ana@example.com", created.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Provision_WithBlankIdentityProviderId_ThrowsDomainException(string? identityProviderId)
    {
        // Act
        var provision = () => User.Provision(identityProviderId!, "Ana", AnaEmail);

        // Assert
        Assert.Throws<DomainException>(provision);
    }

    [Fact]
    public void Provision_WithIdentityProviderIdTooLong_ThrowsDomainException()
    {
        // Act
        var provision = () => User.Provision(new string('a', User.MaxIdentityProviderIdLength + 1), "Ana", AnaEmail);

        // Assert
        Assert.Throws<DomainException>(provision);
    }

    [Fact]
    public void Provision_WithoutEmail_ThrowsDomainException()
    {
        // Act
        var provision = () => User.Provision(AnaSub, "Ana", null!);

        // Assert
        Assert.Throws<DomainException>(provision);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Provision_WithoutName_ThrowsDomainException(string? name)
    {
        // Act
        var provision = () => User.Provision(AnaSub, name!, AnaEmail);

        // Assert
        Assert.Throws<DomainException>(provision);
    }

    [Fact]
    public void Provision_WithNameTooLong_ThrowsDomainException()
    {
        // Act
        var provision = () => User.Provision(AnaSub, new string('a', User.MAX_NAME_LENGTH + 1), AnaEmail);

        // Assert
        Assert.Throws<DomainException>(provision);
    }

    [Fact]
    public void Rename_ChangesNameAndRaisesUserUpdated()
    {
        // Arrange
        var user = ProvisionedAna();

        // Act
        user.Rename("Ana Souza");

        // Assert
        Assert.Equal("Ana Souza", user.Name);
        var updated = Assert.IsType<UserUpdatedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal("Ana Souza", updated.Name);
    }

    [Fact]
    public void Rename_WithoutName_ThrowsAndKeepsCurrentName()
    {
        // Arrange
        var user = ProvisionedAna();

        // Act
        var rename = () => user.Rename("  ");

        // Assert
        Assert.Throws<DomainException>(rename);
        Assert.Equal("Ana", user.Name);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void SyncEmail_WithSameEmail_ReturnsFalseAndRaisesNothing()
    {
        // Arrange
        var user = ProvisionedAna();

        // Act
        var changed = user.SyncEmail(Email.Create("ana@example.com"));

        // Assert
        Assert.False(changed);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void SyncEmail_WithNewEmail_UpdatesAndRaisesUserUpdated()
    {
        // Arrange
        var user = ProvisionedAna();

        // Act
        var changed = user.SyncEmail(Email.Create("Ana.Souza@Example.com"));

        // Assert
        Assert.True(changed);
        Assert.Equal(Email.Create("ana.souza@example.com"), user.Email);
        var updated = Assert.IsType<UserUpdatedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal("ana.souza@example.com", updated.Email);
    }

    [Fact]
    public void SyncEmail_WithoutEmail_ThrowsDomainException()
    {
        // Arrange
        var user = ProvisionedAna();

        // Act
        Action sync = () => user.SyncEmail(null!);

        // Assert
        Assert.Throws<DomainException>(sync);
    }

    [Fact]
    public void Delete_RaisesUserDeletedWithEmail()
    {
        // Arrange
        var user = ProvisionedAna();

        // Act
        user.Delete();

        // Assert
        var deleted = Assert.IsType<UserDeletedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal("ana@example.com", deleted.Email);
    }

    private static User ProvisionedAna()
    {
        var user = User.Provision(AnaSub, "Ana", AnaEmail);
        user.ClearDomainEvents();
        return user;
    }
}
