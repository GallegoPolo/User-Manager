using UserManager.Application.Events;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Users.EventHandlers;
using UserManager.Contracts;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UnitTests.UseCases.Users;

public sealed class UserIntegrationEventHandlersTests
{
    private static readonly Guid OperatorId = Guid.NewGuid();

    private readonly FakeIntegrationEventPublisher _publisher = new();

    [Fact]
    public async Task UserCreated_DuringJustInTimeProvisioning_PublishesThePersonAsAuthor()
    {
        // Arrange
        var domainEvent = new UserCreatedDomainEvent(Guid.NewGuid(), "Ana", "ana@example.com");

        // Act
        await new UserCreatedIntegrationEventHandler(_publisher, new FakeCurrentUser()).Handle(new DomainEventNotification<UserCreatedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(_publisher.Published);
        Assert.Equal(typeof(UserCreated), published.PublishedAs);
        Assert.Equal(new UserCreated
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            UserId = domainEvent.UserId,
            Name = "Ana",
            Email = "ana@example.com",
            PerformedBy = domainEvent.UserId
        }, published.Message);
    }

    [Fact]
    public async Task UserUpdated_ByOperator_PublishesOperatorAsAuthor()
    {
        // Arrange
        var domainEvent = new UserUpdatedDomainEvent(Guid.NewGuid(), "Ana", "ana@example.com");

        // Act
        await new UserUpdatedIntegrationEventHandler(_publisher, new FakeCurrentUser().ProvisionedAs(OperatorId)).Handle(new DomainEventNotification<UserUpdatedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(_publisher.Published);
        Assert.Equal(typeof(UserUpdated), published.PublishedAs);
        Assert.Equal(new UserUpdated
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            UserId = domainEvent.UserId,
            Name = "Ana",
            Email = "ana@example.com",
            PerformedBy = OperatorId
        }, published.Message);
    }

    [Fact]
    public async Task UserUpdated_DuringJustInTimeEmailSync_PublishesThePersonAsAuthor()
    {
        // Arrange
        var domainEvent = new UserUpdatedDomainEvent(Guid.NewGuid(), "Ana", "novo@example.com");

        // Act
        await new UserUpdatedIntegrationEventHandler(_publisher, new FakeCurrentUser()).Handle(new DomainEventNotification<UserUpdatedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        Assert.Equal(domainEvent.UserId, ((UserUpdated)Assert.Single(_publisher.Published).Message).PerformedBy);
    }

    [Fact]
    public async Task UserDeleted_ByOperator_PublishesOperatorAsAuthor()
    {
        // Arrange
        var domainEvent = new UserDeletedDomainEvent(Guid.NewGuid(), "Ana", "ana@example.com");

        // Act
        await new UserDeletedIntegrationEventHandler(_publisher, new FakeCurrentUser().ProvisionedAs(OperatorId)).Handle(new DomainEventNotification<UserDeletedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(_publisher.Published);
        Assert.Equal(typeof(UserDeleted), published.PublishedAs);
        Assert.Equal(new UserDeleted
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            UserId = domainEvent.UserId,
            Name = "Ana",
            Email = "ana@example.com",
            PerformedBy = OperatorId
        }, published.Message);
    }
}
