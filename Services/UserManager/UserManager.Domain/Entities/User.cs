using Flunt.Notifications;
using Flunt.Validations;
using UserManager.Domain.Events.Entities;
using UserManager.Domain.Events.Interfaces;

namespace UserManager.Domain.Entities;

public class User : Notifiable<Notification>, IHasDomainEvents
{
    // Construtor privado (só para EF Core)    
    private User() { }
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public static User Create(string name, string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        user.AddNotifications(new Contract<Notification>().Requires()
                                                          .IsNotNullOrWhiteSpace(user.Name, nameof(Name), "Name is required")
                                                          .IsEmail(user.Email, nameof(Email), "Invalid email"));

        if (user.IsValid)
            user._domainEvents.Add(new UserCreatedDomainEvent(user.Id, user.Name, user.Email));

        return user;
    }

    public void Update(string name, string email)
    {
        Name = name;
        Email = email;

        AddNotifications(new Contract<Notification>().Requires()
                                                     .IsNotNullOrWhiteSpace(Name, nameof(Name), "Name is required")
                                                     .IsEmail(Email, nameof(Email), "Invalid email"));
    }

}
