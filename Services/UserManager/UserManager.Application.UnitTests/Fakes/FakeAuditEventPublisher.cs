using UserManager.Application.Events;
using UserManager.Application.Events.Interfaces;

namespace UserManager.Application.UnitTests.Fakes;

public sealed class FakeAuditEventPublisher : IAuditEventPublisher
{
    private readonly List<AuditEvent> _published = [];

    public IReadOnlyList<AuditEvent> Published => _published;

    public Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        _published.Add(auditEvent);
        return Task.CompletedTask;
    }
}
