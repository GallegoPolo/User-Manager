using UserManager.Domain.Events.Entities;
using UserManager.Domain.Events.Interfaces;
using UserManager.Domain.Exceptions;
using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.Entities;

public class User : IHasDomainEvents
{
    private User() { }
    private readonly List<IDomainEvent> _domainEvents = new();
    public const int MAX_NAME_LENGTH = 200;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public static User Create(string name, Email email)
    {
        EnsureValid(name, email);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        user._domainEvents.Add(new UserCreatedDomainEvent(user.Id, user.Name, user.Email.Value));
        return user;
    }

    public void Update(string name, Email email)
    {
        EnsureValid(name, email);

        Name = name;
        Email = email;

        _domainEvents.Add(new UserUpdatedDomainEvent(Id, Name, Email.Value));
    }

    public void Delete()
    {
        _domainEvents.Add(new UserDeletedDomainEvent(Id, Name, Email.Value));
    }

    private static void EnsureValid(string name, Email email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required");
        if (name.Length > MAX_NAME_LENGTH)
            throw new DomainException($"Name must not exceed {MAX_NAME_LENGTH} characters");
        if (email is null)
            throw new DomainException("Email is required");
    }
}
