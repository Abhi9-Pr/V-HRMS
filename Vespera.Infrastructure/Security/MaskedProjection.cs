using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Security;

/// <summary>
/// Static masking helpers for read models that need a display-safe PII value without
/// materializing (and needing to inject a protector to decrypt into) the full value object.
/// Delegates to the value objects' own <c>Masked()</c> — this doesn't duplicate the masking
/// rule, it just gives Mapster projections a plain static method they can call from a
/// <c>.Map(...)</c> expression.
/// </summary>
public static class MaskedProjection
{
    public static string MaskPan(string rawValue)
    {
        var result = PanNumber.Create(rawValue);
        return result.IsSuccess ? result.Value.Masked() : "***";
    }

    public static string MaskBankAccount(string rawValue)
    {
        var result = BankAccountNumber.Create(rawValue);
        return result.IsSuccess ? result.Value.Masked() : "***";
    }
}
