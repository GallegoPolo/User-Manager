using AuthService.Domain.ValueObjects;
using Flunt.Notifications;
using Flunt.Validations;

namespace AuthService.Domain.Entities
{
    public class UserCredential : Notifiable<Notification>
    {
        private UserCredential() { } // EF Core

        public Guid Id { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public PasswordHash PasswordHash { get; private set; } = null!;
        public string Role { get; private set; } = "ADMIN"; 
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }

        public static UserCredential Create(string email, PasswordHash passwordHash)
        {
            var credential = new UserCredential
            {
                Id = Guid.NewGuid(),
                Email = email.ToLowerInvariant(),
                PasswordHash = passwordHash,
                Role = "ADMIN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            credential.Validate();
            return credential;
        }

        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private void Validate()
        {
            AddNotifications(new Contract<Notification>()
                .Requires()
                .IsNotNullOrWhiteSpace(Email, nameof(Email), "Email is required")
                .IsEmail(Email, nameof(Email), "Invalid email format")
                .IsNotNull(PasswordHash, nameof(PasswordHash), "Password hash is required")
            );
        }
    }
}
