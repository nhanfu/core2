export class PatchDetail {
  field = "";
  label = "";
  oldVal = "";
  value = "";
}

/**
 * Represents a set of changes to be applied to a data entity.
 */
export class PatchVM {
  /** @type {string} */
  comId;
  /** @type {string} */
  table;
  /** @type {PatchDetail[]} */
  changes = [];
}
/**
 * Represents a set of changes to be applied to a data entity.
 */
export class SavePatchVM {
  /** @type {any[][]} */
  detail;
  /** @type {string} */
  table;
  /** @type {string} */
  reasonOfChange;
  /** @type {any[]} */
  changes = [];
  /** @type {any[]} */
  delete = [];
  /** @type {SavePatchVM} */
  child;
}
