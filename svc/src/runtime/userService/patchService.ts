import type {
  Approvement,
  ApprovalConfig,
  CheckDeleteItem,
  Component,
  DetailData,
  Feature,
  FeaturePolicy,
  PatchDetail,
  PatchVM,
  SqlResult,
  SqlViewModel,
  TaskNotification,
  TableName,
} from "../types.js";
import type { UserServiceContext } from "./types.js";
import type { FeatureService } from "./featureService.js";
import { parseJsonSafe } from "../utils.js";
import {
  DEFAULT_CACHE_TTL_MS,
  combineStrings,
  escapeValue,
  formatEntity,
  getChange,
  getChangeValue,
  getRowValue,
  isEmpty,
  isOwner,
  normalizeIdValue,
  setChangeValue,
  toIso,
  toStringSafe,
  distinctBy,
} from "./utils.js";

export class PatchService {
  constructor(private context: UserServiceContext, private featureService: FeatureService) {}

  addDefaultFields(changes: PatchDetail[], defaults: PatchDetail[]): void {
    defaults.forEach((field) => {
      const existing = changes.find((change) => change.Field === field.Field);
      if (existing) {
        existing.Value = field.Value;
      } else {
        changes.push(field);
      }
    });
  }

  async savePatch(vm: PatchVM): Promise<number> {
    await this.resolvePatchConnections(vm);
    const canWrite = await this.hasWritePermission(vm);
    if (!canWrite) throw new Error(`Unauthorized to write on "${vm.Table}"`);
    const sql = this.context.sqlBuilder.buildCreateOrUpdate(vm);
    if (!sql) return 0;
    return this.context.execute(sql);
  }

  async updatePatch(vm: PatchVM): Promise<number> {
    await this.resolvePatchConnections(vm);
    const canWrite = await this.hasWritePermission(vm);
    if (!canWrite) throw new Error(`Unauthorized to write on "${vm.Table}"`);
    const sql = this.context.sqlBuilder.buildUpdate(vm);
    if (!sql) return 0;
    return this.context.execute(sql);
  }

  async hardDelete(vm: PatchVM): Promise<boolean> {
    if (!vm.ComId) {
      const sql = (vm.Delete || []).map((item) => `delete from [${item.Table}] where Id in (${combineStrings(item.Ids, this.context.sqlDialect)})`).join(";");
      if (sql) await this.context.execute(sql);
      return true;
    }
    const componentRows = await this.context.query(`select top 1 * from [Component] where Id = ${escapeValue(vm.ComId, this.context.sqlDialect)}`);
    const com = componentRows[0] as Component | undefined;
    if (!com || !com.Query) return false;
    const data = parseJsonSafe<{ update?: string }>(com.Query);
    const dictionary: Record<string, unknown> = {
      EntityIds: combineStrings(vm.Delete?.flatMap((item) => item.Ids) || [], this.context.sqlDialect),
      NewId: vm.NewId,
    };
    if (data?.update) {
      const updateSql = formatEntity(data.update, dictionary);
      const deleteSql = (vm.Delete || []).map((item) => `delete from [${item.Table}] where Id in (${combineStrings(item.Ids, this.context.sqlDialect)})`).join(";");
      await this.context.execute([updateSql, deleteSql].filter(Boolean).join(";"));
    } else {
      const deleteSql = (vm.Delete || []).map((item) => `delete from [${item.Table}] where Id in (${combineStrings(item.Ids, this.context.sqlDialect)})`).join(";");
      await this.context.execute(deleteSql);
    }
    if (vm.Table === "Component" || vm.Table === "FeaturePolicy") {
      const featureId = toStringSafe(getRowValue(com as Record<string, unknown>, "FeatureId"));
      if (featureId) {
        const featureRows = await this.context.query(`select * from [Feature] where Id = ${escapeValue(featureId, this.context.sqlDialect)}`);
        const feature = featureRows[0] as Feature | undefined;
        if (feature?.Name) await this.featureService.publishFeatureByName(feature.Name);
      }
    }
    return true;
  }

  async sendEntity(vm: PatchVM): Promise<SqlResult> {
    const name = vm.Name || vm.Table;
    const id = getChangeValue(vm, "Id") || "";
    const voucherTypeId = getChangeValue(vm, "VoucherTypeId");
    const userReceiverId = getChangeValue(vm, "UserReceiverId");
    const groupReceiverId = getChangeValue(vm, "GroupReceiverId");
    const receiverIds = getChangeValue(vm, "ReceiverIds");
    const insertedBy = getChangeValue(vm, "InsertedBy");
    const featureName = getChangeValue(vm, "FeatureName");
    const featureName2 = getChangeValue(vm, "FeatureName2");
    const shipmentFeature = getChangeValue(vm, "ShipmentFeature");
    const featureName3 = getChangeValue(vm, "FeatureName3");
    const title = getChangeValue(vm, "FormatChat");
    const noApproved = getChangeValue(vm, "NoApproved");

    if (noApproved === "1") {
      setChangeValue(vm, "StatusId", "3");
      if (getChange(vm, "AutoProgressId")) setChangeValue(vm, "AutoProgressId", "2");
      const rs1 = await this.savePatch2(vm);
      const approval: Approvement = {
        Id: crypto.randomUUID(),
        Approved: true,
        CurrentLevel: 1,
        ReasonOfChange: vm.ReasonOfChange,
        NextLevel: 1,
        Name: name,
        RecordId: id,
        StatusId: 3,
        UserApproveId: this.context.UserId,
        ApprovedBy: this.context.UserId,
        ApprovedDate: toIso(this.context.now()),
        InsertedBy: this.context.UserId,
        InsertedDate: toIso(this.context.now()),
      };
      await this.savePatch({ Table: "Approvement", Changes: Object.entries(approval).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
      const receiverList = receiverIds ? receiverIds.split(",") : [];
      if (!isEmpty(receiverList)) {
        const tasks = receiverList.map((receiver) => ({
          Id: crypto.randomUUID(),
          VoucherTypeId: Number(voucherTypeId || 0),
          EntityId: name,
          Avatar: this.context.Avatar,
          FeatureName: featureName,
          FeatureName2: featureName2,
          FeatureName3: featureName3,
          Title: title || "",
          Title2: `${this.context.FullName} has sent you an approval request.`,
          Icon: "fal fa-smile",
          Description: title || "",
          InsertedBy: this.context.UserId,
          RecordId: id,
          InsertedDate: toIso(this.context.now()),
          Active: true,
          AssignedId: receiver,
        }));
        await Promise.all(tasks.map((task) => this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) })));
      }
      return { status: 200, message: "Your data has been approved.", updatedItem: rs1.updatedItem };
    }

