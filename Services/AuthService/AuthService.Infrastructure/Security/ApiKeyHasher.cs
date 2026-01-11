using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.ValueObjects;

namespace AuthService.Infrastructure.Security
{
    public class ApiKeyHasher
    {
        private readonly IApiKeyDomainService _domainService;

        public ApiKeyHasher(IApiKeyDomainService domainService)
        {
            _domainService = domainService;
        }

        public ApiKeyHash Hash(string apiKey)
        {
            return _domainService.HashApiKey(apiKey);
        }
    }
}
