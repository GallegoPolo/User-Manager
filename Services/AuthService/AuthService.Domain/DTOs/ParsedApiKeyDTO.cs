namespace AuthService.Domain.DTOs
{
  public record ParsedApiKeyDTO(string Prefix, string Secret, string Environment);
}
