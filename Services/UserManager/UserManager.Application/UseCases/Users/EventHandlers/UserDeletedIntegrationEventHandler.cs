using MediatR;
using UserManager.Application.Common;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;
using UserManager.Contracts;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Users.EventHandlers;

public sealed class UserDeletedIntegrationEventHandler : INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ICurrentUser _currentUser;

    public UserDeletedIntegrationEventHandler(IIntegrationEventPublisher integrationEventPublisher, ICurrentUser currentUser)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _currentUser = currentUser;
    }

    public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var integrationEvent = new UserDeleted
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            UserId = domainEvent.UserId,
            Name = domainEvent.Name,
            Email = domainEvent.Email,
            PerformedBy = _currentUser.IsProvisioned ? _currentUser.UserId : domainEvent.UserId
        };

        return _integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
