using MediatR;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Users.EventHandlers;

public sealed class UserDeletedAuditHandler : INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>
{
    private readonly IAuditEventPublisher _auditEventPublisher;

    public UserDeletedAuditHandler(IAuditEventPublisher auditEventPublisher)
    {
        _auditEventPublisher = auditEventPublisher;
    }

    public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var auditEvent = new AuditEvent(domainEvent.EventId,
                                        domainEvent.OccurredAt,
                                        EventType: "user.deleted",
                                        AggregateId: domainEvent.UserId.ToString(),
                                        AggregateType: "User",
                                        Payload: new UserDeletedAuditPayload(domainEvent.Name, domainEvent.Email));

        return _auditEventPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
