using Flunt.Notifications;
using Flunt.Validations;

namespace AuthService.Domain.ValueObjects
{
    public class ApiKeyHash : ValueObjectBase
    {
        private const int MIN_HASH_LENGTH = 32;

        public string Value { get; private set; }

        // Para EF Core
        private ApiKeyHash() { }

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
                .IsGreaterOrEqualsThan(Value.Length, MIN_HASH_LENGTH, nameof(Value), $"Hash must have at least {MIN_HASH_LENGTH} characters to be considered valid");

            if (!contract.IsValid)
            {
                throw new ArgumentException(contract.Notifications.First().Message, nameof(Value));
            }
        }

        protected override object GetEqualityComponents()
        {
            return Value;
        }

        public override string ToString() => Value;
    }
}
