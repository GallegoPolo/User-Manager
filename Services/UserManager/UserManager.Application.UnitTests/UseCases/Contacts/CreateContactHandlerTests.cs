using UserManager.Application.Common;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Application.UseCases.Contacts.Handlers;
using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class CreateContactHandlerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private readonly FakeUserRepository _users = new FakeUserRepository().WithUser(OwnerId);
    private readonly FakeContactRepository _contacts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Handle_WhenOwnerDoesNotExist_ReturnsNotFoundAndSavesNothing()
    {
        // Arrange
        var command = new CreateContactCommand(Guid.NewGuid(), "Ana", "ana@example.com", null);

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(_contacts.Contacts);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyInOwnersBook_ReturnsConflictComparingNormalizedEmail()
    {
        // Arrange
        _contacts.With(Contact.Create(OwnerId, "Ana", Email.Create("ana@example.com"), phone: null));
        var command = new CreateContactCommand(OwnerId, "Ana Souza", "  ANA@Example.com ", null);

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Single(_contacts.Contacts);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenSameEmailBelongsToAnotherOwner_CreatesContact()
    {
        // Arrange
        _contacts.With(Contact.Create(Guid.NewGuid(), "Ana", Email.Create("ana@example.com"), phone: null));
        var command = new CreateContactCommand(OwnerId, "Ana", "ana@example.com", null);

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithValidData_AddsContactSavesOnceAndReturnsId()
    {
        // Arrange
        var command = new CreateContactCommand(OwnerId, "Ana", "ana@example.com", "(11) 98765-4321");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var contact = Assert.Single(_contacts.Contacts);
        Assert.Equal(contact.Id, result.Value);
        Assert.Equal(OwnerId, contact.OwnerId);
        Assert.Equal(PhoneNumber.Create("11987654321"), contact.Phone);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithBlankEmail_TreatsEmailAsAbsent(string? blankEmail)
    {
        // Arrange
        var command = new CreateContactCommand(OwnerId, "Ana", blankEmail, "11987654321");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(_contacts.Contacts).Email);
    }

    private CreateContactHandler CreateHandler() => new(_users, _contacts, _unitOfWork);
}
