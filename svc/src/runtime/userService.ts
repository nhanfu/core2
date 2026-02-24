import type {
  Conversation,
  DataAdapter,
  Gos,
  MetadataStore,
  PatchVM,
  RuntimeContext,
  SqlComResult,
  SqlDialect,
  SqlResult,
  SqlViewModel,
} from "./types.js";
import { SqlBuilder } from "./sql.js";
import type { CacheStore, Claim, FileLike, RequestInfo, UserServiceContext, UserServiceOptions } from "./userService/types.js";
import { MemoryCacheStore } from "./userService/cache.js";
import { FeatureService } from "./userService/featureService.js";
import { PatchService } from "./userService/patchService.js";
import { QueryService } from "./userService/queryService.js";
import { StorageService } from "./userService/storageService.js";
import { ensureArray, escapeValue, toIso, isEmpty } from "./userService/utils.js";

export class UserService implements UserServiceContext {
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

  private featureService: FeatureService;
  private patchService: PatchService;
  private queryService: QueryService;
  private storageService: StorageService;

  constructor(options: UserServiceOptions) {
    this.adapter = options.adapter;
    this.metadataStore = options.metadataStore;
    this.cache = options.cache ?? new MemoryCacheStore();
    this.now = options.now ?? (() => new Date());
    this.resolveConn = options.resolveConn;
    this.request = options.request;
    this.webRootPath = options.webRootPath ?? process.cwd();
    this.metadataRoot = options.metadataRoot ?? this.webRootPath;
    this.sqlDialect = options.sqlDialect ?? "sqlserver";
    this.defaultConnKey = options.defaultConnKey ?? "logistics";
    this.GroupId = options.groupId;
    this.DepartmentId = options.departmentId;
    this.UserId = options.userId;
    this.FullName = options.fullName;
    this.UserName = options.userName;
    this.Avatar = options.avatar;
    this.CLogo = options.cLogo;
    this.CCompanyName = options.cCompanyName;
    this.CAddress = options.cAddress;
    this.CPhoneNumber = options.cPhoneNumber;
    this.CEmail = options.cEmail;
    this.ConnKey = options.connKey;
    this.BranchId = options.branchId;
    this.CenterIds = ensureArray(options.centerIds);
    this.VendorId = options.vendorId;
    this.Env = options.env;
    this.TenantCode = options.tenantCode;
    this.RoleIds = ensureArray(options.roleIds);
    this.RoleNames = ensureArray(options.roleNames);
    this.sqlBuilder = new SqlBuilder(this.UserId || "1", this.sqlDialect);

    this.featureService = new FeatureService(this);
    this.patchService = new PatchService(this, this.featureService);
    this.queryService = new QueryService(this, this.featureService, this.patchService);
    this.storageService = new StorageService(this);
  }

  static fromClaims(options: UserServiceOptions, claims: Claim[]): UserService {
    const userService = new UserService(options);
    userService.extractMeta(claims);
    return userService;
  }

  private extractMeta(claims: Claim[]): void {
    const findClaim = (type: string): string | undefined => claims.find((claim) => claim.type === type)?.value;
    this.BranchId = findClaim("BranchId") ?? this.BranchId;
    this.UserId = findClaim("UserId") ?? this.UserId;
    this.FullName = findClaim("FullName") ?? this.FullName;
    this.Avatar = findClaim("Avatar") ?? this.Avatar;
    this.GroupId = findClaim("TeamId") ?? this.GroupId;
    this.DepartmentId = findClaim("DepartmentId") ?? this.DepartmentId;
    this.UserName = findClaim("UserName") ?? this.UserName;
    this.CenterIds = claims.filter((claim) => claim.type === "CenterIds").map((claim) => claim.value);
    this.RoleIds = claims.filter((claim) => claim.type === "RoleIds").map((claim) => claim.value);
    this.RoleNames = claims.filter((claim) => claim.type === "Role").map((claim) => claim.value);
    this.VendorId = findClaim("PartnerId") ?? this.VendorId;
    const tenant = findClaim("TenantCode");
    this.TenantCode = tenant ? tenant.toUpperCase() : this.TenantCode;
    this.CLogo = findClaim("CLogo") ?? this.CLogo;
    this.CCompanyName = findClaim("CCompanyName") ?? this.CCompanyName;
    this.CAddress = findClaim("CAddress") ?? this.CAddress;
    this.CPhoneNumber = findClaim("CPhoneNumber") ?? this.CPhoneNumber;
    this.CEmail = findClaim("CEmail") ?? this.CEmail;
  }

