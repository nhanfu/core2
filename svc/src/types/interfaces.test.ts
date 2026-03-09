import { assertEquals, assertExists } from "jsr:@std/assert@1";

// Test type interfaces - basic type validation tests

import type {
  User,
  Token,
  Partner,
  PatchVM,
  PatchDetail,
  Feature,
  UserContext,
  LoginVM,
  RegistrationVM,
  EmailVM,
  NotificationVM,
  SqlViewModel,
  Component,
} from "./interfaces.ts";

// ============================================
// User Interface Tests
// ============================================

Deno.test("User interface should have required properties", () => {
  const user: User = {
    id: "user-123",
    code: "USR001",
    email: "test@example.com",
    companyId: "company-001",
    userName: "testuser",
    fullName: "Test User",
    active: true,
    isDepartment: false,
    isTeam: false,
  };

  assertEquals(user.id, "user-123");
  assertEquals(user.userName, "testuser");
  assertEquals(user.active, true);
});

Deno.test("User interface should accept optional properties", () => {
  const user: User = {
    id: "user-123",
    code: "USR001",
    email: "test@example.com",
    companyId: "company-001",
    userName: "testuser",
    fullName: "Test User",
    active: true,
    isDepartment: false,
    isTeam: false,
    avatar: "avatar.png",
    phoneNumber: "1234567890",
    address: "123 Test St",
    roleIds: "role1,role2",
  };

  assertEquals(user.avatar, "avatar.png");
  assertEquals(user.phoneNumber, "1234567890");
});

// ============================================
// Token Interface Tests
// ============================================

Deno.test("Token interface should have required properties", () => {
  const token: Token = {
    userId: "user-123",
    userName: "testuser",
    email: "test@example.com",
    fullName: "Test User",
    departmentId: "dept-001",
    code: "USR001",
    positionId: "pos-001",
    address: "123 Test St",
    avatar: "avatar.png",
    accessToken: "jwt-access-token",
    refreshToken: "refresh-token",
    accessTokenExp: new Date(),
    refreshTokenExp: new Date(),
    roleIds: ["role1"],
    roleNames: ["Admin"],
    centerIds: ["center1"],
    ssn: "123-45-6789",
    phoneNumber: "1234567890",
    teamId: "team-001",
    partnerId: "partner-001",
    regionId: "region-001",
    signinDate: new Date(),
    tenantCode: "tenant-001",
    env: "production",
    connKey: "default",
  };

  assertEquals(token.userId, "user-123");
  assertEquals(token.accessToken, "jwt-access-token");
  assertEquals(token.roleIds.length, 1);
});

Deno.test("Token interface should accept vendor", () => {
  const partner: Partner = {
    id: "company-001",
    name: "Test Company",
    active: true,
    isPublic: false,
    isNoDebt: false,
  };

  const token: Token = {
    userId: "user-123",
    userName: "testuser",
    email: "test@example.com",
    fullName: "Test User",
    departmentId: "dept-001",
    code: "USR001",
    positionId: "pos-001",
    address: "123 Test St",
    avatar: "avatar.png",
    accessToken: "jwt-access-token",
    refreshToken: "refresh-token",
    accessTokenExp: new Date(),
    refreshTokenExp: new Date(),
    roleIds: [],
    roleNames: [],
    centerIds: [],
    ssn: "",
    phoneNumber: "",
    teamId: "",
    partnerId: "",
    regionId: "",
    signinDate: new Date(),
    tenantCode: "tenant-001",
    env: "production",
    connKey: "default",
    vendor: partner,
  };

  assertExists(token.vendor);
  assertEquals(token.vendor?.name, "Test Company");
});

// ============================================
// Partner Interface Tests
// ============================================

Deno.test("Partner interface should have required properties", () => {
  const partner: Partner = {
    id: "partner-001",
    name: "Test Partner",
    active: true,
    isPublic: false,
    isNoDebt: false,
  };

  assertEquals(partner.id, "partner-001");
  assertEquals(partner.name, "Test Partner");
});

Deno.test("Partner interface should accept optional properties", () => {
  const partner: Partner = {
    id: "partner-001",
    name: "Test Partner",
    fullName: "Test Partner Full Name",
    address: "123 Test Address",
    phoneNumber: "1234567890",
    email: "partner@example.com",
    active: true,
    isPublic: false,
    isNoDebt: false,
    taxCode: "12-3456789",
    web: "https://example.com",
  };

  assertEquals(partner.taxCode, "12-3456789");
  assertEquals(partner.web, "https://example.com");
});

