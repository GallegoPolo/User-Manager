using Flunt.Notifications;
using Flunt.Validations;

namespace UserManager.Domain.Entities;

public class User : Notifiable<Notification>
{
    // Construtor privado (só para EF Core)    
    private User() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public DateTime CreatedAt { get; private set; }

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
