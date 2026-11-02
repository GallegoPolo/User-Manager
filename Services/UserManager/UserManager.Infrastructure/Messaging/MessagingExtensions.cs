using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using UserManager.Application.Events.Interfaces;
using UserManager.Infrastructure.Persistence;

namespace UserManager.Infrastructure.Messaging;

public static class MessagingExtensions
{
    private static readonly TimeSpan OutboxQueryDelay = TimeSpan.FromSeconds(1);

    public static IServiceCollection AddIntegrationMessaging(this IServiceCollection services, RabbitMqSettings settings)
    {
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<UserManagerDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
                outbox.QueryDelay = OutboxQueryDelay;
                outbox.DisableInboxCleanupService();
            });

            bus.UsingRabbitMq((_, rabbit) =>
            {
                rabbit.Host(settings.HostName, settings.Port, settings.VirtualHost, host =>
                {
                    host.Username(settings.UserName);
                    host.Password(settings.Password);
                });

                rabbit.ConfigurePublish(publish => publish.UseExecute(RequireRoutableDelivery));
            });
        });

        return services;
    }

    private static void RequireRoutableDelivery(SendContext context)
    {
        if (context.TryGetPayload<RabbitMqSendContext>(out var rabbitMqContext))
            rabbitMqContext.Mandatory = true;
    }
}
