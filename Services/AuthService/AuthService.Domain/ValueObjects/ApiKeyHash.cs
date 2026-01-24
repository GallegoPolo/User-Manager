using Flunt.Notifications;
using Flunt.Validations;

namespace AuthService.Domain.ValueObjects
{
    public class ApiKeyHash : ValueObjectBase
    {
        private const int MIN_HASH_LENGTH = 50;
        private const int MAX_HASH_LENGTH = 200;

        public string Value { get; private set; }

        // Para EF Core
        private ApiKeyHash()
        {
            Value = string.Empty;
        }

        public ApiKeyHash(string hash)
        {
            Value = hash;
            Validate();
        }

        private void Validate()
        {
            var contract = new Contract<Notification>()
                .Requires()
                .IsNotNullOrWhiteSpace(Value, nameof(Value), "Hash cannot be null or empty")
                .IsGreaterOrEqualsThan(Value.Length, MIN_HASH_LENGTH, nameof(Value), $"Hash is too short. Expected at least {MIN_HASH_LENGTH} characters (Argon2 format)")
                .IsLowerOrEqualsThan(Value.Length, MAX_HASH_LENGTH, nameof(Value), $"Hash is too long. Maximum {MAX_HASH_LENGTH} characters allowed");

            if (!contract.IsValid)
                throw new ArgumentException(contract.Notifications.First().Message, nameof(Value));
        }

        protected override object GetEqualityComponents()
        {
            return Value;
        }

        public override string ToString() => Value;
    }
}
