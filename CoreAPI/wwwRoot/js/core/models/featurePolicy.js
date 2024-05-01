export class FeaturePolicy {
    constructor() {
        this.Id = '';
        this.FeatureId = '';
        this.RoleId = '';
        this.CanRead = false;
        this.CanWrite = false;
        this.CanDelete = false;
        this.CanDeleteAll = false;
        this.Active = false;
        this.InsertedDate = new Date();
        this.InsertedBy = '';
        this.UpdatedDate = null;
        this.UpdatedBy = '';
        this.CanDeactivate = false;
        this.CanDeactivateAll = false;
        this.LockDeleteAfterCreated = '';
        this.LockUpdateAfterCreated = '';
        this.EntityId = '';
        this.RecordId = '';
        this.UserId = '';
        this.CanShare = false;
        this.CanShareAll = false;
        this.Desc = '';
        this.CanWriteAll = false;
        this.EntityName = '';
        this.CanWriteMeta = false;
        this.CanWriteMetaAll = false;
    }
}
