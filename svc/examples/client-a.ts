import { DefaultScriptRunner, SqlBuilder } from "../src/index.js";

const runner = new DefaultScriptRunner();
const result = runner.evaluate<number>("1 + 2");
console.log("Script result", result);

const sql = new SqlBuilder("1").buildCreateOrUpdate({
  Table: "Demo",
  Changes: [{ Field: "Id", Value: "demo-1" }, { Field: "Name", Value: "Demo" }],
});
console.log("SQL", sql);
