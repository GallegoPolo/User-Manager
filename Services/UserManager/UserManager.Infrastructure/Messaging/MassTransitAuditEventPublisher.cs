using System.Text.Json;
using Audit.Contracts;
using MassTransit;
using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;

namespace UserManager.Infrastructure.Messaging;

public sealed class MassTransitAuditEventPublisher : IAuditEventPublisher
{
    private const string SystemActor = "system";

    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitAuditEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var message = new AuditEventOccurred
        {
            EventId = auditEvent.EventId,
            EventType = auditEvent.EventType,
            AggregateId = auditEvent.AggregateId,
            AggregateType = auditEvent.AggregateType,
            PerformedBy = SystemActor,
            PerformedAt = auditEvent.OccurredAt,
            Payload = JsonSerializer.SerializeToElement(auditEvent.Payload, auditEvent.Payload.GetType())
        };

        return _publishEndpoint.Publish(message, context => context.MessageId = auditEvent.EventId, cancellationToken);
    }
}
