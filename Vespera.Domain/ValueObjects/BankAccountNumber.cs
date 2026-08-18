using System.Text.RegularExpressions;
using Vespera.Domain.Common;

namespace Vespera.Domain.ValueObjects;

public sealed partial class BankAccountNumber : ValueObject, IPersonalData
{
    private const int VisibleSuffixLength = 4;

    private BankAccountNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<BankAccountNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<BankAccountNumber>(
                Error.Validation("bank_account_number.empty", "Bank account number cannot be empty."));
        }

        var trimmed = value.Trim();

        if (!ValidFormat().IsMatch(trimmed))
        {
            return Result.Failure<BankAccountNumber>(
                Error.Validation("bank_account_number.invalid_format", "Bank account number must be 6-18 digits."));
        }

        return Result.Success(new BankAccountNumber(trimmed));
    }

    public string Masked()
    {
        var suffix = Value.Length <= VisibleSuffixLength ? Value : Value[^VisibleSuffixLength..];
        return string.Concat(new string('*', Math.Max(0, Value.Length - suffix.Length)), suffix);
    }

    public override string ToString() => Masked();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^\d{6,18}$")]
    private static partial Regex ValidFormat();
}
