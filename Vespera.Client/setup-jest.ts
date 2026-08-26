import { setupZoneTestEnv } from 'jest-preset-angular/setup-env/zone';

setupZoneTestEnv();

// jsdom's Crypto implementation doesn't include randomUUID (unlike a real browser), which every
// feature facade needs to generate a fresh Idempotency-Key per mutating call — patch it in from
// Node's own crypto module rather than replacing globalThis.crypto wholesale.
if (typeof globalThis.crypto?.randomUUID !== 'function') {
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const { randomUUID } = require('node:crypto') as typeof import('node:crypto');
  if (!globalThis.crypto) {
    Object.defineProperty(globalThis, 'crypto', { value: {}, configurable: true });
  }
  Object.defineProperty(globalThis.crypto, 'randomUUID', { value: randomUUID, configurable: true });
}
