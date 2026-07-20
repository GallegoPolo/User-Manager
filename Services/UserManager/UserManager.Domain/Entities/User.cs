using UserManager.Domain.Events.Entities;
using UserManager.Domain.Events.Interfaces;
using UserManager.Domain.Exceptions;

namespace UserManager.Domain.Entities;

public class User : IHasDomainEvents
{
    // Construtor privado (só para EF Core)    
    private User() { }
    private readonly List<IDomainEvent> _domainEvents = new();
    public const int MAX_NAME_LENGTH = 200;
    public const int MAX_EMAIL_LENGTH = 200;

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public static User Create(string name, string email)
    {
        EnsureValid(name, email);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        user._domainEvents.Add(new UserCreatedDomainEvent(user.Id, user.Name, user.Email));
        return user;
    }

    public void Update(string name, string email)
    {
        EnsureValid(name, email);

        Name = name;
        Email = email;

        //TODO: Implementar evento de atualização de usuário
    }

    private static void EnsureValid(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required");
        if (name.Length > MAX_NAME_LENGTH)
            throw new DomainException($"Name must not exceed {MAX_NAME_LENGTH} characters");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("Email is invalid");
        if (email.Length > MAX_EMAIL_LENGTH)
            throw new DomainException($"Email must not exceed {MAX_EMAIL_LENGTH} characters");
    }
}
