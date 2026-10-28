using System.Text.RegularExpressions;
using UserManager.Domain.Exceptions;

namespace UserManager.Domain.ValueObjects;

public sealed partial record Email
{
    public const int MaxLength = 200;

    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool IsValid(string? value) => IsWellFormed(Normalize(value));

    public static Email Create(string value)
    {
        var normalized = Normalize(value);

        if (!IsWellFormed(normalized))
            throw new DomainException("E-mail inválido.");

        return new Email(normalized);
    }

    public override string ToString() => Value;

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static bool IsWellFormed(string normalized) =>
        normalized.Length <= MaxLength && AddressPattern().IsMatch(normalized);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex AddressPattern();
}
