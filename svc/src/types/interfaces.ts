/**
 * TypeScript type definitions matching .NET CoreAPI models
 * Migration from CoreAPI project
 */

// ============================================
// User & Authentication Types
// ============================================

export interface User {
  id: string;
  code: string;
  email: string;
  password?: string;
  companyId: string;
  salt?: string;
  userName: string;
  fullName: string;
  address?: string;
  avatar?: string;
  ssn?: string;
  phoneNumber?: string;
  teamId?: string;
  partnerId?: string;
  active: boolean;
  insertedBy?: string;
  insertedDate?: Date;
  updatedBy?: string;
  updatedDate?: Date;
  dob?: Date;
  genderId?: string;
  ssnDate?: Date;
  taxCode?: string;
  seqKey?: number;
  typeId?: number;
  loginFailedCount?: number;
  lastFailedLogin?: Date;
  lastLogin?: Date;
  recover?: string;
  jointDate?: Date;
  roleIds?: string;
  roleIdsText?: string;
  departmentId?: string;
  identityCardDate?: Date;
  identityCard?: string;
  placeIssue?: string;
  nickName?: string;
  knowledge?: string;
  targetLocalCurr?: number;
  targetUSDCurr?: number;
  typeBonusId?: string;
  salaryAmount?: number;
  salaryCoefficient?: number;
  insuranceAmount?: number;
  typeContractId?: string;
  dependents?: number;
  accNumber?: string;
  accName?: string;
  bankId?: string;
  passEmail?: string;
  ip?: string;
  company?: Partner;
  isDepartment: boolean;
  isTeam: boolean;
  positionId?: string;
}

export interface UserLogin {
  id: string;
  userId: string;
  accessToken?: string;
  refreshToken?: string;
  accessTokenExp?: Date;
  refreshTokenExp?: Date;
  active: boolean;
  insertedBy?: string;
  insertedDate?: Date;
  updatedBy?: string;
  updatedDate?: Date;
  createdBy?: string;
  ipAddress?: string;
}

export interface Token {
  userId: string;
  userName: string;
  email: string;
  fullName: string;
  departmentId: string;
  code: string;
  positionId: string;
  address: string;
  avatar: string;
  accessToken: string;
  refreshToken: string;
  accessTokenExp: Date;
  refreshTokenExp: Date;
  vendor?: Partner;
  roleIds: string[];
  roleNames: string[];
  centerIds: string[];
  ssn: string;
  phoneNumber: string;
  teamId: string;
  partnerId: string;
  regionId: string;
  signinDate: Date;
  tenantCode: string;
  env: string;
  connKey: string;
}

export interface RefreshVM {
  accessToken: string;
  refreshToken: string;
}

export interface UpdatePasswordVM {
  newPassword: string;
  password: string;
}

// ============================================
// Partner (Vendor) Type
// ============================================

export interface Partner {
  id: string;
  serviceId?: number;
  typeId?: number;
  name: string;
  fullName?: string;
  debitName?: string;
  address?: string;
  phoneNumber?: string;
  mail?: string;
  email?: string;
  active: boolean;
  insertedDate?: Date;
  insertedBy?: string;
  updatedDate?: Date;
  updatedBy?: string;
  code?: string;
  taxCode?: string;
  createdBy?: string;
  groupId?: string;
  genderId?: string;
  issuedDate?: Date;
  note?: string;
  genderContactId?: string;
  contactName?: string;
  contactEmail?: string;
  contactPhoneNumber?: string;
  debitDay?: number;
  debitAmount?: number;
  creditLimit?: number;
  debitAccountId?: string;
  isPublic: boolean;
  web?: string;
  seqKey?: number;
  saleId?: string;
  attachment?: string;
  sourseId?: string;
  raitingId?: string;
  dob?: Date;
  picId?: string;
  customerTypeId?: string;
  companyName?: string;
  residenceTypeId?: string;
  description?: string;
  companyNameInv?: string;
  emailInv?: string;
  assignmentDebitId?: string;
  assignmentInvId?: string;
  accNumber?: string;
  accName?: string;
  bankId?: string;
  swiftCode?: string;
  idCode?: string;
  password?: string;
  industry?: string;
  po?: string;
  rushMonth?: number;
  rating?: string;
  poDate?: Date;
  minProfitMonth?: number;
  warning?: string;
  actionId?: number;
  regularShippingFrom?: string;
  conditionId?: string;
  distributeInformationIds?: string;
  distributeInformationIdsText?: string;
  zipCode?: string;
  trackingURL?: string;
  isNoDebt: boolean;
  partnerTypeIds?: string;
  additionTypeIds?: string;
  partnerTypeIdsText?: string;
  additionTypeIdsText?: string;
  formatChat?: string;
  addressInv?: string;
  assignId?: string;
  history?: string;
  birthday?: Date;
  logo?: string;
  header?: string;
  footer?: string;
  icon?: string;
}

