using System.Text.RegularExpressions;
using Vespera.Domain.Common;

namespace Vespera.Domain.ValueObjects;

public sealed partial class EmployeeCode : ValueObject
{
    private EmployeeCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<EmployeeCode> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<EmployeeCode>(
                Error.Validation("employee_code.empty", "Employee code cannot be empty."));
        }

        var trimmed = value.Trim();

        if (trimmed.Length > 32 || !ValidFormat().IsMatch(trimmed))
        {
            return Result.Failure<EmployeeCode>(
                Error.Validation("employee_code.invalid_format", "Employee code must be 1-32 alphanumeric characters or hyphens."));
        }

        return Result.Success(new EmployeeCode(trimmed));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex("^[A-Za-z0-9-]+$")]
    private static partial Regex ValidFormat();
}
