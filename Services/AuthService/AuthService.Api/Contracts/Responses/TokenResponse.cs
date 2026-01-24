namespace AuthService.Api.Contracts.Responses
{
    public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);
}
