using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AuditService.Api.Extensions;

public static class ObservabilityExtensions
{
    private const string ServiceName = "audit-service-api";

    public static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation())
            .UseOtlpExporter();

        return builder;
    }
}
