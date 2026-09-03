import { createHmac } from 'crypto';

/**
 * RFC 6238 TOTP, the same default parameters (SHA-1, 30s step, 6 digits) as the backend's own
 * `OtpNet.Totp` used by tests/Vespera.Api.IntegrationTests/TotpTestHelper.cs — no npm dependency
 * needed for something this small. Only used against DevelopmentSeeder's fixed demo
 * FinanceAdminTotpSecretBase32, never a real user's secret.
 */
function base32Decode(base32: string): Buffer {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
  let bits = '';
  for (const char of base32.toUpperCase().replace(/=+$/, '')) {
    const value = alphabet.indexOf(char);
    if (value === -1) {
      throw new Error(`Invalid base32 character: ${char}`);
    }
    bits += value.toString(2).padStart(5, '0');
  }

  const bytes: number[] = [];
  for (let i = 0; i + 8 <= bits.length; i += 8) {
    bytes.push(parseInt(bits.slice(i, i + 8), 2));
  }
  return Buffer.from(bytes);
}

export function computeCurrentTotp(base32Secret: string, stepSeconds = 30, digits = 6): string {
  const counter = Math.floor(Date.now() / 1000 / stepSeconds);
  const counterBuffer = Buffer.alloc(8);
  counterBuffer.writeBigInt64BE(BigInt(counter));

  const hmac = createHmac('sha1', base32Decode(base32Secret)).update(counterBuffer).digest();
  const offset = hmac[hmac.length - 1] & 0x0f;
  const binaryCode =
    ((hmac[offset] & 0x7f) << 24) | ((hmac[offset + 1] & 0xff) << 16) | ((hmac[offset + 2] & 0xff) << 8) | (hmac[offset + 3] & 0xff);

  return (binaryCode % 10 ** digits).toString().padStart(digits, '0');
}