  getRuntimeContext(): RuntimeContext {
    return {
      tenant: this.TenantCode || undefined,
      env: this.Env || undefined,
      roleIds: this.RoleIds || [],
      roleNames: this.RoleNames || [],
      userId: this.UserId || undefined,
      sqlDialect: this.sqlDialect,
    };
  }

  async resolveConnection(conn?: string | null): Promise<string | undefined> {
    if (!this.resolveConn) return conn || undefined;
    const resolved = await this.resolveConn(conn);
    return resolved || conn || undefined;
  }

  getDefaultConn(): string {
    return this.defaultConnKey;
  }

  async query(sql: string, params?: Record<string, unknown>): Promise<Array<Record<string, unknown>>> {
    const conn = this.getDefaultConn();
    const resolved = await this.resolveConnection(conn);
    return this.adapter.query(sql, { conn: resolved, params });
  }

  async queryMany(sql: string, params?: Record<string, unknown>): Promise<Array<Array<Record<string, unknown>>>> {
    const conn = this.getDefaultConn();
    const resolved = await this.resolveConnection(conn);
    if (this.adapter.queryMany) {
      return this.adapter.queryMany(sql, { conn: resolved, params });
    }
    const data = await this.adapter.query(sql, { conn: resolved, params });
    return [data];
  }

  async execute(sql: string, params?: Record<string, unknown>): Promise<number> {
    const conn = this.getDefaultConn();
    const resolved = await this.resolveConnection(conn);
    return this.adapter.execute(sql, { conn: resolved, params });
  }

  go(vm: SqlViewModel): Promise<SqlResult> {
    return this.queryService.go(vm);
  }

  gos(items: Gos[]) {
    return this.queryService.gos(items);
  }

  goByName(vm: SqlViewModel): Promise<SqlResult> {
    return this.queryService.goByName(vm);
  }

  moveHBL(entity: { ShipmentId?: string; ShipmentDetailId?: string[] }) {
    return this.queryService.moveHBL(entity);
  }

  conversation(entity: Conversation) {
    return this.queryService.conversation(entity);
  }

  getMenu() {
    return this.queryService.getMenu();
  }

  getFeature(name: string) {
    return this.featureService.getFeature(name);
  }

  publishAllFeature(tenant: string) {
    return this.featureService.publishAllFeature(tenant);
  }

  publishFeatureByName(name: string, tenant?: string | null) {
    return this.featureService.publishFeatureByName(name, tenant);
  }

  hardDelete(vm: PatchVM) {
    return this.patchService.hardDelete(vm);
  }

  savePatch(vm: PatchVM) {
    return this.patchService.savePatch(vm);
  }

  updatePatch(vm: PatchVM) {
    return this.patchService.updatePatch(vm);
  }

  sendEntity(vm: PatchVM): Promise<SqlResult> {
    return this.patchService.sendEntity(vm);
  }

  approvedEntity(vm: PatchVM): Promise<SqlResult> {
    return this.patchService.approvedEntity(vm);
  }

  forwardEntity(vm: PatchVM): Promise<SqlResult> {
    return this.patchService.forwardEntity(vm);
  }

  declineEntity(vm: PatchVM): Promise<SqlResult> {
    return this.patchService.declineEntity(vm);
  }

  savePatch2(vm: PatchVM): Promise<SqlResult> {
    return this.patchService.savePatch2(vm);
  }

  savePatchs2(vms: PatchVM[]): Promise<SqlResult> {
    return this.patchService.savePatchs2(vms);
  }

  savePatches(patches: PatchVM[]) {
    return this.patchService.savePatches(patches);
  }

  deactivateAsync(vm: SqlViewModel) {
    return this.patchService.deactivateAsync(vm);
  }

  comQuery(vm: SqlViewModel): Promise<SqlComResult> {
    return this.queryService.comQuery(vm);
  }

  report(vm: SqlViewModel) {
    return this.queryService.report(vm);
  }

