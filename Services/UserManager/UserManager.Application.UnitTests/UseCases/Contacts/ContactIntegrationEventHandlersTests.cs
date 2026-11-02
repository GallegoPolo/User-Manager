using UserManager.Application.Events;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Contacts.EventHandlers;
using UserManager.Contracts;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class ContactIntegrationEventHandlersTests
{
    [Fact]
    public async Task ContactCreated_PublishesContactCreatedIntegrationEvent()
    {
        // Arrange
        var publisher = new FakeIntegrationEventPublisher();
        var domainEvent = new ContactCreatedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@example.com", null);

        // Act
        await new ContactCreatedIntegrationEventHandler(publisher).Handle(new DomainEventNotification<ContactCreatedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(publisher.Published);
        Assert.Equal(typeof(ContactCreated), published.PublishedAs);
        Assert.Equal(new ContactCreated
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            ContactId = domainEvent.ContactId,
            OwnerId = domainEvent.OwnerId,
            Name = "Ana",
            Email = "ana@example.com",
            Phone = null
        }, published.Message);
    }

    [Fact]
    public async Task ContactUpdated_PublishesContactUpdatedIntegrationEvent()
    {
        // Arrange
        var publisher = new FakeIntegrationEventPublisher();
        var domainEvent = new ContactUpdatedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", null, "11987654321");

        // Act
        await new ContactUpdatedIntegrationEventHandler(publisher).Handle(new DomainEventNotification<ContactUpdatedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(publisher.Published);
        Assert.Equal(typeof(ContactUpdated), published.PublishedAs);
        Assert.Equal(new ContactUpdated
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            ContactId = domainEvent.ContactId,
            OwnerId = domainEvent.OwnerId,
            Name = "Ana",
            Email = null,
            Phone = "11987654321"
        }, published.Message);
    }

    [Fact]
    public async Task ContactDeleted_PublishesContactDeletedIntegrationEvent()
    {
        // Arrange
        var publisher = new FakeIntegrationEventPublisher();
        var domainEvent = new ContactDeletedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@example.com", "11987654321");

        // Act
        await new ContactDeletedIntegrationEventHandler(publisher).Handle(new DomainEventNotification<ContactDeletedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(publisher.Published);
        Assert.Equal(typeof(ContactDeleted), published.PublishedAs);
        Assert.Equal(new ContactDeleted
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            ContactId = domainEvent.ContactId,
            OwnerId = domainEvent.OwnerId,
            Name = "Ana",
            Email = "ana@example.com",
            Phone = "11987654321"
        }, published.Message);
    }
}