// ============================================
// Patch/CRUD Operation Types
// ============================================

export interface PatchVM {
  byPassPerm: boolean;
  featureId?: string;
  comId?: string;
  newId?: string;
  queueName?: string;
  cacheName?: string;
  name?: string;
  table?: string;
  delete: DeleteItem[];
  tenantCode?: string;
  env?: string;
  metaConn: string;
  dataConn: string;
  changes: PatchDetail[];
  detail: PatchVM[][];
  ids: string[];
  index?: number;
  update: boolean;
  realTime: boolean;
  reasonOfChange?: string;
}

export interface DeleteItem {
  table: string;
  ids: string[];
}

export interface CheckDeleteItem {
  params: string;
  entityIds: string[];
  comId: string;
}

export interface PatchDetail {
  field: string;
  label?: string;
  oldVal?: string;
  value?: string;
  historyValue?: string;
}

// ============================================
// Metadata Types (Feature, Component, Policy)
// ============================================

export interface Feature {
  id: string;
  name?: string;
  label?: string;
  parentId?: string;
  order?: number;
  className?: string;
  style?: string;
  styleSheet?: string;
  script?: string;
  events?: string;
  icon?: string;
  isDevider: boolean;
  isGroup: boolean;
  isMenu: boolean;
  isPublic: boolean;
  startUp: boolean;
  viewClass?: string;
  entityId?: string;
  description?: string;
  active?: boolean;
  insertedDate?: Date;
  insertedBy?: string;
  updatedDate?: Date;
  updatedBy?: string;
  isSystem: boolean;
  ignoreEncode: boolean;
  inheritParentFeature: boolean;
  deleteTemp: boolean;
  customNextCell: boolean;
  loadEntity: boolean;
  isLock: boolean;
  isFlow: boolean;
  codeId?: string;
  componentGroup?: Component[];
  gridPolicies?: Component[];
  components?: Component[];
  featurePolicies?: FeaturePolicy[];
  userSettings?: UserSetting[];
}

export interface Component {
  id: string;
  fieldName?: string;
  order?: number;
  componentType?: string;
  componentGroupId?: string;
  formatData?: string;
  plainText?: string;
  column?: number;
  rowSpan?: number;
  offset?: number;
  row?: number;
  canSearch: boolean;
  canCache: boolean;
  precision?: number;
  groupBy?: string;
  groupFormat?: string;
  label?: string;
  showLabel: boolean;
  icon?: string;
  className?: string;
  style?: string;
  childStyle?: string;
  hotKey?: string;
  refClass?: string;
  events?: string;
  disabled: boolean;
  visibility: boolean;
  validation?: string;
  focus: boolean;
  width?: string;
  populateField?: string;
  groupEvent?: string;
  xsCol?: number;
  smCol?: number;
  lgCol?: number;
  xlCol?: number;
  xxlCol?: number;
  defaultVal?: string;
  dateTimeField?: string;
  active: boolean;
  insertedDate?: Date;
  insertedBy?: string;
  updatedDate?: Date;
  updatedBy?: string;
  canAdd: boolean;
  isPrivate: boolean;
  monthCount?: number;
  query?: string;
  isRealtime: boolean;
  refName?: string;
  topEmpty: boolean;
  isCollapsible: boolean;
  template?: string;
  preQuery?: string;
  disabledExp?: string;
  focusSearch: boolean;
  isSumary: boolean;
  formatSumaryField?: string;
  orderBySumary?: string;
  showHotKey: boolean;
  defaultAddStart?: number;
  defaultAddEnd?: number;
  upperCase: boolean;
  migration?: string;
  listClass?: string;
  excelFieldName?: string;
  liteGrid: boolean;
  showDatetimeField: boolean;
  showNull: boolean;
  addDate: boolean;
  filterEq: boolean;
  headerHeight?: number;
  bodyItemHeight?: number;
  footerHeight?: number;
  scrollHeight?: number;
  scriptValidation?: string;
  filterLocal: boolean;
  hideGrid: boolean;
  groupReferenceId?: string;
  groupReferenceName?: string;
  groupName?: string;
  shortDesc?: string;
  description?: string;
  featureId?: string;
  componentId?: string;
  textAlign?: string;
  hasFilter: boolean;
  frozen: boolean;
  filterTemplate?: string;
  editable: boolean;
  formatExcell?: string;
  databaseName?: string;
  summary?: string;
  summaryColSpan?: number;
  basicSearch: boolean;
  showExp?: string;
  minWidth?: string;
  maxWidth?: string;
  tenantCode?: string;
  orderBy?: string;
  parentId?: string;
  lang?: string;
  name?: string;
  isTab: boolean;
  tabGroup?: string;
  isVertialTab: boolean;
  responsive: boolean;
  outerColumn?: number;
  xsOuterColumn?: number;
  smOuterColumn?: number;
  lgOuterColumn?: number;
  xlOuterColumn?: number;
  xxlOuterColumn?: number;
  badgeMonth?: number;
  isDropDown: boolean;
  html?: string;
  css?: string;
  javascript?: string;
  created_by?: string;
  entityId?: string;
  displayBadge: boolean;
  isMultiple: boolean;
  addRowExp?: string;
  groupTypeId?: number;
  virtualScroll: boolean;
  entityName?: string;
  componentDefaultValueId?: string;
  tableName?: string;
  reportTypeId?: number;
  index?: number;
  codeId?: string;
  excelUrl?: string;
  children?: Component[];
  components?: Component[];
  parent?: Component;
}

