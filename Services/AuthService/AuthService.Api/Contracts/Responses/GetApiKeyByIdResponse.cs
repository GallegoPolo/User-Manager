using AuthService.Domain.Enums;

namespace AuthService.Api.Contracts.Responses
{
    public record GetApiKeyByIdResponse(Guid Id,
                                        string Name,
                                        List<string> Scopes,
                                        EApiKeyStatus Status,
                                        DateTime? ExpiresAt,
                                        DateTime CreatedAt);
}
