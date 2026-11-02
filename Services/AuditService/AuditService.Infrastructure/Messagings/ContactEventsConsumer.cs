using MassTransit;
using UserManager.Contracts;

namespace AuditService.Infrastructure.Messagings;

public sealed class ContactEventsConsumer : IConsumer<ContactCreated>, IConsumer<ContactUpdated>, IConsumer<ContactDeleted>
{
    private readonly AuditLogRecorder _recorder;

    public ContactEventsConsumer(AuditLogRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task Consume(ConsumeContext<ContactCreated> context) =>
        _recorder.RecordAsync(IntegrationEventMapper.ToAuditLog(context.Message), context.CancellationToken);

    public Task Consume(ConsumeContext<ContactUpdated> context) =>
        _recorder.RecordAsync(IntegrationEventMapper.ToAuditLog(context.Message), context.CancellationToken);

    public Task Consume(ConsumeContext<ContactDeleted> context) =>
        _recorder.RecordAsync(IntegrationEventMapper.ToAuditLog(context.Message), context.CancellationToken);
}