export interface FeaturePolicy {
  id: string;
  featureId: string;
  roleId: string;
  canRead: boolean;
  canReadAll: boolean;
  canWrite: boolean;
  canWriteAll: boolean;
  canDelete: boolean;
  canDeleteAll: boolean;
  canCopy: boolean;
  canCopyAll: boolean;
  canDeactivate: boolean;
  canDeactivateAll: boolean;
  recordId?: string;
  userId?: string;
  tableName?: string;
  active: boolean;
  insertedDate?: Date;
  insertedBy?: string;
  updatedDate?: Date;
  updatedBy?: string;
}

export interface UserSetting {
  id: string;
  userId: string;
  featureId?: string;
  settingKey: string;
  settingValue?: string;
  active: boolean;
  insertedDate?: Date;
  insertedBy?: string;
  updatedDate?: Date;
  updatedBy?: string;
}

// ============================================
// SQL Query Types
// ============================================

export interface SqlViewModel {
  svcId?: string;
  feature?: string;
  comId?: string;
  action?: string;
  params?: string;
  table?: string;
  queueName?: string;
  id?: string[];
  component?: Component;
  annonymousTenant?: string;
  annonymousEnv?: string;
  jsScript?: string;
  select: string;
  where?: string;
  whereParams?: string;
  groupBy?: string;
  having?: string;
  orderBy?: string;
  paging?: string;
  top?: number;
  skip?: number;
  count: boolean;
  wrapQuery: boolean;
  fieldName?: string[];
  skipXQuery: boolean;
  dataConn: string;
  metaConn: string;
  format?: string;
}

export interface SqlQuery {
  sql: string;
  total?: string;
  delete?: string;
  update?: string;
}

export interface Gos {
  tableName: string;
  ids: string[];
}

export interface WhereParamVM {
  fieldName: string;
  value: string;
}

// ============================================
// Query Result Types
// ============================================

export interface QueryResult {
  result: unknown;
  query: string;
  dataConn: string;
  xQuery?: string;
  metaConn: string;
}

export interface SqlResult {
  message: string;
  status: number;
  updatedItem?: Record<string, unknown>[];
  detail?: DetailData[];
  data?: Record<string, unknown>[];
}

export interface SqlComResult {
  count?: number;
  value?: Record<string, unknown>[];
}

export interface CheckDeleteResult {
  message: string;
  status: number;
}

export interface DetailData {
  index: number;
  table: string;
  ids: string[];
  comId?: string;
  data?: Record<string, unknown>[];
}

// ============================================
// User Context (Request Context)
// ============================================

export interface UserContext {
  userId: string;
  tenantCode: string;
  env: string;
  connKey: string;
  roles: string[];
  roleNames: string[];
  userName: string;
  email: string;
  fullName: string;
  departmentId?: string;
  positionId?: string;
  teamId?: string;
  partnerId?: string;
  centerIds?: string[];
  isAdmin?: boolean;
}

// ============================================
// Additional View Models
// ============================================

export interface LoginVM {
  username: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegistrationVM {
  username: string;
  email: string;
  password: string;
  confirmPassword?: string;
  fullName?: string;
  tenantCode?: string;
}

export interface PdfVM {
  html: string;
  type?: string;
  fileName?: string;
}

export interface MoveHBLVM {
  shipmentId: string;
  shipmentDetailId: string[];
}

export interface FeeVM {
  shipmentInvoiceId: string;
  shipmentInvoiceDetailId: string[];
}

export interface EntityVM {
  entityId: string;
}

export interface ServiceVM {
  name: string;
}

export interface StreamingRequest {
  prompt: string;
  maxTokens: number;
  temperature: number;
}

export interface EmailVM {
  to: string[];
  cc?: string[];
  bcc?: string[];
  subject: string;
  body: string;
  isHtml?: boolean;
  attachments?: string[];
}

export interface NotificationVM {
  userId: string;
  title: string;
  message: string;
  type?: 'info' | 'success' | 'warning' | 'error';
  link?: string;
}

export interface ReportVM {
  reportId: string;
  parameters?: Record<string, unknown>;
  format?: 'pdf' | 'excel' | 'csv';
}

export interface Server {
  id: string;
  name: string;
  host: string;
  port: number;
  username?: string;
  password?: string;
  database: string;
  type: 'mssql' | 'postgresql' | 'mysql';
  active: boolean;
}

