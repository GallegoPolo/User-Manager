namespace AuthService.Application.UseCases.ApiKeys.DTOs
{
    public record TokenDTO(string AccessToken, string TokenType, int ExpiresIn);
}
