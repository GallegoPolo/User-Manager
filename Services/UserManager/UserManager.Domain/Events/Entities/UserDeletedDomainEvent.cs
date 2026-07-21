using UserManager.Domain.Events.Interfaces;

namespace UserManager.Domain.Events.Entities;

public sealed class UserDeletedDomainEvent(Guid userId, string name, string email) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid UserId { get; } = userId;
    public string Name { get; } = name;
    public string Email { get; } = email;
}