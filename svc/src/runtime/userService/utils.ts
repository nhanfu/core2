import { access, mkdir, readFile, writeFile } from "fs/promises";
import path from "path";
import type { PatchDetail, PatchVM } from "../types.js";
import { escapeSqlValue, formatTemplate, isNullOrWhiteSpace, parseJsonSafe } from "../utils.js";

export const DEFAULT_CACHE_TTL_MS = 30 * 60 * 1000;

export const ensureArray = <T>(value?: T[] | null): T[] => (value ? [...value] : []);

export const isEmpty = (value?: Array<unknown> | null): boolean => !value || value.length === 0;

export const toIso = (date: Date): string => date.toISOString();

export const toStringSafe = (value: unknown): string => (value === null || value === undefined ? "" : String(value));

export const normalizeIdValue = (value: string | null | undefined): string | null => {
  if (!value) return null;
  if (value.startsWith("-")) return value.substring(1);
  return value;
};

export const escapeValue = (value: string | null | undefined): string => {
  if (value === null || value === undefined) return "null";
  return escapeSqlValue(String(value));
};

export const combineStrings = (values?: Array<string | number | null | undefined>): string => {
  const filtered = (values || []).filter((value): value is string | number => value !== null && value !== undefined);
  if (filtered.length === 0) return "";
  return filtered.map((value) => escapeValue(String(value))).join(",");
};

export const distinctBy = <T, K>(items: T[], selector: (item: T) => K): T[] => {
  const seen = new Set<K>();
  const result: T[] = [];
  for (const item of items) {
    const key = selector(item);
    if (seen.has(key)) continue;
    seen.add(key);
    result.push(item);
  }
  return result;
};

export const mapPatch = (table: string, entity: Record<string, unknown>, update: boolean): PatchVM => {
  const changes: PatchDetail[] = Object.entries(entity).map(([key, value]) => ({
    Field: key,
    Value: value === undefined ? null : String(value),
  }));
  const idChange = changes.find((change) => change.Field === "Id");
  if (update && idChange) {
    idChange.OldVal = idChange.Value ?? null;
  }
  return { Table: table, Changes: changes };
};

export const formatEntity = (template: string, data: Record<string, unknown>): string => {
  if (isNullOrWhiteSpace(template)) return "";
  return formatTemplate(template, data || {});
};

export const getChange = (vm: PatchVM, excludeField: string): PatchDetail | undefined =>
  vm.Changes?.find((change) => change.Field !== excludeField);

export const getChangeValue = (vm: PatchVM, excludeField: string): string | null => {
  const change = getChange(vm, excludeField);
  if (!change) return null;
  return change.Value ?? null;
};

export const setChangeValue = (vm: PatchVM, field: string, value: string | null): void => {
  if (!vm.Changes) vm.Changes = [];
  const existing = vm.Changes.find((change) => change.Field === field);
  if (existing) {
    existing.Value = value;
    return;
  }
  vm.Changes.push({ Field: field, Value: value });
};

export const readText = async (filePath: string): Promise<string | null> => {
  try {
    if (typeof Bun !== "undefined") {
      return await Bun.file(filePath).text();
    }
    return await readFile(filePath, "utf-8");
  } catch {
    return null;
  }
};

export const writeText = async (filePath: string, content: string): Promise<void> => {
  if (typeof Bun !== "undefined") {
    await Bun.write(filePath, content);
    return;
  }
  await writeFile(filePath, content, "utf-8");
};

export const writeBinary = async (filePath: string, content: ArrayBuffer): Promise<void> => {
  if (typeof Bun !== "undefined") {
    await Bun.write(filePath, new Uint8Array(content));
    return;
  }
  await writeFile(filePath, Buffer.from(content));
};

export const ensureDirectoryExists = async (filePath: string): Promise<void> => {
  const dir = path.dirname(filePath);
  await mkdir(dir, { recursive: true });
};

export const fileExists = async (filePath: string): Promise<boolean> => {
  if (typeof Bun !== "undefined") {
    return await Bun.file(filePath).exists();
  }
  try {
    await access(filePath);
    return true;
  } catch {
    return false;
  }
};

export const increaseFileName = async (filePath: string): Promise<string> => {
  let index = 0;
  let current = filePath;
  // eslint-disable-next-line no-constant-condition
  while (true) {
    const exists = await fileExists(current);
    if (!exists) return current;
    const dir = path.dirname(filePath);
    const ext = path.extname(filePath);
    const base = path.basename(filePath, ext);
    index += 1;
    current = path.join(dir, `${base}_${index}${ext}`);
  }
};

export const getRowValue = (row: Record<string, unknown>, key: string): unknown => {
  if (key in row) return row[key];
  const lower = key.toLowerCase();
  const match = Object.keys(row).find((k) => k.toLowerCase() === lower);
  if (!match) return undefined;
  return row[match];
};

export const isOwner = (entity: Record<string, unknown>, userId?: string, roleIds?: string[]): boolean => {
  if (!entity || !userId) return false;
  const ownerUserIds = toStringSafe(getRowValue(entity, "OwnerUserIds"));
  const ownerRoleIds = toStringSafe(getRowValue(entity, "OwnerRoleIds"));
  const createdId = toStringSafe(getRowValue(entity, "InsertedBy"));
  const isOwnerUser = ownerUserIds !== "" && ownerUserIds.split(",").includes(userId);
  const roleSet = new Set(roleIds || []);
  const isOwnerRole = ownerRoleIds !== "" && ownerRoleIds.split(",").some((role) => roleSet.has(role));
  const isOwnerInsert = ownerUserIds === "" && createdId === userId;
  return isOwnerUser || isOwnerRole || isOwnerInsert;
};

export const parseWhereParams = (value?: string | null): Record<string, unknown> => {
  if (!value) return {};
  const params = parseJsonSafe<Array<{ FieldName?: string; Value?: string | null }>>(value);
  if (!params) return {};
  return params.reduce<Record<string, unknown>>((acc, item) => {
    if (item.FieldName) acc[item.FieldName] = item.Value ?? null;
    return acc;
  }, {});
};
