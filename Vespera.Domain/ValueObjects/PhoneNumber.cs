using System.Text.RegularExpressions;
using Vespera.Domain.Common;

namespace Vespera.Domain.ValueObjects;

public sealed partial class PhoneNumber : ValueObject
{
    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<PhoneNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PhoneNumber>(
                Error.Validation("phone_number.empty", "Phone number cannot be empty."));
        }

        var trimmed = value.Trim();

        if (!ValidFormat().IsMatch(trimmed))
        {
            return Result.Failure<PhoneNumber>(
                Error.Validation("phone_number.invalid_format", "Phone number must be in E.164 format, e.g. +14155552671."));
        }

        return Result.Success(new PhoneNumber(trimmed));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^\+[1-9]\d{6,14}$")]
    private static partial Regex ValidFormat();
}
