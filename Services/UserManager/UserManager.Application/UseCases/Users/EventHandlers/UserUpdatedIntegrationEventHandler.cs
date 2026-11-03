using MediatR;
using UserManager.Application.Common;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;
using UserManager.Contracts;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Users.EventHandlers;

public sealed class UserUpdatedIntegrationEventHandler : INotificationHandler<DomainEventNotification<UserUpdatedDomainEvent>>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ICurrentUser _currentUser;

    public UserUpdatedIntegrationEventHandler(IIntegrationEventPublisher integrationEventPublisher, ICurrentUser currentUser)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _currentUser = currentUser;
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
            Email = domainEvent.Email,
            PerformedBy = _currentUser.IsProvisioned ? _currentUser.UserId : domainEvent.UserId
        };

        return _integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
