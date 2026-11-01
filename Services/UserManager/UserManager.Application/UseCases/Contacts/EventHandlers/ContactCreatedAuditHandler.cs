using MediatR;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Contacts.EventHandlers;

public sealed class ContactCreatedAuditHandler : INotificationHandler<DomainEventNotification<ContactCreatedDomainEvent>>
{
    private readonly IAuditEventPublisher _auditEventPublisher;

    public ContactCreatedAuditHandler(IAuditEventPublisher auditEventPublisher)
    {
        _auditEventPublisher = auditEventPublisher;
    }

    public Task Handle(DomainEventNotification<ContactCreatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var auditEvent = new AuditEvent(domainEvent.EventId,
                                        domainEvent.OccurredAt,
                                        EventType: "contact.created",
                                        AggregateId: domainEvent.ContactId.ToString(),
                                        AggregateType: "Contact",
                                        Payload: new ContactAuditPayload(domainEvent.OwnerId, domainEvent.Name, domainEvent.Email, domainEvent.Phone));

        return _auditEventPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
