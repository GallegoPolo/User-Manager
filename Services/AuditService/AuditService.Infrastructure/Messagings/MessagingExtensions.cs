using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace AuditService.Infrastructure.Messagings;

public static class MessagingExtensions
{
    public static IServiceCollection AddAuditEventConsumer(this IServiceCollection services, RabbitMqSettings settings)
    {
        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<AuditEventConsumer>();

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
                });
            });
        });

        return services;
    }
}
