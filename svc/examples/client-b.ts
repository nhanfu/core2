import {
  DefaultScriptRunner,
  EventRegistry,
  RuntimeEngine,
  type Feature,
} from "../src/index.js";

const feature: Feature = {
  Name: "event-demo",
  FeaturePolicies: [{ RoleId: "OPS", CanRead: true }],
  Events: JSON.stringify({ click: "Ping" }),
};

const store = {
  async getFeature() {
    return feature;
  },
};

const adapter = {
  async query() {
    return [];
  },
  async execute() {
    return 1;
  },
};

const registry = new EventRegistry();
registry.register("Ping", () => "pong");

const runtime = new RuntimeEngine(store, adapter, new DefaultScriptRunner(), registry);
const result = await runtime.executeEvent(feature, feature.Events, "click");
console.log("Event result", result);
