using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.ValueObjects;
using System.Security.Cryptography;

namespace AuthService.Domain.Services
{
    public class ApiKeyDomainService : IApiKeyDomainService
    {
        private const int API_KEY_BYTES_LENGTH = 32;
        private const string API_KEY_PREFIX = "ak_";
        private const int MIN_API_KEY_LENGTH = 10;

        private readonly IApiKeyHasher _hasher;

        public ApiKeyDomainService(IApiKeyHasher hasher)
        {
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        }

        public string GenerateApiKey()
        {
            var randomBytes = new byte[API_KEY_BYTES_LENGTH];
            using var randomNumberGenerator = RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(randomBytes);

            var hexKey = Convert.ToHexString(randomBytes);
            return $"{API_KEY_PREFIX}{hexKey}";
        }

        public ApiKeyHash HashApiKey(string apiKey)
        {
            return _hasher.Hash(apiKey);
        }

        public bool VerifyApiKey(string apiKey, ApiKeyHash storedHash)
        {
            return _hasher.Verify(apiKey, storedHash);
        }

        public bool ValidateApiKeyFormat(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            if (!apiKey.StartsWith(API_KEY_PREFIX, StringComparison.Ordinal))
                return false;

            if (apiKey.Length < MIN_API_KEY_LENGTH)
                return false;

            return true;
        }
    }
}
