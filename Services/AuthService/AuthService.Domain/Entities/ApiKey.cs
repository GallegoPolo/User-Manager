using AuthService.Domain.Enums;
using AuthService.Domain.ValueObjects;
using Flunt.Notifications;
using Flunt.Validations;

namespace AuthService.Domain.Entities
{
    public class ApiKey : Notifiable<Notification>
    {
        // Construtor privado para EF Core
        private ApiKey() { }

        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public ApiKeyHash KeyHash { get; private set; } = null!;
        public List<Scope> Scopes { get; private set; } = new();
        public EApiKeyStatus Status { get; private set; }
        public DateTime? ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public static ApiKey Create(string name, ApiKeyHash keyHash, List<Scope> scopes, DateTime? expiresAt = null)
        {
            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                Name = name,
                KeyHash = keyHash,
                Scopes = scopes ?? new List<Scope>(),
                Status = EApiKeyStatus.Active,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            apiKey.Validate();

            return apiKey;
        }

        public void Revoke()
        {
            if (Status == EApiKeyStatus.Revoked)
            {
                AddNotification("Status", "API Key is already revoked");
                return;
            }

            Status = EApiKeyStatus.Revoked;
        }

        private bool IsExpired()
        {
            if (ExpiresAt == null)
                return false;

            return DateTime.UtcNow > ExpiresAt.Value;
        }

        public bool IsActive()
        {
            return Status == EApiKeyStatus.Active && !IsExpired();
        }

        private void Validate()
        {
            AddNotifications(new Contract<Notification>()
                .Requires()
                .IsNotNullOrWhiteSpace(Name, nameof(Name), "Name is required")
                .IsGreaterOrEqualsThan(Name.Length, 3, nameof(Name), "Name must have at least 3 characters")
                .IsLowerOrEqualsThan(Name.Length, 200, nameof(Name), "Name must not exceed 200 characters")
                .IsNotNull(KeyHash, nameof(KeyHash), "Key hash is required")
                .IsNotNull(Scopes, nameof(Scopes), "Scopes list is required")
            );

            if (Scopes != null && Scopes.Count == 0)
                AddNotification("Scopes", "At least one scope is required");
        }
    }
}
