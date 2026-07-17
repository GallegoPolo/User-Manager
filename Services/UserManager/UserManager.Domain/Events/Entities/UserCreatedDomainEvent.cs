using UserManager.Domain.Events.Interfaces;

namespace UserManager.Domain.Events.Entities
{
    public sealed class UserCreatedDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public Guid UserId { get; }
        public string Name { get; }
        public string Email { get; }

        public UserCreatedDomainEvent(Guid userId, string name, string email)
        {
            UserId = userId;
            Name = name;
            Email = email;
        }
    }
}
