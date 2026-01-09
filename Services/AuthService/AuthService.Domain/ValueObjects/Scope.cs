using Flunt.Notifications;
using Flunt.Validations;

namespace AuthService.Domain.ValueObjects
{
    public class Scope : ValueObjectBase
    {
        public string Value { get; private set; }

        // Para EF Core
        private Scope() { }

        public Scope(string value)
        {
            Value = value;
            Validate();
        }

        private void Validate()
        {
            var contract = new Contract<Notification>()
                .Requires()
                .IsNotNullOrWhiteSpace(Value, nameof(Value), "Scope cannot be null or empty")
                .IsLowerOrEqualsThan(Value.Length, 100, nameof(Value), "Scope cannot exceed 100 characters");

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
