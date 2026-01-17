namespace AuthService.Api.Contracts.Responses
{
    public record CreateApiKeyResponse(Guid Id,
                                       string Name,
                                       string ApiKey,
                                       List<string> Scopes,
                                       string Status,
                                       DateTime? ExpiresAt,
                                       DateTime CreatedAt);
}
