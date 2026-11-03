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
    public const int MaxIdentityProviderIdLength = 255;

    public Guid Id { get; private set; }
    public string IdentityProviderId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public static User Provision(string identityProviderId, string name, Email email)
    {
        EnsureValidIdentityProviderId(identityProviderId);
        EnsureValidName(name);
        EnsureValidEmail(email);

        var user = new User
        {
            Id = Guid.NewGuid(),
            IdentityProviderId = identityProviderId,
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        user._domainEvents.Add(new UserCreatedDomainEvent(user.Id, user.Name, user.Email.Value));
        return user;
    }

    public void Rename(string name)
    {
        EnsureValidName(name);

        Name = name;

        _domainEvents.Add(new UserUpdatedDomainEvent(Id, Name, Email.Value));
    }

    public bool SyncEmail(Email email)
    {
        EnsureValidEmail(email);

        if (Email == email)
            return false;

        Email = email;

        _domainEvents.Add(new UserUpdatedDomainEvent(Id, Name, Email.Value));
        return true;
    }

    public void Delete()
    {
        _domainEvents.Add(new UserDeletedDomainEvent(Id, Name, Email.Value));
    }

    private static void EnsureValidIdentityProviderId(string identityProviderId)
    {
        if (string.IsNullOrWhiteSpace(identityProviderId))
            throw new DomainException("IdentityProviderId is required");
        if (identityProviderId.Length > MaxIdentityProviderIdLength)
            throw new DomainException($"IdentityProviderId must not exceed {MaxIdentityProviderIdLength} characters");
    }

    private static void EnsureValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required");
        if (name.Length > MAX_NAME_LENGTH)
            throw new DomainException($"Name must not exceed {MAX_NAME_LENGTH} characters");
    }

    private static void EnsureValidEmail(Email email)
    {
        if (email is null)
            throw new DomainException("Email is required");
    }
}
