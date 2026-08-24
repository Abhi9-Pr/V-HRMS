export function newIdempotencyKey(): string {
  return crypto.randomUUID();
}

export function idempotencyHeaders(): { [header: string]: string } {
  return { 'Idempotency-Key': newIdempotencyKey() };
}
