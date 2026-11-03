using UserManager.Application.Common;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Handlers;
using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UnitTests.UseCases.Users;

public sealed class ProvisionCurrentUserHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsItsIdAndSyncsEmail()
    {
        // Arrange
        var existing = User.Provision("kc-ana", "Ana", Email.Create("old@example.com"));
        _users.With(existing);
        var currentUser = new FakeCurrentUser { IdentityProviderId = "kc-ana", Email = "new@example.com" };

        // Act
        var result = await Handle(currentUser);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Value);
        Assert.Equal(Email.Create("new@example.com"), existing.Email);
        Assert.Single(_users.Users);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenUserExistsWithSameEmail_SavesNothing()
    {
        // Arrange
        _users.With(User.Provision("kc-ana", "Ana", Email.Create("ana@example.com")));

        // Act
        var result = await Handle(new FakeCurrentUser { IdentityProviderId = "kc-ana", Email = "ANA@example.com" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ProvisionsWithTokenData()
    {
        // Arrange
        var currentUser = new FakeCurrentUser { IdentityProviderId = "kc-bia", Email = "bia@example.com", Name = "Bia Lima" };

        // Act
        var result = await Handle(currentUser);

        // Assert
        Assert.True(result.IsSuccess);
        var created = Assert.Single(_users.Users);
        Assert.Equal(created.Id, result.Value);
        Assert.Equal("kc-bia", created.IdentityProviderId);
        Assert.Equal("Bia Lima", created.Name);
        Assert.Equal(Email.Create("bia@example.com"), created.Email);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_WithoutNameClaim_UsesEmailAsName()
    {
        // Act
        await Handle(new FakeCurrentUser { IdentityProviderId = "kc-bia", Email = "bia@example.com", Name = null });

        // Assert
        Assert.Equal("bia@example.com", Assert.Single(_users.Users).Name);
    }

    [Fact]
    public async Task Handle_WithNameClaimTooLong_TruncatesToMaxLength()
    {
        // Act
        await Handle(new FakeCurrentUser { IdentityProviderId = "kc-bia", Name = new string('a', User.MAX_NAME_LENGTH + 10) });

        // Assert
        Assert.Equal(User.MAX_NAME_LENGTH, Assert.Single(_users.Users).Name.Length);
    }

    [Fact]
    public async Task Handle_WhenAnotherRequestWinsTheRace_ReturnsWinnerId()
    {
        // Arrange
        var winner = User.Provision("kc-ana", "Ana", Email.Create("ana@example.com"));
        _unitOfWork.OnSave = () =>
        {
            _users.ReplaceAllWith(winner);
            throw new UniqueConstraintViolationException("IX_Users_IdentityProviderId", new Exception());
        };

        // Act
        var result = await Handle(new FakeCurrentUser { IdentityProviderId = "kc-ana" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(winner.Id, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ana@localhost")]
    public async Task Handle_WithInvalidEmailClaim_FailsWithoutSaving(string? email)
    {
        // Act
        var result = await Handle(new FakeCurrentUser { Email = email });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Email", Assert.Single(result.Errors).Property);
        Assert.Empty(_users.Users);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithoutSubClaim_FailsWithoutSaving(string sub)
    {
        // Act
        var result = await Handle(new FakeCurrentUser { IdentityProviderId = sub });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("IdentityProviderId", Assert.Single(result.Errors).Property);
        Assert.Empty(_users.Users);
    }

    private Task<Result<Guid>> Handle(FakeCurrentUser currentUser) =>
        new ProvisionCurrentUserHandler(currentUser, _users, _unitOfWork).Handle(new ProvisionCurrentUserCommand(), CancellationToken.None);
}
