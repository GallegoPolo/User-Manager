using UserManager.Application.Events;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Users.EventHandlers;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UnitTests.UseCases.Users;

public sealed class UserAuditHandlersTests
{
    [Fact]
    public async Task UserCreatedAuditHandler_PublishesUserCreatedAuditEvent()
    {
        // Arrange
        var publisher = new FakeAuditEventPublisher();
        var domainEvent = new UserCreatedDomainEvent(Guid.NewGuid(), "Ana", "ana@example.com");

        // Act
        await new UserCreatedAuditHandler(publisher).Handle(new DomainEventNotification<UserCreatedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(publisher.Published);
        Assert.Equal(new AuditEvent(domainEvent.EventId,
                                    domainEvent.OccurredAt,
                                    "user.created",
                                    domainEvent.UserId.ToString(),
                                    "User",
                                    new UserCreatedAuditPayload("Ana", "ana@example.com")),
                     published);
    }

    [Fact]
    public async Task UserUpdatedAuditHandler_PublishesUserUpdatedAuditEvent()
    {
        // Arrange
        var publisher = new FakeAuditEventPublisher();
        var domainEvent = new UserUpdatedDomainEvent(Guid.NewGuid(), "Ana", "ana@example.com");

        // Act
        await new UserUpdatedAuditHandler(publisher).Handle(new DomainEventNotification<UserUpdatedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(publisher.Published);
        Assert.Equal(new AuditEvent(domainEvent.EventId,
                                    domainEvent.OccurredAt,
                                    "user.updated",
                                    domainEvent.UserId.ToString(),
                                    "User",
                                    new UserUpdatedAuditPayload("Ana", "ana@example.com")),
                     published);
    }

    [Fact]
    public async Task UserDeletedAuditHandler_PublishesUserDeletedAuditEvent()
    {
        // Arrange
        var publisher = new FakeAuditEventPublisher();
        var domainEvent = new UserDeletedDomainEvent(Guid.NewGuid(), "Ana", "ana@example.com");

        // Act
        await new UserDeletedAuditHandler(publisher).Handle(new DomainEventNotification<UserDeletedDomainEvent>(domainEvent), CancellationToken.None);

        // Assert
        var published = Assert.Single(publisher.Published);
        Assert.Equal(new AuditEvent(domainEvent.EventId,
                                    domainEvent.OccurredAt,
                                    "user.deleted",
                                    domainEvent.UserId.ToString(),
                                    "User",
                                    new UserDeletedAuditPayload("Ana", "ana@example.com")),
                     published);
    }
}
