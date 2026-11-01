using System.Text.Json;
using UserManager.Application.Events;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Contacts.EventHandlers;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class ContactUpdatedAuditHandlerTests
{
    [Fact]
    public async Task Handle_AddsContactUpdatedMessageToOutbox()
    {
        // Arrange
        var outbox = new FakeOutboxRepository();
        var domainEvent = new ContactUpdatedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", null, "11987654321");
        var notification = new DomainEventNotification<ContactUpdatedDomainEvent>(domainEvent);

        // Act
        await new ContactUpdatedAuditHandler(outbox).Handle(notification, CancellationToken.None);

        // Assert
        var entry = Assert.Single(outbox.Entries);
        Assert.Equal("contact.updated", entry.EventType);
        Assert.Equal(domainEvent.ContactId.ToString(), entry.AggregateId);
        Assert.Equal("Contact", entry.AggregateType);

        var payload = JsonSerializer.Deserialize<ContactAuditPayload>(entry.PayloadJson)!;
        Assert.Equal(new ContactAuditPayload(domainEvent.OwnerId, "Ana", null, "11987654321"), payload);
    }
}
