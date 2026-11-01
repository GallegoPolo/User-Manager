using System.Text.Json;

namespace Audit.Contracts;

public sealed record AuditEventOccurred
{
    public required Guid EventId { get; init; }
    public required string EventType { get; init; }
    public required string AggregateId { get; init; }
    public required string AggregateType { get; init; }
    public required string PerformedBy { get; init; }
    public required DateTime PerformedAt { get; init; }
    public JsonElement? Payload { get; init; }
}
