using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace UserManager.Bff.Observability;

public static class ObservabilityExtensions
{
    private const string ServiceName = "user-manager-bff";
    private const string YarpSourceName = "Yarp.ReverseProxy";

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
                .AddSource(YarpSourceName))
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation())
            .UseOtlpExporter();

        return builder;
    }
}
