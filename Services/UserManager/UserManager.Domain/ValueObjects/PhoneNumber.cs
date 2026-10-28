using System.Text.RegularExpressions;
using UserManager.Domain.Exceptions;

namespace UserManager.Domain.ValueObjects;

public sealed partial record PhoneNumber
{
    public const int MaxLength = 16;

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool IsValid(string? value) => NumberPattern().IsMatch(Normalize(value));

    public static PhoneNumber Create(string value)
    {
        var normalized = Normalize(value);

        if (!NumberPattern().IsMatch(normalized))
            throw new DomainException("Telefone inválido.");

        return new PhoneNumber(normalized);
    }

    public override string ToString() => Value;

    private static string Normalize(string? value) =>
        value is null ? string.Empty : FormattingCharacters().Replace(value, string.Empty);

    [GeneratedRegex(@"[\s\-().]")]
    private static partial Regex FormattingCharacters();

    [GeneratedRegex(@"^\+?[0-9]{8,15}$")]
    private static partial Regex NumberPattern();
}
