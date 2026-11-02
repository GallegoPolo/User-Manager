using MediatR;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;
using UserManager.Contracts;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Contacts.EventHandlers;

public sealed class ContactCreatedIntegrationEventHandler : INotificationHandler<DomainEventNotification<ContactCreatedDomainEvent>>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;

    public ContactCreatedIntegrationEventHandler(IIntegrationEventPublisher integrationEventPublisher)
    {
        _integrationEventPublisher = integrationEventPublisher;
    }

    public Task Handle(DomainEventNotification<ContactCreatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var integrationEvent = new ContactCreated
        {
            EventId = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredAt,
            ContactId = domainEvent.ContactId,
            OwnerId = domainEvent.OwnerId,
            Name = domainEvent.Name,
            Email = domainEvent.Email,
            Phone = domainEvent.Phone
        };

        return _integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
