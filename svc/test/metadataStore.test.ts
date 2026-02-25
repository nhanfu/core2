import { assertEquals } from "https://deno.land/std@0.208.0/assert/mod.ts";
import { FileMetadataStore } from "../src/runtime/metadataStore.js";

Deno.test("FileMetadataStore: loads YAML and YML features", async () => {
  const baseDir = await Deno.makeTempDir();
  const featureDir = `${baseDir}/system/features`;
  await Deno.mkdir(featureDir, { recursive: true });
  
  await Deno.writeTextFile(
    `${featureDir}/yaml-feature.yaml`,
    "Id: yaml-feature\nName: yaml-feature\nLabel: YAML Feature\n",
  );
  await Deno.writeTextFile(
    `${featureDir}/yml-feature.yml`,
    "Id: yml-feature\nName: yml-feature\nLabel: YML Feature\n",
  );

  const store = new FileMetadataStore(baseDir, "system");
  const yaml = await store.getFeature("yaml-feature", { tenant: "system" });
  const yml = await store.getFeature("yml-feature", { tenant: "system" });

  assertEquals(yaml?.Name, "yaml-feature");
  assertEquals(yml?.Name, "yml-feature");
  
  // Cleanup
  await Deno.remove(baseDir, { recursive: true });
});
