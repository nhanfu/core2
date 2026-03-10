export class FeaturePolicy {
    id = '';
    featureId = '';
    roleId = '';
    recordId = '';
    userId = '';
    tableName = '';
    canRead = false;
    canReadAll = false;
    canWrite = false;
    canWriteAll = false;
    canDelete = false;
    canDeleteAll = false;
    canCopy = false;
    canCopyAll = false;
    canDeactivate = false;
    canDeactivateAll = false;
    canExport = false;
    insertedDate = new Date();
    insertedBy = '';
    updatedDate = null;
    updatedBy = '';
}
