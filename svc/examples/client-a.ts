import {
  DefaultScriptRunner,
  EventRegistry,
  RuntimeEngine,
  SqlBuilder,
  type Feature,
} from "../src/index.js";

const feature: Feature = {
  Name: "demo",
  FeaturePolicies: [{ RoleId: "ADMIN", CanWrite: true, CanRead: true }],
  Components: [
    {
      Id: "list",
      FieldName: "List",
      Query: JSON.stringify({ sql: "select * from Demo" }),
    },
  ],
};

const store = {
  async getFeature() {
    return feature;
  },
};

const adapter = {
  async query(sql: string) {
    return [{ sql, ok: true }];
  },
  async execute(sql: string) {
    return sql.length;
  },
};

const registry = new EventRegistry();
registry.register("Ping", () => "pong");

const runtime = new RuntimeEngine(store, adapter, new DefaultScriptRunner(), registry);
const context = { roleIds: ["ADMIN"], userId: "1" };

const initial = await runtime.loadFeatureInitialData("demo", context);
console.log("Initial data", initial.data[0]);

const sql = new SqlBuilder("1").buildCreateOrUpdate({
  Table: "Demo",
  Changes: [{ Field: "Id", Value: "demo-1" }, { Field: "Name", Value: "Demo" }],
});
console.log("SQL", sql);
