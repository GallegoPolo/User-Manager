namespace AuthService.Application.UseCases.ApiKeys.DTOs
{
    public class ValidateApiKeyDTO
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
