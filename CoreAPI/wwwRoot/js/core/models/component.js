/** @typedef {import('./validationRule.js').ValidationRule} ValidationRule */
/**
 * Represents a Component.
 * @class Component
 * @property {string} Id
 * @property {string} TenantCode
 * @property {string} FieldName
 * @property {number|null} Order
 * @property {string} ComponentType
 * @property {string} ComponentGroupId
 * @property {string} ReferenceId
 * @property {string} FormatData
 * @property {string} FormatEntity
 * @property {string} PlainText
 * @property {number|null} Column
 * @property {number|null} Offset
 * @property {number|null} Row
 * @property {boolean} CanSearch
 * @property {boolean} CanCache
 * @property {number|null} Precision
 * @property {string} GroupBy
 * @property {string} GroupFormat
 * @property {string} Label
 * @property {boolean} ShowLabel
 * @property {string} Icon
 * @property {string} ClassName
 * @property {string} Style
 * @property {string} ChildStyle
 * @property {string} HotKey
 * @property {string} RefClass
 * @property {string} Events
 * @property {boolean} Disabled
 * @property {boolean} Visibility
 * @property {boolean} Hidden
 * @property {string | ValidationRule[]} Validation
 * @property {boolean} Focus
 * @property {string} Width
 * @property {string} PopulateField
 * @property {string} CascadeField
 * @property {string} GroupEvent
 * @property {number|null} XsCol
 * @property {number|null} SmCol
 * @property {number|null} LgCol
 * @property {number|null} XlCol
 * @property {number|null} XxlCol
 * @property {string} DefaultVal
 * @property {string} DateTimeField
 * @property {boolean} Active
 * @property {Date} InsertedDate
 * @property {string} InsertedBy
 * @property {Date|null} UpdatedDate
 * @property {string} UpdatedBy
 * @property {string} RoleId
 * @property {boolean} IgnoreSync
 * @property {boolean} CanAdd
 * @property {boolean} ShowAudit
 * @property {boolean} IsPrivate
 * @property {string} IdField
 * @property {string} DescFieldName
 * @property {number|null} MonthCount
 * @property {boolean|null} IsDoubleLine
 * @property {string} Query
 * @property {string} LocalQuery
 * @property {boolean} IsRealtime
 * @property {string} RefName
 * @property {boolean} TopEmpty
 * @property {boolean} IsCollapsible
 * @property {string} Template
 * @property {string} Renderer
 * @property {string} PreQuery
 * @property {string} DisabledExp
 * @property {boolean} FocusSearch
 * @property {boolean} IsSumary
 * @property {string} FormatSumaryField
 * @property {string} OrderBySumary
 * @property {boolean} ShowHotKey
 * @property {number|null} DefaultAddStart
 * @property {number|null} DefaultAddEnd
 * @property {boolean} UpperCase
 * @property {boolean} VirtualScroll
 * @property {string} Migration
 * @property {string} ListClass
 * @property {string} ExcelFieldName
 * @property {boolean} LiteGrid
 * @property {boolean} ShowDatetimeField
 * @property {boolean} ShowNull
 * @property {boolean} AddDate
 * @property {boolean} FilterEq
 * @property {number|null} HeaderHeight
 * @property {number|null} BodyItemHeight
 * @property {number|null} FooterHeight
 * @property {number|null} ScrollHeight
 * @property {string} ScriptValidation
 * @property {boolean} FilterLocal
 * @property {boolean} HideGrid
 * @property {string} GroupReferenceId
 * @property {string} GroupReferenceName
 * @property {string} GroupName
 * @property {string} Description
 * @property {string} FeatureId
 * @property {string} EntityId
 * @property {string} EntityName
 * @property {string} ComponentId
 * @property {string} TextAlign
 * @property {boolean} HasFilter
 * @property {boolean} Frozen
 * @property {string} FilterTemplate
 * @property {boolean} Editable
 * @property {string} FormatExcell
 * @property {string} DatabaseName
 * @property {string} Summary
 * @property {number|null} SummaryColSpan
 * @property {boolean} IsExport
 * @property {number|null} OrderExport
 * @property {string} ShowExp
 * @property {string} MinWidth
 * @property {string} MaxWidth
 * @property {boolean} AdvancedSearch
 * @property {boolean} AutoFit
 * @property {boolean} DisplayNone
 * @property {string} FieldText
 * @property {string} OrderBy
 * @property {string} QueueName
 * @property {string} MetaConn
 * @property {string} DataConn
 * @property {boolean} ShouldSaveText
 * @property {string} CacheName
 * @property {string} Lang
 * @property {string} DelCmd
 * @property {string} DelParam
 */

import { FeaturePolicy } from './featurePolicy.js';

