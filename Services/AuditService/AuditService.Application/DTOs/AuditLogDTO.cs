namespace AuditService.Application.DTOs
{
    public record AuditLogDTO(string Id,
                              Guid EventId,
                              string AggregateId,
                              string AggregateType,
                              string EventType,
                              string PerformedBy,
                              DateTime Timestamp,
                              Dictionary<string, object>? Payload);
}