// ============================================
// PatchVM Interface Tests
// ============================================

Deno.test("PatchVM interface should have required properties", () => {
  const patch: PatchVM = {
    byPassPerm: false,
    delete: [],
    metaConn: "meta",
    dataConn: "data",
    changes: [],
    detail: [],
    ids: [],
    update: false,
    realTime: false,
  };

  assertEquals(patch.byPassPerm, false);
  assertEquals(patch.changes.length, 0);
});

Deno.test("PatchVM interface should accept changes", () => {
  const patch: PatchVM = {
    byPassPerm: false,
    delete: [],
    metaConn: "meta",
    dataConn: "data",
    changes: [
      { field: "name", oldVal: "Old Name", value: "New Name" },
      { field: "active", value: "true" },
    ],
    detail: [],
    ids: ["id-1", "id-2"],
    update: true,
    realTime: true,
  };

  assertEquals(patch.changes.length, 2);
  assertEquals(patch.changes[0].field, "name");
  assertEquals(patch.changes[0].value, "New Name");
});

Deno.test("PatchDetail interface should have required properties", () => {
  const detail: PatchDetail = {
    field: "status",
    value: "active",
  };

  assertEquals(detail.field, "status");
  assertEquals(detail.value, "active");
});

Deno.test("PatchDetail interface should accept optional properties", () => {
  const detail: PatchDetail = {
    field: "name",
    label: "Name",
    oldVal: "Old Value",
    value: "New Value",
    historyValue: "History",
  };

  assertEquals(detail.label, "Name");
  assertEquals(detail.oldVal, "Old Value");
});

// ============================================
// UserContext Interface Tests
// ============================================

Deno.test("UserContext interface should have required properties", () => {
  const context: UserContext = {
    userId: "user-123",
    tenantCode: "tenant-001",
    env: "production",
    connKey: "default",
    roles: ["admin", "user"],
    roleNames: ["Admin", "User"],
    userName: "testuser",
    email: "test@example.com",
    fullName: "Test User",
  };

  assertEquals(context.userId, "user-123");
  assertEquals(context.roles.length, 2);
});

Deno.test("UserContext interface should accept optional properties", () => {
  const context: UserContext = {
    userId: "user-123",
    tenantCode: "tenant-001",
    env: "production",
    connKey: "default",
    roles: ["admin"],
    roleNames: ["Admin"],
    userName: "testuser",
    email: "test@example.com",
    fullName: "Test User",
    departmentId: "dept-001",
    positionId: "pos-001",
    teamId: "team-001",
    partnerId: "partner-001",
    centerIds: ["center1", "center2"],
    isAdmin: true,
  };

  assertEquals(context.isAdmin, true);
  assertEquals(context.centerIds?.length, 2);
});

// ============================================
// LoginVM Interface Tests
// ============================================

Deno.test("LoginVM interface should have required properties", () => {
  const login: LoginVM = {
    username: "testuser",
    password: "password123",
  };

  assertEquals(login.username, "testuser");
  assertEquals(login.password, "password123");
});

Deno.test("LoginVM interface should accept optional rememberMe", () => {
  const login: LoginVM = {
    username: "testuser",
    password: "password123",
    rememberMe: true,
  };

  assertEquals(login.rememberMe, true);
});

// ============================================
// RegistrationVM Interface Tests
// ============================================

Deno.test("RegistrationVM interface should have required properties", () => {
  const reg: RegistrationVM = {
    username: "testuser",
    email: "test@example.com",
    password: "password123",
  };

  assertEquals(reg.username, "testuser");
  assertEquals(reg.email, "test@example.com");
});

Deno.test("RegistrationVM interface should accept optional properties", () => {
  const reg: RegistrationVM = {
    username: "testuser",
    email: "test@example.com",
    password: "password123",
    confirmPassword: "password123",
    fullName: "Test User",
    tenantCode: "tenant-001",
  };

  assertEquals(reg.confirmPassword, "password123");
  assertEquals(reg.fullName, "Test User");
});

// ============================================
// EmailVM Interface Tests
// ============================================

Deno.test("EmailVM interface should have required properties", () => {
  const email: EmailVM = {
    to: ["recipient@example.com"],
    subject: "Test Subject",
    body: "Test Body",
  };

  assertEquals(email.to.length, 1);
  assertEquals(email.subject, "Test Subject");
});

