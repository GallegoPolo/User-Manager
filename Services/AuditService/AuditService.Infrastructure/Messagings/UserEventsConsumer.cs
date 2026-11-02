using MassTransit;
using UserManager.Contracts;

namespace AuditService.Infrastructure.Messagings;

public sealed class UserEventsConsumer : IConsumer<UserCreated>, IConsumer<UserUpdated>, IConsumer<UserDeleted>
{
    private readonly AuditLogRecorder _recorder;

    public UserEventsConsumer(AuditLogRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task Consume(ConsumeContext<UserCreated> context) =>
        _recorder.RecordAsync(IntegrationEventMapper.ToAuditLog(context.Message), context.CancellationToken);

    public Task Consume(ConsumeContext<UserUpdated> context) =>
        _recorder.RecordAsync(IntegrationEventMapper.ToAuditLog(context.Message), context.CancellationToken);

    public Task Consume(ConsumeContext<UserDeleted> context) =>
        _recorder.RecordAsync(IntegrationEventMapper.ToAuditLog(context.Message), context.CancellationToken);
}
