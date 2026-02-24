import type { CacheStore } from "./types.js";
import { DEFAULT_CACHE_TTL_MS } from "./utils.js";

export class MemoryCacheStore implements CacheStore {
  private store = new Map<string, { value: string; expiresAt: number }>();

  async get(key: string): Promise<string | null> {
    const entry = this.store.get(key);
    if (!entry) return null;
    if (Date.now() > entry.expiresAt) {
      this.store.delete(key);
      return null;
    }
    return entry.value;
  }

  async set(key: string, value: string, ttlMs: number = DEFAULT_CACHE_TTL_MS): Promise<void> {
    this.store.set(key, { value, expiresAt: Date.now() + ttlMs });
  }
}
