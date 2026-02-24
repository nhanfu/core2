# Corejs Runtime (Bun)

This package provides a Bun-native runtime for Corejs metadata-driven execution (query, action, and event flows) with permission enforcement and initial data loading.

## Install

```bash
bun add @corejs/runtime
```

## Quick Start

```ts
import {
  DefaultScriptRunner,
  EventRegistry,
  FileMetadataStore,
  RuntimeEngine,
  SqlBuilder,
} from "@corejs/runtime";

const store = new FileMetadataStore("D:/project/core2/CoreAPI/wwwroot/upload", "crm");

const adapter = {
  async query(sql: string) {
    console.log("Query", sql);
    return [];
  },
  async execute(sql: string) {
    console.log("Execute", sql);
    return 1;
  },
};

const registry = new EventRegistry();
registry.register("Ping", () => "pong");

const runtime = new RuntimeEngine(store, adapter, new DefaultScriptRunner(), registry);

const context = { tenant: "crm", env: "test", roleIds: ["ADMIN"], userId: "1" };
const feature = await runtime.getFeature("menu", context);
const initialData = await runtime.loadFeatureInitialData("menu", context);
console.log(feature?.Name, initialData.data.length);
```

## Key APIs

- `RuntimeEngine.getFeature(name, context)`
- `RuntimeEngine.loadFeatureInitialData(name, context)`
- `RuntimeEngine.executeQuery(request, context)`
- `RuntimeEngine.executePatch(patch, feature, component, context)`
- `RuntimeEngine.executeEvent(feature, eventsJson, eventType, args)`

## Metadata Formats

Metadata files can be stored as `.json`, `.yaml`, or `.yml` under the tenant `features` folder.

## SQL Dialect

`RuntimeEngine.executePatch` can emit SQL Server or PostgreSQL syntax. Set `context.sqlDialect` to `"postgres"` to enable PostgreSQL quoting and string literal behavior.

## Permissions

The permission pipeline uses `FeaturePolicies` and role IDs in the runtime context. Set `PatchVM.ByPassPerm = true` to bypass checks (use with caution).

## Initial Data Loading

Components with `Query` metadata are loaded in parallel. `PreQuery` is evaluated using the script runner to supply template params.

## Examples

- `svc/examples/client-a.ts`
- `svc/examples/client-b.ts`

## Tests

```bash
bun test
```
