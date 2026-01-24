using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Interfaces.Services
{
    public interface IApiKeyDomainService
    {
        string GenerateApiKey();
        ApiKeyHash HashApiKey(string apiKey);
        bool VerifyApiKey(string apiKey, ApiKeyHash storedHash);
        bool ValidateApiKeyFormat(string apiKey);
    }
}
