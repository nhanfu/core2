import { FileMetadataStore } from "../src/index.js";

const store = new FileMetadataStore(process.cwd(), "system");
const feature = await store.getFeature("demo");
console.log("Feature name", feature?.Name ?? null);
