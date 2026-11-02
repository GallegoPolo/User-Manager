using Audit.Contracts;
using MassTransit;

namespace AuditService.Infrastructure.Messagings;

public sealed class AuditEventConsumer : IConsumer<AuditEventOccurred>
{
    private readonly AuditLogRecorder _recorder;

    public AuditEventConsumer(AuditLogRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task Consume(ConsumeContext<AuditEventOccurred> context) =>
        _recorder.RecordAsync(AuditEventMapper.ToAuditLog(context.Message), context.CancellationToken);
}
