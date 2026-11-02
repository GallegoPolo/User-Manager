using UserManager.Application.Events;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Users.EventHandlers;
using UserManager.Contracts;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UnitTests.UseCases.Users;

public sealed class UserIntegrationEventHandlersTests
{
    [Fact]
    public async Task UserCreated_PublishesUserCreatedIntegrationEvent()
    {
        // Arrange
        var publisher = new FakeIntegrationEventPublisher();
        var domainEvent = new UserCreatedDomainEvent(Guid.NewGuid(), "Ana", "ana@example.com");

        // Act
        await new UserCreatedIntegrationEventHandler(publisher).Handle(new DomainEventNotification<UserCreatedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(publisher.Published);
        Assert.Equal(typeof(UserCreated), published.PublishedAs);
        Assert.Equal(new UserCreated
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            UserId = domainEvent.UserId,
            Name = "Ana",
            Email = "ana@example.com"
        }, published.Message);
    }

    [Fact]
    public async Task UserUpdated_PublishesUserUpdatedIntegrationEvent()
    {
        // Arrange
        var publisher = new FakeIntegrationEventPublisher();
        var domainEvent = new UserUpdatedDomainEvent(Guid.NewGuid(), "Ana", "ana@example.com");

        // Act
        await new UserUpdatedIntegrationEventHandler(publisher).Handle(new DomainEventNotification<UserUpdatedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(publisher.Published);
        Assert.Equal(typeof(UserUpdated), published.PublishedAs);
        Assert.Equal(new UserUpdated
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            UserId = domainEvent.UserId,
            Name = "Ana",
            Email = "ana@example.com"
        }, published.Message);
    }

    [Fact]
    public async Task UserDeleted_PublishesUserDeletedIntegrationEvent()
    {
        // Arrange
        var publisher = new FakeIntegrationEventPublisher();
        var domainEvent = new UserDeletedDomainEvent(Guid.NewGuid(), "Ana", "ana@example.com");

        // Act
        await new UserDeletedIntegrationEventHandler(publisher).Handle(new DomainEventNotification<UserDeletedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(publisher.Published);
        Assert.Equal(typeof(UserDeleted), published.PublishedAs);
        Assert.Equal(new UserDeleted
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            UserId = domainEvent.UserId,
            Name = "Ana",
            Email = "ana@example.com"
        }, published.Message);
    }
}