    if (getChange(vm, "AutoProgressId")) setChangeValue(vm, "AutoProgressId", "2");
    if ((userReceiverId && userReceiverId !== "") || (groupReceiverId && groupReceiverId !== "")) {
      const rs = await this.savePatch2(vm);
      if (userReceiverId && userReceiverId !== "") {
        const baseNotification = {
          Id: crypto.randomUUID(),
          VoucherTypeId: Number(voucherTypeId || 0),
          EntityId: name,
          Avatar: this.context.Avatar,
          Title: title || "",
          Icon: "fal fa-smile",
          Description: title || "",
          RecordId: id,
          Active: true,
          AssignedId: userReceiverId,
          InsertedDate: toIso(this.context.now()),
        };
        const notification: TaskNotification = vm.Table === "ShipmentSI"
          ? {
              ...baseNotification,
              FeatureName: featureName,
              FeatureName2: shipmentFeature,
              FeatureName3: shipmentFeature ? shipmentFeature.replace("-editor", "") : null,
              Title2: `${this.context.FullName} has sent you an approval SI.`,
              InsertedBy: insertedBy || this.context.UserId,
            }
          : {
              ...baseNotification,
              FeatureName: featureName,
              FeatureName2: featureName2,
              FeatureName3: featureName3,
              Title2: `${this.context.FullName} has sent you an approval request.`,
              InsertedBy: this.context.UserId,
            };
        await this.savePatch({ Table: "TaskNotification", Changes: Object.entries(notification).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
      }
      if (groupReceiverId && groupReceiverId !== "") {
        const users = await this.context.query(`SELECT * FROM [User] where TeamId = ${escapeValue(groupReceiverId, this.context.sqlDialect)}`);
        const tasks = users.map((user) => ({
          Id: crypto.randomUUID(),
          VoucherTypeId: Number(voucherTypeId || 0),
          EntityId: name,
          Avatar: this.context.Avatar,
          FeatureName: featureName,
          FeatureName2: featureName2,
          FeatureName3: featureName3,
          Title: title || "",
          Title2: `${this.context.FullName} has sent you an approval request.`,
          Icon: "fal fa-smile",
          Description: title || "",
          InsertedBy: this.context.UserId,
          RecordId: id,
          InsertedDate: toIso(this.context.now()),
          Active: true,
          AssignedId: toStringSafe(getRowValue(user, "Id")),
        }));
        await Promise.all(tasks.map((task) => this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) })));
      }
      return { status: 200, updatedItem: rs.updatedItem };
    }

    const approvalConfig = await this.context.query(`SELECT * FROM ApprovalConfig where VoucherTypeId = ${escapeValue(voucherTypeId || "", this.context.sqlDialect)} and ParentId is not null order by Level asc`);
    if (isEmpty(approvalConfig)) {
      return { status: 500 };
    }
    const configRows = approvalConfig as ApprovalConfig[];
    const matchApprovalConfig = configRows.find((row) => row.Level === 1);
    if (!matchApprovalConfig) {
      return { status: 500 };
    }
    let userApproved = matchApprovalConfig.UserIds ? matchApprovalConfig.UserIds.split(",") : [];
    if (matchApprovalConfig.IsTeam) {
      const teamUsers = await this.context.query(`SELECT * FROM [USER] where [TeamId] = ${escapeValue(this.context.GroupId || "", this.context.sqlDialect)} and IsTeam = 1`);
      userApproved = teamUsers.map((user) => toStringSafe(getRowValue(user, "Id")));
    }
    if (matchApprovalConfig.IsDepartment) {
      const deptUsers = await this.context.query(`SELECT * FROM [USER] where [DepartmentId] = ${escapeValue(this.context.DepartmentId || "", this.context.sqlDialect)} and IsDepartment = 1`);
      userApproved = deptUsers.map((user) => toStringSafe(getRowValue(user, "Id")));
    }
    if (isEmpty(userApproved)) {
      return { status: 500, message: "Please config user approved" };
    }
    const tasks = userApproved.map((receiver) => ({
      Id: crypto.randomUUID(),
      VoucherTypeId: Number(voucherTypeId || 0),
      EntityId: name,
      Avatar: this.context.Avatar,
      FeatureName: featureName,
      FeatureName2: featureName2,
      FeatureName3: featureName3,
      Title: title || "",
      Title2: `${this.context.FullName} has sent you an approval request.`,
      Icon: "fal fa-smile",
      Description: title || "",
      InsertedBy: this.context.UserId,
      RecordId: id,
      InsertedDate: toIso(this.context.now()),
      Active: true,
      AssignedId: receiver,
    }));
    setChangeValue(vm, "UserApprovedIds", userApproved.join(","));
    const rs2 = await this.savePatch2(vm);
    await Promise.all(tasks.map((task) => this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) })));
    return { status: 200, updatedItem: rs2.updatedItem };
  }

  async approvedEntity(vm: PatchVM): Promise<SqlResult> {
    vm.Detail = [];
    vm.Delete = [];
    const now = this.context.now();
    const name = vm.Name || vm.Table;
    const id = getChangeValue(vm, "Id") || "";
    const userReceiverId = getChangeValue(vm, "UserReceiverId");
    const groupReceiverId = getChangeValue(vm, "GroupReceiverId");
    const insertedBy = getChangeValue(vm, "InsertedBy");
    const userCreateId = getChangeValue(vm, "UserCreateId");
    const voucherTypeId = getChangeValue(vm, "VoucherTypeId");
    const title = getChangeValue(vm, "FormatChat");
    const featureName = getChangeValue(vm, "FeatureName");

    if ((userReceiverId && userReceiverId !== "") || (groupReceiverId && groupReceiverId !== "")) {
      if (getChange(vm, "AutoProgressId")) setChangeValue(vm, "AutoProgressId", "2");
      const rs = await this.savePatch2(vm);
      if (userReceiverId && userReceiverId !== "" && this.context.UserId === userReceiverId) {
        const approval: Approvement = {
          Id: crypto.randomUUID(),
          Approved: true,
          CurrentLevel: 1,
          NextLevel: 1,
          Name: name,
          RecordId: id,
          StatusId: 3,
          UserApproveId: this.context.UserId,
          ApprovedBy: this.context.UserId,
          ApprovedDate: toIso(now),
          InsertedBy: this.context.UserId,
          InsertedDate: toIso(now),
        };
        await this.savePatch({ Table: "Approvement", Changes: Object.entries(approval).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
        const task: TaskNotification = {
          Id: crypto.randomUUID(),
          VoucherTypeId: Number(voucherTypeId || 0),
          EntityId: name,
          Avatar: this.context.Avatar,
          FeatureName: featureName,
          Title: title || "",
          Title2: `${this.context.FullName} has approved your request.`,
          Icon: "fal fa-smile",
          Description: title || "",
          InsertedBy: this.context.UserId,
          RecordId: id,
          InsertedDate: toIso(now),
          Active: true,
          AssignedId: userCreateId || insertedBy || "",
        };
        await this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
      }
      if (groupReceiverId && groupReceiverId !== "") {
        const users = await this.context.query(`SELECT * FROM [User] where TeamId = ${escapeValue(groupReceiverId, this.context.sqlDialect)}`);
        const ids = users.map((user) => toStringSafe(getRowValue(user, "Id")));
        if (!ids.includes(this.context.UserId || "")) {
          return { status: 500, message: "You do not have permission to browse the data" };
        }
        const approval: Approvement = {
          Id: crypto.randomUUID(),
          Approved: true,
          CurrentLevel: 1,
          NextLevel: 1,
          Name: name,
          RecordId: id,
          StatusId: 3,
          UserApproveId: this.context.UserId,
          ApprovedBy: this.context.UserId,
          ApprovedDate: toIso(now),
          InsertedBy: this.context.UserId,
          InsertedDate: toIso(now),
        };
        await this.savePatch({ Table: "Approvement", Changes: Object.entries(approval).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
        const task: TaskNotification = {
          Id: crypto.randomUUID(),
          VoucherTypeId: Number(voucherTypeId || 0),
          EntityId: name,
          Avatar: this.context.Avatar,
          FeatureName: featureName,
          Title: title || "",
          Title2: `${this.context.FullName} has approved your request.`,
          Icon: "fal fa-smile",
          Description: title || "",
          InsertedBy: this.context.UserId,
          RecordId: id,
          InsertedDate: toIso(now),
          Active: true,
          AssignedId: userCreateId || insertedBy || "",
        };
        await this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
      }
      return { status: 200, updatedItem: rs.updatedItem };
    }

    const approvalConfigRows = await this.context.query(`SELECT * FROM ApprovalConfig where VoucherTypeId = ${escapeValue(voucherTypeId || "", this.context.sqlDialect)} and ParentId is not null  order by Level asc`);
    if (isEmpty(approvalConfigRows)) {
      return { status: 500, message: "Please config approved" };
    }
    const approvalConfig = approvalConfigRows as ApprovalConfig[];
    const approvements = await this.context.query(`SELECT * FROM Approvement where Name = ${escapeValue(name || "", this.context.sqlDialect)} and RecordId = ${escapeValue(id, this.context.sqlDialect)} and Approved = 1 and IsEnd = 0 order by CurrentLevel desc`);
    const matchApprovalConfig = approvalConfig.find((row) => row.Level === 1);
    if (!matchApprovalConfig) {
      return { status: 500, message: "Please config approved" };
    }
    const maxLevel = Math.max(...approvalConfig.map((row) => row.Level || 0));
    const currentLevel = approvements.length === 0 ? 1 : Number(getRowValue(approvements[0], "NextLevel")) || 1;
    if ((approvements.length === 0 && maxLevel === 1) || approvements.some((row) => Number(getRowValue(row, "CurrentLevel")) === maxLevel)) {
      const nextConfig = approvalConfig.find((row) => row.Level === maxLevel);
      if (!nextConfig) return { status: 500, message: "Please config approved" };
      let userApproved = nextConfig.UserIds ? nextConfig.UserIds.split(",") : [];
      if (nextConfig.IsTeam) {
        const users = await this.context.query(`SELECT * FROM [USER] where [TeamId] = ${escapeValue(this.context.GroupId || "", this.context.sqlDialect)} and IsTeam = 1`);
        userApproved = users.map((user) => toStringSafe(getRowValue(user, "Id")));
      }
      if (nextConfig.IsDepartment) {
        const users = await this.context.query(`SELECT * FROM [USER] where [DepartmentId] = ${escapeValue(this.context.DepartmentId || "", this.context.sqlDialect)} and IsDepartment = 1`);
        userApproved = users.map((user) => toStringSafe(getRowValue(user, "Id")));
      }
      if (isEmpty(userApproved)) return { status: 500, message: "Please config user approved" };
      if (!userApproved.includes(this.context.UserId || "")) {
        return { status: 500, message: "You do not have permission to browse the data" };
      }
      setChangeValue(vm, "StatusId", "3");
      if (getChange(vm, "AutoProgressId")) setChangeValue(vm, "AutoProgressId", "2");
      const rs1 = await this.savePatch2(vm);
      const approval: Approvement = {
        Id: crypto.randomUUID(),
        Approved: true,
        CurrentLevel: 1,
        ReasonOfChange: vm.ReasonOfChange,
        NextLevel: 1,
        Name: name,
        RecordId: id,
        StatusId: 3,
        UserApproveId: this.context.UserId,
        ApprovedBy: this.context.UserId,
        ApprovedDate: toIso(now),
        InsertedBy: this.context.UserId,
        InsertedDate: toIso(now),
      };
      await this.savePatch({ Table: "Approvement", Changes: Object.entries(approval).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
      const tasks = userApproved.map((receiver) => ({
        Id: crypto.randomUUID(),
        VoucherTypeId: Number(voucherTypeId || 0),
        EntityId: name,
        Avatar: this.context.Avatar,
        FeatureName: featureName,
        Title: title || "",
        Title2: `${this.context.FullName} has approved your request.`,
        Icon: "fal fa-smile",
        Description: title || "",
        InsertedBy: this.context.UserId,
        RecordId: id,
        InsertedDate: toIso(now),
        Active: true,
        AssignedId: userCreateId || insertedBy || "",
      }));
      await Promise.all(tasks.map((task) => this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) })));
      return { status: 200, message: "Your data has been approved.", updatedItem: rs1.updatedItem };
    }

    const nextConfig = approvalConfig.find((row) => row.Level === currentLevel);
    if (!nextConfig) return { status: 500, message: "Please config approved" };
    let userApproved = nextConfig.UserIds ? nextConfig.UserIds.split(",") : [];
    if (nextConfig.IsTeam) {
      const users = await this.context.query(`SELECT * FROM [USER] where [TeamId] = ${escapeValue(this.context.GroupId || "", this.context.sqlDialect)} and IsTeam = 1`);
      userApproved = users.map((user) => toStringSafe(getRowValue(user, "Id")));
    }
    if (nextConfig.IsDepartment) {
      const users = await this.context.query(`SELECT * FROM [USER] where [DepartmentId] = ${escapeValue(this.context.DepartmentId || "", this.context.sqlDialect)} and IsDepartment = 1`);
      userApproved = users.map((user) => toStringSafe(getRowValue(user, "Id")));
    }
    if (isEmpty(userApproved)) return { status: 500, message: "Please config user approved" };
    if (!userApproved.includes(this.context.UserId || "")) {
      return { status: 500, message: "You do not have permission to browse the data" };
    }
    const approval: Approvement = {
      Id: crypto.randomUUID(),
      Approved: true,
      CurrentLevel: currentLevel,
      ReasonOfChange: vm.ReasonOfChange,
      NextLevel: currentLevel + 1,
      Name: name,
      RecordId: id,
      StatusId: 3,
      UserApproveId: this.context.UserId,
      ApprovedBy: this.context.UserId,
      ApprovedDate: toIso(now),
      InsertedBy: this.context.UserId,
      InsertedDate: toIso(now),
    };
    await this.savePatch({ Table: "Approvement", Changes: Object.entries(approval).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
    const nextLevelConfig = approvalConfig.find((row) => row.Level === approval.NextLevel);
    if (!nextLevelConfig) {
      setChangeValue(vm, "StatusId", "3");
      if (getChange(vm, "AutoProgressId")) setChangeValue(vm, "AutoProgressId", "2");
      const rs2 = await this.savePatch2(vm);
      const tasks = userApproved.map((receiver) => ({
        Id: crypto.randomUUID(),
        VoucherTypeId: Number(voucherTypeId || 0),
        EntityId: name,
        Avatar: this.context.Avatar,
        FeatureName: featureName,
        Title: title || "",
        Title2: `${this.context.FullName} has approved your request.`,
        Icon: "fal fa-smile",
        Description: title || "",
        InsertedBy: this.context.UserId,
        RecordId: id,
        InsertedDate: toIso(now),
        Active: true,
        AssignedId: userCreateId || insertedBy || "",
      }));
      await Promise.all(tasks.map((task) => this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) })));
      return { status: 200, updatedItem: rs2.updatedItem };
    }
    let nextUserApproved = nextLevelConfig.UserIds ? nextLevelConfig.UserIds.split(",") : [];
    if (nextLevelConfig.IsTeam) {
      const users = await this.context.query(`SELECT * FROM [USER] where [TeamId] = ${escapeValue(this.context.GroupId || "", this.context.sqlDialect)} and IsTeam = 1`);
      nextUserApproved = users.map((user) => toStringSafe(getRowValue(user, "Id")));
    }
    if (nextLevelConfig.IsDepartment) {
      const users = await this.context.query(`SELECT * FROM [USER] where [DepartmentId] = ${escapeValue(this.context.DepartmentId || "", this.context.sqlDialect)} and IsDepartment = 1`);
      nextUserApproved = users.map((user) => toStringSafe(getRowValue(user, "Id")));
    }
    if (isEmpty(nextUserApproved)) return { status: 500, message: "Please config user approved" };
    const userViewIds = getChangeValue(vm, "UserViewIds");
    const currentApproved = getChangeValue(vm, "UserApprovedIds");
    if (userViewIds) {
      setChangeValue(vm, "UserViewIds", userViewIds === "" ? currentApproved : `${userViewIds},${currentApproved}`);
    }
    setChangeValue(vm, "UserApprovedIds", nextUserApproved.join(","));
    const tasks = nextUserApproved.map((receiver) => ({
      Id: crypto.randomUUID(),
      VoucherTypeId: Number(voucherTypeId || 0),
      EntityId: name,
      Avatar: this.context.Avatar,
      FeatureName: featureName,
      Title: title || "",
      Title2: `${this.context.FullName} has sent you an approval request.`,
      Icon: "fal fa-smile",
      Description: title || "",
      InsertedBy: this.context.UserId,
      RecordId: id,
      InsertedDate: toIso(now),
      Active: true,
      AssignedId: receiver,
    }));
    await Promise.all(tasks.map((task) => this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) })));
    const rs2 = await this.savePatch2(vm);
    return { status: 200, updatedItem: rs2.updatedItem };
  }

  async forwardEntity(vm: PatchVM): Promise<SqlResult> {
    vm.Detail = [];
    vm.Delete = [];
    const name = vm.Name || vm.Table;
    const id = getChangeValue(vm, "Id") || "";
    const forwardId = getChangeValue(vm, "ForwardId") || "";
    const title = getChangeValue(vm, "FormatChat") || "";
    const voucherTypeId = getChangeValue(vm, "VoucherTypeId") || "0";
    const featureName = getChangeValue(vm, "FeatureName");
    const featureName2 = getChangeValue(vm, "FeatureName2");
    const featureName3 = getChangeValue(vm, "FeatureName3");
    const rs = await this.savePatch2(vm);
    const task: TaskNotification = {
      Id: crypto.randomUUID(),
      VoucherTypeId: Number(voucherTypeId),
      EntityId: name,
      Avatar: this.context.Avatar,
      FeatureName: featureName,
      FeatureName2: featureName2,
      FeatureName3: featureName3,
      Title: title,
      Title2: `${this.context.FullName} has forward you an approval request.`,
      Icon: "fal fa-smile",
      Description: title,
      InsertedBy: this.context.UserId,
      RecordId: id,
      InsertedDate: toIso(this.context.now()),
      Active: true,
      AssignedId: forwardId,
    };
    await this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
    return { status: 200, updatedItem: rs.updatedItem };
  }

  async declineEntity(vm: PatchVM): Promise<SqlResult> {
    vm.Detail = [];
    vm.Delete = [];
    setChangeValue(vm, "StatusId", "4");
    if (getChange(vm, "AutoProgressId")) setChangeValue(vm, "AutoProgressId", "4");
    const now = this.context.now();
    const name = vm.Name || vm.Table;
    const id = getChangeValue(vm, "Id") || "";
    const voucherTypeId = getChangeValue(vm, "VoucherTypeId");
    const userReceiverId = getChangeValue(vm, "UserReceiverId");
    const receiverIds = getChangeValue(vm, "ReceiverIds");
    const userCreateId = getChangeValue(vm, "UserCreateId");
    const groupReceiverId = getChangeValue(vm, "GroupReceiverId");
    const insertedBy = getChangeValue(vm, "InsertedBy");
    const title = getChangeValue(vm, "FormatChat");
    const featureName = getChangeValue(vm, "FeatureName");
    const featureName2 = getChangeValue(vm, "FeatureName2");
    const featureName3 = getChangeValue(vm, "FeatureName3");

    if ((userReceiverId && userReceiverId !== "") || (groupReceiverId && groupReceiverId !== "") || (receiverIds && receiverIds !== "")) {
      const rs = await this.savePatch2(vm);
      if (userReceiverId && userReceiverId !== "" && this.context.UserId === userReceiverId) {
        const approval: Approvement = {
          Id: crypto.randomUUID(),
          Approved: false,
          CurrentLevel: 1,
          NextLevel: 1,
          Name: name,
          RecordId: id,
          ReasonOfChange: vm.ReasonOfChange,
          StatusId: 3,
          UserApproveId: this.context.UserId,
          ApprovedBy: this.context.UserId,
          ApprovedDate: toIso(now),
          InsertedBy: this.context.UserId,
          InsertedDate: toIso(now),
        };
        await this.savePatch({ Table: "Approvement", Changes: Object.entries(approval).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
        const task: TaskNotification = {
          Id: crypto.randomUUID(),
          VoucherTypeId: Number(voucherTypeId || 0),
          EntityId: name,
          Avatar: this.context.Avatar,
          FeatureName: featureName,
          FeatureName2: featureName2,
          FeatureName3: featureName3,
          Title: title || "",
          Title2: `${this.context.FullName} has rejected your request.`,
          Icon: "fal fa-smile",
          Description: title || "",
          InsertedBy: this.context.UserId,
          RecordId: id,
          InsertedDate: toIso(now),
          Active: true,
          AssignedId: userCreateId || insertedBy || "",
        };
        await this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
      }
      if (groupReceiverId && groupReceiverId !== "") {
        const users = await this.context.query(`SELECT * FROM [User] where TeamId = ${escapeValue(groupReceiverId, this.context.sqlDialect)}`);
        const ids = users.map((user) => toStringSafe(getRowValue(user, "Id")));
        if (!ids.includes(this.context.UserId || "")) {
          return { status: 500, message: "You do not have permission to browse the data" };
        }
        const approval: Approvement = {
          Id: crypto.randomUUID(),
          Approved: false,
          CurrentLevel: 1,
          NextLevel: 1,
          Name: name,
          RecordId: id,
          StatusId: 3,
          ReasonOfChange: vm.ReasonOfChange,
          UserApproveId: this.context.UserId,
          ApprovedBy: this.context.UserId,
          ApprovedDate: toIso(now),
          InsertedBy: this.context.UserId,
          InsertedDate: toIso(now),
        };
        await this.savePatch({ Table: "Approvement", Changes: Object.entries(approval).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
        const task: TaskNotification = {
          Id: crypto.randomUUID(),
          VoucherTypeId: Number(voucherTypeId || 0),
          EntityId: name,
          Avatar: this.context.Avatar,
          FeatureName: featureName,
          FeatureName2: featureName2,
          FeatureName3: featureName3,
          Title: title || "",
          Title2: `${this.context.FullName} has rejected your request.`,
          Icon: "fal fa-smile",
          Description: title || "",
          InsertedBy: this.context.UserId,
          RecordId: id,
          InsertedDate: toIso(now),
          Active: true,
          AssignedId: userCreateId || insertedBy || "",
        };
        await this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
      }
      if (receiverIds && receiverIds !== "") {
        const ids = receiverIds.split(",");
        const users = await this.context.query(`SELECT * FROM [User] where Id in (${combineStrings(ids, this.context.sqlDialect)})`);
        const userIds = users.map((user) => toStringSafe(getRowValue(user, "Id")));
        if (!userIds.includes(this.context.UserId || "")) {
          return { status: 500, message: "You do not have permission to browse the data" };
        }
        const approval: Approvement = {
          Id: crypto.randomUUID(),
          Approved: false,
          CurrentLevel: 1,
          NextLevel: 1,
          Name: name,
          RecordId: id,
          StatusId: 3,
          ReasonOfChange: vm.ReasonOfChange,
          UserApproveId: this.context.UserId,
          ApprovedBy: this.context.UserId,
          ApprovedDate: toIso(now),
          InsertedBy: this.context.UserId,
          InsertedDate: toIso(now),
        };
        await this.savePatch({ Table: "Approvement", Changes: Object.entries(approval).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
        const task: TaskNotification = {
          Id: crypto.randomUUID(),
          VoucherTypeId: Number(voucherTypeId || 0),
          EntityId: name,
          Avatar: this.context.Avatar,
          FeatureName: featureName,
          FeatureName2: featureName2,
          FeatureName3: featureName3,
          Title: title || "",
          Title2: `${this.context.FullName} has rejected your request.`,
          Icon: "fal fa-smile",
          Description: title || "",
          InsertedBy: this.context.UserId,
          RecordId: id,
          InsertedDate: toIso(now),
          Active: true,
          AssignedId: userCreateId || insertedBy || "",
        };
        await this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
      }
      return { status: 200, updatedItem: rs.updatedItem };
    }

    const approvalConfigRows = await this.context.query(`SELECT * FROM ApprovalConfig where VoucherTypeId = ${escapeValue(voucherTypeId || "", this.context.sqlDialect)} and ParentId is not null  order by Level asc`);
    if (isEmpty(approvalConfigRows)) return { status: 500, message: "Please config approved" };
    const approvalConfig = approvalConfigRows as ApprovalConfig[];
    const matchApprovalConfig = approvalConfig.find((row) => row.Level === 1);
    if (!matchApprovalConfig) return { status: 500, message: "Please config approved" };
    const maxLevel = Math.max(...approvalConfig.map((row) => row.Level || 0));
    const approvements = await this.context.query(`SELECT * FROM Approvement where Name = ${escapeValue(name || "", this.context.sqlDialect)} and RecordId = ${escapeValue(id, this.context.sqlDialect)} and IsEnd = 0 order by CurrentLevel desc`);
    const nextLevel = approvements.length === 0 ? 1 : Number(getRowValue(approvements[0], "NextLevel")) || 1;
    const nextConfig = approvalConfig.find((row) => row.Level === nextLevel);
    if (!nextConfig) return { status: 500, message: "Please config approved" };
    let userApproved = nextConfig.UserIds ? nextConfig.UserIds.split(",") : [];
    if (nextConfig.IsTeam) {
      const users = await this.context.query(`SELECT * FROM [USER] where [TeamId] = ${escapeValue(this.context.GroupId || "", this.context.sqlDialect)} and IsTeam = 1`);
      userApproved = users.map((user) => toStringSafe(getRowValue(user, "Id")));
    }
    if (nextConfig.IsDepartment) {
      const users = await this.context.query(`SELECT * FROM [USER] where [DepartmentId] = ${escapeValue(this.context.DepartmentId || "", this.context.sqlDialect)} and IsDepartment = 1`);
      userApproved = users.map((user) => toStringSafe(getRowValue(user, "Id")));
    }
    if (!userApproved.includes(this.context.UserId || "")) return { status: 500, message: "You do not have permission to browse the data" };
    const approval: Approvement = {
      Id: crypto.randomUUID(),
      Approved: false,
      CurrentLevel: nextLevel,
      ReasonOfChange: vm.ReasonOfChange,
      NextLevel: nextLevel + 1,
      Name: name,
      RecordId: id,
      StatusId: 4,
      UserApproveId: this.context.UserId,
      ApprovedBy: this.context.UserId,
      ApprovedDate: toIso(now),
      InsertedBy: this.context.UserId,
      InsertedDate: toIso(now),
    };
    await this.savePatch({ Table: "Approvement", Changes: Object.entries(approval).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
    const task: TaskNotification = {
      Id: crypto.randomUUID(),
      VoucherTypeId: Number(voucherTypeId || 0),
      EntityId: name,
      Avatar: this.context.Avatar,
      FeatureName: featureName,
      FeatureName2: featureName2,
      FeatureName3: featureName3,
      Title: title || "",
      Title2: `${this.context.FullName} has rejected your request.`,
      Icon: "fal fa-smile",
      Description: title || "",
      InsertedBy: this.context.UserId,
      RecordId: id,
      InsertedDate: toIso(now),
      Active: true,
      AssignedId: userCreateId || insertedBy || "",
    };
    await this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) });
    const rs1 = await this.savePatch2(vm);
    await this.context.execute(`Update Approvement set IsEnd = 1 where Name = ${escapeValue(name || "", this.context.sqlDialect)} and RecordId = ${escapeValue(id, this.context.sqlDialect)}`);
    return { status: 200, updatedItem: rs1.updatedItem };
  }

  async savePatch2(vm: PatchVM): Promise<SqlResult> {
    if (!vm.Changes) return { status: 400, message: "Changes are required" };
    const idRaw = getChangeValue(vm, "Id") || "";
    const tableColumns = await this.getTableColumnsForTables([vm.Table || ""]);
    const filteredChanges = this.filterChanges(vm.Changes, tableColumns.get(vm.Table || "") || []);
    const selectIds: DetailData[] = [];
    const isSend = filteredChanges.find((change) => change.Field === "IsSend");
    const oldIsSend = isSend ? isSend.Value || "1" : "1";
    const receiverIds = filteredChanges.find((change) => change.Field === "ReceiverIds") || null;
    if (isSend) isSend.Value = "1";
    const id = normalizeIdValue(idRaw) || "";

    if (idRaw.startsWith("-")) {
      const [dup, mess, currentEntity] = await this.checkDuplicate(vm, false);
      if (dup) {
        return {
          updatedItem: undefined,
          status: 409,
          message: currentEntity ? formatEntity(mess || "", currentEntity) : mess || "",
        };
      }
      this.addDefaultFields(filteredChanges, [
        { Field: "InsertedDate", Value: toIso(this.context.now()) },
        { Field: "InsertedBy", Value: this.context.UserId || "" },
        { Field: "UpdatedDate", Value: null },
        { Field: "UpdatedBy", Value: null },
        { Field: "Active", Value: "1" },
      ]);

      const sqlParts: string[] = [];
      if (!isEmpty(vm.Delete)) {
        sqlParts.push(...(vm.Delete || []).map((item) => `delete from [${item.Table}] where Id in (${combineStrings(item.Ids, this.context.sqlDialect)})`));
      }
      const insertSql = this.buildInsertSql(vm.Table || "", id, filteredChanges);
      if (insertSql) sqlParts.push(insertSql);

      if (!isEmpty(vm.Detail)) {
        const tableNames = new Set<string>();
        vm.Detail?.forEach((detailArray) => detailArray.forEach((detail) => detail.Table && tableNames.add(detail.Table)));
        const detailColumns = await this.getTableColumnsForTables([...tableNames]);
        let index = 1;
        for (const detailArray of vm.Detail || []) {
          for (const detail of detailArray) {
            const detailChanges = this.filterChanges(detail.Changes || [], detailColumns.get(detail.Table || "") || []);
            const detailIdValue = normalizeIdValue(getChangeValue(detail, "Id") || "") || "";
            if (getChangeValue(detail, "Id")?.startsWith("-")) {
              this.addDefaultFields(detailChanges, [
                { Field: "InsertedDate", Value: toIso(this.context.now()) },
                { Field: "InsertedBy", Value: this.context.UserId || "" },
                { Field: "UpdatedDate", Value: null },
                { Field: "UpdatedBy", Value: null },
                { Field: "Active", Value: "1" },
              ]);
              const detailInsert = this.buildInsertSql(detail.Table || "", detailIdValue, detailChanges);
              if (detailInsert) sqlParts.push(detailInsert);
            } else {
              this.addDefaultFields(detailChanges, [
                { Field: "UpdatedDate", Value: toIso(this.context.now()) },
                { Field: "UpdatedBy", Value: this.context.UserId || "" },
              ]);
              const detailUpdate = this.buildUpdateSql(detail.Table || "", detailIdValue, detailChanges);
              if (detailUpdate) sqlParts.push(detailUpdate);
            }
          }
          selectIds.push({
            Index: index,
            Table: detailArray[0]?.Table,
            ComId: detailArray[0]?.ComId,
            Ids: detailArray.flatMap((item) => item.Changes || []).filter((change) => change.Field === "Id").map((change) => normalizeIdValue(change.Value || "") || ""),
          });
          index += 1;
        }
      }

      await this.context.execute(sqlParts.join(";"));
      const entity = await this.readEntityWithDetails(vm.Table || "", id, selectIds);
      await this.notification(vm, id, filteredChanges, oldIsSend, receiverIds);
      return {
        updatedItem: entity[0],
        Detail: selectIds,
        status: 200,
        message: "create successfull",
      };
    }

    const [dup, mess, currentEntity] = await this.checkDuplicate(vm, true);
    if (dup) {
      const entity = await this.context.query(`SELECT * FROM [${vm.Table}] where Id = ${escapeValue(id, this.context.sqlDialect)}`);
      return {
        updatedItem: entity,
        status: 409,
        message: currentEntity ? formatEntity(mess || "", currentEntity) : mess || "",
      };
    }
    this.addDefaultFields(filteredChanges, [
      { Field: "UpdatedDate", Value: toIso(this.context.now()) },
      { Field: "UpdatedBy", Value: this.context.UserId || "" },
    ]);

    const sqlParts: string[] = [];
    if (!isEmpty(vm.Delete)) {
      sqlParts.push(...(vm.Delete || []).map((item) => `delete from [${item.Table}] where Id in (${combineStrings(item.Ids, this.context.sqlDialect)})`));
    }
    const updateSql = this.buildUpdateSql(vm.Table || "", id, filteredChanges);
    if (updateSql) sqlParts.push(updateSql);

    const historyChanges = filteredChanges.filter((change) => change.HistoryValue && change.HistoryValue.trim() !== "");
    if (!isEmpty(historyChanges)) {
      const history = historyChanges.map((change) => change.HistoryValue).filter((value): value is string => !!value).join("\n").replace(/'/g, "''");
      if (history) {
        sqlParts.push(
          `INSERT INTO [History](Id,TextContent,RecordId,TableName,Active,InsertedDate,InsertedBy) values('${crypto.randomUUID()}',N'${history}','${id}','${vm.Table}',1,'${toIso(this.context.now())}','${this.context.UserId}')`,
        );
      }
    }

    if (!isEmpty(vm.Detail)) {
      const tableNames = new Set<string>();
      vm.Detail?.forEach((detailArray) => detailArray.forEach((detail) => detail.Table && tableNames.add(detail.Table)));
      const detailColumns = await this.getTableColumnsForTables([...tableNames]);
      let index = 1;
      for (const detailArray of vm.Detail || []) {
        for (const detail of detailArray) {
          const detailChanges = this.filterChanges(detail.Changes || [], detailColumns.get(detail.Table || "") || []);
          const detailIdValue = normalizeIdValue(getChangeValue(detail, "Id") || "") || "";
          if (getChangeValue(detail, "Id")?.startsWith("-")) {
            this.addDefaultFields(detailChanges, [
              { Field: "InsertedDate", Value: toIso(this.context.now()) },
              { Field: "InsertedBy", Value: this.context.UserId || "" },
              { Field: "UpdatedDate", Value: null },
              { Field: "UpdatedBy", Value: null },
              { Field: "Active", Value: "1" },
            ]);
            const detailInsert = this.buildInsertSql(detail.Table || "", detailIdValue, detailChanges);
            if (detailInsert) sqlParts.push(detailInsert);
          } else {
            this.addDefaultFields(detailChanges, [
              { Field: "UpdatedDate", Value: toIso(this.context.now()) },
              { Field: "UpdatedBy", Value: this.context.UserId || "" },
            ]);
            const detailUpdate = this.buildUpdateSql(detail.Table || "", detailIdValue, detailChanges);
            if (detailUpdate) sqlParts.push(detailUpdate);
          }
        }
        selectIds.push({
          Index: index,
          Table: detailArray[0]?.Table,
          ComId: detailArray[0]?.ComId,
          Ids: detailArray.flatMap((item) => item.Changes || []).filter((change) => change.Field === "Id").map((change) => normalizeIdValue(change.Value || "") || ""),
        });
        index += 1;
      }
    }

    await this.context.execute(sqlParts.join(";"));
    const entity = await this.readEntityWithDetails(vm.Table || "", id, selectIds);
    if (vm.Table === "Feature") {
      const name = filteredChanges.find((change) => change.Field === "Name")?.Value || "";
      if (name) await this.featureService.publishFeatureByName(name);
    } else if (vm.Table === "Component" || vm.Table === "FeaturePolicy") {
      const featureId = filteredChanges.find((change) => change.Field === "FeatureId")?.Value || "";
      if (featureId) {
        const featureRows = await this.context.query(`SELECT * FROM Feature where Id = ${escapeValue(featureId, this.context.sqlDialect)}`);
        const feature = featureRows[0] as Feature | undefined;
        if (feature?.Name) await this.featureService.publishFeatureByName(feature.Name);
      }
    }
    await this.notification(vm, id, filteredChanges, oldIsSend, receiverIds);
    return {
      updatedItem: entity[0],
      Detail: selectIds,
      status: 200,
      message: "update successfull",
    };
  }

  async savePatchs2(vms: PatchVM[]): Promise<SqlResult> {
    if (isEmpty(vms)) return { status: 400, message: "Patches are required" };
    const selectIds: DetailData[] = [];
    const tableNames = new Set<string>();
    vms.forEach((vm) => {
      if (vm.Table) tableNames.add(vm.Table);
      vm.Detail?.forEach((detailArray) => detailArray.forEach((detail) => detail.Table && tableNames.add(detail.Table)));
    });
    const columns = await this.getTableColumnsForTables([...tableNames]);
    const sqlParts: string[] = [];

    for (const vm of vms) {
      if (!vm.Changes) continue;
      const idRaw = getChangeValue(vm, "Id") || "";
      const filteredChanges = this.filterChanges(vm.Changes, columns.get(vm.Table || "") || []);
      if (idRaw.startsWith("-")) {
        const id = normalizeIdValue(idRaw) || "";
        this.addDefaultFields(filteredChanges, [
          { Field: "InsertedDate", Value: toIso(this.context.now()) },
          { Field: "InsertedBy", Value: this.context.UserId || "" },
          { Field: "UpdatedDate", Value: null },
          { Field: "UpdatedBy", Value: null },
          { Field: "Active", Value: "1" },
        ]);
        if (!isEmpty(vm.Delete)) {
          sqlParts.push(...(vm.Delete || []).map((item) => `delete from [${item.Table}] where Id in (${combineStrings(item.Ids, this.context.sqlDialect)})`));
        }
        const insertSql = this.buildInsertSql(vm.Table || "", id, filteredChanges);
        if (insertSql) sqlParts.push(insertSql);
      } else {
        const id = normalizeIdValue(idRaw) || "";
        this.addDefaultFields(filteredChanges, [
          { Field: "UpdatedDate", Value: toIso(this.context.now()) },
          { Field: "UpdatedBy", Value: this.context.UserId || "" },
        ]);
        if (!isEmpty(vm.Delete)) {
          sqlParts.push(...(vm.Delete || []).map((item) => `delete from [${item.Table}] where Id in (${combineStrings(item.Ids, this.context.sqlDialect)})`));
        }
        const updateSql = this.buildUpdateSql(vm.Table || "", id, filteredChanges);
        if (updateSql) sqlParts.push(updateSql);
      }

      if (!isEmpty(vm.Detail)) {
        let index = 1;
        for (const detailArray of vm.Detail || []) {
          for (const detail of detailArray) {
            const detailChanges = this.filterChanges(detail.Changes || [], columns.get(detail.Table || "") || []);
            const detailIdRaw = getChangeValue(detail, "Id") || "";
            const detailId = normalizeIdValue(detailIdRaw) || "";
            if (detailIdRaw.startsWith("-")) {
              this.addDefaultFields(detailChanges, [
                { Field: "InsertedDate", Value: toIso(this.context.now()) },
                { Field: "InsertedBy", Value: this.context.UserId || "" },
                { Field: "UpdatedDate", Value: null },
                { Field: "UpdatedBy", Value: null },
                { Field: "Active", Value: "1" },
              ]);
              const detailInsert = this.buildInsertSql(detail.Table || "", detailId, detailChanges);
              if (detailInsert) sqlParts.push(detailInsert);
            } else {
              this.addDefaultFields(detailChanges, [
                { Field: "UpdatedDate", Value: toIso(this.context.now()) },
                { Field: "UpdatedBy", Value: this.context.UserId || "" },
              ]);
              const detailUpdate = this.buildUpdateSql(detail.Table || "", detailId, detailChanges);
              if (detailUpdate) sqlParts.push(detailUpdate);
            }
          }
          selectIds.push({
            Index: index,
            Table: detailArray[0]?.Table,
            ComId: detailArray[0]?.ComId,
            Ids: detailArray.flatMap((item) => item.Changes || []).filter((change) => change.Field === "Id").map((change) => normalizeIdValue(change.Value || "") || ""),
          });
          index += 1;
        }
      }
    }

    if (!isEmpty(sqlParts)) {
      await this.context.execute(sqlParts.join(";"));
    }
    const firstVm = vms[0];
    const firstId = normalizeIdValue(getChangeValue(firstVm, "Id") || "") || "";
    const entity = await this.readEntityWithDetails(firstVm.Table || "", firstId, selectIds);
    if (firstVm.Table === "Component") {
      const featureId = getChangeValue(firstVm, "FeatureId") || "";
      if (featureId) {
        const featureRows = await this.context.query(`SELECT * FROM Feature where Id = ${escapeValue(featureId, this.context.sqlDialect)}`);
        const feature = featureRows[0] as Feature | undefined;
        if (feature?.Name) await this.featureService.publishFeatureByName(feature.Name);
      }
    }
    return {
      updatedItem: entity[0],
      Detail: selectIds,
      status: 200,
      message: "All patches processed successfully",
    };
  }

  async savePatches(patches: PatchVM[]): Promise<number> {
    if (isEmpty(patches)) throw new Error("patches is null or empty");
    const usable = patches.filter((patch) => getChangeValue(patch, "Id") != null);
    if (isEmpty(usable)) throw new Error("patches is null or empty");
    await this.resolvePatchConnections(usable[0]);
    const tables = usable.map((patch) => patch.Table || "");
    const permissionQuery = `select * from [FeaturePolicy] where Active = 1 and (CanWrite = 1 or CanWriteAll = 1) and EntityName in (${combineStrings(tables, this.context.sqlDialect)}) and RoleId in (${combineStrings(this.context.RoleIds, this.context.sqlDialect)})`;
    const permissions = distinctBy(await this.context.query(permissionQuery), (perm) => toStringSafe(getRowValue(perm, "TableName")));
    const permissionTables = permissions.map((perm) => toStringSafe(getRowValue(perm, "TableName")));
    const lack = tables.filter((table) => !permissionTables.includes(table));
    if (!isEmpty(lack)) {
      throw new Error(`All table must have write permission ${lack.join(",")}`);
    }
    const sql = usable
      .map((patch) => this.context.sqlBuilder.buildCreateOrUpdate(patch))
      .filter((statement) => statement && statement.trim() !== "")
      .join(";\n");
    return this.context.execute(sql);
  }

  async deactivateAsync(vm: SqlViewModel): Promise<string[] | null> {
    vm.CachedDataConn = await this.context.resolveConnection(vm.DataConn || "default") || vm.CachedDataConn;
    const allRights = await this.getEntityPerm(vm.Table || "", null);
    const canDeactivateAll = allRights.some((perm) => perm.CanDeactivateAll);
    const canDeactivateSelf = allRights.some((perm) => perm.CanDeactivate);
    const rows = await this.context.query(`select * from [${vm.Table}] where Id in (${combineStrings(vm.Id, this.context.sqlDialect)})`);
    if (isEmpty(rows)) return null;
    const canDeactivateRows = rows
      .filter((row) => canDeactivateAll || (canDeactivateSelf && isOwner(row, this.context.UserId, this.context.RoleIds)))
      .map((row) => toStringSafe(getRowValue(row, "Id")));
    if (isEmpty(canDeactivateRows)) return null;
    await this.context.execute(`update ${vm.Table} set Active = 0 where Id in (${combineStrings(canDeactivateRows, this.context.sqlDialect)})`);
    return canDeactivateRows;
  }

  async getTableColumns(tableName: string): Promise<Array<Array<Record<string, unknown>>>> {
    const sql = `SELECT c.TABLE_NAME, c.COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS c JOIN sys.columns sc ON c.COLUMN_NAME = sc.name AND OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME) = sc.object_id WHERE c.TABLE_NAME = ${escapeValue(tableName, this.context.sqlDialect)} AND sc.is_computed = 0`;
    return this.context.queryMany(sql);
  }

  async getEntityPermissions(entityName: string, recordId: string | null): Promise<FeaturePolicy[]> {
    return this.getEntityPerm(entityName, recordId);
  }

  private async getTableColumnsForTables(tableNames: string[]): Promise<Map<string, string[]>> {
    const filtered = tableNames.filter((name) => name && name.trim() !== "");
    if (filtered.length === 0) return new Map();
    const sql = `SELECT c.TABLE_NAME, c.COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS c JOIN sys.columns sc ON c.COLUMN_NAME = sc.name AND OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME) = sc.object_id WHERE c.TABLE_NAME in (${combineStrings(filtered, this.context.sqlDialect)}) AND sc.is_computed = 0`;
    const rows = await this.context.query(sql);
    const map = new Map<string, string[]>();
    rows.forEach((row) => {
      const table = toStringSafe(getRowValue(row, "TABLE_NAME"));
      const column = toStringSafe(getRowValue(row, "COLUMN_NAME"));
      if (!table || !column) return;
      if (!map.has(table)) map.set(table, []);
      map.get(table)?.push(column);
    });
    return map;
  }

  private filterChanges(changes: PatchDetail[], columns: string[]): PatchDetail[] {
    if (isEmpty(columns)) return changes;
    const set = new Set(columns);
    return changes.filter((change) => set.has(change.Field));
  }

  private buildInsertSql(table: string, id: string, changes: PatchDetail[]): string {
    const fields = changes.map((change) => `[${change.Field}]`);
    const values = changes.map((change) => escapeValue(change.Value || null, this.context.sqlDialect));
    if (isEmpty(fields) || isEmpty(values)) return "";
    return `INSERT into [${table}](${fields.join(",")}) values(${values.join(",")})`;
  }

  private buildUpdateSql(table: string, id: string, changes: PatchDetail[]): string {
    const updateFields = changes
      .filter((change) => change.Field !== "Id")
      .map((change) => `[${change.Field}] = ${escapeValue(change.Value || null, this.context.sqlDialect)}`);
    if (isEmpty(updateFields)) return "";
    return `UPDATE [${table}] SET ${updateFields.join(",")} WHERE Id = ${escapeValue(id, this.context.sqlDialect)}`;
  }

  private async readEntityWithDetails(table: string, id: string, details: DetailData[]): Promise<Array<Array<Record<string, unknown>>>> {
    const queries = [`SELECT * FROM [${table}] where Id = ${escapeValue(id, this.context.sqlDialect)}`];
    details.forEach((detail) => {
      if (!detail.Table || isEmpty(detail.Ids)) return;
      queries.push(`SELECT * FROM [${detail.Table}] where Id in (${combineStrings(detail.Ids, this.context.sqlDialect)})`);
    });
    const data = await this.context.queryMany(queries.join(";"));
    details.forEach((detail, index) => {
      detail.Data = data[index + 1] || [];
    });
    return data;
  }

  private async notification(vm: PatchVM, id: string, filteredChanges: PatchDetail[], isSend: string, receiverIds: PatchDetail | null): Promise<void> {
    const featureName = getChangeValue(vm, "FeatureName");
    const voucherTypeId = getChangeValue(vm, "VoucherTypeId");
    const title = getChangeValue(vm, "FormatChat") || "";
    const recordId = getChangeValue(vm, "RecordId") || id;
    const featureName2 = getChangeValue(vm, "FeatureName2");
    const featureName3 = getChangeValue(vm, "FeatureName3");
    if (isSend === "0" && receiverIds?.Value) {
      const receivers = receiverIds.Value.split(",");
      const users = await this.context.query(`SELECT * FROM [User] where Id in (${combineStrings(receivers, this.context.sqlDialect)})`);
      let templateMessage = " has sent you an approval request.";
      let f2 = featureName2;
      let f3 = featureName3;
      if (vm.Table === "Conversation") {
        templateMessage = " has invited you to join the conversation.";
        f2 = "chat-editor";
        f3 = "chat-editor";
      }
      const tasks = users.map((user) => ({
        Id: crypto.randomUUID(),
        VoucherTypeId: Number(voucherTypeId || 0),
        EntityId: vm.Table,
        Avatar: this.context.Avatar,
        FeatureName: featureName,
        FeatureName2: f2,
        FeatureName3: f3,
        Title: title,
        Title2: `${this.context.FullName}${templateMessage}`,
        Icon: "fal fa-smile",
        Description: title,
        InsertedBy: this.context.UserId,
        RecordId: recordId,
        InsertedDate: toIso(this.context.now()),
        Active: true,
        AssignedId: toStringSafe(getRowValue(user, "Id")),
      }));
      await Promise.all(tasks.map((task) => this.savePatch({ Table: "TaskNotification", Changes: Object.entries(task).map(([Field, Value]) => ({ Field, Value: Value ? String(Value) : null })) })));
    }
  }

  private async checkDuplicate(vm: PatchVM, update: boolean): Promise<[boolean, string | null, Record<string, unknown> | null]> {
    const tableRows = await this.context.query(`Select * from TableName where [Name] = ${escapeValue(vm.Table || "", this.context.sqlDialect)}`);
    if (isEmpty(tableRows)) return [false, null, null];
    const table = tableRows[0] as TableName;
    if (!table.Duplicate) return [false, null, null];
    const fields = table.Duplicate.split(",").map((field) => field.trim()).filter((field) => field !== "");
    if (fields.length === 0) return [false, table.Description || null, null];
    const conditions = fields.map((field) => {
      const change = vm.Changes?.find((item) => item.Field === field);
      if (!change) return `[${field}] is null`;
      if (change.Value === null || change.Value === undefined) return `[${field}] is null`;
      return `[${field}] = ${escapeValue(change.Value, this.context.sqlDialect)}`;
    });
    if (update) {
      const idChange = vm.Changes?.find((item) => item.Field === "Id");
      if (idChange?.Value) conditions.push(`[Id] != ${escapeValue(idChange.Value, this.context.sqlDialect)}`);
    }
    const sql = `Select Top 1 * from [${vm.Table}] where ${conditions.join(" and ")}`;
    try {
      const rows = await this.context.query(sql);
      if (!isEmpty(rows)) {
        return [true, table.Description || null, rows[0]];
      }
      return [false, table.Description || null, null];
    } catch {
      return [true, table.Description || null, null];
    }
  }

  private async resolvePatchConnections(vm: PatchVM): Promise<void> {
    if (!vm.CachedDataConn) vm.CachedDataConn = await this.context.resolveConnection(vm.DataConn) || vm.DataConn || undefined;
    if (!vm.CachedMetaConn) vm.CachedMetaConn = await this.context.resolveConnection(vm.MetaConn) || vm.MetaConn || undefined;
  }

  private async hasWritePermission(vm: PatchVM): Promise<boolean> {
    if (vm.ByPassPerm) return true;
    const allRights = await this.getEntityPerm(vm.Table || "", null);
    const idField = vm.Changes?.find((change) => change.Field === "Id");
    const oldId = idField?.OldVal;
    if (!oldId) {
      return allRights.some((perm) => perm.CanWriteAll);
    }
    const originRows = await this.context.query(`select t.* from [${vm.Table}] as t where t.Id = ${escapeValue(oldId, this.context.sqlDialect)}`);
    const originRow = originRows[0];
    return isOwner(originRow || {}, this.context.UserId, this.context.RoleIds) || allRights.some((perm) => perm.CanWriteAll);
  }

  private async getEntityPerm(entityName: string, recordId: string | null): Promise<FeaturePolicy[]> {
    if (!entityName || isEmpty(this.context.RoleIds)) return [];
    const key = `${entityName}_AllRights`;
    const cached = await this.context.cache.get(key);
    if (cached) {
      const parsed = parseJsonSafe<FeaturePolicy[]>(cached);
      if (parsed) return parsed;
    }
    const recordValue = recordId || "";
    const query = `select * from [FeaturePolicy] where Active = 1 and EntityName = ${escapeValue(entityName, this.context.sqlDialect)} and (RecordId = ${escapeValue(recordValue, this.context.sqlDialect)} or ${escapeValue(recordValue, this.context.sqlDialect)} = '') and RoleId in (${combineStrings(this.context.RoleIds, this.context.sqlDialect)})`;
    const permissions = await this.context.query(query);
    await this.context.cache.set(key, JSON.stringify(permissions), DEFAULT_CACHE_TTL_MS);
    return permissions as FeaturePolicy[];
  }
}
