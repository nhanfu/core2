import { assertEquals, assertExists } from "https://deno.land/std@0.208.0/assert/mod.ts";
import path from "node:path";
import { UserService } from "../src/runtime/userService.ts";
import { StorageService } from "../src/runtime/userService/storageService.ts";
import type { DataAdapter, MetadataStore, PatchVM } from "../src/runtime/types.ts";

const createAdapter = () => {
  const queries: string[] = [];
  const executes: string[] = [];
  const adapter: DataAdapter = {
    async query(sql: string) {
      queries.push(sql);
      if (sql.includes("FeaturePolicy")) {
        return [{ TableName: "Demo", CanWriteAll: true }];
      }
      return [];
    },
    async execute(sql: string) {
      executes.push(sql);
      return 1;
    },
  };
  return { adapter, queries, executes };
};

const createStore = (): MetadataStore => ({
  async getFeature() {
    return null;
  },
});

Deno.test("UserService: savePatch executes insert sql", async () => {
  const { adapter, executes } = createAdapter();
  const service = new UserService({
    adapter,
    metadataStore: createStore(),
    userId: "1",
    roleIds: ["ADMIN"],
    tenantCode: "system",
  });

  const patch: PatchVM = {
    Table: "Demo",
    Changes: [
      { Field: "Id", Value: "demo-1" },
      { Field: "Name", Value: "Demo" },
    ],
  };

  await service.savePatch(patch);

  assertEquals(executes.length, 1);
  assertEquals(executes[0].includes('insert into "Demo"'), true);
});

Deno.test("UserService: parseCsvFile creates patches", async () => {
  const { adapter } = createAdapter();
  const service = new UserService({
    adapter,
    metadataStore: createStore(),
    tenantCode: "system",
    userId: "1",
  });
  const csvPath = path.join(Deno.cwd(), "test", "tmp-user-service.csv");
  const csvContent = "Name,Value\nAlpha,1\nBeta,2\n";
  await Deno.writeTextFile(csvPath, csvContent);

  const storage = new StorageService(service);
  const parsed = await storage.parseCsvFile(csvPath, "Demo");
  assertEquals(parsed.length, 2);
  
  // Cleanup
  await Deno.remove(csvPath);
});
