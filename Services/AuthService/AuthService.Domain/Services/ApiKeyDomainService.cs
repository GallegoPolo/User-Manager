using AuthService.Domain.DTOs;
using AuthService.Domain.Interfaces.Services;
using System.Security.Cryptography;

namespace AuthService.Domain.Services
{
    public class ApiKeyDomainService : IApiKeyDomainService
    {
        private const string CHARS = "abcdefghijklmnopqrstuvwxyz0123456789";
        private const int PREFIX_LENGTH = 8;
        private const int SECRET_LENGTH = 32;

        public GeneratedApiKeyDTO GenerateApiKey(string environment = "live")
        {
            var prefix = GenerateRandomString(PREFIX_LENGTH);
            var secret = GenerateRandomString(SECRET_LENGTH);
            var fullKey = $"ak_{environment}_{prefix}_{secret}";

            return new GeneratedApiKeyDTO(fullKey, prefix, secret, environment);
        }

        public ParsedApiKeyDTO ParseApiKey(string apiKey)
        {
            if (!ValidateApiKeyFormat(apiKey))
                throw new ArgumentException("Invalid API Key format", nameof(apiKey));

            var parts = apiKey.Split('_');

            return new ParsedApiKeyDTO(Prefix: parts[2], Secret: parts[3], Environment: parts[1]);
        }

        //TODO: Create Validator for ApiKey format
        public bool ValidateApiKeyFormat(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            var parts = apiKey.Split('_');

            if (parts.Length != 4)
                return false;

            if (parts[0] != "ak")
                return false;

            var validEnvironments = new[] { "live", "test", "dev" };
            if (!validEnvironments.Contains(parts[1]))
                return false;

            if (parts[2].Length != PREFIX_LENGTH)
                return false;

            if (parts[3].Length != SECRET_LENGTH)
                return false;

            return true;
        }

        private string GenerateRandomString(int length)
        {
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = CHARS[bytes[i] % CHARS.Length];
            }

            return new string(result);
        }
    }
}