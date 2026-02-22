import { describe, expect, test } from "bun:test";
import { loadInitialData } from "../src/runtime/initialData.js";
import { DefaultScriptRunner } from "../src/runtime/scriptRunner.js";

const adapter = {
  async query(sql: string): Promise<Array<Record<string, unknown>>> {
    return [{ sql }];
  },
  async execute(): Promise<number> {
    return 1;
  },
};

describe("loadInitialData", () => {
  test("loads query data with preQuery params", async () => {
    const feature = {
      Components: [
        {
          Id: "comp1",
          FieldName: "Comp1",
          Query: JSON.stringify({ sql: "select * from X where Id = '{Id}'" }),
          PreQuery: "return { Id: '1' }",
        },
      ],
    };
    const items = await loadInitialData(feature, { dataConn: "default" }, adapter, new DefaultScriptRunner());
    expect(items.length).toBe(1);
    expect(items[0].data[0].sql).toContain("Id = '1'");
  });
});
