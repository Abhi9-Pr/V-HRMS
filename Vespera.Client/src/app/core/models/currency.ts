// Mirrors Vespera.Domain.ValueObjects.Currency exactly (ordinal values — the API has no
// JsonStringEnumConverter registered, so System.Text.Json serializes enums as their numeric
// ordinal, not their name).
export enum Currency {
  Inr = 0,
  Usd = 1,
  Eur = 2,
  Gbp = 3,
  Aed = 4,
  Sgd = 5,
}

export const CURRENCY_LABELS: Record<Currency, string> = {
  [Currency.Inr]: 'INR',
  [Currency.Usd]: 'USD',
  [Currency.Eur]: 'EUR',
  [Currency.Gbp]: 'GBP',
  [Currency.Aed]: 'AED',
  [Currency.Sgd]: 'SGD',
};

export const CURRENCY_OPTIONS = Object.values(Currency).filter(
  (value): value is Currency => typeof value === 'number',
);
