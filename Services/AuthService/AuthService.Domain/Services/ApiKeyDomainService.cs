using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Domain.Services
{
    public class ApiKeyDomainService : IApiKeyDomainService
    {
        // Constantes para evitar números mágicos
        private const int API_KEY_BYTES_LENGTH = 32; 
        private const string API_KEY_PREFIX = "ak_";
        private const int MIN_API_KEY_LENGTH = 10; 

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
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API Key cannot be null or empty", nameof(apiKey));

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
            var hashString = Convert.ToHexString(hashBytes);

            return new ApiKeyHash(hashString);
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
