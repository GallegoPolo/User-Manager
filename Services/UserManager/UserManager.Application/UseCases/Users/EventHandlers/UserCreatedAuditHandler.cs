using MediatR;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Users.EventHandlers;

public sealed class UserCreatedAuditHandler : INotificationHandler<DomainEventNotification<UserCreatedDomainEvent>>
{
    private readonly IAuditEventPublisher _auditEventPublisher;

    public UserCreatedAuditHandler(IAuditEventPublisher auditEventPublisher)
    {
        _auditEventPublisher = auditEventPublisher;
    }

    public Task Handle(DomainEventNotification<UserCreatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var auditEvent = new AuditEvent(domainEvent.EventId,
                                        domainEvent.OccurredAt,
                                        EventType: "user.created",
                                        AggregateId: domainEvent.UserId.ToString(),
                                        AggregateType: "User",
                                        Payload: new UserCreatedAuditPayload(domainEvent.Name, domainEvent.Email));

        return _auditEventPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
