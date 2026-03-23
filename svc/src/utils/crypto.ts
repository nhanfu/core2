/**
 * Crypto utilities for password hashing
 * Compatible with .NET System.Security.Cryptography.SHA256
 */

const SALT_LENGTH = 32;

/**
 * Generate a random salt for password hashing
 * @returns Random salt string (uppercase letters only, 32 characters)
 */
export function GenerateSalt(): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
  let result = '';
  const randomValues = new Uint32Array(SALT_LENGTH);
  crypto.getRandomValues(randomValues);

  for (let i = 0; i < SALT_LENGTH; i++) {
    result += chars[randomValues[i] % chars.length];
  }

  return result;
}

/**
 * Compute SHA256 hash of the input string
 * Uses Web Crypto API (crypto.subtle)
 * Returns lowercase hex string, compatible with .NET's SHA256
 * @param input - The string to hash (UTF-8 encoded)
 * @returns Lowercase hex string of the hash
 */
export async function SHA256(input: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(input);
  const hashBuffer = await crypto.subtle.digest('SHA-256', data);
  const hashArray = new Uint8Array(hashBuffer);
  const hashHex = Array.from(hashArray)
    .map(b => b.toString(16).padStart(2, '0'))
    .join('');
  return hashHex;
}

/**
 * Hash a password with the given salt
 * @param password - The plain text password
 * @param salt - The salt to use for hashing
 * @returns SHA256 hash as lowercase hex string (password + salt concatenated)
 */
export async function HashPassword(password: string, salt: string): Promise<string> {
  return SHA256(password + salt);
}

