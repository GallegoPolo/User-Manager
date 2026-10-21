using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AuditService.Worker;

public static class ObservabilityExtensions
{
    private const string ServiceName = "audit-service-worker";
    private const string RabbitMqSubscriberSourceName = "RabbitMQ.Client.Subscriber";

    public static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing.AddSource(RabbitMqSubscriberSourceName))
            .UseOtlpExporter();

        return builder;
    }
}
