import { describe, expect, test } from "bun:test";
import { mkdtemp, mkdir, writeFile } from "fs/promises";
import os from "os";
import path from "path";
import { FileMetadataStore } from "../src/runtime/metadataStore.js";

describe("FileMetadataStore", () => {
  test("loads YAML and YML features", async () => {
    const baseDir = await mkdtemp(path.join(os.tmpdir(), "corejs-meta-"));
    const featureDir = path.join(baseDir, "system", "features");
    await mkdir(featureDir, { recursive: true });
    await writeFile(
      path.join(featureDir, "yaml-feature.yaml"),
      "Id: yaml-feature\nName: yaml-feature\nLabel: YAML Feature\n",
    );
    await writeFile(
      path.join(featureDir, "yml-feature.yml"),
      "Id: yml-feature\nName: yml-feature\nLabel: YML Feature\n",
    );

    const store = new FileMetadataStore(baseDir, "system");
    const yaml = await store.getFeature("yaml-feature", { tenant: "system" });
    const yml = await store.getFeature("yml-feature", { tenant: "system" });

    expect(yaml?.Name).toBe("yaml-feature");
    expect(yml?.Name).toBe("yml-feature");
  });
});
