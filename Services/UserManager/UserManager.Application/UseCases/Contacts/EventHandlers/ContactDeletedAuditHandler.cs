using MediatR;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Contacts.EventHandlers;

public sealed class ContactDeletedAuditHandler : INotificationHandler<DomainEventNotification<ContactDeletedDomainEvent>>
{
    private readonly IAuditEventPublisher _auditEventPublisher;

    public ContactDeletedAuditHandler(IAuditEventPublisher auditEventPublisher)
    {
        _auditEventPublisher = auditEventPublisher;
    }

    public Task Handle(DomainEventNotification<ContactDeletedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var auditEvent = new AuditEvent(domainEvent.EventId,
                                        domainEvent.OccurredAt,
                                        EventType: "contact.deleted",
                                        AggregateId: domainEvent.ContactId.ToString(),
                                        AggregateType: "Contact",
                                        Payload: new ContactAuditPayload(domainEvent.OwnerId, domainEvent.Name, domainEvent.Email, domainEvent.Phone));

        return _auditEventPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
