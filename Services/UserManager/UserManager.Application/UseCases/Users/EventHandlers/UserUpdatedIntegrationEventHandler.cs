using MediatR;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;
using UserManager.Contracts;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Users.EventHandlers;

public sealed class UserUpdatedIntegrationEventHandler : INotificationHandler<DomainEventNotification<UserUpdatedDomainEvent>>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;

    public UserUpdatedIntegrationEventHandler(IIntegrationEventPublisher integrationEventPublisher)
    {
        _integrationEventPublisher = integrationEventPublisher;
    }

    public Task Handle(DomainEventNotification<UserUpdatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var integrationEvent = new UserUpdated
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            UserId = domainEvent.UserId,
            Name = domainEvent.Name,
            Email = domainEvent.Email
        };

        return _integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
