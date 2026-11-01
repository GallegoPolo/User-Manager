using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Handlers;
using UserManager.Domain.Entities;
using UserManager.Domain.Services;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UnitTests.UseCases.Users;

public sealed class CreateUserHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Handle_WhenNormalizedEmailAlreadyExists_FailsOnEmailAndSavesNothing()
    {
        // Arrange
        _users.With(User.Create("Ana", Email.Create("ana@example.com")));
        var command = new CreateUserCommand("Outra Ana", "  ANA@Example.com ");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Email", Assert.Single(result.Errors).Property);
        Assert.Single(_users.Users);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_WithNewEmail_StoresNormalizedEmailBeforeSaving()
    {
        // Arrange
        Email? emailAtSave = null;
        _unitOfWork.OnSave = () => emailAtSave = _users.Users.SingleOrDefault()?.Email;

        // Act
        var result = await CreateHandler().Handle(new CreateUserCommand("Ana", " ANA@Example.com "), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(Email.Create("ana@example.com"), emailAtSave);
    }

    private CreateUserHandler CreateHandler() => new(_users, _unitOfWork, new UserDomainService(_users));
}
