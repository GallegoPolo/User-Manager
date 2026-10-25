using System.Diagnostics;

namespace UserManager.Infrastructure.Outbox;

public static class OutboxTelemetry
{
    public const string ActivitySourceName = "UserManager.Outbox";

    private const string PublishActivityName = "outbox publish";
    private const string MessageIdTag = "outbox.message.id";

    private static readonly ActivitySource Source = new(ActivitySourceName);

    internal static Activity? StartPublishActivity(OutboxMessage message)
    {
        ActivityContext.TryParse(message.TraceParent, traceState: null, isRemote: true, out var originalRequest);

        var activity = Source.StartActivity(PublishActivityName, ActivityKind.Internal, originalRequest);
        activity?.SetTag(MessageIdTag, message.Id);

        return activity;
    }
}
