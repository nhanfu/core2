import { assertEquals, assertExists, assertRejects } from "jsr:@std/assert@1";

// Test YAML parser utilities
import { parseYaml, stringifyYaml } from "./yamlParser.ts";

Deno.test("parseYaml should parse valid YAML string", () => {
  const yaml = `
name: test
age: 25
active: true
`;
  const result = parseYaml(yaml) as any;
  assertEquals(result.name, "test");
  assertEquals(result.age, 25);
  assertEquals(result.active, true);
});

Deno.test("parseYaml should parse YAML array", () => {
  const yaml = `
items:
  - item1
  - item2
  - item3
`;
  const result = parseYaml(yaml) as any;
  assertEquals(result.items.length, 3);
  assertEquals(result.items[0], "item1");
});

Deno.test("parseYaml should parse nested objects", () => {
  const yaml = `
user:
  name: John
  address:
    city: NYC
    zip: 10001
`;
  const result = parseYaml(yaml) as any;
  assertEquals(result.user.name, "John");
  assertEquals(result.user.address.city, "NYC");
});

Deno.test("stringifyYaml should convert object to YAML", () => {
  const obj = { name: "test", age: 25 };
  const result = stringifyYaml(obj);
  assertExists(result);
  assertEquals(result.includes("name: test"), true);
  assertEquals(result.includes("age: 25"), true);
});

Deno.test("stringifyYaml should convert array to YAML", () => {
  const arr = ["item1", "item2", "item3"];
  const result = stringifyYaml(arr);
  assertExists(result);
  assertEquals(result.includes("item1"), true);
  assertEquals(result.includes("item2"), true);
});

Deno.test("stringifyYaml should handle nested objects", () => {
  const obj = {
    user: {
      name: "John",
      address: {
        city: "NYC"
      }
    }
  };
  const result = stringifyYaml(obj);
  assertExists(result);
  assertEquals(result.includes("user:"), true);
  assertEquals(result.includes("name: John"), true);
  assertEquals(result.includes("city: NYC"), true);
});

Deno.test("parseYaml should handle empty string", () => {
  const result = parseYaml("");
  // YAML parser returns null for empty string
  assertEquals(result === null || result === undefined, true);
});

Deno.test("stringifyYaml should handle empty object", () => {
  const result = stringifyYaml({});
  assertEquals(result, "{}\n");
});