  sql(vm: SqlViewModel) {
    return this.queryService.sql(vm);
  }

  checkDelete(item: { ComId?: string; EntityIds?: string[]; Params?: string | null }) {
    return this.queryService.checkDelete(item);
  }

  getMessageActive() {
    return this.queryService.getMessageActive();
  }

  convertHtmlToPlainText(htmlContent: string): string {
    let plainText = htmlContent.replace(/<[^>]+>|&nbsp;/g, "").trim();
    plainText = plainText.replace(/&(amp|quot|gt|lt|nbsp);/g, (_match, entity) => this.decodeEntity(entity));
    return plainText;
  }

  decodeEntity(entity: string): string {
    switch (entity) {
      case "amp":
        return "&";
      case "quot":
        return '"';
      case "gt":
        return ">";
      case "lt":
        return "<";
      case "nbsp":
        return " ";
      default:
        return entity;
    }
  }

  postImageAsync(imageBase64: string, name = "Captured", reup = false) {
    return this.storageService.postImageAsync(imageBase64, name, reup);
  }

  postFileAsync(file: FileLike, reup = false) {
    return this.storageService.postFileAsync(file, reup);
  }

  getHttpPath(path: string, webRootPath: string) {
    return this.storageService.getHttpPath(path, webRootPath);
  }

  async importCsv(files: FileLike[], table: string, comId: string, connKey: string): Promise<boolean> {
    if (comId.trim() === "" || table.trim() === "") throw new Error("ComId or table cannot be null");
    if (files.length === 0) throw new Error("No file uploaded");
    await this.comQuery({ ComId: comId, DataConn: connKey });
    const connStr = await this.resolveConnection(connKey);
    const tableRights = await this.patchService.getEntityPermissions(table, null);
    if (!tableRights.some((perm) => perm.CanWriteAll)) {
      throw new Error("Cannot import data due to lack of permission");
    }
    const file = files[0];
    const finalPath = await this.storageService.saveFileToUpload(file, true);
    const patches = await this.storageService.parseCsvFile(finalPath, table);
    if (patches.length === 0) return true;
    const sqlStatements = patches.map((patch) => {
      const changes = patch.Changes || [];
      const fields = changes.map((change) => `[${change.Field}]`);
      const values = changes.map((change) => escapeValue(change.Value || null, this.sqlDialect));
      const now = toIso(this.now());
      if (!fields.includes("[Id]")) {
        fields.unshift("[Id]");
        values.unshift(escapeValue(crypto.randomUUID(), this.sqlDialect));
      }
      if (!fields.includes("[TenantCode]")) {
        fields.unshift("[TenantCode]");
        values.unshift(escapeValue(this.TenantCode || "system", this.sqlDialect));
      }
      if (!fields.includes("[Active]")) {
        fields.unshift("[Active]");
        values.unshift("1");
      }
      if (!fields.includes("[InsertedBy]")) {
        fields.unshift("[InsertedBy]");
        values.unshift(escapeValue(this.UserId || "0", this.sqlDialect));
      }
      if (!fields.includes("[InsertedDate]") && !fields.includes("[Inserteddate]")) {
        fields.unshift("[InsertedDate]");
        values.unshift(escapeValue(now, this.sqlDialect));
      }
      return `insert into [${table}] (${fields.join(", ")}) values (${values.join(", ")})`;
    });
    if (!isEmpty(sqlStatements)) {
      await this.execute(sqlStatements.join(";"));
    }
    await this.storageService.deleteFile(finalPath);
    return true;
  }

  getUploadPath(fileName: string, webRootPath: string): string {
    return this.storageService.getUploadPath(fileName, webRootPath);
  }

  deleteFile(path: string) {
    return this.storageService.deleteFile(path);
  }

  readDs(query: string) {
    return this.queryService.readDs(query);
  }

  getStringAsync(key: string): Promise<string | null> {
    return this.cache.get(key?.toUpperCase()) as Promise<string | null>;
  }

  setStringAsync(key: string, val: string, ttlMs?: number): Promise<void> {
    return this.cache.set(key?.toUpperCase(), val, ttlMs) as Promise<void>;
  }
}

export type { CacheStore, Claim, FileLike, RequestInfo, UserServiceOptions };
