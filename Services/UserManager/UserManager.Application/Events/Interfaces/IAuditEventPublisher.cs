namespace UserManager.Application.Events.Interfaces;

public interface IAuditEventPublisher
{
    Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
