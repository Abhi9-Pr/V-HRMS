using System.Text.RegularExpressions;
using Vespera.Domain.Common;

namespace Vespera.Domain.ValueObjects;

public sealed partial class EmailAddress : ValueObject
{
    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<EmailAddress> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<EmailAddress>(
                Error.Validation("email_address.empty", "Email address cannot be empty."));
        }

        var trimmed = value.Trim();

        if (trimmed.Length > 254 || !ValidFormat().IsMatch(trimmed))
        {
            return Result.Failure<EmailAddress>(
                Error.Validation("email_address.invalid_format", "Email address is not a valid format."));
        }

        return Result.Success(new EmailAddress(trimmed));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToUpperInvariant();
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex ValidFormat();
}
