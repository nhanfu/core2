export type IdValue = string | number;
export interface FeaturePolicy {
    Id?: string;
    FeatureId?: string;
    RoleId?: string;
    RecordId?: string;
    UserId?: string;
    TableName?: string;
    CanRead?: boolean;
    CanReadAll?: boolean;
    CanWrite?: boolean;
    CanWriteAll?: boolean;
    CanDelete?: boolean;
    CanDeleteAll?: boolean;
    CanCopy?: boolean;
    CanCopyAll?: boolean;
    CanDeactivate?: boolean;
    CanDeactivateAll?: boolean;
    CanExport?: boolean;
    Active?: boolean;
}
export interface Component {
    Id?: string;
    FieldName?: string;
    ComponentType?: string;
    ComponentGroupId?: string;
    Label?: string;
    ShowLabel?: boolean;
    Icon?: string;
    ClassName?: string;
    Style?: string;
    ChildStyle?: string;
    HotKey?: string;
    RefName?: string;
    RefClass?: string;
    Query?: string;
    PreQuery?: string;
    Events?: string;
    DisabledExp?: string;
    ShowExp?: string;
    Validation?: string;
    EntityName?: string;
    TableName?: string;
    CanRead?: boolean;
    CanReadAll?: boolean;
    CanWrite?: boolean;
    CanWriteAll?: boolean;
    CanDelete?: boolean;
    CanDeleteAll?: boolean;
    CanDeactivate?: boolean;
    CanDeactivateAll?: boolean;
    CanExport?: boolean;
    Components?: Component[];
    ComponentGroup?: Component[];
    Parent?: Component;
    ParentId?: string;
    GroupBy?: string;
    GroupFormat?: string;
    FormatData?: string;
    Template?: string;
    OrderBy?: string;
    IsRealtime?: boolean;
    IsTab?: boolean;
}
export interface Feature {
    Id?: string;
    Name?: string;
    Label?: string;
    ParentId?: string;
    Order?: number;
    ClassName?: string;
    Style?: string;
    StyleSheet?: string;
    Script?: string;
    Events?: string;
    Icon?: string;
    IsMenu?: boolean;
    IsPublic?: boolean;
    StartUp?: boolean;
    ViewClass?: string;
    EntityId?: string;
    Description?: string;
    Active?: boolean;
    IsSystem?: boolean;
    IgnoreEncode?: boolean;
    InheritParentFeature?: boolean;
    DeleteTemp?: boolean;
    CustomNextCell?: boolean;
    LoadEntity?: boolean;
    IsLock?: boolean;
    CodeId?: string;
    ComponentGroup?: Component[];
    GridPolicies?: Component[];
    Components?: Component[];
    FeaturePolicies?: FeaturePolicy[];
    UserSettings?: unknown[];
}
export interface PatchDetail {
    Field: string;
    Label?: string;
    OldVal?: string | null;
    Value?: string | null;
    HistoryValue?: string | null;
}
export interface DeleteItem {
    Table: string;
    Ids: string[];
}
export interface PatchVM {
    ByPassPerm?: boolean;
    FeatureId?: string;
    ComId?: string;
    NewId?: string;
    QueueName?: string;
    CacheName?: string;
    Name?: string;
    Table?: string;
    Delete?: DeleteItem[];
    TenantCode?: string;
    Env?: string;
    MetaConn?: string;
    DataConn?: string;
    CachedDataConn?: string;
    CachedMetaConn?: string;
    Changes?: PatchDetail[];
    Detail?: PatchVM[][];
    Ids?: string[];
    Index?: number;
    Update?: boolean;
    RealTime?: boolean;
    ReasonOfChange?: string;
}
export interface SqlQuery {
    sql?: string;
    total?: string;
    delete?: string;
    update?: string;
}
export interface SqlViewModel {
    SvcId?: string;
    Feature?: string;
    ComId?: string;
    Action?: string;
    Params?: string | null;
    Table?: string;
    QueueName?: string;
    Id?: string[];
    Component?: Component;
    AnnonymousTenant?: string;
    AnnonymousEnv?: string;
    JsScript?: string;
    Select?: string;
    Where?: string;
    WhereParams?: string;
    GroupBy?: string;
    Having?: string;
    OrderBy?: string;
    Paging?: string;
    Top?: number;
    Skip?: number;
    Count?: boolean;
    WrapQuery?: boolean;
    FieldName?: string[];
    SkipXQuery?: boolean;
    DataConn?: string;
    MetaConn?: string;
    CachedDataConn?: string;
    CachedMetaConn?: string;
    Format?: string;
}
export interface QueryRequest {
    feature?: Feature;
    component?: Component;
    params?: Record<string, unknown> | null;
    tenant?: string;
    env?: string;
    metaConn?: string;
    dataConn?: string;
}
export interface QueryResult {
    data: Array<Record<string, unknown>>[];
    total?: Array<Record<string, unknown>>[];
}
export interface ActionRequest {
    feature?: Feature;
    component?: Component;
    actionName: string;
    args?: unknown[];
}
export interface RuntimeContext {
    tenant?: string;
    env?: string;
    roleIds?: string[];
    userId?: string;
    metaConn?: string;
    dataConn?: string;
    variables?: Record<string, unknown>;
}
export interface DataAdapter {
    query(sql: string, options?: {
        params?: Record<string, unknown>;
        conn?: string;
    }): Promise<Array<Record<string, unknown>>>;
    execute(sql: string, options?: {
        params?: Record<string, unknown>;
        conn?: string;
    }): Promise<number>;
}
export interface MetadataStore {
    getFeature(name: string, context?: RuntimeContext): Promise<Feature | null>;
    getPublicFeature?(name: string, context?: RuntimeContext): Promise<Feature | null>;
}
export interface ScriptRunner {
    evaluate<T = unknown>(expression: string, scope?: Record<string, unknown>): T | null;
    invoke<T = unknown>(expression: string, scope?: Record<string, unknown>, args?: unknown[]): T | null;
}
//# sourceMappingURL=types.d.ts.map