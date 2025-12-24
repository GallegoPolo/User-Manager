using Flunt.Notifications;
using Flunt.Validations;

namespace UserManager.Domain.Entities;

public class User : Notifiable<Notification>
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User(string name, string email)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        CreatedAt = DateTime.UtcNow;

        AddNotifications(new Contract<Notification>()
            .Requires()
            .IsNotNullOrWhiteSpace(Name, nameof(Name), "Name is required")
            .IsEmail(Email, nameof(Email), "Invalid email")
        );
    }
}