Deno.test("EmailVM interface should accept optional properties", () => {
  const email: EmailVM = {
    to: ["recipient@example.com"],
    cc: ["cc@example.com"],
    bcc: ["bcc@example.com"],
    subject: "Test Subject",
    body: "Test Body",
    isHtml: true,
    attachments: ["file1.pdf", "file2.doc"],
  };

  assertEquals(email.cc?.length, 1);
  assertEquals(email.isHtml, true);
  assertEquals(email.attachments?.length, 2);
});

// ============================================
// NotificationVM Interface Tests
// ============================================

Deno.test("NotificationVM interface should have required properties", () => {
  const notification: NotificationVM = {
    userId: "user-123",
    title: "Test Notification",
    message: "This is a test message",
  };

  assertEquals(notification.userId, "user-123");
  assertEquals(notification.title, "Test Notification");
});

Deno.test("NotificationVM interface should accept optional properties", () => {
  const notification: NotificationVM = {
    userId: "user-123",
    title: "Test Notification",
    message: "This is a test message",
    type: "success",
    link: "/dashboard",
  };

  assertEquals(notification.type, "success");
  assertEquals(notification.link, "/dashboard");
});

// ============================================
// SqlViewModel Interface Tests
// ============================================

Deno.test("SqlViewModel interface should have required properties", () => {
  const sqlVM: SqlViewModel = {
    select: "*",
    count: false,
    wrapQuery: false,
    dataConn: "data",
    metaConn: "meta",
    skipXQuery: false,
  };

  assertEquals(sqlVM.select, "*");
  assertEquals(sqlVM.count, false);
});

Deno.test("SqlViewModel interface should accept optional query properties", () => {
  const sqlVM: SqlViewModel = {
    select: "id, name, email",
    where: "active = true",
    groupBy: "department",
    having: "count(*) > 5",
    orderBy: "name ASC",
    top: 10,
    skip: 20,
    count: true,
    wrapQuery: true,
    dataConn: "data",
    metaConn: "meta",
    skipXQuery: false,
  };

  assertEquals(sqlVM.top, 10);
  assertEquals(sqlVM.skip, 20);
  assertEquals(sqlVM.groupBy, "department");
});

// ============================================
// Feature Interface Tests
// ============================================

Deno.test("Feature interface should have required properties", () => {
  const feature: Feature = {
    id: "feature-001",
    isDevider: false,
    isGroup: false,
    isMenu: true,
    isPublic: false,
    startUp: false,
    isSystem: false,
    ignoreEncode: false,
    inheritParentFeature: false,
    deleteTemp: false,
    customNextCell: false,
    loadEntity: false,
    isLock: false,
    isFlow: false,
  };

  assertEquals(feature.id, "feature-001");
  assertEquals(feature.isMenu, true);
});

Deno.test("Feature interface should accept optional properties", () => {
  const feature: Feature = {
    id: "feature-001",
    name: "User Management",
    label: "User Management",
    parentId: "parent-001",
    order: 1,
    icon: "users-icon",
    isDevider: false,
    isGroup: false,
    isMenu: true,
    isPublic: false,
    startUp: false,
    isSystem: false,
    ignoreEncode: false,
    inheritParentFeature: false,
    deleteTemp: false,
    customNextCell: false,
    loadEntity: false,
    isLock: false,
    isFlow: false,
    active: true,
    description: "Manage users",
  };

  assertEquals(feature.name, "User Management");
  assertEquals(feature.description, "Manage users");
});

// ============================================
// Component Interface Tests
// ============================================

Deno.test("Component interface should have required properties", () => {
  const component: Component = {
    id: "component-001",
    canSearch: false,
    canCache: false,
    disabled: false,
    visibility: true,
    focus: false,
    canAdd: false,
    isPrivate: false,
    liteGrid: false,
    upperCase: false,
    showHotKey: false,
    responsive: false,
    isMultiple: false,
    frozen: false,
    editable: true,
    basicSearch: false,
    topEmpty: false,
    isCollapsible: false,
    showDatetimeField: false,
    showNull: false,
    addDate: false,
    filterEq: false,
    filterLocal: false,
    hideGrid: false,
    isDropDown: false,
    displayBadge: false,
    virtualScroll: false,
    isTab: false,
    showLabel: true,
    active: true,
    isRealtime: false,
    focusSearch: false,
    hasFilter: false,
    isSumary: false,
    isVertialTab: false,
  };

  assertEquals(component.id, "component-001");
  assertEquals(component.editable, true);
  assertEquals(component.showLabel, true);
  assertEquals(component.active, true);
});
