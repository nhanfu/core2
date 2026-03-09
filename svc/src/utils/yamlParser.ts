import { parse as parseYamlFn, stringify as stringifyYamlFn } from "@std/yaml";

/**
 * Parses a YAML string into a JavaScript object.
 * @param content - The YAML string to parse
 * @returns The parsed object
 */
export function parseYaml(content: string): unknown {
  return parseYamlFn(content);
}

/**
 * Converts a JavaScript object to a YAML string.
 * @param obj - The object to convert to YAML
 * @returns The YAML string representation
 */
export function stringifyYaml(obj: unknown): string {
  return stringifyYamlFn(obj);
}
