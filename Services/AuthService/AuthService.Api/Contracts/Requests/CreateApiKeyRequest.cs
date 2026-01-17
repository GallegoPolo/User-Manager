namespace AuthService.Api.Contracts.Requests
{
    public record CreateApiKeyRequest(string Name, List<string> Scopes, DateTime? ExpiresAt);
}
