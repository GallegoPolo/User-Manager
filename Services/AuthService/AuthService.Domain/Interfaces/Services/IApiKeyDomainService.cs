using AuthService.Domain.DTOs;

namespace AuthService.Domain.Interfaces.Services
{
    public interface IApiKeyDomainService
    {
        GeneratedApiKeyDTO GenerateApiKey(string environment = "live");
        bool ValidateApiKeyFormat(string apiKey);
        ParsedApiKeyDTO ParseApiKey(string apiKey);
    }
}
