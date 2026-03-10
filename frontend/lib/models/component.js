import { Action } from './action';

/**
 * @typedef {import('../index').EditableComponent} EditableComponent
 * Represents a component.
 * @class
 */
export class Component {
    /** @type {string} */
    id = null;
    /** @type {string} */
    fieldName = null;
    /** @type {string} */
    populateFieldName = null;
    /** @type {number} */
    order = 0;
    /** @type {string | (...args) => EditableComponent} */
    componentType = null;
    /** @type {string} */
    componentGroupId = null;
    /** @type {string} */
    formatData = null;
    /** @type {string} */
    plainText = null;
    /** @type {number} */
    column = 0;
    /** @type {number} */
    offset = 0;
    /** @type {number} */
    row = 0;
    /** @type {boolean} */
    canSearch = false;
    /** @type {boolean} */
    canCache = false;
    /** @type {number} */
    precision = 0;
    /** @type {string} */
    groupBy = null;
    /** @type {string} */
    groupFormat = null;
    /** @type {string} */
    label = null;
    /** @type {boolean} */
    showLabel = false;
    /** @type {string} */
    icon = null;
    /** @type {string} */
    className = null;
    /** @type {string} */
    style = null;
    /** @type {string} */
    childStyle = null;
    /** @type {string} */
    hotKey = null;
    /** @type {string} */
    refClass = null;
    /** @type {string} */
    events = null;
    /** @type {boolean} */
    disabled = false;
    /** @type {boolean} */
    visibility = false;
    /** @type {string} */
    validation = null;
    /** @type {boolean} */
    focus = false;
    /** @type {string} */
    width = null;
    /** @type {string} */
    populateField = null;
    /** @type {string} */
    groupEvent = null;
    /** @type {number} */
    xsCol = 0;
    /** @type {number} */
    smCol = 0;
    /** @type {number} */
    lgCol = 0;
    /** @type {number} */
    xlCol = 0;
    /** @type {number} */
    xxlCol = 0;
    /** @type {string} */
    defaultVal = null;
    /** @type {string} */
    dateTimeField = null;
    /** @type {boolean} */
    active = false;
    /** @type {Date} */
    insertedDate = new Date();
    /** @type {string} */
    insertedBy = null;
    /** @type {Date} */
    updatedDate = new Date();
    /** @type {string} */
    updatedBy = null;
    /** @type {boolean} */
    canAdd = false;
    /** @type {boolean} */
    isPrivate = false;
    /** @type {number} */
    monthCount = 0;
    /** @type {string} */
    query = null;
    /** @type {boolean} */
    isRealtime = false;
    /** @type {string} */
    refName = null;
    /** @type {boolean} */
    topEmpty = false;
    /** @type {boolean} */
    isCollapsible = false;
    /** @type {string} */
    template = null;
    /** @type {string} */
    preQuery = null;
    /** @type {string} */
    disabledExp = null;
    /** @type {boolean} */
    focusSearch = false;
    /** @type {boolean} */
    isSumary = false;
    /** @type {string} */
    formatSumaryField = null;
    /** @type {string} */
    orderBySumary = null;
    /** @type {boolean} */
    showHotKey = false;
    /** @type {number} */
    defaultAddStart = 0;
    /** @type {number} */
    defaultAddEnd = 0;
    /** @type {boolean} */
    upperCase = false;
    /** @type {boolean} */
    virtualScroll = false;
    /** @type {string} */
    migration = null;
    /** @type {string} */
    listClass = null;
    /** @type {string} */
    excelFieldName = null;
    /** @type {boolean} */
    liteGrid = false;
    /** @type {boolean} */
    showDatetimeField = false;
    /** @type {boolean} */
    showNull = false;
    /** @type {boolean} */
    addDate = false;
    /** @type {boolean} */
    displayBadge = false;
    /** @type {boolean} */
    filterEq = false;
    /** @type {number} */
    headerHeight = 0;
    /** @type {number} */
    bodyItemHeight = 0;
    /** @type {number} */
    footerHeight = 0;
    /** @type {number} */
    scrollHeight = 0;
    /** @type {string} */
    scriptValidation = null;
    /** @type {boolean} */
    filterLocal = false;
    /** @type {boolean} */
    hideGrid = false;
    /** @type {string} */
    groupReferenceId = null;
    /** @type {string} */
    groupReferenceName = null;
    /** @type {string} */
    groupName = null;
    /** @type {string} */
    shortDesc = null;
    /** @type {string} */
    description = null;
    /** @type {string} */
    featureId = null;
    /** @type {string} */
    componentId = null;
    /** @type {string} */
    textAlign = null;
    /** @type {boolean} */
    hasFilter = false;
    /** @type {boolean} */
    frozen = false;
    /** @type {boolean} */
    frozenRight = false;
    /** @type {string} */
    filterTemplate = null;
    /** @type {boolean} */
    editable = false;
    /** @type {string} */
    formatExcell = null;
    /** @type {string} */
    databaseName = null;
    /** @type {string} */
    summary = null;
    /** @type {number} */
    summaryColSpan = 0;
    /** @type {boolean} */
    basicSearch = false;
    /** @type {string} */
    showExp = null;
    /** @type {string} */
    minWidth = null;
    /** @type {string} */
    maxWidth = null;
    /** @type {string} */
    tenantCode = null;
    /** @type {string} */
    orderBy = null;
    /** @type {string} */
    parentId = null;
    /** @type {string} */
    lang = null;
    /** @type {string} */
    name = null;
    /** @type {boolean} */
    isTab = false;
    /** @type {string} */
    tabGroup = null;
    /** @type {boolean} */
    isVertialTab = false;
    /** @type {boolean} */
    responsive = false;
    /** @type {number} */
    outerColumn = 0;
    /** @type {number} */
    xsOuterColumn = 0;
    /** @type {number} */
    smOuterColumn = 0;
    /** @type {number} */
    lgOuterColumn = 0;
    /** @type {number} */
    xlOuterColumn = 0;
    /** @type {number} */
    xxlOuterColumn = 0;
    /** @type {number} */
    badgeMonth = 0;
    /** @type {boolean} */
    isDropDown = false;
    /** @type {boolean} */
    isMultiple = false;
    /** @type {string} */
    html = null;
    /** @type {string} */
    css = null;
    /** @type {string} */
    javascript = null;
    /** @type {HTMLElement} */
    parentElement = null;
    /** @type {Component[]} */
    columns = [];
    /** @type {Component[]|string} */
    localQuery;
    /** @type {Component[]} */
    components;
    /** @type {any} */
    layout;
    /** @type {Function} */
    onClick;
    /** @type {string} */
    addRowExp;
    /** @type {string} */
    entityName;
    /** @type {string} */
    tableName;
    /** @type {string} */
    rowSpan;
    /** @type {string} */
    componentDefaultValueId;
    /** @type {string} */
    groupTypeId;
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
    isPublic = false;
}