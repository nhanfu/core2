import { assertEquals, assertStringIncludes } from "https://deno.land/std@0.208.0/assert/mod.ts";
import { SqlBuilder } from "../src/runtime/sql.ts";

Deno.test("SqlBuilder: buildCreateOrUpdate inserts when no OldVal", () => {
  const builder = new SqlBuilder("user1");
  const sql = builder.buildCreateOrUpdate({
    Table: "User",
    Changes: [
      { Field: "Id", Value: "abc" },
      { Field: "Name", Value: "Jane" },
    ],
  });
  assertStringIncludes(sql, 'insert into "User"');
  assertStringIncludes(sql, "'abc'");
  assertStringIncludes(sql, "Jane");
});

Deno.test("SqlBuilder: buildCreateOrUpdate updates when OldVal exists", () => {
  const builder = new SqlBuilder("user1");
  const sql = builder.buildCreateOrUpdate({
    Table: "User",
    Changes: [
      { Field: "Id", Value: "abc", OldVal: "old" },
      { Field: "Name", Value: "Jane" },
    ],
  });
  assertStringIncludes(sql, 'update "User"');
  assertStringIncludes(sql, 'where "Id" = \'old\'');
});

Deno.test("SqlBuilder: buildCreateOrUpdate uses postgres quoting", () => {
  const builder = new SqlBuilder("user1");
  const sql = builder.buildCreateOrUpdate({
    Table: "User",
    Changes: [
      { Field: "Id", Value: "abc" },
      { Field: "Name", Value: "Jane" },
    ],
  });
  assertStringIncludes(sql, 'insert into "User"');
  assertStringIncludes(sql, '"Name"');
  assertStringIncludes(sql, "'Jane'");
});

Deno.test("SqlBuilder: buildUpdate requires Id", () => {
  const builder = new SqlBuilder("user1");
  try {
    builder.buildUpdate({ Table: "User", Changes: [{ Field: "Id", Value: null }] });
    throw new Error("Should have thrown");
  } catch (e) {
    assertEquals((e as Error).message, "Id cannot be null");
  }
});
