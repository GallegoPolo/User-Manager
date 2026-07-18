namespace UserManager.Application.Events.Interfaces
{
    public interface IAuditEventPublisher
    {
        Task PublishAsync(string eventType,
                          string aggregateId,
                          string aggregateType,
                          string performedBy,
                          DateTime occurredAt,
                          string payloadJson,
                          CancellationToken cancellationToken = default);
    }
}
