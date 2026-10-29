using UserManager.Application.Outbox.Interfaces;

namespace UserManager.Application.UnitTests.Fakes;

public sealed class FakeOutboxRepository : IOutboxRepository
{
    private readonly List<OutboxEntry> _entries = [];

    public IReadOnlyList<OutboxEntry> Entries => _entries;

    public void Add(string eventType, string aggregateId, string aggregateType, string payloadJson) =>
        _entries.Add(new OutboxEntry(eventType, aggregateId, aggregateType, payloadJson));

    public sealed record OutboxEntry(string EventType, string AggregateId, string AggregateType, string PayloadJson);
}
