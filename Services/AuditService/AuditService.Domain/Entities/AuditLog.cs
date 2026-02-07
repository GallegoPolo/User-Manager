using AuditService.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuditService.Domain.Entities
{
    public class AuditLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; private set; } = ObjectId.GenerateNewId().ToString();
        [BsonElement("eventId")]
        [BsonRepresentation(BsonType.String)]
        public Guid EventId { get; private set; }
        [BsonElement("aggregateId")]
        public string AggregateId { get; private set; } = string.Empty;
        [BsonElement("aggregateType")]
        public string AggregateType { get; private set; } = string.Empty;
        [BsonElement("eventType")]
        [BsonRepresentation(BsonType.String)]
        public EEventType EventType { get; private set; }
        [BsonElement("performedBy")]
        public string PerformedBy { get; private set; } = string.Empty;
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; private set; }
        [BsonElement("payload")]
        public Dictionary<string, object>? Payload { get; private set; }
        private AuditLog() { }

        public static AuditLog Create(Guid eventId, string aggregateId, string aggregateType, EEventType eventType, string performedBy, DateTime timestamp, Dictionary<string, object>? payload = null)
        {
            if (eventId == Guid.Empty)
                throw new ArgumentException("EventId não pode ser vazio", nameof(eventId));

            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("AggregateId é obrigatório", nameof(aggregateId));

            if (string.IsNullOrWhiteSpace(aggregateType))
                throw new ArgumentException("AggregateType é obrigatório", nameof(aggregateType));

            if (string.IsNullOrWhiteSpace(performedBy))
                throw new ArgumentException("PerformedBy é obrigatório", nameof(performedBy));

            return new AuditLog
            {
                EventId = eventId,
                AggregateId = aggregateId,
                AggregateType = aggregateType,
                EventType = eventType,
                PerformedBy = performedBy,
                Timestamp = timestamp,
                Payload = payload
            };
        }
    }
}
