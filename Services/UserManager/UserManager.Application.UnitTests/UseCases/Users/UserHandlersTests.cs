using UserManager.Application.Common;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Handlers;
using UserManager.Application.UseCases.Users.Queries;
using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UnitTests.UseCases.Users;

public sealed class UserHandlersTests
{
    private readonly User _ana = User.Provision("kc-ana", "Ana", Email.Create("ana@example.com"));
    private readonly User _bia = User.Provision("kc-bia", "Bia", Email.Create("bia@example.com"));
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    public UserHandlersTests()
    {
        _users.With(_ana).With(_bia);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsCurrentUser()
    {
        // Act
        var result = await new GetCurrentUserHandler(CurrentUser(_ana), _users).Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(_ana.Id, result.Value!.Id);
        Assert.Equal("ana@example.com", result.Value.Email);
    }

    [Fact]
    public async Task GetCurrentUser_WhenNoLongerExists_ReturnsNotFound()
    {
        // Act
        var result = await new GetCurrentUserHandler(new FakeCurrentUser().ProvisionedAs(Guid.NewGuid()), _users).Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task RenameCurrentUser_RenamesOnlyCurrentUser()
    {
        // Act
        var result = await new RenameCurrentUserHandler(CurrentUser(_ana), _users, _unitOfWork).Handle(new RenameCurrentUserCommand("Ana Souza"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Ana Souza", _ana.Name);
        Assert.Equal("Bia", _bia.Name);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RenameUser_RenamesTheGivenUser()
    {
        // Act
        var result = await new RenameUserHandler(_users, _unitOfWork).Handle(new RenameUserCommand(_bia.Id, "Bia Lima"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Bia Lima", _bia.Name);
    }

    [Fact]
    public async Task RenameUser_WhenMissing_ReturnsNotFoundAndSavesNothing()
    {
        // Act
        var result = await new RenameUserHandler(_users, _unitOfWork).Handle(new RenameUserCommand(Guid.NewGuid(), "X"), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetUserById_WhenMissing_ReturnsNotFound()
    {
        // Act
        var result = await new GetUserByIdHandler(_users).Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteUser_RemovesTheGivenUser()
    {
        // Act
        var result = await new DeleteUserHandler(_users, _unitOfWork).Handle(new DeleteUserCommand(_bia.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(_bia, _users.Users);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task DeleteUser_WhenMissing_ReturnsNotFoundAndSavesNothing()
    {
        // Act
        var result = await new DeleteUserHandler(_users, _unitOfWork).Handle(new DeleteUserCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    private static FakeCurrentUser CurrentUser(User user) =>
        new FakeCurrentUser { IdentityProviderId = user.IdentityProviderId }.ProvisionedAs(user.Id);
}
