using System.Text.RegularExpressions;
using Vespera.Domain.Common;

namespace Vespera.Domain.ValueObjects;

public sealed partial class PanNumber : ValueObject, IPersonalData
{
    private PanNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<PanNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PanNumber>(
                Error.Validation("pan_number.empty", "PAN cannot be empty."));
        }

        var trimmed = value.Trim().ToUpperInvariant();

        if (!ValidFormat().IsMatch(trimmed))
        {
            return Result.Failure<PanNumber>(
                Error.Validation("pan_number.invalid_format", "PAN must match the format AAAAA9999A."));
        }

        return Result.Success(new PanNumber(trimmed));
    }

    public string Masked() => string.Concat(Value[..2], new string('*', Value.Length - 3), Value[^1]);

    public override string ToString() => Masked();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex("^[A-Z]{5}[0-9]{4}[A-Z]$")]
    private static partial Regex ValidFormat();
}
