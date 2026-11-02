using MassTransit;
using UserManager.Application.Events.Interfaces;

namespace UserManager.Infrastructure.Messaging;

public sealed class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        where TIntegrationEvent : class
    {
        return _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
