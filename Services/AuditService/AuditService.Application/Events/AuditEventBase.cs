namespace AuditService.Application.Events
{
    public abstract class AuditEventBase
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string EventType { get; set; } = string.Empty;
        public string AggregateId { get; set; } = string.Empty;
        public string AggregateType { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
        public Dictionary<string, object>? Payload { get; set; }
    }

    public class UserCreatedEvent : AuditEventBase
    {
    }

    public class UserUpdatedEvent : AuditEventBase
    {
    }

    public class UserDeletedEvent : AuditEventBase
    {
    }

    public class AdminLoggedInEvent : AuditEventBase
    {
    }

    public class ApiKeyCreatedEvent : AuditEventBase
    {
    }

    public class ApiKeyRevokedEvent : AuditEventBase
    {
    }
}
