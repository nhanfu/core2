import { describe, expect, test } from "bun:test";
import { EventRegistry } from "../src/runtime/eventRegistry.js";
import { DefaultScriptRunner } from "../src/runtime/scriptRunner.js";
import { RuntimeEngine } from "../src/runtime/runtime.js";

const adapter = {
  async query(sql: string): Promise<Array<Record<string, unknown>>> {
    return [{ sql }];
  },
  async execute(): Promise<number> {
    return 1;
  },
};

const store = {
  async getFeature() {
    return null;
  },
};

describe("RuntimeEngine", () => {
  test("executeEvent resolves registry handler", async () => {
    const registry = new EventRegistry();
    registry.register("Ping", () => "pong");
    const runtime = new RuntimeEngine(store, adapter, new DefaultScriptRunner(), registry);
    const result = await runtime.executeEvent(undefined, JSON.stringify({ click: "Ping" }), "click");
    expect(result).toBe("pong");
  });
});
