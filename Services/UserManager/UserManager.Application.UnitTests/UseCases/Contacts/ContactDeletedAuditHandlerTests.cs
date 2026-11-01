using UserManager.Application.Events;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Contacts.EventHandlers;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class ContactDeletedAuditHandlerTests
{
    [Fact]
    public async Task Handle_PublishesContactDeletedAuditEvent()
    {
        // Arrange
        var publisher = new FakeAuditEventPublisher();
        var domainEvent = new ContactDeletedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@example.com", null);
        var notification = new DomainEventNotification<ContactDeletedDomainEvent>(domainEvent);

        // Act
        await new ContactDeletedAuditHandler(publisher).Handle(notification, CancellationToken.None);

        // Assert
        var published = Assert.Single(publisher.Published);
        Assert.Equal(new AuditEvent(domainEvent.EventId,
                                    domainEvent.OccurredAt,
                                    "contact.deleted",
                                    domainEvent.ContactId.ToString(),
                                    "Contact",
                                    new ContactAuditPayload(domainEvent.OwnerId, "Ana", "ana@example.com", null)),
                     published);
    }
}
