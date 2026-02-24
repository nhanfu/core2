import type { DataAdapter, MetadataStore, SqlDialect } from "../types.js";
import type { SqlBuilder } from "../sql.js";

export interface CacheStore {
  get(key: string): Promise<string | null> | string | null;
  set(key: string, value: string, ttlMs?: number): Promise<void> | void;
}

export interface RequestInfo {
  scheme?: string;
  host?: string;
}

export interface FileLike {
  name: string;
  arrayBuffer(): Promise<ArrayBuffer>;
}

export interface UserServiceOptions {
  adapter: DataAdapter;
  metadataStore: MetadataStore;
  cache?: CacheStore;
  now?: () => Date;
  resolveConn?: (connKey?: string | null) => Promise<string | null> | string | null;
  request?: RequestInfo;
  webRootPath?: string;
  metadataRoot?: string;
  sqlDialect?: SqlDialect;
  defaultConnKey?: string;
  groupId?: string;
  departmentId?: string;
  userId?: string;
  fullName?: string;
  userName?: string;
  avatar?: string;
  cLogo?: string;
  cCompanyName?: string;
  cAddress?: string;
  cPhoneNumber?: string;
  cEmail?: string;
  connKey?: string;
  branchId?: string;
  centerIds?: string[];
  vendorId?: string;
  env?: string;
  tenantCode?: string;
  roleIds?: string[];
  roleNames?: string[];
}

export type Claim = { type: string; value: string };

export interface UserServiceContext {
  adapter: DataAdapter;
  metadataStore: MetadataStore;
  cache: CacheStore;
  now: () => Date;
  resolveConn?: (connKey?: string | null) => Promise<string | null> | string | null;
  request?: RequestInfo;
  webRootPath: string;
  metadataRoot: string;
  sqlDialect: SqlDialect;
  defaultConnKey: string;
  sqlBuilder: SqlBuilder;
  GroupId?: string;
  DepartmentId?: string;
  UserId?: string;
  FullName?: string;
  UserName?: string;
  Avatar?: string;
  CLogo?: string;
  CCompanyName?: string;
  CAddress?: string;
  CPhoneNumber?: string;
  CEmail?: string;
  ConnKey?: string;
  BranchId?: string;
  CenterIds?: string[];
  VendorId?: string;
  Env?: string;
  TenantCode?: string;
  RoleIds?: string[];
  RoleNames?: string[];
  getRuntimeContext(): import("../types.js").RuntimeContext;
  query(sql: string, params?: Record<string, unknown>): Promise<Array<Record<string, unknown>>>;
  queryMany(sql: string, params?: Record<string, unknown>): Promise<Array<Array<Record<string, unknown>>>>;
  execute(sql: string, params?: Record<string, unknown>): Promise<number>;
  resolveConnection(conn?: string | null): Promise<string | undefined>;
  getDefaultConn(): string;
}
