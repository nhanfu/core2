import type { PatchDetail, PatchVM } from "./types.js";
import { SystemFields, escapeSqlValue, isNullOrWhiteSpace, toLowerSafe } from "./utils.js";

export class SqlBuilder {
  constructor(private userId: string = "1") {}

  buildCreateOrUpdate(vm: PatchVM): string {
    const normalized = this.normalizePatch(vm);
    const idField = this.getIdField(normalized.Changes || []);
    if (!idField) {
      throw new Error("Id cannot be null");
    }
    const oldId = idField.OldVal || null;
    if (oldId) {
      return this.buildUpdateInternal(normalized, oldId);
    }
    return this.buildInsertInternal(normalized, idField.Value);
  }

  buildUpdate(vm: PatchVM): string {
    const normalized = this.normalizePatch(vm);
    const idField = this.getIdField(normalized.Changes || []);
    if (!idField || isNullOrWhiteSpace(idField.Value || undefined)) {
      throw new Error("Id cannot be null");
    }
    return this.buildUpdateInternal(normalized, idField.Value as string);
  }

  private normalizePatch(vm: PatchVM): PatchVM {
    if (!vm || isNullOrWhiteSpace(vm.Table || undefined) || !vm.Changes || vm.Changes.length === 0) {
      throw new Error("Table name and change details can not be empty");
    }
    const table = (vm.Table || "").trim();
    const changes = (vm.Changes || [])
      .filter((patch) => {
        if (isNullOrWhiteSpace(patch.Field)) {
          throw new Error("Field name of the patch can not be empty");
        }
        const field = toLowerSafe(patch.Field);
        if (field === "id") return true;
        return !SystemFields.includes(field);
      })
      .map((patch) => ({
        ...patch,
        Field: patch.Field.trim(),
        Value: patch.Value ?? null,
        OldVal: patch.OldVal ?? null,
      }));
    return { ...vm, Table: table, Changes: changes };
  }

  private getIdField(changes: PatchDetail[]): PatchDetail | null {
    const idField = changes.find((x) => x.Field === "Id");
    return idField || null;
  }

  private buildUpdateInternal(vm: PatchVM, id: string): string {
    const updateFields = (vm.Changes || [])
      .filter((x) => toLowerSafe(x.Field) !== "id")
      .map((x) => {
        const field = this.wrapIdent(x.Field);
        return x.Value === null ? `${field} = null` : `${field} = ${escapeSqlValue(x.Value)}`;
      });
    if (updateFields.length === 0) return "";
    const now = new Date().toISOString();
    const updatedBy = this.wrapIdent("UpdatedBy");
    const updatedDate = this.wrapIdent("UpdatedDate");
    const idField = this.wrapIdent("Id");
    return `update ${this.wrapTable(vm.Table)} set ${updateFields.join(", ")}, ${updatedBy} = '${this.userId}', ${updatedDate} = '${now}' where ${idField} = '${id}';`;
  }

  private buildInsertInternal(vm: PatchVM, id: string | null | undefined): string {
    if (isNullOrWhiteSpace(id || undefined)) {
      throw new Error("Id cannot be null");
    }
    const valueFields = (vm.Changes || [])
      .filter((x) => toLowerSafe(x.Field) !== "active" && toLowerSafe(x.Field) !== "id");
    const fields = valueFields.map((x) => this.wrapIdent(x.Field));
    const values = valueFields.map((x) => (x.Value === null ? "null" : escapeSqlValue(x.Value)));
    if (fields.length === 0 || values.length === 0) return "";
    const now = new Date().toISOString();
    const baseFields = ["Id", "Active", "InsertedBy", "InsertedDate"].map((field) => this.wrapIdent(field));
    const baseValues = [`'${id}'`, "1", `'${this.userId}'`, `'${now}'`];
    return `insert into ${this.wrapTable(vm.Table)} (${baseFields.concat(fields).join(", ")}) values (${baseValues.concat(values).join(", ")});`;
  }

  private wrapIdent(name: string): string {
    return `"${name}"`;
  }

  private wrapTable(name: string | null | undefined): string {
    if (!name) return '""';
    return this.wrapIdent(name);
  }
}
