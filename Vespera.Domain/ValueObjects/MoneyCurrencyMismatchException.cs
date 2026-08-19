namespace Vespera.Domain.ValueObjects;

public sealed class MoneyCurrencyMismatchException : InvalidOperationException
{
    public MoneyCurrencyMismatchException(Currency left, Currency right)
        : base($"Cannot operate on Money in different currencies: {left} and {right}.")
    {
        Left = left;
        Right = right;
    }

    public Currency Left { get; }

    public Currency Right { get; }
}
