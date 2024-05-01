export class PatchDetail {
    /**
     * Constructs an instance of PatchDetail.
     * @param {string} field - The field name.
     * @param {string} label - The label for the field.
     * @param {string} oldVal - The old value of the field.
     * @param {string} value - The new value of the field.
     * @param {string} historyValue - The historical value of the field.
     */
    constructor(field, label, oldVal, value, historyValue) {
      this.Field = field;
      this.Label = label;
      this.OldVal = oldVal;
      this.Value = value;
      this.HistoryValue = historyValue;
    }
  }
  
  /**
   * Represents a set of changes to be applied to a data entity.
   */
  export class PatchVM {
    /**
     * Constructs an instance of PatchVM.
     * @param {string} featureId - The feature identifier.
     * @param {string} comId - The component identifier.
     * @param {string} table - The table name.
     * @param {string[]} deletedIds - Array of deleted identifiers.
     * @param {string} queueName - The queue name.
     * @param {string} cacheName - The cache name.
     * @param {string} metaConn - The metadata connection string.
     * @param {string} dataConn - The data connection string.
     */
    constructor(featureId, comId, table, deletedIds, queueName, cacheName, metaConn, dataConn) {
      this.FeatureId = featureId;
      this.ComId = comId;
      this.Table = table;
      this.DeletedIds = deletedIds;
      this.QueueName = queueName;
      this.CacheName = cacheName;
      this.MetaConn = metaConn;
      this.DataConn = dataConn;
      this.Changes = [];
    }
  
    /**
     * Gets the entity identifier from the first applicable change.
     * @returns {string|null} The entity identifier.
     */
    get EntityId() {
      const firstIdChange = this.Changes.find(x => x.Field === Utils.IdField);
      return firstIdChange ? firstIdChange.Value : null;
    }
  
    /**
     * Sets the old identifier in the change list.
     * @param {string} value - The old identifier value to set.
     */
    set OldId(value) {
      let idChange = this.Changes.find(x => x.Field === Utils.IdField);
      if (idChange) {
        idChange.OldVal = value;
      } else {
        this.Changes.push(new PatchDetail(Utils.IdField, '', value, '', ''));
      }
    }
  }
  