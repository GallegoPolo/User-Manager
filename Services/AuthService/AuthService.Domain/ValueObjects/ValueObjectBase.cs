namespace AuthService.Domain.ValueObjects
{
    public abstract class ValueObjectBase
    {
        protected abstract object GetEqualityComponents();

        public override bool Equals(object? obj)
        {
            if (obj?.GetType() != GetType())
                return false;

            var other = (ValueObjectBase)obj;
            return GetEqualityComponents().Equals(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents().GetHashCode();
        }

        public static bool operator ==(ValueObjectBase? left, ValueObjectBase? right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(ValueObjectBase? left, ValueObjectBase? right)
        {
            return !(left == right);
        }
    }
}
