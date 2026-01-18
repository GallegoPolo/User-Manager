using AuthService.Domain.Enums;

namespace AuthService.Application.UseCases.ApiKeys.DTOs
{
    public class ApiKeyDTO
    {
        public ApiKeyDTO(Guid id, string name, List<string> scopes, EApiKeyStatus status, DateTime? expiresAt, DateTime createdAt, string? apiKey = null)
        {
            Id = id;
            Name = name;
            Scopes = scopes;
            Status = status;
            ExpiresAt = expiresAt;
            CreatedAt = createdAt;
            ApiKey = apiKey;
        }

        public Guid Id { get; }
        public string Name { get; } = string.Empty;
        public List<string> Scopes { get; } = new();
        public EApiKeyStatus Status { get; }
        public DateTime? ExpiresAt { get; }
        public DateTime CreatedAt { get; }
        public string? ApiKey { get; }

    }
}
