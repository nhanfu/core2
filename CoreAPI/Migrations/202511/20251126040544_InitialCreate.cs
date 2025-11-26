using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAPI.Migrations._202511
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Component",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: true),
                    ComponentType = table.Column<string>(type: "text", nullable: true),
                    ComponentGroupId = table.Column<string>(type: "text", nullable: true),
                    FormatData = table.Column<string>(type: "text", nullable: true),
                    PlainText = table.Column<string>(type: "text", nullable: true),
                    Column = table.Column<int>(type: "integer", nullable: true),
                    RowSpan = table.Column<int>(type: "integer", nullable: true),
                    Offset = table.Column<int>(type: "integer", nullable: true),
                    Row = table.Column<int>(type: "integer", nullable: true),
                    CanSearch = table.Column<bool>(type: "boolean", nullable: false),
                    CanCache = table.Column<bool>(type: "boolean", nullable: false),
                    Precision = table.Column<int>(type: "integer", nullable: true),
                    GroupBy = table.Column<string>(type: "text", nullable: true),
                    GroupFormat = table.Column<string>(type: "text", nullable: true),
                    Label = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShowLabel = table.Column<bool>(type: "boolean", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    ClassName = table.Column<string>(type: "text", nullable: true),
                    Style = table.Column<string>(type: "text", nullable: true),
                    ChildStyle = table.Column<string>(type: "text", nullable: true),
                    HotKey = table.Column<string>(type: "text", nullable: true),
                    RefClass = table.Column<string>(type: "text", nullable: true),
                    Events = table.Column<string>(type: "text", nullable: true),
                    Disabled = table.Column<bool>(type: "boolean", nullable: false),
                    Visibility = table.Column<bool>(type: "boolean", nullable: false),
                    Validation = table.Column<string>(type: "text", nullable: true),
                    Focus = table.Column<bool>(type: "boolean", nullable: false),
                    Width = table.Column<string>(type: "text", nullable: true),
                    PopulateField = table.Column<string>(type: "text", nullable: true),
                    GroupEvent = table.Column<string>(type: "text", nullable: true),
                    XsCol = table.Column<int>(type: "integer", nullable: true),
                    SmCol = table.Column<int>(type: "integer", nullable: true),
                    LgCol = table.Column<int>(type: "integer", nullable: true),
                    XlCol = table.Column<int>(type: "integer", nullable: true),
                    XxlCol = table.Column<int>(type: "integer", nullable: true),
                    DefaultVal = table.Column<string>(type: "text", nullable: true),
                    DateTimeField = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InsertedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InsertedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    CanAdd = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    MonthCount = table.Column<int>(type: "integer", nullable: true),
                    Query = table.Column<string>(type: "text", nullable: true),
                    IsRealtime = table.Column<bool>(type: "boolean", nullable: false),
                    RefName = table.Column<string>(type: "text", nullable: true),
                    TopEmpty = table.Column<bool>(type: "boolean", nullable: false),
                    IsCollapsible = table.Column<bool>(type: "boolean", nullable: false),
                    Template = table.Column<string>(type: "text", nullable: true),
                    PreQuery = table.Column<string>(type: "text", nullable: true),
                    DisabledExp = table.Column<string>(type: "text", nullable: true),
                    FocusSearch = table.Column<bool>(type: "boolean", nullable: false),
                    IsSumary = table.Column<bool>(type: "boolean", nullable: false),
                    FormatSumaryField = table.Column<string>(type: "text", nullable: true),
                    OrderBySumary = table.Column<string>(type: "text", nullable: true),
                    ShowHotKey = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultAddStart = table.Column<int>(type: "integer", nullable: true),
                    DefaultAddEnd = table.Column<int>(type: "integer", nullable: true),
                    UpperCase = table.Column<bool>(type: "boolean", nullable: false),
                    Migration = table.Column<string>(type: "text", nullable: true),
                    ListClass = table.Column<string>(type: "text", nullable: true),
                    ExcelFieldName = table.Column<string>(type: "text", nullable: true),
                    LiteGrid = table.Column<bool>(type: "boolean", nullable: false),
                    ShowDatetimeField = table.Column<bool>(type: "boolean", nullable: false),
                    ShowNull = table.Column<bool>(type: "boolean", nullable: false),
                    AddDate = table.Column<bool>(type: "boolean", nullable: false),
                    FilterEq = table.Column<bool>(type: "boolean", nullable: false),
                    HeaderHeight = table.Column<int>(type: "integer", nullable: true),
                    BodyItemHeight = table.Column<int>(type: "integer", nullable: true),
                    FooterHeight = table.Column<int>(type: "integer", nullable: true),
                    ScrollHeight = table.Column<int>(type: "integer", nullable: true),
                    ScriptValidation = table.Column<string>(type: "text", nullable: true),
                    FilterLocal = table.Column<bool>(type: "boolean", nullable: false),
                    HideGrid = table.Column<bool>(type: "boolean", nullable: false),
                    GroupReferenceId = table.Column<string>(type: "text", nullable: true),
                    GroupReferenceName = table.Column<string>(type: "text", nullable: true),
                    GroupName = table.Column<string>(type: "text", nullable: true),
                    ShortDesc = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FeatureId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ComponentId = table.Column<string>(type: "text", nullable: true),
                    TextAlign = table.Column<string>(type: "text", nullable: true),
                    HasFilter = table.Column<bool>(type: "boolean", nullable: false),
                    Frozen = table.Column<bool>(type: "boolean", nullable: false),
                    FilterTemplate = table.Column<string>(type: "text", nullable: true),
                    Editable = table.Column<bool>(type: "boolean", nullable: false),
                    FormatExcell = table.Column<string>(type: "text", nullable: true),
                    DatabaseName = table.Column<string>(type: "text", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    SummaryColSpan = table.Column<int>(type: "integer", nullable: true),
                    BasicSearch = table.Column<bool>(type: "boolean", nullable: false),
                    ShowExp = table.Column<string>(type: "text", nullable: true),
                    MinWidth = table.Column<string>(type: "text", nullable: true),
                    MaxWidth = table.Column<string>(type: "text", nullable: true),
                    TenantCode = table.Column<string>(type: "text", nullable: true),
                    OrderBy = table.Column<string>(type: "text", nullable: true),
                    ParentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Lang = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    IsTab = table.Column<bool>(type: "boolean", nullable: false),
                    TabGroup = table.Column<string>(type: "text", nullable: true),
                    IsVertialTab = table.Column<bool>(type: "boolean", nullable: false),
                    Responsive = table.Column<bool>(type: "boolean", nullable: false),
                    OuterColumn = table.Column<int>(type: "integer", nullable: true),
                    XsOuterColumn = table.Column<int>(type: "integer", nullable: true),
                    SmOuterColumn = table.Column<int>(type: "integer", nullable: true),
                    LgOuterColumn = table.Column<int>(type: "integer", nullable: true),
                    XlOuterColumn = table.Column<int>(type: "integer", nullable: true),
                    XxlOuterColumn = table.Column<int>(type: "integer", nullable: true),
                    BadgeMonth = table.Column<int>(type: "integer", nullable: true),
                    IsDropDown = table.Column<bool>(type: "boolean", nullable: false),
                    Html = table.Column<string>(type: "text", nullable: true),
                    Css = table.Column<string>(type: "text", nullable: true),
                    Javascript = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    EntityId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DisplayBadge = table.Column<bool>(type: "boolean", nullable: false),
                    IsMultiple = table.Column<bool>(type: "boolean", nullable: false),
                    AddRowExp = table.Column<string>(type: "text", nullable: true),
                    GroupTypeId = table.Column<string>(type: "text", nullable: true),
                    VirtualScroll = table.Column<bool>(type: "boolean", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: true),
                    ComponentDefaultValueId = table.Column<string>(type: "text", nullable: true),
                    TableName = table.Column<string>(type: "text", nullable: true),
                    ReportTypeId = table.Column<int>(type: "integer", nullable: true),
                    Index = table.Column<int>(type: "integer", nullable: true),
                    CodeId = table.Column<string>(type: "text", nullable: true),
                    ExcelUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Component", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Entity",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AliasFor = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InsertedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RefDetailClass = table.Column<string>(type: "text", nullable: true),
                    RefListClass = table.Column<string>(type: "text", nullable: true),
                    Namespace = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feature",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Label = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ParentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: true),
                    ClassName = table.Column<string>(type: "text", nullable: true),
                    Style = table.Column<string>(type: "text", nullable: true),
                    StyleSheet = table.Column<string>(type: "text", nullable: true),
                    Script = table.Column<string>(type: "text", nullable: true),
                    Events = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    IsDevider = table.Column<bool>(type: "boolean", nullable: false),
                    IsGroup = table.Column<bool>(type: "boolean", nullable: false),
                    IsMenu = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    StartUp = table.Column<bool>(type: "boolean", nullable: false),
                    ViewClass = table.Column<string>(type: "text", nullable: true),
                    EntityId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: true),
                    InsertedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InsertedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IgnoreEncode = table.Column<bool>(type: "boolean", nullable: false),
                    InheritParentFeature = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteTemp = table.Column<bool>(type: "boolean", nullable: false),
                    CustomNextCell = table.Column<bool>(type: "boolean", nullable: false),
                    LoadEntity = table.Column<bool>(type: "boolean", nullable: false),
                    IsLock = table.Column<bool>(type: "boolean", nullable: false),
                    IsFlow = table.Column<bool>(type: "boolean", nullable: false),
                    CodeId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feature", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeaturePolicy",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FeatureId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RoleId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CanRead = table.Column<bool>(type: "boolean", nullable: false),
                    CanReadAll = table.Column<bool>(type: "boolean", nullable: false),
                    CanWrite = table.Column<bool>(type: "boolean", nullable: false),
                    CanWriteAll = table.Column<bool>(type: "boolean", nullable: false),
                    CanDelete = table.Column<bool>(type: "boolean", nullable: false),
                    CanDeleteAll = table.Column<bool>(type: "boolean", nullable: false),
                    CanCopy = table.Column<bool>(type: "boolean", nullable: false),
                    CanCopyAll = table.Column<bool>(type: "boolean", nullable: false),
                    CanDeactivate = table.Column<bool>(type: "boolean", nullable: false),
                    CanDeactivateAll = table.Column<bool>(type: "boolean", nullable: false),
                    RecordId = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    TableName = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InsertedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InsertedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturePolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Partner",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    TypeId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    DebitName = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Mail = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InsertedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InsertedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TaxCode = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    GroupId = table.Column<string>(type: "text", nullable: true),
                    GenderId = table.Column<string>(type: "text", nullable: true),
                    IssuedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    GenderContactId = table.Column<string>(type: "text", nullable: true),
                    ContactName = table.Column<string>(type: "text", nullable: true),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    ContactPhoneNumber = table.Column<string>(type: "text", nullable: true),
                    DebitDay = table.Column<int>(type: "integer", nullable: true),
                    DebitAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    DebitAccountId = table.Column<string>(type: "text", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    Web = table.Column<string>(type: "text", nullable: true),
                    SeqKey = table.Column<int>(type: "integer", nullable: true),
                    SaleId = table.Column<string>(type: "text", nullable: true),
                    Attachment = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SourseId = table.Column<string>(type: "text", nullable: true),
                    RaitingId = table.Column<string>(type: "text", nullable: true),
                    Dob = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PicId = table.Column<string>(type: "text", nullable: true),
                    CustomerTypeId = table.Column<string>(type: "text", nullable: true),
                    CompanyName = table.Column<string>(type: "text", nullable: true),
                    ResidenceTypeId = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CompanyNameInv = table.Column<string>(type: "text", nullable: true),
                    EmailInv = table.Column<string>(type: "text", nullable: true),
                    AssignmentDebitId = table.Column<string>(type: "text", nullable: true),
                    AssignmentInvId = table.Column<string>(type: "text", nullable: true),
                    AccNumber = table.Column<string>(type: "text", nullable: true),
                    AccName = table.Column<string>(type: "text", nullable: true),
                    BankId = table.Column<string>(type: "text", nullable: true),
                    SwiftCode = table.Column<string>(type: "text", nullable: true),
                    IdCode = table.Column<string>(type: "text", nullable: true),
                    Password = table.Column<string>(type: "text", nullable: true),
                    Industry = table.Column<string>(type: "text", nullable: true),
                    Po = table.Column<string>(type: "text", nullable: true),
                    RushMonth = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<string>(type: "text", nullable: true),
                    PoDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MinProfitMonth = table.Column<decimal>(type: "numeric", nullable: true),
                    Warning = table.Column<string>(type: "text", nullable: true),
                    ActionId = table.Column<int>(type: "integer", nullable: true),
                    RegularShippingFrom = table.Column<string>(type: "text", nullable: true),
                    ConditionId = table.Column<string>(type: "text", nullable: true),
                    DistributeInformationIds = table.Column<string>(type: "text", nullable: true),
                    DistributeInformationIdsText = table.Column<string>(type: "text", nullable: true),
                    ZipCode = table.Column<string>(type: "text", nullable: true),
                    TrackingURL = table.Column<string>(type: "text", nullable: true),
                    IsNoDebt = table.Column<bool>(type: "boolean", nullable: false),
                    PartnerTypeIds = table.Column<string>(type: "text", nullable: true),
                    AdditionTypeIds = table.Column<string>(type: "text", nullable: true),
                    PartnerTypeIdsText = table.Column<string>(type: "text", nullable: true),
                    AdditionTypeIdsText = table.Column<string>(type: "text", nullable: true),
                    FormatChat = table.Column<string>(type: "text", nullable: true),
                    AddressInv = table.Column<string>(type: "text", nullable: true),
                    AssignId = table.Column<string>(type: "text", nullable: true),
                    History = table.Column<string>(type: "text", nullable: true),
                    Birthday = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Logo = table.Column<string>(type: "text", nullable: true),
                    Header = table.Column<string>(type: "text", nullable: true),
                    Footer = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partner", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resource",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Annonymous = table.Column<bool>(type: "boolean", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InsertedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resource", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InsertedBy = table.Column<string>(type: "text", nullable: true),
                    InsertedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubTenant = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Env = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConnKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ConnStr = table.Column<string>(type: "text", nullable: true),
                    Area = table.Column<string>(type: "text", nullable: true),
                    Template = table.Column<string>(type: "text", nullable: true),
                    SvcId = table.Column<string>(type: "text", nullable: true),
                    Css = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InsertedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Password = table.Column<string>(type: "text", nullable: true),
                    CompanyId = table.Column<string>(type: "text", nullable: true),
                    Salt = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    Ssn = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TeamId = table.Column<string>(type: "text", nullable: true),
                    PartnerId = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InsertedBy = table.Column<string>(type: "text", nullable: true),
                    InsertedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Dob = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GenderId = table.Column<string>(type: "text", nullable: true),
                    SsnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TaxCode = table.Column<string>(type: "text", nullable: true),
                    SeqKey = table.Column<int>(type: "integer", nullable: true),
                    TypeId = table.Column<int>(type: "integer", nullable: true),
                    LoginFailedCount = table.Column<int>(type: "integer", nullable: true),
                    LastFailedLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Recover = table.Column<string>(type: "text", nullable: true),
                    JointDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RoleIds = table.Column<string>(type: "text", nullable: true),
                    RoleIdsText = table.Column<string>(type: "text", nullable: true),
                    DepartmentId = table.Column<string>(type: "text", nullable: true),
                    IdentityCardDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdentityCard = table.Column<string>(type: "text", nullable: true),
                    PlaceIssue = table.Column<string>(type: "text", nullable: true),
                    NickName = table.Column<string>(type: "text", nullable: true),
                    Knowledge = table.Column<string>(type: "text", nullable: true),
                    TargetLocalCurr = table.Column<decimal>(type: "numeric", nullable: true),
                    TargetUSDCurr = table.Column<decimal>(type: "numeric", nullable: true),
                    TypeBonusId = table.Column<string>(type: "text", nullable: true),
                    SalaryAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    SalaryCoefficient = table.Column<decimal>(type: "numeric", nullable: true),
                    InsuranceAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    TypeContractId = table.Column<string>(type: "text", nullable: true),
                    Dependents = table.Column<int>(type: "integer", nullable: true),
                    AccNumber = table.Column<string>(type: "text", nullable: true),
                    AccName = table.Column<string>(type: "text", nullable: true),
                    BankId = table.Column<string>(type: "text", nullable: true),
                    PassEmail = table.Column<string>(type: "text", nullable: true),
                    Ip = table.Column<string>(type: "text", nullable: true),
                    IsDepartment = table.Column<bool>(type: "boolean", nullable: false),
                    IsTeam = table.Column<bool>(type: "boolean", nullable: false),
                    PositionId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RoleId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InsertedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSetting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FeatureId = table.Column<string>(type: "text", nullable: true),
                    ComponentId = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InsertedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InsertedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetting", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feature_ParentId",
                table: "Feature",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_TenantCode",
                table: "Tenant",
                column: "TenantCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_User_UserName",
                table: "User",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                table: "UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_UserId",
                table: "UserRole",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Component");

            migrationBuilder.DropTable(
                name: "Entity");

            migrationBuilder.DropTable(
                name: "Feature");

            migrationBuilder.DropTable(
                name: "FeaturePolicy");

            migrationBuilder.DropTable(
                name: "Partner");

            migrationBuilder.DropTable(
                name: "Resource");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Tenant");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "UserSetting");
        }
    }
}
