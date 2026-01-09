using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Interfaces.Services
{
    public interface IApiKeyDomainService
    {
        string GenerateApiKey();
        ApiKeyHash HashApiKey(string apiKey);
        bool ValidateApiKeyFormat(string apiKey);
    }
}
