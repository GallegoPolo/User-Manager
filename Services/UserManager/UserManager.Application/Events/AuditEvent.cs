namespace UserManager.Application.Events;

public sealed record AuditEvent(Guid EventId,
                                DateTime OccurredAt,
                                string EventType,
                                string AggregateId,
                                string AggregateType,
                                object Payload);
