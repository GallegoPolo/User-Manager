using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.ValueObjects;
using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Infrastructure.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int MEMORY_SIZE = 19456;
        private const int ITERATIONS = 2;
        private const int PARALLELISM = 1;
        private const int HASH_BYTE_SIZE = 32;
        private const int SALT_BYTE_SIZE = 16;

        public PasswordHash Hash(string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
                throw new ArgumentException("Password cannot be null or empty", nameof(plainPassword));

            byte[] salt = GenerateSalt();
            byte[] passwordBytes = Encoding.UTF8.GetBytes(plainPassword);

            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                DegreeOfParallelism = PARALLELISM,
                MemorySize = MEMORY_SIZE,
                Iterations = ITERATIONS
            };

            byte[] hashBytes = argon2.GetBytes(HASH_BYTE_SIZE);
            string hashString = FormatHash(salt, hashBytes);

            return new PasswordHash(hashString);
        }

        public bool Verify(string plainPassword, PasswordHash storedHash)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
                throw new ArgumentException("Password cannot be null or empty", nameof(plainPassword));

            if (storedHash == null)
                throw new ArgumentNullException(nameof(storedHash));

            try
            {
                var (salt, hash) = ParseHash(storedHash.Value);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(plainPassword);

                using var argon2 = new Argon2id(passwordBytes)
                {
                    Salt = salt,
                    DegreeOfParallelism = PARALLELISM,
                    MemorySize = MEMORY_SIZE,
                    Iterations = ITERATIONS
                };

                byte[] newHash = argon2.GetBytes(HASH_BYTE_SIZE);

                return CryptographicOperations.FixedTimeEquals(hash, newHash);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static byte[] GenerateSalt()
        {
            byte[] salt = new byte[SALT_BYTE_SIZE];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return salt;
        }

        private static string FormatHash(byte[] salt, byte[] hash)
        {
            string saltBase64 = Convert.ToBase64String(salt);
            string hashBase64 = Convert.ToBase64String(hash);
            return $"$argon2id$v=19$m={MEMORY_SIZE},t={ITERATIONS},p={PARALLELISM}${saltBase64}${hashBase64}";
        }

        private static (byte[] salt, byte[] hash) ParseHash(string hashString)
        {
            string[] parts = hashString.Split('$');
            if (parts.Length != 6)
                throw new FormatException("Invalid Argon2 hash format");

            byte[] salt = Convert.FromBase64String(parts[4]);
            byte[] hash = Convert.FromBase64String(parts[5]);

            return (salt, hash);
        }
    }
}
