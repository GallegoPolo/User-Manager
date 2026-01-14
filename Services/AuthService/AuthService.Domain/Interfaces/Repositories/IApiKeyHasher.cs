using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Interfaces.Repositories
{
    public interface IApiKeyHasher
    {
        ApiKeyHash Hash(string apiKey);
    }
}
