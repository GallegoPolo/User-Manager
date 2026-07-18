namespace UserManager.Infrastructure.Outbox
{
    public class OutboxMessage
    {
        private OutboxMessage() { } // EF Core

        public Guid Id { get; private set; }
        public string EventType { get; private set; } = string.Empty;
        public string AggregateId { get; private set; } = string.Empty;
        public string AggregateType { get; private set; } = string.Empty;
        public string PayloadJson { get; private set; } = string.Empty;
        public DateTime OccurredAt { get; private set; }
        public DateTime? ProcessedAt { get; private set; }

        public static OutboxMessage Create(string eventType, string aggregateId, string aggregateType, string payloadJson)
        {
            return new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                AggregateId = aggregateId,
                AggregateType = aggregateType,
                PayloadJson = payloadJson,
                OccurredAt = DateTime.UtcNow
            };
        }

        public void MarkAsProcessed() => ProcessedAt = DateTime.UtcNow;
    }
}
