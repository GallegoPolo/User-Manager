using MediatR;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;
using UserManager.Domain.Events.Entities;

namespace UserManager.Application.UseCases.Users.EventHandlers;

public sealed class UserUpdatedAuditHandler : INotificationHandler<DomainEventNotification<UserUpdatedDomainEvent>>
{
    private readonly IAuditEventPublisher _auditEventPublisher;

    public UserUpdatedAuditHandler(IAuditEventPublisher auditEventPublisher)
    {
        _auditEventPublisher = auditEventPublisher;
    }

    public Task Handle(DomainEventNotification<UserUpdatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var auditEvent = new AuditEvent(domainEvent.EventId,
                                        domainEvent.OccurredAt,
                                        EventType: "user.updated",
                                        AggregateId: domainEvent.UserId.ToString(),
                                        AggregateType: "User",
                                        Payload: new UserUpdatedAuditPayload(domainEvent.Name, domainEvent.Email));

        return _auditEventPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
