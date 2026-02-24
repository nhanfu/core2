import { describe, expect, test } from "bun:test";
import path from "path";
import { UserService } from "../src/runtime/userService.js";
import { StorageService } from "../src/runtime/userService/storageService.js";
import type { DataAdapter, MetadataStore, PatchVM } from "../src/runtime/types.js";

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

describe("UserService", () => {
  test("savePatch executes insert sql", async () => {
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

    expect(executes.length).toBe(1);
    expect(executes[0]).toContain("insert into [Demo]");
  });

  test("getMenu builds query with role ids", async () => {
    const { adapter, queries } = createAdapter();
    const service = new UserService({
      adapter,
      metadataStore: createStore(),
      roleIds: ["ADMIN", "OPS"],
      tenantCode: "system",
    });

    await service.getMenu();
    expect(queries.some((query) => query.includes("RoleId in"))).toBe(true);
  });

  test("parseCsvFile creates patches", async () => {
    const { adapter } = createAdapter();
    const service = new UserService({
      adapter,
      metadataStore: createStore(),
      tenantCode: "system",
      userId: "1",
    });
    const csvPath = path.join(process.cwd(), "test", "tmp-user-service.csv");
    const csvContent = "Name,Value\nAlpha,1\nBeta,2\n";
    await Bun.write(csvPath, csvContent);

    const storage = new StorageService(service);
    const parsed = await storage.parseCsvFile(csvPath, "Demo");
    expect(parsed.length).toBe(2);
  });
});
