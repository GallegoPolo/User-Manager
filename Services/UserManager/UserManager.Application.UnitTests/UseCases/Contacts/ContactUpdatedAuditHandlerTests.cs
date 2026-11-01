using UserManager.Application.Events;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Contacts.EventHandlers;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class ContactUpdatedAuditHandlerTests
{
    [Fact]
    public async Task Handle_PublishesContactUpdatedAuditEvent()
    {
        // Arrange
        var publisher = new FakeAuditEventPublisher();
        var domainEvent = new ContactUpdatedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", null, "11987654321");
        var notification = new DomainEventNotification<ContactUpdatedDomainEvent>(domainEvent);

        // Act
        await new ContactUpdatedAuditHandler(publisher).Handle(notification, CancellationToken.None);

        // Assert
        var published = Assert.Single(publisher.Published);
        Assert.Equal(new AuditEvent(domainEvent.EventId,
                                    domainEvent.OccurredAt,
                                    "contact.updated",
                                    domainEvent.ContactId.ToString(),
                                    "Contact",
                                    new ContactAuditPayload(domainEvent.OwnerId, "Ana", null, "11987654321")),
                     published);
    }
}
