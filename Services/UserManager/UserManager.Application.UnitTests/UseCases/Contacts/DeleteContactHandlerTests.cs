using UserManager.Application.Common;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Application.UseCases.Contacts.Handlers;
using UserManager.Domain.Entities;
using UserManager.Domain.Events.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class DeleteContactHandlerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private readonly Contact _ana = Contact.Create(OwnerId, "Ana", Email.Create("ana@example.com"), phone: null);
    private readonly FakeContactRepository _contacts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    public DeleteContactHandlerTests()
    {
        _contacts.With(_ana);
    }

    [Fact]
    public async Task Handle_WhenContactBelongsToAnotherOwner_ReturnsNotFoundAndKeepsContact()
    {
        // Act
        var result = await CreateHandler(Guid.NewGuid()).Handle(new DeleteContactCommand(_ana.Id), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Single(_contacts.Contacts);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenContactBelongsToOwner_RemovesRaisesEventAndSavesOnce()
    {
        // Act
        var result = await CreateHandler().Handle(new DeleteContactCommand(_ana.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_contacts.Contacts);
        Assert.Contains(_ana.DomainEvents, e => e is ContactDeletedDomainEvent);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_DeletesAndRemovesContactBeforeSaving()
    {
        // Arrange
        var removedAtSave = false;
        var deletedEventAtSave = false;
        _unitOfWork.OnSave = () =>
        {
            removedAtSave = !_contacts.Contacts.Contains(_ana);
            deletedEventAtSave = _ana.DomainEvents.Any(e => e is ContactDeletedDomainEvent);
        };

        // Act
        await CreateHandler().Handle(new DeleteContactCommand(_ana.Id), CancellationToken.None);

        // Assert
        Assert.True(removedAtSave);
        Assert.True(deletedEventAtSave);
    }

    private DeleteContactHandler CreateHandler(Guid? currentUserId = null) => new(new FakeCurrentUser().ProvisionedAs(currentUserId ?? OwnerId), _contacts, _unitOfWork);
}
