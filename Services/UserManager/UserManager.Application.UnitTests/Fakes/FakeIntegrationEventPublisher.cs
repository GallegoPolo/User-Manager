using UserManager.Application.Events.Interfaces;

namespace UserManager.Application.UnitTests.Fakes;

public sealed class FakeIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly List<PublishedIntegrationEvent> _published = [];

    public IReadOnlyList<PublishedIntegrationEvent> Published => _published;

    public Task PublishAsync<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        where TIntegrationEvent : class
    {
        _published.Add(new PublishedIntegrationEvent(typeof(TIntegrationEvent), integrationEvent));
        return Task.CompletedTask;
    }

    public sealed record PublishedIntegrationEvent(Type PublishedAs, object Message);
}
