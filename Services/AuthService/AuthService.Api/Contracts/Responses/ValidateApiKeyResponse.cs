namespace AuthService.Api.Contracts.Responses
{
    public record ValidateApiKeyResponse(string Token, DateTime ExpiresAt);
}
