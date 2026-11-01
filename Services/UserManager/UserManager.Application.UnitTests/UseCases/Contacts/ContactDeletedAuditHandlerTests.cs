using System.Text.Json;
using UserManager.Application.Events;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Contacts.EventHandlers;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class ContactDeletedAuditHandlerTests
{
    [Fact]
    public async Task Handle_AddsContactDeletedMessageToOutbox()
    {
        // Arrange
        var outbox = new FakeOutboxRepository();
        var domainEvent = new ContactDeletedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@example.com", null);
        var notification = new DomainEventNotification<ContactDeletedDomainEvent>(domainEvent);

        // Act
        await new ContactDeletedAuditHandler(outbox).Handle(notification, CancellationToken.None);

        // Assert
        var entry = Assert.Single(outbox.Entries);
        Assert.Equal("contact.deleted", entry.EventType);
        Assert.Equal(domainEvent.ContactId.ToString(), entry.AggregateId);
        Assert.Equal("Contact", entry.AggregateType);

        var payload = JsonSerializer.Deserialize<ContactAuditPayload>(entry.PayloadJson)!;
        Assert.Equal(new ContactAuditPayload(domainEvent.OwnerId, "Ana", "ana@example.com", null), payload);
    }
}
