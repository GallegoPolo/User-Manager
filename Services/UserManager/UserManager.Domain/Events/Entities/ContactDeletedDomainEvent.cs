using UserManager.Domain.Events.Interfaces;

namespace UserManager.Domain.Events.Entities;

public sealed record ContactDeletedDomainEvent(Guid ContactId, Guid OwnerId, string Name, string? Email, string? Phone) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
