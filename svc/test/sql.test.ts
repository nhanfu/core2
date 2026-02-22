import { describe, expect, test } from "bun:test";
import { SqlBuilder } from "../src/runtime/sql.js";

describe("SqlBuilder", () => {
  test("buildCreateOrUpdate inserts when no OldVal", () => {
    const builder = new SqlBuilder("user1");
    const sql = builder.buildCreateOrUpdate({
      Table: "User",
      Changes: [
        { Field: "Id", Value: "abc" },
        { Field: "Name", Value: "Jane" },
      ],
    });
    expect(sql).toContain("insert into [User]");
    expect(sql).toContain("'abc'");
    expect(sql).toContain("Jane");
  });

  test("buildCreateOrUpdate updates when OldVal exists", () => {
    const builder = new SqlBuilder("user1");
    const sql = builder.buildCreateOrUpdate({
      Table: "User",
      Changes: [
        { Field: "Id", Value: "abc", OldVal: "old" },
        { Field: "Name", Value: "Jane" },
      ],
    });
    expect(sql).toContain("update [User]");
    expect(sql).toContain("where Id = 'old'");
  });

  test("buildUpdate requires Id", () => {
    const builder = new SqlBuilder("user1");
    expect(() => builder.buildUpdate({ Table: "User", Changes: [] })).toThrow();
  });
});
