using UserManager.Application.Common;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Application.UseCases.Contacts.Handlers;
using UserManager.Domain.Entities;
using UserManager.Domain.Events.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class UpdateContactHandlerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private readonly Contact _ana = Contact.Create(OwnerId, "Ana", Email.Create("ana@example.com"), phone: null);
    private readonly Contact _bruno = Contact.Create(OwnerId, "Bruno", Email.Create("bruno@example.com"), phone: null);
    private readonly FakeContactRepository _contacts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    public UpdateContactHandlerTests()
    {
        _contacts.With(_ana).With(_bruno);
    }

    [Fact]
    public async Task Handle_WhenContactBelongsToAnotherOwner_ReturnsNotFoundAndSavesNothing()
    {
        // Arrange
        var command = new UpdateContactCommand(Guid.NewGuid(), _ana.Id, "Intrusa", "intrusa@example.com", null);

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Ana", _ana.Name);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_KeepingOwnEmail_IsNotAConflict()
    {
        // Arrange
        var command = new UpdateContactCommand(OwnerId, _ana.Id, "Ana Souza", " ANA@Example.com ", "11987654321");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Ana Souza", _ana.Name);
        Assert.Equal(PhoneNumber.Create("11987654321"), _ana.Phone);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_UsingEmailOfAnotherContactInSameBook_ReturnsConflictAndKeepsContact()
    {
        // Arrange
        var command = new UpdateContactCommand(OwnerId, _ana.Id, "Ana", "bruno@example.com", null);

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(Email.Create("ana@example.com"), _ana.Email);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_WithOnlyPhone_RemovesEmail()
    {
        // Arrange
        var command = new UpdateContactCommand(OwnerId, _ana.Id, "Ana", "", "11987654321");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(_ana.Email);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_UpdatesContactBeforeSaving()
    {
        // Arrange
        string? nameAtSave = null;
        var updatedEventAtSave = false;
        _unitOfWork.OnSave = () =>
        {
            nameAtSave = _ana.Name;
            updatedEventAtSave = _ana.DomainEvents.Any(e => e is ContactUpdatedDomainEvent);
        };
        var command = new UpdateContactCommand(OwnerId, _ana.Id, "Ana Souza", "ana@example.com", null);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Ana Souza", nameAtSave);
        Assert.True(updatedEventAtSave);
    }

    private UpdateContactHandler CreateHandler() => new(_contacts, _unitOfWork);
}
