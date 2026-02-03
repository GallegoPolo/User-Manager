using AuthService.Domain.Enums;

namespace AuthService.Infrastructure.Caching.DTOs
{
    public class CachedApiKeyDTO
    {
        public CachedApiKeyDTO(Guid id, string name, string prefix, string secretHash, List<string> scopes, EApiKeyStatus status, DateTime? expiresAt, DateTime createdAt)
        {
            Id = id;
            Name = name;
            Prefix = prefix;
            SecretHash = secretHash;
            Scopes = scopes;
            Status = status;
            ExpiresAt = expiresAt;
            CreatedAt = createdAt;
        }

        public Guid Id { get; }
        public string Name { get; } = string.Empty;
        public string Prefix { get; } = string.Empty;
        public string SecretHash { get; } = string.Empty;
        public List<string> Scopes { get; } = new();
        public EApiKeyStatus Status { get; }
        public DateTime? ExpiresAt { get; }
        public DateTime CreatedAt { get; }
        public bool IsActive => Status == EApiKeyStatus.Active && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
    }
}