// Initializing data
export class Component {
    Id = '';
    TenantCode = '';
    Name = '';
    FieldName = '';
    Order = 0;
    ComponentType = '';
    ComponentGroupId = '';
    ReferenceId = '';
    FormatData = '';
    FormatEntity = '';
    PlainText = '';
    Column = null;
    Offset = null;
    Row = null;
    CanSearch = false;
    CanCache = false;
    Precision = null;
    GroupBy = '';
    GroupFormat = '';
    Label = '';
    ShowLabel = false;
    Icon = '';
    ClassName = '';
    Style = '';
    ChildStyle = '';
    HotKey = '';
    RefClass = '';
    Events = '';
    Disabled = false;
    Visibility = false;
    Hidden = false;
    /** @type {String | ValidationRule[]} */
    Validation;
    Focus = false;
    Width = '';
    PopulateField = '';
    CascadeField = '';
    GroupEvent = '';
    XsCol = null;
    SmCol = null;
    LgCol = null;
    XlCol = null;
    XxlCol = null;
    DefaultVal = '';
    DateTimeField = '';
    Active = false;
    InsertedDate = new Date();
    InsertedBy = '';
    UpdatedDate = null;
    UpdatedBy = '';
    RoleId = '';
    IgnoreSync = false;
    CanAdd = false;
    ShowAudit = false;
    IsPrivate = false;
    IdField = '';
    DescFieldName = '';
    MonthCount = null;
    IsDoubleLine = null;
    Query = '';
    /** @type {String | any[]} */
    LocalQuery = '';
    IsRealtime = false;
    RefName = '';
    TopEmpty = false;
    IsCollapsible = false;
    Template = '';
    /** @type {String | function} */
    Renderer;
    PreQuery = '';
    DisabledExp = '';
    FocusSearch = false;
    IsSumary = false;
    FormatSumaryField = '';
    OrderBySumary = '';
    ShowHotKey = false;
    DefaultAddStart = null;
    DefaultAddEnd = null;
    UpperCase = false;
    VirtualScroll = false;
    Migration = '';
    ListClass = '';
    ExcelFieldName = '';
    LiteGrid = false;
    ShowDatetimeField = false;
    ShowNull = false;
    AddDate = false;
    FilterEq = false;
    HeaderHeight = null;
    BodyItemHeight = null;
    FooterHeight = null;
    ScrollHeight = null;
    ScriptValidation = '';
    FilterLocal = false;
    HideGrid = false;
    GroupReferenceId = '';
    GroupReferenceName = '';
    GroupName = '';
    Description = '';
    FeatureId = '';
    EntityId = '';
    EntityName = '';
    ComponentId = '';
    TextAlign = '';
    HasFilter = false;
    Frozen = false;
    FilterTemplate = '';
    Editable = false;
    FormatExcell = '';
    DatabaseName = '';
    Summary = '';
    SummaryColSpan = null;
    IsExport = false;
    OrderExport = null;
    ShowExp = '';
    MinWidth = '';
    MaxWidth = '';
    AdvancedSearch = false;
    AutoFit = false;
    DisplayNone = false;
    FieldText = '';
    OrderBy = '';
    QueueName = '';
    MetaConn = '';
    DataConn = '';
    ShouldSaveText = false;
    CacheName = '';
    Lang = '';
    DelCmd = '';
    DelParam = '';
    DisplayField = '';
    DisplayDetail = '';
    TextAlignEnum = null;
    IsPivot = false;
    PostOrder = 0;
    LocalData = [];
    /** @type {Component[]} */
    LocalHeader = [];
    StatusBar = false;
    SimpleText = false;
    LocalRender = false;
    IgnoreConfirmHardDelete = false;
    ComponentGroup = null;
    Reference = null;
    /**
     * Represents whether the component is a tab.
     * @type {boolean}
     */
    IsTab;

    /**
     * Represents the group to which the tab belongs.
     * @type {string}
     */
    TabGroup;

    /**
     * Represents whether the tab is vertical.
     * @type {boolean}
     */
    IsVerticalTab;

    /**
     * Represents whether the component is responsive.
     * @type {boolean}
     */
    Responsive;

    /**
     * Represents the policy ID associated with the component.
     * @type {string}
     */
    PolicyId;

    /**
     * Represents the parent of the component.
     * @type {Component}
     */
    Parent;

    /**
     * Represents the parent of the component.
     * @type {Component[]}
     */
    Component = [];

    /**
     * Represents the parent ID of the component.
     * @type {string}
     */
    ParentId;

    /**
     * Represents the number of outer columns.
     * @type {number | null}
     */
    OuterColumn;

    /**
     * Represents the number of outer columns for extra small screens.
     * @type {number | null}
     */
    XsOuterColumn;

    /**
     * Represents the number of outer columns for small screens.
     * @type {number | null}
     */
    SmOuterColumn;

    /**
     * Represents the number of outer columns for large screens.
     * @type {number | null}
     */
    LgOuterColumn;

    /**
     * Represents the number of outer columns for extra large screens.
     * @type {number | null}
     */
    XlOuterColumn;

    /**
     * Represents the number of outer columns for extra extra large screens.
     * @type {number | null}
     */
    XxlOuterColumn;

    /**
     * Represents the month for which a badge is displayed.
     * @type {number | null}
     */
    BadgeMonth;

    /**
     * Represents whether the component is a drop-down.
     * @type {boolean}
     */
    IsDropDown;

    /**
     * Represents the number of items in a row.
     * @type {number | null}
     */
    ItemInRow;

    /**
     * Represents the HTML content of the component.
     * @type {string}
     */
    Html;

    /**
     * Represents the CSS styles of the component.
     * @type {string}
     */
    Css;

    /**
     * Represents the JavaScript code associated with the component.
     * @type {string}
     */
    Javascript;
    
    /**
     * If set to true, then not to encode the entire children
     * @type {Boolean}
     */
    IgnoreEncode;
    /** @type {FeaturePolicy[]} */
    FeaturePolicy = [];

    /** @type {Component[]} */
    Children = [];
};