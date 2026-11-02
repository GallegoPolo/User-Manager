using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace AuditService.Infrastructure.Messagings;

public static class MessagingExtensions
{
    public static IServiceCollection AddAuditConsumers(this IServiceCollection services, RabbitMqSettings settings)
    {
        services.AddScoped<AuditLogRecorder>();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<AuditEventConsumer>();
            bus.AddConsumer<UserEventsConsumer>();
            bus.AddConsumer<ContactEventsConsumer>();

            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(settings.HostName, settings.Port, settings.VirtualHost, host =>
                {
                    host.Username(settings.UserName);
                    host.Password(settings.Password);
                });

                rabbit.ReceiveEndpoint(settings.QueueName, endpoint =>
                {
                    endpoint.PrefetchCount = settings.PrefetchCount;
                    endpoint.UseMessageRetry(retry =>
                    {
                        retry.Ignore<InvalidAuditEventException>();
                        retry.Intervals(settings.GetRetryIntervals());
                    });
                    endpoint.ConfigureConsumer<AuditEventConsumer>(context);
                    endpoint.ConfigureConsumer<UserEventsConsumer>(context);
                    endpoint.ConfigureConsumer<ContactEventsConsumer>(context);
                });
            });
        });

        return services;
    }
}
