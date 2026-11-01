using MediatR;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Contacts.EventHandlers;

public sealed class ContactUpdatedAuditHandler : INotificationHandler<DomainEventNotification<ContactUpdatedDomainEvent>>
{
    private readonly IAuditEventPublisher _auditEventPublisher;

    public ContactUpdatedAuditHandler(IAuditEventPublisher auditEventPublisher)
    {
        _auditEventPublisher = auditEventPublisher;
    }

    public Task Handle(DomainEventNotification<ContactUpdatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var auditEvent = new AuditEvent(domainEvent.EventId,
                                        domainEvent.OccurredAt,
                                        EventType: "contact.updated",
                                        AggregateId: domainEvent.ContactId.ToString(),
                                        AggregateType: "Contact",
                                        Payload: new ContactAuditPayload(domainEvent.OwnerId, domainEvent.Name, domainEvent.Email, domainEvent.Phone));

        return _auditEventPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
