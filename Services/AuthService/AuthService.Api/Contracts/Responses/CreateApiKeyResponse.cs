using AuthService.Domain.Enums;

namespace AuthService.Api.Contracts.Responses
{
    public record CreateApiKeyResponse(Guid Id,
                                       string Name,
                                       string ApiKey,
                                       List<string> Scopes,
                                       EApiKeyStatus Status,
                                       DateTime? ExpiresAt,
                                       DateTime CreatedAt);
}
