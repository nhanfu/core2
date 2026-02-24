import type { Component, Feature } from "./types.js";

export const SystemFields = [
  "id",
  "insertedby",
  "inserteddate",
  "updatedby",
  "updateddate",
  "tenantcode",
];

export const isNullOrWhiteSpace = (value?: string | null): boolean => {
  if (value === null || value === undefined) return true;
  return value.toString().trim() === "";
};

export const escapeSqlValue = (value: string | null | undefined): string => {
  if (value === null || value === undefined) return "null";
  const escaped = value.replace(/'/g, "''");
  return `'${escaped}'`;
};

export const toLowerSafe = (value?: string | null): string => {
  return (value || "").toLowerCase();
};

export const parseJsonSafe = <T>(value?: string | null): T | null => {
  if (value === null || value === undefined) return null;
  try {
    return JSON.parse(value) as T;
  } catch {
    return null;
  }
};

export const formatTemplate = (template: string, data: Record<string, unknown>): string => {
  return template.replace(/\{([^}]+)\}/g, (_, key: string) => {
    const value = data[key];
    if (value === null || value === undefined) return "";
    return String(value);
  });
};

export const flattenComponents = (feature?: Feature | null): Component[] => {
  if (!feature) return [];
  const roots: Component[] = [];
  if (Array.isArray(feature.Components)) roots.push(...feature.Components);
  if (Array.isArray(feature.ComponentGroup)) roots.push(...feature.ComponentGroup);
  const stack = [...roots];
  const result: Component[] = [];
  while (stack.length > 0) {
    const current = stack.pop();
    if (!current) continue;
    result.push(current);
    if (Array.isArray(current.Components)) {
      stack.push(...current.Components);
    }
    if (Array.isArray(current.ComponentGroup)) {
      stack.push(...current.ComponentGroup);
    }
  }
  return result;
};
