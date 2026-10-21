using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace UserManager.Api.Extensions;

public static class ObservabilityExtensions
{
    private const string ServiceName = "user-manager-api";
    private const string NpgsqlSourceName = "Npgsql";
    private const string RabbitMqPublisherSourceName = "RabbitMQ.Client.Publisher";

    public static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddSource(NpgsqlSourceName)
                .AddSource(RabbitMqPublisherSourceName))
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation())
            .UseOtlpExporter();

        return builder;
    }
}
