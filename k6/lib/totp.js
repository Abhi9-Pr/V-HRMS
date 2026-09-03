// Minimal RFC 6238 TOTP (SHA1, 30s step, 6 digits — Otp.NET's own defaults, matching
// tests/Vespera.Api.IntegrationTests/TotpTestHelper.cs, which uses the same library server-side
// against DevelopmentSeeder.FinanceAdminTotpSecretBase32). No external dependency: k6 doesn't
// ship a base32 decoder, just crypto.hmac(...), so base32 decoding is implemented here from
// scratch rather than pulled from a CDN (which would need network access at script-load time —
// not something to depend on in CI).
//
// Bytes are passed to crypto.hmac as ArrayBuffers, not JS strings — a JS string built via
// String.fromCharCode(...bytes) is NOT a safe stand-in for raw bytes here: k6's crypto module
// treats a string argument as UTF-8 text, so any byte >= 0x80 gets multi-byte-encoded and
// silently corrupts the HMAC input. This bit once already (auth.totp_required failures with a
// seemingly-correct implementation) before switching to ArrayBuffer, which is genuinely
// binary-safe.

import crypto from 'k6/crypto';

const BASE32_ALPHABET = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';

function base32Decode(input) {
  const clean = input.toUpperCase().replace(/=+$/, '');
  let bits = '';
  for (const char of clean) {
    const index = BASE32_ALPHABET.indexOf(char);
    if (index === -1) {
      throw new Error(`Invalid base32 character: ${char}`);
    }
    bits += index.toString(2).padStart(5, '0');
  }

  const byteCount = Math.floor(bits.length / 8);
  const bytes = new Uint8Array(byteCount);
  for (let i = 0; i < byteCount; i++) {
    bytes[i] = parseInt(bits.substring(i * 8, i * 8 + 8), 2);
  }
  return bytes;
}

function hexToBytes(hex) {
  const bytes = new Uint8Array(hex.length / 2);
  for (let i = 0; i < bytes.length; i++) {
    bytes[i] = parseInt(hex.substring(i * 2, i * 2 + 2), 16);
  }
  return bytes;
}

/** Computes the current 6-digit TOTP code for a base32-encoded secret. */
export function computeCurrentTotp(base32Secret, timeStepSeconds = 30, digits = 6) {
  const secretBytes = base32Decode(base32Secret);
  const counter = Math.floor(Date.now() / 1000 / timeStepSeconds);

  const counterBytes = new Uint8Array(8);
  let remaining = counter;
  for (let i = 7; i >= 0 && remaining > 0; i--) {
    counterBytes[i] = remaining & 0xff;
    remaining = Math.floor(remaining / 256);
  }

  const digestHex = crypto.hmac('sha1', secretBytes.buffer, counterBytes.buffer, 'hex');
  const digest = hexToBytes(digestHex);

  const offset = digest[digest.length - 1] & 0x0f;
  const binaryCode =
    ((digest[offset] & 0x7f) << 24) |
    ((digest[offset + 1] & 0xff) << 16) |
    ((digest[offset + 2] & 0xff) << 8) |
    (digest[offset + 3] & 0xff);

  const code = (binaryCode % Math.pow(10, digits)).toString();
  return code.padStart(digits, '0');
}
