// Build script for Deno - Simple build that copies TypeScript files
// Deno can run TypeScript directly, so we just copy the files to dist
import { resolve } from "jsr:@std/path@0.208.0";

const srcDir = resolve(Deno.cwd(), "src");
const outDir = resolve(Deno.cwd(), "dist");

// Create dist directory
await Deno.mkdir(outDir, { recursive: true });

console.log("Copying TypeScript files to dist...");

async function buildDir(dir: string, outDir: string) {
  for await (const entry of Deno.readDir(dir)) {
    const srcPath = resolve(dir, entry.name);
    const outPath = resolve(outDir, entry.name);

    if (entry.isDirectory) {
      await Deno.mkdir(outPath, { recursive: true });
      await buildDir(srcPath, outPath);
    } else if (entry.name.endsWith(".ts")) {
      // Copy TypeScript files - Deno can run them directly
      const content = await Deno.readTextFile(srcPath);
      await Deno.writeTextFile(outPath, content);
      console.log(`Copied: ${outPath}`);
    } else if (entry.name.endsWith(".js")) {
      // Copy JS files directly
      const content = await Deno.readTextFile(srcPath);
      await Deno.writeTextFile(outPath, content);
      console.log(`Copied: ${outPath}`);
    }
  }
}

await buildDir(srcDir, outDir);

// Copy package.json to dist
const pkg = await Deno.readTextFile("package.json");
await Deno.writeTextFile(resolve(outDir, "package.json"), pkg);

// Copy deno.json to dist
const denoJson = await Deno.readTextFile("deno.json");
await Deno.writeTextFile(resolve(outDir, "deno.json"), denoJson);

console.log("Build complete!");
console.log("Note: Deno can run TypeScript files directly. The dist folder contains the source files.");
