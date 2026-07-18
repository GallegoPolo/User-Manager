namespace UserManager.Application.Outbox.Interfaces
{
    public interface IOutboxRepository
    {
        void Add(string eventType, string aggregateId, string aggregateType, string payloadJson);
    }
}
