using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Domain.Services
{
    public class ApiKeyDomainService : IApiKeyDomainService
    {
        private const int API_KEY_LENGTH = 64; // Tamanho da API Key em caracteres

        public string GenerateApiKey()
        {
            // Gera uma API Key segura usando RandomNumberGenerator
            var bytes = new byte[API_KEY_LENGTH / 2]; // 32 bytes = 64 caracteres hex
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            // Converte para hexadecimal e adiciona prefixo
            var key = Convert.ToHexString(bytes);
            return $"ak_{key}"; // Prefixo "ak_" para identificar como API Key
        }

        public ApiKeyHash HashApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API Key cannot be null or empty", nameof(apiKey));

            // Usa SHA-256 para hash (seguro e rápido)
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
            var hashString = Convert.ToHexString(hashBytes);

            return new ApiKeyHash(hashString);
        }

        public bool ValidateApiKeyFormat(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            // Valida formato: deve começar com "ak_" e ter tamanho mínimo
            if (!apiKey.StartsWith("ak_"))
                return false;

            if (apiKey.Length < 10) // Mínimo razoável
                return false;

            return true;
        }
    }
}
