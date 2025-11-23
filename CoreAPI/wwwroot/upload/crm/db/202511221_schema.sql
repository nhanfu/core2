create database crm
go
use crm
go
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AccountNo](
	[Id] [varchar](50) NOT NULL,
	[ParentId] [varchar](50) NULL,
	[No] [int] NULL,
	[Code] [nvarchar](500) NULL,
	[Name] [nvarchar](500) NULL,
	[NameEnglish] [nvarchar](500) NULL,
	[TypeId] [int] NULL,
	[CurrencyId] [varchar](50) NULL,
	[OgCurrencyId] [varchar](50) NULL,
	[CurrencyCode] [varchar](50) NULL,
	[Reason] [nvarchar](500) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[IsLast] [bit] NOT NULL,
 CONSTRAINT [PK_AccountNo] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AccountTransferSetup]    Script Date: 9/15/2025 4:05:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AccountTransferSetup](
	[Id] [varchar](50) NOT NULL,
	[Code] [varchar](50) NULL,
	[Priority] [int] NULL,
	[FromAccountNoId] [varchar](50) NULL,
	[ToAccountNoId] [varchar](50) NULL,
	[TransferSideId] [int] NULL,
	[Description] [nvarchar](250) NULL,
	[CompanyId] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_AccountTransferSetup] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ApprovalConfig]    Script Date: 9/15/2025 4:05:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ApprovalConfig](
	[Id] [varchar](50) NOT NULL,
	[VoucherTypeId] [int] NULL,
	[ParentId] [varchar](50) NULL,
	[NameId] [varchar](50) NULL,
	[Level] [int] NULL,
	[UserIds] [varchar](1500) NULL,
	[RoleIds] [varchar](1500) NULL,
	[IsDepartment] [bit] NOT NULL,
	[IsTeam] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ApprovalConfig] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Approvement]    Script Date: 9/15/2025 4:05:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Approvement](
	[Id] [varchar](50) NOT NULL,
	[ReasonOfChange] [nvarchar](250) NULL,
	[Name] [varchar](250) NULL,
	[RecordId] [varchar](50) NULL,
	[StatusId] [int] NULL,
	[UserApproveId] [varchar](50) NULL,
	[IsEnd] [bit] NOT NULL,
	[NextLevel] [int] NULL,
	[CurrentLevel] [int] NULL,
	[Approved] [bit] NOT NULL,
	[ApprovedBy] [varchar](50) NULL,
	[ApprovedDate] [datetime2](7) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_Approvement] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Asset]    Script Date: 9/15/2025 4:05:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Asset](
	[Id] [varchar](50) NOT NULL,
	[Code] [varchar](50) NULL,
	[UnitId] [varchar](50) NULL,
	[Name] [nvarchar](255) NULL,
	[AccGroupId] [varchar](50) NULL,
	[GroupCode] [varchar](50) NULL,
	[CountryId] [varchar](50) NULL,
	[Year] [int] NULL,
	[TypeText] [nvarchar](255) NULL,
	[DepartmentId] [varchar](50) NULL,
	[ReasonChange] [nvarchar](255) NULL,
	[ChangeDate] [datetime2](7) NULL,
	[AccAmoutId] [varchar](50) NULL,
	[Amount] [money] NULL,
	[AccCostId] [varchar](50) NULL,
	[DepAmount] [money] NULL,
	[DepFromDate] [datetime2](7) NULL,
	[UseMonth] [money] NULL,
	[RemainMonth] [money] NULL,
	[DepToDate] [datetime2](7) NULL,
	[AccDepId] [varchar](50) NULL,
	[DepAmountMonth] [money] NULL,
	[DescriptionId] [varchar](50) NULL,
	[DescriptionIdText] [nvarchar](500) NULL,
	[Accumulated] [money] NULL,
	[DepMonth] [money] NULL,
	[DepreciatedAmount] [money] NULL,
	[RemainAmount] [money] NULL,
	[IsDisposal] [bit] NOT NULL,
	[WarrantyPeriod] [money] NULL,
	[PartnerId] [varchar](50) NULL,
	[AttachedFile] [nvarchar](500) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_Asset] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Booking]    Script Date: 9/15/2025 4:05:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Booking](
	[Id] [varchar](50) NOT NULL,
	[TypeId] [int] NULL,
	[CreatedOn] [datetime2](7) NULL,
	[BookingNo] [nvarchar](250) NULL,
	[BookingDate] [datetime2](7) NULL,
	[CustomerId] [varchar](50) NULL,
	[PolId] [varchar](50) NULL,
	[PodId] [varchar](50) NULL,
	[ShipperId] [varchar](50) NULL,
	[ConsigneeId] [varchar](50) NULL,
	[Volume] [money] NULL,
	[PickupDate] [datetime2](7) NULL,
	[DropOffDate] [datetime2](7) NULL,
	[StatusId] [int] NULL,
	[Pic] [nvarchar](500) NULL,
	[PhoneNumber] [nvarchar](250) NULL,
	[PaymentTermId] [varchar](50) NULL,
	[DeliveryTerm] [nvarchar](250) NULL,
	[IncotermId] [varchar](50) NULL,
	[ShipmentTypeId] [int] NULL,
	[ServiceId] [int] NULL,
	[HblNo] [nvarchar](250) NULL,
	[HblTypeId] [varchar](50) NULL,
	[CommodityId] [varchar](50) NULL,
	[ServiceText] [nvarchar](250) NULL,
	[SaleId] [varchar](50) NULL,
	[Quantity] [money] NULL,
	[UnitId] [varchar](50) NULL,
	[CBM] [money] NULL,
	[ShipperDes] [nvarchar](500) NULL,
	[ConsigneeDes] [nvarchar](500) NULL,
	[PortTransitId] [varchar](50) NULL,
	[PlaceDeliveryId] [varchar](50) NULL,
	[PickupAddress] [nvarchar](500) NULL,
	[DropOffId] [varchar](50) NULL,
	[PlaceStuffingId] [varchar](50) NULL,
	[PlaceStuffingText] [nvarchar](500) NULL,
	[ContactStuffing] [nvarchar](500) NULL,
	[PlaceStuffingDate] [datetime2](7) NULL,
	[LadenOnBoardDate] [datetime2](7) NULL,
	[DocsCutOffDate] [datetime2](7) NULL,
	[SiCutOffDate] [datetime2](7) NULL,
	[VgmCutOffDate] [datetime2](7) NULL,
	[CyCutOffDate] [datetime2](7) NULL,
	[EmptyReturnDate] [datetime2](7) NULL,
	[FinalDestinationId] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[EtdDate] [datetime2](7) NULL,
	[EtaDate] [datetime2](7) NULL,
	[EtdTsDate] [datetime2](7) NULL,
	[EtaTsDate] [datetime2](7) NULL,
	[ClosingTimeDate] [datetime2](7) NULL,
	[FreightRate] [money] NULL,
	[DemFree] [money] NULL,
	[DetFree] [money] NULL,
	[DeliveryAddress] [nvarchar](500) NULL,
	[Remark] [nvarchar](1500) NULL,
	[RateComfirm] [money] NULL,
	[Dimension] [nvarchar](500) NULL,
	[PoNo] [varchar](50) NULL,
	[WareHouseId] [varchar](50) NULL,
	[CutOffDate] [datetime2](7) NULL,
	[SpecialRequest] [nvarchar](500) NULL,
	[DetailsGoods] [nvarchar](500) NULL,
	[CarrierId] [varchar](50) NULL,
	[PicVendor] [nvarchar](500) NULL,
	[ReferenceNo] [varchar](50) NULL,
	[Vessel] [nvarchar](500) NULL,
	[VesselVoy] [nvarchar](500) NULL,
	[OceanVessel] [nvarchar](500) NULL,
	[OceanVesselVoy] [nvarchar](500) NULL,
	[PickupId] [varchar](50) NULL,
	[PickupAt] [nvarchar](500) NULL,
	[DropOffAt] [nvarchar](500) NULL,
	[Note] [nvarchar](500) NULL,
	[SeqKey] [int] NULL,
	[VolumeWeight] [money] NULL,
	[Kgs] [money] NULL,
	[CW] [money] NULL,
	[GW] [money] NULL,
	[TypeMoveId] [varchar](50) NULL,
	[VendorId] [varchar](50) NULL,
	[ContPickupAtId] [varchar](50) NULL,
	[ContReturnAtId] [varchar](50) NULL,
	[MblNo] [varchar](50) NULL,
	[MblTypeId] [varchar](50) NULL,
	[FileNo] [varchar](50) NULL,
	[RefNo] [varchar](50) NULL,
	[AttachedFile] [nvarchar](500) NULL,
	[Notes] [nvarchar](500) NULL,
	[ReceiverIds] [varchar](250) NULL,
	[CheckedBy] [varchar](50) NULL,
	[FlightNo] [varchar](50) NULL,
	[FlightNoConnect] [varchar](50) NULL,
	[AgentId] [varchar](50) NULL,
	[InvNo] [varchar](50) NULL,
	[CustomsId] [varchar](50) NULL,
	[TruckingTypeId] [varchar](50) NULL,
	[DeliveryId] [varchar](50) NULL,
	[DeliveryDate] [datetime2](7) NULL,
	[VoucherTypeId] [int] NULL,
	[Url] [nvarchar](500) NULL,
	[FormatChat] [nvarchar](500) NULL,
	[UserViewIds] [nvarchar](500) NULL,
	[UserApprovedIds] [nvarchar](500) NULL,
	[ForwardId] [varchar](50) NULL,
	[BookingLocalId] [varchar](50) NULL,
	[BookingNoteId] [varchar](50) NULL,
	[BookingOrderId] [varchar](50) NULL,
	[IsSend] [bit] NOT NULL,
	[NoApproved] [bit] NOT NULL,
	[PlaceReceiptId] [varchar](50) NULL,
	[ShipmentId] [varchar](50) NULL,
	[ShipmentDetailId] [varchar](50) NULL,
	[QuotationId] [varchar](50) NULL,
	[QuotationCode] [varchar](50) NULL,
	[FeatureName] [nvarchar](500) NULL,
 CONSTRAINT [PK_Booking] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UC_BookingNo] UNIQUE NONCLUSTERED 
(
	[BookingNo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BookingDetail]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BookingDetail](
	[Id] [varchar](50) NOT NULL,
	[BookingId] [varchar](50) NULL,
	[FromId] [varchar](50) NULL,
	[ToId] [varchar](50) NULL,
	[Flight] [nvarchar](250) NULL,
	[EtdDate] [datetime2](7) NULL,
	[EtaDate] [datetime2](7) NULL,
	[GroupFee] [nvarchar](250) NULL,
	[OtherFee] [bit] NOT NULL,
	[VendorId] [varchar](50) NULL,
	[DescriptionId] [varchar](50) NULL,
	[Order] [int] NULL,
	[IsContainer] [bit] NOT NULL,
	[IsCBM] [bit] NOT NULL,
	[IsFreight] [bit] NOT NULL,
	[IsKGS] [bit] NOT NULL,
	[IsGW] [bit] NOT NULL,
	[IsGrossWeight] [bit] NOT NULL,
	[Quantity] [money] NULL,
	[UnitId] [varchar](50) NULL,
	[TotalAmountTax] [money] NULL,
	[TotalAmount] [money] NULL,
	[AmountTax] [money] NULL,
	[CurrencyId] [varchar](50) NULL,
	[CurrencyCode] [varchar](10) NULL,
	[ExchangeRateVND] [decimal](30, 24) NULL,
	[ExchangeRateUSD] [decimal](30, 24) NULL,
	[Vat] [money] NULL,
	[Amount] [money] NULL,
	[IsBuying] [bit] NOT NULL,
	[IsObh] [bit] NOT NULL,
	[IsCM] [bit] NOT NULL,
	[Note] [nvarchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[IsLogistics] [bit] NOT NULL,
	[IsTrucking] [bit] NOT NULL,
	[IsAdv] [bit] NOT NULL,
	[IsAdvCustomer] [bit] NOT NULL,
	[IsAdvProvider] [bit] NOT NULL,
 CONSTRAINT [PK_BookingDetail] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ChatEntity]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChatEntity](
	[Id] [varchar](50) NOT NULL,
	[Icon] [nvarchar](400) NULL,
	[Avatar] [nvarchar](400) NULL,
	[TableName] [varchar](100) NULL,
	[Name] [nvarchar](500) NULL,
	[RecordId] [varchar](50) NULL,
	[FromId] [varchar](50) NULL,
	[ToId] [varchar](50) NULL,
	[TextContent] [nvarchar](500) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ChatEntity] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Cluster]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Cluster](
	[Id] [varchar](50) NOT NULL,
	[TenantCode] [varbinary](150) NULL,
	[Env] [varchar](250) NULL,
	[Host] [varchar](250) NULL,
	[Port] [int] NULL,
	[Scheme] [varchar](250) NULL,
	[ClusterRole] [varchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
 CONSTRAINT [PK_Cluster] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Component]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Component](
	[Id] [varchar](50) NOT NULL,
	[FieldName] [varchar](250) NULL,
	[Order] [int] NULL,
	[ComponentType] [nvarchar](500) NULL,
	[ComponentGroupId] [varchar](50) NULL,
	[FormatData] [nvarchar](max) NULL,
	[PlainText] [nvarchar](250) NULL,
	[Column] [int] NULL,
	[RowSpan] [int] NULL,
	[Offset] [int] NULL,
	[Row] [int] NULL,
	[CanSearch] [bit] NOT NULL,
	[CanCache] [bit] NOT NULL,
	[Precision] [int] NULL,
	[GroupBy] [varchar](100) NULL,
	[GroupFormat] [nvarchar](1500) NULL,
	[Label] [nvarchar](250) NULL,
	[ShowLabel] [bit] NOT NULL,
	[Icon] [varchar](50) NULL,
	[ClassName] [varchar](50) NULL,
	[Style] [varchar](200) NULL,
	[ChildStyle] [varchar](200) NULL,
	[HotKey] [varchar](50) NULL,
	[RefClass] [varchar](200) NULL,
	[Events] [nvarchar](1500) NULL,
	[Disabled] [bit] NOT NULL,
	[Visibility] [bit] NOT NULL,
	[Validation] [nvarchar](1000) NULL,
	[Focus] [bit] NOT NULL,
	[Width] [varchar](20) NULL,
	[PopulateField] [nvarchar](max) NULL,
	[GroupEvent] [varchar](200) NULL,
	[XsCol] [int] NULL,
	[SmCol] [int] NULL,
	[LgCol] [int] NULL,
	[XlCol] [int] NULL,
	[XxlCol] [int] NULL,
	[DefaultVal] [nvarchar](2500) NULL,
	[DateTimeField] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[CanAdd] [bit] NOT NULL,
	[IsPrivate] [bit] NOT NULL,
	[MonthCount] [int] NULL,
	[Query] [nvarchar](max) NULL,
	[IsRealtime] [bit] NOT NULL,
	[RefName] [varchar](250) NULL,
	[TopEmpty] [bit] NOT NULL,
	[IsCollapsible] [bit] NOT NULL,
	[Template] [nvarchar](max) NULL,
	[PreQuery] [nvarchar](max) NULL,
	[DisabledExp] [nvarchar](2000) NULL,
	[FocusSearch] [bit] NOT NULL,
	[IsSumary] [bit] NOT NULL,
	[FormatSumaryField] [nvarchar](250) NULL,
	[OrderBySumary] [nvarchar](250) NULL,
	[ShowHotKey] [bit] NOT NULL,
	[DefaultAddStart] [int] NULL,
	[DefaultAddEnd] [int] NULL,
	[UpperCase] [bit] NOT NULL,
	[Migration] [nvarchar](max) NULL,
	[ListClass] [nvarchar](250) NULL,
	[ExcelFieldName] [nvarchar](1000) NULL,
	[LiteGrid] [bit] NOT NULL,
	[ShowDatetimeField] [bit] NOT NULL,
	[ShowNull] [bit] NOT NULL,
	[AddDate] [bit] NOT NULL,
	[FilterEq] [bit] NOT NULL,
	[HeaderHeight] [int] NULL,
	[BodyItemHeight] [int] NULL,
	[FooterHeight] [int] NULL,
	[ScrollHeight] [int] NULL,
	[ScriptValidation] [nvarchar](max) NULL,
	[FilterLocal] [bit] NOT NULL,
	[HideGrid] [bit] NOT NULL,
	[GroupReferenceId] [varchar](50) NULL,
	[GroupReferenceName] [nvarchar](250) NULL,
	[GroupName] [nvarchar](1000) NULL,
	[ShortDesc] [nvarchar](1000) NULL,
	[Description] [nvarchar](1000) NULL,
	[FeatureId] [varchar](50) NULL,
	[ComponentId] [varchar](50) NULL,
	[TextAlign] [nvarchar](1000) NULL,
	[HasFilter] [bit] NOT NULL,
	[Frozen] [bit] NOT NULL,
	[FilterTemplate] [nvarchar](1000) NULL,
	[Editable] [bit] NOT NULL,
	[FormatExcell] [nvarchar](1000) NULL,
	[DatabaseName] [nvarchar](1000) NULL,
	[Summary] [nvarchar](1000) NULL,
	[SummaryColSpan] [int] NULL,
	[BasicSearch] [bit] NOT NULL,
	[ShowExp] [nvarchar](1000) NULL,
	[MinWidth] [varchar](20) NULL,
	[MaxWidth] [varchar](20) NULL,
	[TenantCode] [varchar](50) NULL,
	[OrderBy] [varchar](500) NULL,
	[ParentId] [varchar](50) NULL,
	[Lang] [varchar](50) NULL,
	[Name] [nvarchar](250) NULL,
	[IsTab] [bit] NOT NULL,
	[TabGroup] [nvarchar](250) NULL,
	[IsVertialTab] [bit] NOT NULL,
	[Responsive] [bit] NOT NULL,
	[OuterColumn] [int] NULL,
	[XsOuterColumn] [int] NULL,
	[SmOuterColumn] [int] NULL,
	[LgOuterColumn] [int] NULL,
	[XlOuterColumn] [int] NULL,
	[XxlOuterColumn] [int] NULL,
	[BadgeMonth] [int] NULL,
	[IsDropDown] [bit] NOT NULL,
	[Html] [nvarchar](max) NULL,
	[Css] [nvarchar](max) NULL,
	[Javascript] [nvarchar](max) NULL,
	[created_by] [nvarchar](255) NULL,
	[EntityId] [varchar](250) NULL,
	[DisplayBadge] [bit] NOT NULL,
	[IsMultiple] [bit] NOT NULL,
	[AddRowExp] [nvarchar](255) NULL,
	[GroupTypeId] [varchar](50) NULL,
	[VirtualScroll] [bit] NOT NULL,
	[EntityName] [varchar](255) NULL,
	[TableName] [varchar](255) NULL,
	[Index] [int] NULL,
	[ExcelUrl] [nvarchar](500) NULL,
	[ReportTypeId] [int] NULL,
	[ComponentDefaultValueId] [varchar](50) NULL,
	[CodeId] [varchar](50) NULL,
 CONSTRAINT [PK_Component] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ComponentDefaultValue]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ComponentDefaultValue](
	[Id] [varchar](50) NOT NULL,
	[ComponentId] [varchar](50) NULL,
	[Value] [nvarchar](max) NULL,
	[UserId] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ComponentDefaultValue] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Config]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Config](
	[Id] [varchar](50) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[P1] [nvarchar](50) NULL,
	[P2] [nvarchar](50) NULL,
	[P3] [nvarchar](50) NULL,
	[P4] [nvarchar](50) NULL,
	[P5] [nvarchar](50) NULL,
	[Increment] [int] NULL,
	[TableName] [nvarchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NOT NULL,
	[InsertedDate] [datetime2](7) NOT NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[ResetTypeId] [varchar](50) NULL,
	[RecordId] [varchar](50) NULL,
	[InsertedByIds] [nvarchar](250) NULL,
	[RoleIds] [nvarchar](250) NULL,
	[GroupKey] [nvarchar](250) NULL,
	[EnumId] [int] NULL,
 CONSTRAINT [PK_Config] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ConfigUnique]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ConfigUnique](
	[Id] [varchar](50) NOT NULL,
	[IsSave] [bit] NOT NULL,
	[ObjectId] [varchar](50) NULL,
	[ObjectText] [nvarchar](250) NULL,
	[ComponentIds] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ConfigUnique] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Conversation]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Conversation](
	[Id] [varchar](50) NOT NULL,
	[Icon] [nvarchar](250) NULL,
	[RecordId] [varchar](50) NULL,
	[EntityId] [nvarchar](150) NULL,
	[IsSend] [bit] NOT NULL,
	[ReceiverIds] [nvarchar](1500) NULL,
	[UserIds] [nvarchar](1500) NULL,
	[Message] [nvarchar](2500) NULL,
	[FormatChat] [nvarchar](2500) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[FeatureName] [nvarchar](1500) NULL,
	[FeatureName2] [nvarchar](1500) NULL,
	[FeatureName3] [nvarchar](1500) NULL,
	[Label] [nvarchar](1500) NULL,
 CONSTRAINT [PK_Conversation] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ConversationDetail]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ConversationDetail](
	[Id] [varchar](50) NOT NULL,
	[ConversationId] [varchar](50) NULL,
	[FromName] [nvarchar](2500) NULL,
	[FromId] [varchar](50) NULL,
	[Avatar] [nvarchar](400) NULL,
	[Message] [nvarchar](2500) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ConversationDetail] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ConversationRead]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ConversationRead](
	[Id] [varchar](50) NOT NULL,
	[ConversationId] [varchar](50) NULL,
	[UserId] [varchar](50) NULL,
	[Read] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ConversationRead] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DefaultFee]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DefaultFee](
	[Id] [varchar](50) NOT NULL,
	[GroupFee] [nvarchar](250) NULL,
	[InquiryDetailId] [varchar](50) NULL,
	[ServiceId] [varchar](50) NULL,
	[VendorId] [varchar](50) NULL,
	[DescriptionId] [varchar](50) NULL,
	[Quantity] [money] NULL,
	[UnitId] [varchar](50) NULL,
	[MinUnitPrice] [money] NULL,
	[CurrencyId] [varchar](50) NULL,
	[Vat] [money] NULL,
	[Tax] [money] NULL,
	[TotalAmountTax] [money] NULL,
	[TotalAmount] [money] NULL,
	[AmountTax] [money] NULL,
	[Amount] [money] NULL,
	[IsObh] [bit] NOT NULL,
	[CmId] [varchar](50) NULL,
	[Note] [nvarchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[InquiryId] [varchar](50) NULL,
	[ExchangeRate] [money] NULL,
	[OtherUnitId] [varchar](50) NULL,
	[Order] [int] NULL,
	[IsContainer] [bit] NOT NULL,
	[IsCBM] [bit] NOT NULL,
	[IsFreight] [bit] NOT NULL,
	[IsLogistics] [bit] NOT NULL,
	[IsTrucking] [bit] NOT NULL,
	[IsKGS] [bit] NOT NULL,
	[IsGW] [bit] NOT NULL,
	[IsGrossWeight] [bit] NOT NULL,
	[ExchangeRateVND] [money] NULL,
	[ExchangeRateUSD] [money] NULL,
 CONSTRAINT [PK_DefaultFee] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Dictionary]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Dictionary](
	[Id] [varchar](50) NOT NULL,
	[LangCode] [nvarchar](250) NULL,
	[Key] [nvarchar](250) NULL,
	[Value] [nvarchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetimeoffset](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetimeoffset](7) NULL,
 CONSTRAINT [PK_Dictionary] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EntityAttached]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntityAttached](
	[Id] [varchar](50) NOT NULL,
	[TableName] [varchar](250) NULL,
	[EntityId] [varchar](50) NULL,
	[Name] [nvarchar](250) NULL,
	[File] [nvarchar](1500) NULL,
	[TypeId] [int] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_EntityAttached] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EntityContainer]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntityContainer](
	[Id] [varchar](50) NOT NULL,
	[TableName] [varchar](250) NULL,
	[EntityId] [varchar](50) NULL,
	[Quantity] [money] NULL,
	[ContainerTypeId] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[ContainerNo] [varchar](50) NULL,
	[SealNo] [varchar](50) NULL,
	[PKG] [money] NULL,
	[Weight] [money] NULL,
	[CBM] [money] NULL,
	[Tare] [money] NULL,
	[Description] [nvarchar](500) NULL,
	[HSCode] [varchar](50) NULL,
	[IsPartial] [bit] NOT NULL,
	[UnitId] [varchar](50) NULL,
 CONSTRAINT [PK_BookingContainer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EntityDimension]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntityDimension](
	[Id] [varchar](50) NOT NULL,
	[TableName] [varchar](250) NULL,
	[EntityId] [varchar](50) NULL,
	[Length] [money] NULL,
	[Width] [money] NULL,
	[Height] [money] NULL,
	[Quantity] [money] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_InquiryDeminsion] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EntityProcess]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntityProcess](
	[Id] [varchar](50) NOT NULL,
	[ServiceId] [varchar](50) NULL,
	[TypeId] [int] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_EntityProcess] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EntityProcessDetail]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntityProcessDetail](
	[Id] [varchar](50) NOT NULL,
	[EntityProcessId] [varchar](50) NULL,
	[TaskName] [nvarchar](500) NULL,
	[IsAuto] [bit] NOT NULL,
	[IsTracing] [bit] NOT NULL,
	[IsDefaultShow] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_EntityProcessDetail] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EntityTransit]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntityTransit](
	[Id] [varchar](50) NOT NULL,
	[TableName] [varchar](250) NULL,
	[FromId] [varchar](50) NULL,
	[ToId] [varchar](50) NULL,
	[PortTransitId] [varchar](50) NULL,
	[EtdTsDate] [datetime2](7) NULL,
	[EtaTsDate] [datetime2](7) NULL,
	[TransitNo] [varchar](50) NULL,
	[EntityId] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_BookingTransit] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Enum]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Enum](
	[Name] [nvarchar](250) NULL,
	[TableName] [varchar](50) NULL,
	[Enum] [int] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_Enum] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ExchangeRate]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExchangeRate](
	[Id] [varchar](50) NOT NULL,
	[CurrencyId] [varchar](50) NULL,
	[CurrencyCode] [varchar](50) NULL,
	[FromDate] [datetime2](7) NULL,
	[ToDate] [datetime2](7) NULL,
	[RateUSD] [decimal](30, 24) NULL,
	[RateVND] [decimal](30, 24) NULL,
	[RateSaleUSD] [decimal](30, 24) NULL,
	[RateSaleVND] [decimal](30, 24) NULL,
	[RateProfitUSD] [decimal](30, 24) NULL,
	[RateProfitVND] [decimal](30, 24) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ExchangeRate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ExchangeRateHistory]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExchangeRateHistory](
	[Id] [varchar](50) NOT NULL,
	[Code] [varchar](50) NULL,
	[FileIds] [nvarchar](max) NULL,
	[PostDate] [datetime2](7) NULL,
	[FromDate] [datetime2](7) NULL,
	[ToDate] [datetime2](7) NULL,
	[CompanyId] [varchar](50) NULL,
	[RateUSD] [decimal](30, 24) NULL,
	[RateVND] [decimal](30, 24) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ExchangeRateHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ExchangeRateHistoryDetail]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExchangeRateHistoryDetail](
	[Id] [varchar](50) NOT NULL,
	[HistoryId] [varchar](50) NULL,
	[CurrencyId] [varchar](50) NULL,
	[CurrencyCode] [varchar](50) NULL,
	[RateUSD] [decimal](30, 24) NULL,
	[RateVND] [decimal](30, 24) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ExchangeRateHistoryDetail] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Feature]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Feature](
	[Id] [nvarchar](50) NOT NULL,
	[Name] [nvarchar](100) NULL,
	[Label] [nvarchar](100) NULL,
	[ParentId] [nvarchar](50) NULL,
	[Order] [int] NULL,
	[ClassName] [nvarchar](50) NULL,
	[Style] [nvarchar](50) NULL,
	[StyleSheet] [nvarchar](max) NULL,
	[Script] [nvarchar](max) NULL,
	[Events] [nvarchar](1500) NULL,
	[Icon] [nvarchar](250) NULL,
	[IsDevider] [bit] NOT NULL,
	[IsGroup] [bit] NOT NULL,
	[IsMenu] [bit] NOT NULL,
	[IsPublic] [bit] NOT NULL,
	[StartUp] [bit] NOT NULL,
	[ViewClass] [nvarchar](150) NULL,
	[EntityId] [nvarchar](50) NULL,
	[Description] [nvarchar](500) NULL,
	[Active] [bit] NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [nvarchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [nvarchar](50) NULL,
	[IsSystem] [bit] NOT NULL,
	[IgnoreEncode] [bit] NOT NULL,
	[InheritParentFeature] [bit] NOT NULL,
	[DeleteTemp] [bit] NOT NULL,
	[CustomNextCell] [bit] NOT NULL,
	[LoadEntity] [bit] NOT NULL,
	[IsLock] [bit] NOT NULL,
	[IsFlow] [bit] NOT NULL,
	[CodeId] [nvarchar](50) NULL,
 CONSTRAINT [PK__Feature__3214EC071D5C24E1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FeaturePolicy]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FeaturePolicy](
	[Id] [varchar](50) NOT NULL,
	[Label] [nvarchar](50) NULL,
	[FeatureId] [varchar](50) NULL,
	[RoleId] [varchar](50) NULL,
	[CanRead] [bit] NOT NULL,
	[CanReadAll] [bit] NOT NULL,
	[CanWrite] [bit] NOT NULL,
	[CanWriteAll] [bit] NOT NULL,
	[CanDelete] [bit] NOT NULL,
	[CanDeleteAll] [bit] NOT NULL,
	[CanCopy] [bit] NOT NULL,
	[CanCopyAll] [bit] NOT NULL,
	[CanDeactivate] [bit] NOT NULL,
	[CanDeactivateAll] [bit] NOT NULL,
	[CanExport] [bit] NOT NULL,
	[RecordId] [varchar](50) NULL,
	[UserId] [varchar](50) NULL,
	[TableName] [varchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_FeaturePolicy] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FeeType]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FeeType](
	[Id] [varchar](50) NOT NULL,
	[Code] [nvarchar](250) NULL,
	[Name] [nvarchar](250) NULL,
	[Description] [nvarchar](250) NULL,
	[GroupId] [varchar](50) NULL,
	[GroupName] [nvarchar](250) NULL,
	[IsFreight] [bit] NOT NULL,
	[OtherUnitId] [varchar](50) NULL,
	[CurrencyId] [varchar](50) NULL,
	[IsObh] [bit] NOT NULL,
	[UnitPrice] [money] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[SeqKey] [int] NULL,
	[IsTrucking] [bit] NOT NULL,
	[IsLogistics] [bit] NOT NULL,
	[IsAdv] [bit] NOT NULL,
	[IsAdvCustomer] [bit] NOT NULL,
	[IsAdvProvider] [bit] NOT NULL,
	[IsCom] [bit] NOT NULL,
	[RevenueAccId] [varchar](50) NULL,
	[CostAccId] [varchar](50) NULL,
	[DebitAccId] [varchar](50) NULL,
	[CreditAccId] [varchar](50) NULL,
 CONSTRAINT [PK_FeeType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FileUpload]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FileUpload](
	[Id] [varchar](50) NOT NULL,
	[EntityName] [nvarchar](250) NULL,
	[RecordId] [varchar](50) NULL,
	[SectionId] [varchar](50) NULL,
	[FieldName] [nvarchar](100) NULL,
	[FileName] [nvarchar](400) NULL,
	[FilePath] [nvarchar](400) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_FileUpload] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[History]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[History](
	[Id] [varchar](50) NOT NULL,
	[TextContent] [nvarchar](max) NULL,
	[Value] [nvarchar](max) NULL,
	[OldValue] [nvarchar](max) NULL,
	[ComponentId] [varchar](50) NULL,
	[RecordId] [varchar](50) NULL,
	[TableName] [varchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[created_by] [nvarchar](255) NULL,
 CONSTRAINT [PK_History] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Inquiry]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Inquiry](
	[Id] [varchar](50) NOT NULL,
	[TypeId] [int] NULL,
	[Code] [varchar](150) NULL,
	[CustomerId] [varchar](50) NULL,
	[PodId] [varchar](50) NULL,
	[PolId] [varchar](50) NULL,
	[DestinationId] [varchar](50) NULL,
	[Quantity] [money] NULL,
	[DeliveryTerm] [nvarchar](250) NULL,
	[CommodityId] [varchar](50) NULL,
	[EtdDate] [datetime2](7) NULL,
	[EtaDate] [datetime2](7) NULL,
	[ContainerTypeId] [varchar](50) NULL,
	[UnitId] [varchar](50) NULL,
	[DeminsionText] [nvarchar](250) NULL,
	[DeminsionLength] [money] NULL,
	[DeminsionWidth] [money] NULL,
	[DeminsionHeight] [money] NULL,
	[DeminsionQuantity] [money] NULL,
	[CW] [money] NULL,
	[GW] [money] NULL,
	[CBM] [money] NULL,
	[Pickup] [nvarchar](500) NULL,
	[Delivery] [nvarchar](500) NULL,
	[Attachment] [nvarchar](500) NULL,
	[SaleId] [varchar](50) NULL,
	[ServiceText] [nvarchar](250) NULL,
	[UserReceiverText] [varchar](50) NULL,
	[GroupReceiverId] [varchar](50) NULL,
	[StatusId] [int] NULL,
	[Note] [nvarchar](500) NULL,
	[SendDate] [datetime2](7) NULL,
	[SendBy] [varchar](50) NULL,
	[ApprovedBy] [varchar](50) NULL,
	[ApprovedDate] [datetime2](7) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[SeqKey] [int] NULL,
	[TotalAmount] [money] NULL,
	[FromDate] [datetime2](7) NULL,
	[ToDate] [datetime2](7) NULL,
	[FreightStateId] [int] NULL,
	[ShipmentTypeId] [int] NULL,
	[ServiceDate] [datetime2](7) NULL,
	[Address] [nvarchar](500) NULL,
	[Pic] [nvarchar](500) NULL,
	[PhoneNumber] [nvarchar](250) NULL,
	[TransitTime] [datetime2](7) NULL,
	[TransitPortId] [varchar](50) NULL,
	[IncotermId] [varchar](50) NULL,
	[ShipperId] [varchar](50) NULL,
	[ShipperIdName] [nvarchar](500) NULL,
	[AgentId] [varchar](50) NULL,
	[AgentIdText] [nvarchar](500) NULL,
	[Subject] [nvarchar](500) NULL,
	[ServiceId] [int] NULL,
	[Cont20Id] [varchar](50) NULL,
	[Cont20x2Id] [varchar](50) NULL,
	[Cont40Id] [varchar](50) NULL,
	[Cont45Id] [varchar](50) NULL,
	[Cont40HCId] [varchar](50) NULL,
	[ConsigneeId] [varchar](50) NULL,
	[ConsigneeIdText] [nvarchar](500) NULL,
	[Remark] [nvarchar](1500) NULL,
	[IsSimple] [bit] NOT NULL,
	[VolumeWeight] [money] NULL,
	[Kgs] [money] NULL,
	[PickupDate] [datetime2](7) NULL,
	[DeliveryDate] [datetime2](7) NULL,
	[EmptyPickupId] [varchar](50) NULL,
	[EmptyReturnId] [varchar](50) NULL,
	[CustomsOfficeId] [varchar](50) NULL,
	[CargoReadyDate] [datetime2](7) NULL,
	[VoucherTypeId] [int] NULL,
	[StatusText] [nvarchar](250) NULL,
	[ExchangeRateVND] [money] NULL,
	[ExchangeRateUSD] [money] NULL,
	[UserReceiverIds] [nvarchar](250) NULL,
	[GroupReceiverIds] [nvarchar](1250) NULL,
	[UserReceiverIdsText] [nvarchar](1250) NULL,
	[GroupReceiverIdsText] [nvarchar](1250) NULL,
	[Url] [nvarchar](500) NULL,
	[FormatChat] [nvarchar](500) NULL,
	[ProgressId] [int] NULL,
	[InquiryDetailId] [varchar](50) NULL,
	[DemFree] [money] NULL,
	[DetFree] [money] NULL,
	[PkEmptyId] [varchar](50) NULL,
	[ReturnsId] [varchar](50) NULL,
	[UserViewIds] [nvarchar](500) NULL,
	[UserApprovedIds] [nvarchar](500) NULL,
	[ForwardId] [varchar](50) NULL,
	[NoApproved] [bit] NOT NULL,
	[BookingLocalId] [varchar](50) NULL,
	[FeatureName] [nvarchar](500) NULL,
 CONSTRAINT [PK_Inquiry] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UC_InquiryCode] UNIQUE NONCLUSTERED 
(
	[Code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[InquiryDetail]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[InquiryDetail](
	[Id] [varchar](50) NOT NULL,
	[DescriptionId] [varchar](50) NULL,
	[OtherFee] [bit] NOT NULL,
	[InquiryId] [varchar](50) NULL,
	[ServiceId] [int] NULL,
	[PodId] [varchar](50) NULL,
	[PolId] [varchar](50) NULL,
	[TotalAmount] [money] NULL,
	[UserReceiverId] [varchar](50) NULL,
	[GroupReceiverId] [varchar](50) NULL,
	[Deadline] [datetime2](7) NULL,
	[IsApproved] [bit] NOT NULL,
	[IsDecline] [bit] NOT NULL,
	[UserApprovedId] [varchar](50) NULL,
	[UserDeclineId] [varchar](50) NULL,
	[Note] [nvarchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[StatusId] [int] NULL,
	[Freq] [nvarchar](250) NULL,
	[CurrencyId] [varchar](50) NULL,
	[CurrencyCode] [varchar](10) NULL,
	[CarrierId] [varchar](50) NULL,
	[VendorId] [varchar](50) NULL,
	[ProviderId] [varchar](50) NULL,
	[PlaceReceiptId] [varchar](50) NULL,
	[PlaceDeliveryId] [varchar](50) NULL,
	[FinalDestinationId] [varchar](50) NULL,
	[ViaId] [varchar](50) NULL,
	[TransitTime] [nvarchar](250) NULL,
	[Cost] [money] NULL,
	[Rate] [money] NULL,
	[Vat] [money] NULL,
	[IsObh] [bit] NOT NULL,
	[GroupFee] [nvarchar](500) NULL,
	[RouteId] [nvarchar](500) NULL,
	[MinLCLCost] [money] NULL,
	[MinLCLRate] [money] NULL,
	[Pickup] [nvarchar](250) NULL,
	[Delivery] [nvarchar](250) NULL,
	[Order] [int] NULL,
	[UnitPrice] [money] NULL,
	[CutOff] [datetime2](7) NULL,
	[PayeeId] [varchar](50) NULL,
	[TruckTypeId] [varchar](50) NULL,
	[UnitId] [varchar](50) NULL,
	[Quantity] [money] NULL,
	[ScheduleIds] [varchar](250) NULL,
	[ScheduleIdsText] [nvarchar](250) NULL,
	[PricingId] [varchar](50) NULL,
	[VoucherTypeId] [int] NULL,
	[EtaDate] [datetime2](7) NULL,
	[EtdDate] [datetime2](7) NULL,
	[FormatChat] [nvarchar](500) NULL,
	[ExchangeRateVND] [decimal](30, 24) NULL,
	[ExchangeRateUSD] [decimal](30, 24) NULL,
	[CW] [money] NULL,
	[GW] [money] NULL,
	[CBM] [money] NULL,
	[IsFreight] [bit] NOT NULL,
	[IsContainer] [bit] NOT NULL,
	[IsCBM] [bit] NOT NULL,
	[IsKGS] [bit] NOT NULL,
	[IsGW] [bit] NOT NULL,
	[ProgressId] [int] NULL,
	[IsSend] [bit] NOT NULL,
	[MinQuantityCost] [money] NULL,
	[MinQuantityRate] [money] NULL,
	[PayerId] [varchar](50) NULL,
	[OtherUnitId] [varchar](50) NULL,
	[IsGrossWeight] [bit] NOT NULL,
	[DisableRow] [bit] NOT NULL,
	[IsLogistics] [bit] NOT NULL,
	[IsTrucking] [bit] NOT NULL,
	[QuotationId] [varchar](50) NULL,
	[FeatureName] [nvarchar](500) NULL,
	[EmptyReturnId] [varchar](50) NULL,
 CONSTRAINT [PK_InquiryDetail] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[InvoiceConfig]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[InvoiceConfig](
	[Id] [varchar](50) NOT NULL,
	[Description] [nvarchar](250) NULL,
	[Form] [nvarchar](250) NULL,
	[SeriNo] [varchar](50) NULL,
	[IsMultiple] [bit] NOT NULL,
	[StartNumer] [int] NULL,
	[EndNumer] [int] NULL,
	[CompanyId] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_InvoiceConfig] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MarkupPricing]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MarkupPricing](
	[Id] [varchar](50) NOT NULL,
	[TypeId] [varchar](50) NULL,
	[Description] [nvarchar](250) NULL,
	[ServiceId] [varchar](50) NULL,
	[PartnerId] [varchar](50) NULL,
	[PolId] [varchar](50) NULL,
	[PodId] [varchar](50) NULL,
	[PriceTypeId] [varchar](50) NULL,
	[MarkupLevel] [money] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_MarkupPricing] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MasterData]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MasterData](
	[Id] [varchar](50) NOT NULL,
	[Code] [nvarchar](500) NULL,
	[Name] [nvarchar](500) NULL,
	[Description] [nvarchar](500) NULL,
	[ParentId] [varchar](50) NULL,
	[Path] [nvarchar](1000) NULL,
	[IsLocal] [bit] NOT NULL,
	[IsContainer] [bit] NOT NULL,
	[IsCBM] [bit] NOT NULL,
	[IsKGS] [bit] NOT NULL,
	[IsGW] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[Enum] [int] NULL,
	[GroupEnum] [int] NULL,
	[CodeMn] [varchar](150) NULL,
 CONSTRAINT [PK_MasterData] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Partner]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Partner](
	[Id] [varchar](50) NOT NULL,
	[ServiceId] [int] NULL,
	[TypeId] [int] NULL,
	[Name] [nvarchar](500) NULL,
	[FullName] [nvarchar](500) NULL,
	[DebitName] [nvarchar](500) NULL,
	[Address] [nvarchar](500) NULL,
	[PhoneNumber] [varchar](50) NULL,
	[Mail] [nvarchar](200) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[Code] [varchar](250) NULL,
	[TaxCode] [varchar](250) NULL,
	[GroupId] [varchar](50) NULL,
	[GenderId] [varchar](50) NULL,
	[IssuedDate] [datetime2](7) NULL,
	[Note] [nvarchar](500) NULL,
	[GenderContactId] [varchar](50) NULL,
	[ContactName] [nvarchar](500) NULL,
	[ContactEmail] [nvarchar](255) NULL,
	[ContactPhoneNumber] [varchar](50) NULL,
	[DebitDay] [int] NULL,
	[DebitAmount] [money] NULL,
	[CreditLimit] [money] NULL,
	[DebitAccountId] [varchar](50) NULL,
	[IsPublic] [bit] NOT NULL,
	[Web] [nvarchar](255) NULL,
	[SeqKey] [int] NULL,
	[SaleId] [varchar](50) NULL,
	[Attachment] [nvarchar](500) NULL,
	[Email] [nvarchar](255) NULL,
	[SourseId] [varchar](50) NULL,
	[RaitingId] [varchar](50) NULL,
	[Dob] [datetime2](7) NULL,
	[PicId] [varchar](50) NULL,
	[CustomerTypeId] [varchar](500) NULL,
	[CompanyName] [nvarchar](500) NULL,
	[ResidenceTypeId] [varchar](50) NULL,
	[ResidenceTypeIdText] [nvarchar](500) NULL,
	[Description] [nvarchar](500) NULL,
	[CompanyNameInv] [nvarchar](500) NULL,
	[EmailInv] [nvarchar](255) NULL,
	[AssignmentDebitId] [varchar](50) NULL,
	[AssignmentInvId] [varchar](50) NULL,
	[AccountNumber] [varchar](50) NULL,
	[AccountName] [nvarchar](255) NULL,
	[BankId] [varchar](50) NULL,
	[AccountBranch] [nvarchar](250) NULL,
	[SwiftCode] [varchar](50) NULL,
	[OpenedAt] [nvarchar](250) NULL,
	[IdCode] [varchar](50) NULL,
	[Password] [nvarchar](255) NULL,
	[Industry] [nvarchar](255) NULL,
	[Po] [nvarchar](255) NULL,
	[RushMonth] [int] NULL,
	[Rating] [nvarchar](255) NULL,
	[PoDate] [datetime2](7) NULL,
	[MinProfitMonth] [money] NULL,
	[ToastWarning] [nvarchar](255) NULL,
	[ActionId] [int] NULL,
	[RegularShippingFrom] [nvarchar](255) NULL,
	[ConditionId] [varchar](50) NULL,
	[DistributeInformationIds] [varchar](255) NULL,
	[DistributeInformationIdsText] [nvarchar](255) NULL,
	[ZipCode] [varchar](50) NULL,
	[TrackingURL] [varchar](50) NULL,
	[IsNoDebt] [bit] NOT NULL,
	[PartnerTypeIds] [varchar](255) NULL,
	[PartnerTypeIdsText] [nvarchar](255) NULL,
	[FormatChat] [nvarchar](250) NULL,
	[AddressInv] [nvarchar](250) NULL,
	[AssignId] [varchar](50) NULL,
	[History] [nvarchar](500) NULL,
	[Birthday] [datetime2](7) NULL,
	[Logo] [nvarchar](500) NULL,
	[WebSite] [nvarchar](500) NULL,
	[Fax] [varchar](50) NULL,
	[ReceivesAccountNumber] [varchar](50) NULL,
	[ReceivesAccountName] [nvarchar](255) NULL,
	[ReceivesBankId] [varchar](50) NULL,
	[ReceivesSwiftCode] [varchar](50) NULL,
	[CustomerTypeIdText] [nvarchar](255) NULL,
	[FeatureName] [nvarchar](500) NULL,
	[Header] [nvarchar](4000) NULL,
	[Footer] [nvarchar](4000) NULL,
 CONSTRAINT [PK_Partner] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PartnerBankAccount]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PartnerBankAccount](
	[Id] [varchar](50) NOT NULL,
	[PartnerId] [varchar](50) NULL,
	[AccountName] [nvarchar](250) NULL,
	[AccountNumber] [nvarchar](250) NULL,
	[BankId] [varchar](50) NULL,
	[AccountBranch] [nvarchar](250) NULL,
	[OpenedAt] [nvarchar](250) NULL,
	[SwiftCode] [nvarchar](250) NULL,
	[IsMain] [bit] NOT NULL,
	[IsReceives] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
 CONSTRAINT [PK_BankAccount] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PartnerCare]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PartnerCare](
	[Id] [varchar](50) NOT NULL,
	[PartnerId] [varchar](50) NULL,
	[TaskName] [nvarchar](500) NULL,
	[PriorityId] [varchar](50) NULL,
	[CategoryId] [varchar](50) NULL,
	[IssuedDate] [datetime2](7) NULL,
	[ProgressId] [int] NULL,
	[Deadline] [datetime2](7) NULL,
	[AssigneeId] [varchar](50) NULL,
	[NextDate] [datetime2](7) NULL,
	[ReminderSettingId] [int] NULL,
	[NotificationNumber] [int] NULL,
	[Notes] [nvarchar](500) NULL,
	[Attachment] [nvarchar](500) NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[LastNotificationDate] [datetime2](7) NULL,
 CONSTRAINT [PK_CustomerCare] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PartnerContact]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PartnerContact](
	[Id] [varchar](50) NOT NULL,
	[GenderId] [varchar](50) NULL,
	[ContactName] [nvarchar](250) NULL,
	[ContactPhoneNumber] [varchar](50) NULL,
	[ContactEmail] [nvarchar](250) NULL,
	[JobTitles] [nvarchar](250) NULL,
	[Birthday] [datetime2](7) NULL,
	[Note] [nvarchar](250) NULL,
	[PartnerId] [varchar](50) NULL,
	[IsMain] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
 CONSTRAINT [PK_TerminalPartner] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PartnerCreditPeriod]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PartnerCreditPeriod](
	[Id] [varchar](50) NOT NULL,
	[PartnerId] [varchar](50) NULL,
	[ShippmentTypeId] [varchar](50) NULL,
	[CreditPeriodTypeId] [varchar](50) NULL,
	[Days] [int] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_PartnerCreditPeriod] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PlanEmail]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PlanEmail](
	[Id] [varchar](50) NOT NULL,
	[ComponentId] [varchar](50) NULL,
	[FromName] [nvarchar](250) NULL,
	[FromEmail] [nvarchar](250) NULL,
	[PassEmail] [nvarchar](250) NULL,
	[ToEmail] [nvarchar](250) NULL,
	[SubjectMail] [nvarchar](250) NULL,
	[FeatureId] [varchar](50) NULL,
	[DailyDate] [datetime2](7) NULL,
	[ReminderSettingId] [int] NULL,
	[NotificationNumber] [int] NULL,
	[UserId] [varchar](50) NULL,
	[Name] [nvarchar](250) NULL,
	[Template] [nvarchar](max) NULL,
	[LastNotificationDate] [datetime2](7) NULL,
	[IsCompany] [bit] NOT NULL,
	[IsPause] [bit] NOT NULL,
	[IsStart] [bit] NOT NULL,
	[IsToId] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[StartDate] [datetime2](7) NULL,
	[LastStartDate] [datetime2](7) NULL,
	[NextStartDate] [datetime2](7) NULL,
	[EmailFieldId] [varchar](50) NULL,
	[Field1Id] [varchar](50) NULL,
	[Value1] [nvarchar](500) NULL,
	[Field2Id] [varchar](50) NULL,
	[Value2] [nvarchar](500) NULL,
	[Field3Id] [varchar](50) NULL,
	[Value3] [nvarchar](500) NULL,
 CONSTRAINT [PK_EmailTemplate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PlanEmailDetail]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PlanEmailDetail](
	[Id] [varchar](50) NOT NULL,
	[PlanEmailId] [varchar](50) NULL,
	[TableName] [nvarchar](250) NULL,
	[Email] [nvarchar](250) NULL,
	[RecordId] [varchar](250) NULL,
	[DailyDate] [datetime2](7) NULL,
	[NextStartDate] [datetime2](7) NULL,
	[Template] [nvarchar](max) NULL,
	[IsPause] [bit] NOT NULL,
	[IsStart] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
 CONSTRAINT [PK_PlanEmailDetail] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Pricing]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Pricing](
	[Id] [varchar](50) NOT NULL,
	[Code] [varchar](50) NULL,
	[TypeId] [int] NULL,
	[VendorTypeId] [varchar](50) NULL,
	[DemFree] [money] NULL,
	[DetFree] [money] NULL,
	[FreeTime] [money] NULL,
	[Sto] [money] NULL,
	[VendorId] [varchar](50) NULL,
	[AgentId] [varchar](50) NULL,
	[BookingDate] [datetime2](7) NULL,
	[CarrierId] [varchar](50) NULL,
	[PolId] [varchar](50) NULL,
	[PodId] [varchar](50) NULL,
	[ViaId] [varchar](50) NULL,
	[PkEmptyId] [varchar](50) NULL,
	[ReturnsId] [varchar](50) NULL,
	[ScheduleIds] [varchar](250) NULL,
	[ScheduleIdsText] [nvarchar](250) NULL,
	[FromDate] [datetime2](7) NULL,
	[ToDate] [datetime2](7) NULL,
	[TransitTime] [nvarchar](250) NULL,
	[CreatedOn] [datetime2](7) NULL,
	[StatusId] [int] NULL,
	[Cont20Text] [nvarchar](250) NULL,
	[Cont40Text] [nvarchar](250) NULL,
	[ContText] [nvarchar](250) NULL,
	[CommodityId] [varchar](50) NULL,
	[ServiceText] [nvarchar](250) NULL,
	[CutOff] [nvarchar](250) NULL,
	[MakeupLevel] [varchar](50) NULL,
	[Note] [nvarchar](250) NULL,
	[ExchangeRate] [money] NULL,
	[IsPublic] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[IsLock] [bit] NOT NULL,
	[VoucherTypeId] [int] NULL,
	[AmountText] [nvarchar](250) NULL,
	[ServiceId] [int] NULL,
	[ModeId] [nvarchar](250) NULL,
	[Scc] [money] NULL,
	[Fsc] [money] NULL,
	[MinQuantity] [money] NULL,
	[Url] [nvarchar](500) NULL,
	[AttachedFile] [nvarchar](500) NULL,
	[FormatChat] [nvarchar](2500) NULL,
	[EmptyReturnId] [varchar](50) NULL,
	[FeatureName] [nvarchar](500) NULL,
 CONSTRAINT [PK_Pricing] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PricingDetail]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PricingDetail](
	[Id] [varchar](50) NOT NULL,
	[GroupFee] [nvarchar](250) NULL,
	[PricingId] [varchar](50) NULL,
	[UnitId] [varchar](50) NULL,
	[OtherUnitId] [varchar](50) NULL,
	[DescriptionId] [varchar](50) NULL,
	[CurrencyId] [varchar](50) NULL,
	[UnitPrice] [money] NULL,
	[MinUnitPrice] [money] NULL,
	[Vat] [money] NULL,
	[VendorId] [varchar](50) NULL,
	[IsObh] [bit] NOT NULL,
	[Note] [nvarchar](250) NULL,
	[Order] [int] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[ExchangeRateVND] [decimal](30, 24) NULL,
	[ExchangeRateUSD] [decimal](30, 24) NULL,
	[IsFreight] [bit] NOT NULL,
	[IsContainer] [bit] NOT NULL,
	[IsCBM] [bit] NOT NULL,
	[IsKGS] [bit] NOT NULL,
	[IsGW] [bit] NOT NULL,
	[IsGrossWeight] [bit] NOT NULL,
	[DemFree] [money] NULL,
	[DetFree] [money] NULL,
	[PkEmptyId] [varchar](50) NULL,
	[ReturnsId] [varchar](50) NULL,
	[IsLogistics] [bit] NOT NULL,
	[IsTrucking] [bit] NOT NULL,
 CONSTRAINT [PK_PricingDetail] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Role]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Role](
	[Id] [varchar](50) NOT NULL,
	[Hidden] [bit] NOT NULL,
	[Name] [nvarchar](250) NULL,
	[Description] [nvarchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetimeoffset](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetimeoffset](7) NULL,
	[created_by] [nvarchar](255) NULL,
 CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SaleFunction]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SaleFunction](
	[Id] [varchar](50) NOT NULL,
	[Name] [nvarchar](250) NULL,
	[Code] [nvarchar](250) NULL,
	[Description] [nvarchar](500) NULL,
	[Value] [nvarchar](500) NULL,
	[IsYes] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[GroupName] [nvarchar](500) NULL,
 CONSTRAINT [PK_SaleFuntion] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Services]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Services](
	[Id] [varchar](50) NOT NULL,
	[System] [varchar](250) NULL,
	[TenantCode] [varchar](250) NULL,
	[ComId] [varchar](50) NULL,
	[Action] [nvarchar](250) NULL,
	[Content] [nvarchar](max) NULL,
	[Env] [varchar](250) NULL,
	[RoleIds] [varchar](250) NULL,
	[IsPublicInTenant] [bit] NOT NULL,
	[Annonymous] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
 CONSTRAINT [PK_Services] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Shipment]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Shipment](
	[Id] [varchar](50) NOT NULL,
	[ParentId] [varchar](50) NULL,
	[TypeId] [int] NULL,
	[Code] [varchar](150) NULL,
	[ShipmentDate] [datetime2](7) NULL,
	[PodId] [varchar](50) NULL,
	[PolId] [varchar](50) NULL,
	[EtdDate] [datetime2](7) NULL,
	[EtaDate] [datetime2](7) NULL,
	[Vessel] [nvarchar](250) NULL,
	[VesselVoy] [nvarchar](250) NULL,
	[OceanVessel] [nvarchar](250) NULL,
	[OceanVesselVoy] [nvarchar](250) NULL,
	[CommodityId] [varchar](50) NULL,
	[PoNo] [varchar](50) NULL,
	[VendorId] [varchar](50) NULL,
	[AgentId] [varchar](50) NULL,
	[BookingNo] [varchar](50) NULL,
	[CW] [money] NULL,
	[GW] [money] NULL,
	[CBM] [money] NULL,
	[KGS] [nchar](10) NULL,
	[CarrierId] [varchar](50) NULL,
	[AtdDate] [datetime2](7) NULL,
	[AtaDate] [datetime2](7) NULL,
	[OtherRefno] [varchar](50) NULL,
	[Notes] [nvarchar](500) NULL,
	[DeliveryTermId] [varchar](50) NULL,
	[CyId] [varchar](50) NULL,
	[IncotermId] [varchar](50) NULL,
	[Service] [nvarchar](250) NULL,
	[SubscribersIds] [varchar](250) NULL,
	[Quantity] [money] NULL,
	[UnitId] [varchar](50) NULL,
	[TotalCW] [money] NULL,
	[TotalCBM] [money] NULL,
	[ForwardId] [varchar](50) NULL,
	[ReceiverIds] [varchar](250) NULL,
	[UserViewIds] [varchar](250) NULL,
	[UserApprovedIds] [varchar](250) NULL,
	[StatusId] [int] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[CustomerId] [varchar](50) NULL,
	[HblNo] [varchar](100) NULL,
	[HblTypeId] [varchar](50) NULL,
	[NumberOriginalIdText] [varchar](100) NULL,
	[NumberOriginalId] [varchar](50) NULL,
	[MblNo] [varchar](100) NULL,
	[MblTypeId] [varchar](50) NULL,
	[ExportReferences] [nvarchar](100) NULL,
	[SeriBillNo] [nvarchar](100) NULL,
	[Deadline] [datetime2](7) NULL,
	[SaleId] [varchar](50) NULL,
	[QuotationId] [varchar](50) NULL,
	[HSCode] [nvarchar](100) NULL,
	[SaillingDate] [datetime2](7) NULL,
	[PlaceReceiptId] [varchar](50) NULL,
	[PortTransitId] [varchar](50) NULL,
	[PortTransit2Id] [varchar](50) NULL,
	[PortTransit3Id] [varchar](50) NULL,
	[PlaceDeliveryId] [varchar](50) NULL,
	[FinalDestinationId] [varchar](50) NULL,
	[ClosingDate] [datetime2](7) NULL,
	[ShipperId] [varchar](50) NULL,
	[ConsigneeId] [varchar](50) NULL,
	[NotifyPartyId] [varchar](50) NULL,
	[ForDeliveryGoodsId] [varchar](50) NULL,
	[ShipperDes] [nvarchar](500) NULL,
	[ConsigneeDes] [nvarchar](500) NULL,
	[NotifyPartyDes] [nvarchar](500) NULL,
	[ForDeliveryGoodsDes] [nvarchar](250) NULL,
	[IssuingId] [varchar](50) NULL,
	[IssuingDes] [nvarchar](500) NULL,
	[DetailsGoods] [nvarchar](1000) NULL,
	[AgentCode] [nvarchar](250) NULL,
	[AccountNo] [nvarchar](250) NULL,
	[AccountingInformation] [nvarchar](250) NULL,
	[ShippingMark] [nvarchar](250) NULL,
	[TypeMoveId] [varchar](50) NULL,
	[PlaceIssueId] [varchar](50) NULL,
	[IssueDate] [datetime2](7) NULL,
	[PaymentTermId] [varchar](50) NULL,
	[DemFree] [money] NULL,
	[DetFree] [money] NULL,
	[FreightPayableId] [varchar](50) NULL,
	[FreightChargeId] [varchar](50) NULL,
	[PrepaidId] [varchar](50) NULL,
	[TotalPrepaidId] [varchar](50) NULL,
	[Storage] [money] NULL,
	[Freetime] [money] NULL,
	[ClauseId] [varchar](50) NULL,
	[WarehouseId] [varchar](50) NULL,
	[Measuament] [money] NULL,
	[CargoType] [nvarchar](250) NULL,
	[PickupDate] [datetime2](7) NULL,
	[DeliveryDate] [datetime2](7) NULL,
	[ShipmentTypeId] [int] NULL,
	[InWords] [nvarchar](250) NULL,
	[NominationParty] [nvarchar](250) NULL,
	[SeqKey] [int] NULL,
	[SeqDate] [datetime2](7) NULL,
	[FreightId] [varchar](50) NULL,
	[LockDate] [datetime2](7) NULL,
	[IsDirectMaster] [bit] NOT NULL,
	[NoPieces] [money] NULL,
	[KgsLgl] [varchar](50) NULL,
	[RateClass] [varchar](50) NULL,
	[CommodityItemNo] [varchar](50) NULL,
	[ChargeableWeight] [money] NULL,
	[RateChange] [money] NULL,
	[Total] [money] NULL,
	[NatureQuantityGoods] [nvarchar](250) NULL,
	[OtherCharges] [nvarchar](250) NULL,
	[CurrencyRatesText] [nvarchar](500) NULL,
	[CCCharges] [nvarchar](500) NULL,
	[ExecutedDate] [datetime2](7) NULL,
	[Place] [nvarchar](500) NULL,
	[AirportDeparture] [nvarchar](500) NULL,
	[ReferenceNumber] [nvarchar](250) NULL,
	[OptinalShippingInformation] [nvarchar](250) NULL,
	[FlightsDate] [datetime2](7) NULL,
	[CFlights1] [nvarchar](250) NULL,
	[CFlights1Date] [datetime2](7) NULL,
	[CFlights2] [nvarchar](250) NULL,
	[CFlights2Date] [datetime2](7) NULL,
	[To1] [nvarchar](250) NULL,
	[ByFirstCarrier] [nvarchar](250) NULL,
	[To2] [nvarchar](250) NULL,
	[By1] [nvarchar](250) NULL,
	[To3] [nvarchar](250) NULL,
	[By2] [nvarchar](250) NULL,
	[Currency] [nvarchar](250) NULL,
	[CHGSCode] [nvarchar](250) NULL,
	[WTVALId] [varchar](50) NULL,
	[OtherId] [varchar](50) NULL,
	[DeclaredCarriage] [nvarchar](250) NULL,
	[DeclaredCustoms] [nvarchar](250) NULL,
	[SpecialHandling] [nvarchar](250) NULL,
	[AmountInsurance] [nvarchar](250) NULL,
	[SCI] [nvarchar](250) NULL,
	[FromDate] [datetime2](7) NULL,
	[ToDate] [datetime2](7) NULL,
	[ServiceId] [varchar](50) NULL,
	[PartyContact] [nvarchar](250) NULL,
	[ContainerText] [nvarchar](500) NULL,
	[ShipmentId] [varchar](50) NULL,
	[ClearanceNo] [varchar](250) NULL,
	[ClearanceDate] [datetime2](7) NULL,
	[CdsTypeId] [varchar](50) NULL,
	[ThreadingCdsId] [varchar](50) NULL,
	[CustomsOfficeId] [varchar](50) NULL,
	[CommercialInvoiceNo] [varchar](250) NULL,
	[HblSeaNo] [varchar](250) NULL,
	[CsdEdit] [money] NULL,
	[SubService] [nvarchar](250) NULL,
	[CoFormId] [varchar](50) NULL,
	[CoQuantity] [money] NULL,
	[ContainerNo] [varchar](50) NULL,
	[TruckerId] [varchar](50) NULL,
	[TruckNo] [varchar](50) NULL,
	[Driver] [nvarchar](50) NULL,
	[PickupId] [varchar](50) NULL,
	[PickupAddress] [nvarchar](250) NULL,
	[PickupContact] [nvarchar](250) NULL,
	[DeliveryId] [varchar](50) NULL,
	[DeliveryAddress] [nvarchar](250) NULL,
	[DeliveryContact] [nvarchar](250) NULL,
	[EmtyPickupId] [varchar](50) NULL,
	[CommodityText] [nvarchar](250) NULL,
	[Packages] [money] NULL,
	[TruckStorageDate] [datetime2](7) NULL,
	[NumberDaysTruck] [money] NULL,
	[MoocStorageDate] [datetime2](7) NULL,
	[NumberDaysMooc] [money] NULL,
	[ClosingTime] [datetime2](7) NULL,
	[ContainerTypeId] [varchar](50) NULL,
	[ShipmentDOId] [varchar](50) NULL,
	[ShipmentSIId] [varchar](50) NULL,
	[NoApproved] [bit] NOT NULL,
	[VolumeWeight] [money] NULL,
	[AirportDepartureId] [varchar](50) NULL,
	[PlaceId] [varchar](50) NULL,
	[ToId] [varchar](50) NULL,
	[SaleIds] [varchar](500) NULL,
	[DocsNo] [varchar](50) NULL,
	[DocsDate] [datetime2](7) NULL,
	[MblDate] [datetime2](7) NULL,
	[BookingLocalId] [varchar](50) NULL,
	[QuotationCode] [varchar](50) NULL,
	[ReleaseDate] [datetime2](7) NULL,
	[IsLockHblNo] [bit] NOT NULL,
	[SubmitSIId] [varchar](50) NULL,
	[ActualCostLocal] [money] NULL,
	[ActualRevenueLocal] [money] NULL,
	[ActualProfitLocal] [money] NULL,
	[ActualCostUSD] [money] NULL,
	[ActualRevenueUSD] [money] NULL,
	[ActualProfitUSD] [money] NULL,
	[ActualComLocal] [money] NULL,
	[ActualComUSD] [money] NULL,
	[IsComplete] [bit] NOT NULL,
	[PriorityDate] [datetime2](7) NULL,
	[ShipmentDetailId] [varchar](50) NULL,
	[CompleteDate] [datetime2](7) NULL,
	[FormatChat] [varchar](50) NULL,
	[Teus] [int] NULL,
	[ResidenceTypeId] [nvarchar](255) NULL,
	[AdditionTypeIds] [nvarchar](255) NULL,
	[PartnerTypeIds] [nvarchar](255) NULL,
	[ArrivalNoticeNotes] [nvarchar](500) NULL,
	[ArrivalNoticeTo] [nvarchar](100) NULL,
	[DeliveryOrderNotes] [nvarchar](500) NULL,
	[DeliveryOrderTo] [nvarchar](100) NULL,
	[FeatureName] [nvarchar](500) NULL,
	[DocumentNo] [nvarchar](250) NULL,
	[ArrivaNo] [nvarchar](250) NULL,
	[ReleaseNotice] [datetime2](7) NULL,
	[DONo] [nvarchar](250) NULL,
	[Sercure] [nvarchar](250) NULL,
	[DODate] [datetime2](7) NULL,
	[NominationPartyId] [varchar](50) NULL,
 CONSTRAINT [PK_Shipment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShipmentDO]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShipmentDO](
	[Id] [varchar](50) NOT NULL,
	[ShipmentId] [varchar](50) NULL,
	[TypeId] [int] NULL,
	[Code] [varchar](50) NULL,
	[DoNo] [varchar](50) NULL,
	[DoDate] [datetime2](7) NULL,
	[TimeDeliveryDate] [datetime2](7) NULL,
	[DeliveryPlaceId] [varchar](50) NULL,
	[ShipperId] [varchar](50) NULL,
	[ShipperIdText] [nvarchar](500) NULL,
	[ConsigneeId] [varchar](50) NULL,
	[ConsigneeIdText] [nvarchar](500) NULL,
	[ShipperDes] [nvarchar](500) NULL,
	[ConsigneeDes] [nvarchar](500) NULL,
	[Representative1] [nvarchar](250) NULL,
	[Representative2] [nvarchar](250) NULL,
	[Position1] [nvarchar](250) NULL,
	[Position2] [nvarchar](250) NULL,
	[IndentityCard1] [varchar](50) NULL,
	[IndentityCard2] [varchar](50) NULL,
	[Tel1] [varchar](50) NULL,
	[Tel2] [varchar](50) NULL,
	[Reason] [nvarchar](500) NULL,
	[Evidence] [nvarchar](500) NULL,
	[SpecialNotes] [nvarchar](500) NULL,
	[ConditionGoods] [nvarchar](500) NULL,
	[DescriptionGoods] [nvarchar](500) NULL,
	[SeqKey] [int] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ShipmentDO] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShipmentFee]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShipmentFee](
	[Id] [varchar](50) NOT NULL,
	[VoucherId] [varchar](50) NULL,
	[TypeId] [int] NULL,
	[ShipmentId] [varchar](50) NULL,
	[FileId] [varchar](50) NULL,
	[VendorId] [varchar](50) NULL,
	[DescriptionId] [varchar](50) NULL,
	[TotalAmountTax] [money] NULL,
	[TotalAmount] [money] NULL,
	[AmountTax] [money] NULL,
	[Amount] [money] NULL,
	[Quantity] [money] NULL,
	[UnitId] [varchar](50) NULL,
	[Vat] [money] NULL,
	[CurrencyId] [varchar](50) NULL,
	[CurrencyCode] [varchar](10) NULL,
	[Tax] [money] NULL,
	[Notes] [nvarchar](250) NULL,
	[Docs] [nvarchar](250) NULL,
	[IsObh] [bit] NOT NULL,
	[IsManual] [bit] NOT NULL,
	[ObhId] [varchar](50) NULL,
	[IsNoDocs] [bit] NOT NULL,
	[ExchangeRateVND] [decimal](30, 24) NULL,
	[ExchangeRateUSD] [decimal](30, 24) NULL,
	[ExchangeRateINV] [decimal](30, 24) NULL,
	[IsContainer] [bit] NOT NULL,
	[IsCBM] [bit] NOT NULL,
	[IsFreight] [bit] NOT NULL,
	[IsLogistics] [bit] NOT NULL,
	[IsTrucking] [bit] NOT NULL,
	[IsCom] [bit] NOT NULL,
	[IsKGS] [bit] NOT NULL,
	[IsGW] [bit] NOT NULL,
	[IsGrossWeight] [bit] NOT NULL,
	[IsAbs] [bit] NOT NULL,
	[IsAn] [bit] NOT NULL,
	[Order] [int] NULL,
	[ExchangeRate] [money] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[BasedId] [varchar](50) NULL,
	[PmTypeId] [varchar](50) NULL,
	[ShipmentInvoiceDetailId] [varchar](50) NULL,
	[ShipmentInvoiceId] [varchar](50) NULL,
	[ShipmentInvoiceCode] [varchar](50) NULL,
	[ShipmentInvoiceDate] [datetime2](7) NULL,
	[DebitDate] [datetime2](7) NULL,
	[ParentId] [varchar](50) NULL,
	[NoSubmit] [bit] NOT NULL,
	[IsLock] [bit] NOT NULL,
	[SaleId] [varchar](50) NULL,
	[DeadlineDate] [datetime2](7) NULL,
	[BookingId] [varchar](50) NULL,
	[IsAdv] [bit] NOT NULL,
	[IsAdvCustomer] [bit] NOT NULL,
	[IsAdvProvider] [bit] NOT NULL,
	[AssignId] [varchar](50) NULL,
	[SettlementNo] [varchar](50) NULL,
	[InvoiceNo] [varchar](50) NULL,
	[PaymentRequestId] [varchar](50) NULL,
	[PaymentRequestDetailId] [varchar](50) NULL,
	[PaymentDate] [datetime2](7) NULL,
	[PaymentCode] [varchar](50) NULL,
	[IsPayment] [bit] NOT NULL,
	[ServiceId] [int] NULL,
	[ShipmentDate] [datetime2](7) NULL,
	[EtdDate] [datetime2](7) NULL,
	[EtaDate] [datetime2](7) NULL,
	[IsInvoice] [bit] NOT NULL,
	[InvoiceId] [varchar](50) NULL,
	[InvoiceDetailId] [varchar](50) NULL,
	[InvoiceDate] [datetime2](7) NULL,
	[InvoiceCode] [varchar](50) NULL,
	[HblNo] [varchar](50) NULL,
	[ExTotalAmountTax] [money] NULL,
	[ExTotalAmount] [money] NULL,
	[ExAmountTax] [money] NULL,
	[ExAmount] [money] NULL,
	[DebtId] [varchar](50) NULL,
	[IsDebtAcc] [bit] NOT NULL,
	[IsPaid] [bit] NOT NULL,
	[DebtCode] [varchar](50) NULL,
	[DebtDate] [datetime2](7) NULL,
	[PaymentAccId] [varchar](50) NULL,
	[PaymentAccCode] [varchar](50) NULL,
	[PaymentAccDate] [datetime2](7) NULL,
	[IsPaymentAcc] [bit] NOT NULL,
	[PaidDate] [datetime2](7) NULL,
	[LockHblNo] [bit] NOT NULL,
	[IsComplete] [bit] NOT NULL,
	[CompleteDate] [datetime2](7) NULL,
	[MblNo] [varchar](50) NULL,
	[IsLockExchange] [bit] NOT NULL,
	[TotalPaid] [money] NULL,
	[ExTotalPaid] [money] NULL,
	[TotalRemain]  AS (isnull([TotalAmount],(0))-isnull([TotalPaid],(0))) PERSISTED,
	[ExTotalRemain]  AS (isnull([TotalAmountTax],(0))*isnull([ExchangeRateINV],[ExchangeRateVND])-isnull([ExTotalPaid],(0))) PERSISTED,
	[ExchangeRateDebt] [money] NULL,
	[ShipmentTypeId] [int] NULL,
 CONSTRAINT [PK_ShipmentFee] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShipmentFreight]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShipmentFreight](
	[Id] [varchar](50) NOT NULL,
	[ShipmentId] [varchar](50) NULL,
	[PmTypeId] [int] NULL,
	[WeightCharge] [money] NULL,
	[ValuationCharge] [money] NULL,
	[Tax] [money] NULL,
	[TotalDueAgent] [money] NULL,
	[TotalDueCarrier] [money] NULL,
	[Total] [money] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ShipmentFreight] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShipmentInvoice]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShipmentInvoice](
	[Id] [varchar](50) NOT NULL,
	[TypeId] [int] NULL,
	[ShipmentId] [varchar](50) NULL,
	[VendorIds] [varchar](500) NULL,
	[VendorIdsText] [nvarchar](500) NULL,
	[Code] [varchar](50) NULL,
	[OtherCode] [varchar](50) NULL,
	[ActionId] [int] NULL,
	[IsSendPartner] [bit] NOT NULL,
	[PostDate] [datetime2](7) NULL,
	[DueDate] [datetime2](7) NULL,
	[InvoiceDate] [datetime2](7) NULL,
	[PostOtherDate] [datetime2](7) NULL,
	[RevisedDate] [datetime2](7) NULL,
	[InvoiceTypeId] [int] NULL,
	[HblIds] [varchar](max) NULL,
	[FileIds] [varchar](max) NULL,
	[MblNos] [varchar](500) NULL,
	[HblNo] [varchar](500) NULL,
	[FileNo] [varchar](500) NULL,
	[ObhTypeId] [int] NULL,
	[FeeTypeId] [varchar](50) NULL,
	[IncludeChargeId] [int] NULL,
	[ExchangeRateVND] [decimal](30, 24) NULL,
	[ExchangeRateUSD] [decimal](30, 24) NULL,
	[ExchangeRateINV] [decimal](30, 24) NULL,
	[ExchangeRateINV2] [decimal](30, 24) NULL,
	[Remark] [nvarchar](500) NULL,
	[Description] [nvarchar](500) NULL,
	[RequestTypeId] [varchar](50) NULL,
	[ReceiverIds] [varchar](500) NULL,
	[SeqKey] [int] NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[DeadlineDate] [datetime2](7) NULL,
	[ProgressId] [int] NULL,
	[FromDate] [datetime2](7) NULL,
	[ToDate] [datetime2](7) NULL,
	[DateModeId] [int] NULL,
	[ServiceIds] [varchar](50) NULL,
	[DescriptionId] [varchar](50) NULL,
	[DocsNo] [varchar](50) NULL,
	[ChargePaidId] [int] NULL,
	[NoDocsId] [int] NULL,
	[DebitAmount] [money] NULL,
	[CurrencyId] [varchar](50) NULL,
	[CurrencyCode] [varchar](10) NULL,
	[PaymentMethodId] [int] NULL,
	[StatusId] [int] NULL,
	[VoucherTypeId] [int] NULL,
	[TaxInvoiceId] [int] NULL,
	[DateFieldId] [varchar](50) NULL,
	[UserViewIds] [nvarchar](500) NULL,
	[UserApprovedIds] [nvarchar](500) NULL,
	[ForwardId] [varchar](50) NULL,
	[FormatChat] [nvarchar](500) NULL,
	[PaymentId] [int] NULL,
	[AttachedFile] [nvarchar](500) NULL,
	[TaxCode] [varchar](50) NULL,
	[CompanyIdText] [nvarchar](250) NULL,
	[CompanyId] [varchar](50) NULL,
	[CompanyAddress] [nvarchar](250) NULL,
	[BuyerName] [nvarchar](250) NULL,
	[CustomerId] [varchar](50) NULL,
	[CustomerIdText] [nvarchar](250) NULL,
	[IsRevenue] [bit] NOT NULL,
	[InvoiceNo] [varchar](50) NULL,
	[SeriNo] [varchar](50) NULL,
	[Notes] [nvarchar](500) NULL,
	[IsPaid] [bit] NOT NULL,
	[PaidDate] [datetime2](7) NULL,
	[IsReceiptPayment] [bit] NOT NULL,
	[IsSend] [bit] NOT NULL,
	[NoApproved] [bit] NOT NULL,
	[ShipmentStatusId] [int] NULL,
	[TotalAmountTax] [money] NULL,
	[TotalAmount] [money] NULL,
	[AmountTax] [money] NULL,
	[VatInv] [money] NULL,
	[EinvoiceLink] [nvarchar](500) NULL,
	[EinvoiceCode] [varchar](50) NULL,
	[AmountText] [nvarchar](500) NULL,
	[InvoiceConfigId] [varchar](50) NULL,
	[IsMultiple] [bit] NOT NULL,
	[DebitTypeId] [int] NULL,
	[ReleaseDate] [datetime2](7) NULL,
	[DocsTypeId] [int] NULL,
	[IsSendToPartner] [bit] NOT NULL,
	[ShipmentInvoiceId] [varchar](50) NULL,
	[PaymentAccId] [varchar](50) NULL,
	[DebtAccId] [varchar](50) NULL,
	[InvoiceIds] [nvarchar](500) NULL,
	[IssueInvoiceId] [varchar](50) NULL,
	[VatInvoiceId] [varchar](50) NULL,
 CONSTRAINT [PK_ShipmentInvoice] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShipmentInvoiceDetail]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShipmentInvoiceDetail](
	[Id] [varchar](50) NOT NULL,
	[TypeId] [int] NULL,
	[ShipmentInvoiceId] [varchar](50) NULL,
	[PaymentRequestId] [varchar](50) NULL,
	[ShipmentId] [varchar](50) NULL,
	[VendorId] [varchar](50) NULL,
	[DescriptionId] [varchar](50) NULL,
	[TotalAmountTax] [money] NULL,
	[TotalAmount] [money] NULL,
	[AmountTax] [money] NULL,
	[Amount] [money] NULL,
	[Quantity] [money] NULL,
	[UnitId] [varchar](50) NULL,
	[Vat] [money] NULL,
	[CurrencyId] [varchar](50) NULL,
	[CurrencyCode] [varchar](10) NULL,
	[Tax] [money] NULL,
	[Notes] [nvarchar](250) NULL,
	[Docs] [nvarchar](250) NULL,
	[IsObh] [bit] NOT NULL,
	[ObhId] [varchar](50) NULL,
	[IsNoDocs] [bit] NOT NULL,
	[ExchangeRateVND] [decimal](30, 24) NULL,
	[ExchangeRateUSD] [decimal](30, 24) NULL,
	[ExchangeRateINV] [decimal](30, 24) NULL,
	[ExchangeRateINV2] [decimal](30, 24) NULL,
	[IsContainer] [bit] NOT NULL,
	[IsCBM] [bit] NOT NULL,
	[IsFreight] [bit] NOT NULL,
	[IsLogistics] [bit] NOT NULL,
	[IsTrucking] [bit] NOT NULL,
	[IsKGS] [bit] NOT NULL,
	[IsGW] [bit] NOT NULL,
	[Order] [int] NULL,
	[ExchangeRate] [money] NULL,
	[SettlementNo] [nvarchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[BasedId] [varchar](50) NULL,
	[PmTypeId] [varchar](50) NULL,
	[ShipmentFeeId] [varchar](50) NULL,
	[IsLock] [bit] NOT NULL,
	[IsPayment] [bit] NOT NULL,
	[Payable] [money] NULL,
	[Receivable] [money] NULL,
	[VoucherId] [varchar](50) NULL,
	[FileId] [varchar](50) NULL,
	[IsManual] [bit] NOT NULL,
	[IsGrossWeight] [bit] NOT NULL,
	[ShipmentInvoiceDetailId] [varchar](50) NULL,
	[ShipmentInvoiceCode] [varchar](50) NULL,
	[ShipmentInvoiceDate] [datetime2](7) NULL,
	[DebitDate] [datetime2](7) NULL,
	[ParentId] [varchar](50) NULL,
	[NoSubmit] [bit] NOT NULL,
	[SaleId] [varchar](50) NULL,
	[DeadlineDate] [datetime2](7) NULL,
	[BookingId] [varchar](50) NULL,
	[IsAdv] [bit] NOT NULL,
	[IsAdvCustomer] [bit] NOT NULL,
	[IsAdvProvider] [bit] NOT NULL,
	[AssignId] [varchar](50) NULL,
	[PaymentRequestDetailId] [varchar](50) NULL,
	[PaymentDate] [datetime2](7) NULL,
	[PaymentCode] [varchar](50) NULL,
	[ServiceId] [int] NULL,
	[ShipmentDate] [datetime2](7) NULL,
	[EtdDate] [datetime2](7) NULL,
	[EtaDate] [datetime2](7) NULL,
	[IsInvoice] [bit] NOT NULL,
	[InvoiceId] [varchar](50) NULL,
	[InvoiceDetailId] [varchar](50) NULL,
	[InvoiceDate] [datetime2](7) NULL,
	[InvoiceCode] [varchar](50) NULL,
	[ExTotalAmountTax] [money] NULL,
	[ExTotalAmount] [money] NULL,
	[ExAmountTax] [money] NULL,
	[ExAmount] [money] NULL,
	[FileNo] [varchar](50) NULL,
	[HblNo] [varchar](50) NULL,
 CONSTRAINT [PK_InvoiceDetail] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShipmentProcess]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShipmentProcess](
	[Id] [varchar](50) NOT NULL,
	[TypeId] [int] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ShipmentProcessId] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShipmentProcessDetail]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShipmentProcessDetail](
	[Id] [varchar](50) NOT NULL,
	[ProcessId] [varchar](50) NULL,
	[TypeId] [int] NULL,
	[JobName] [nvarchar](3000) NULL,
	[ModeId] [int] NULL,
	[IsTracing] [bit] NOT NULL,
	[IsDefault] [bit] NOT NULL,
	[BaseId] [int] NULL,
	[DeadLineNumer] [int] NULL,
	[AlertNumer] [int] NULL,
	[IsLockShipment] [bit] NOT NULL,
	[IsCompleteShipment] [bit] NOT NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_ShipmentProcess] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShipmentSI]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShipmentSI](
	[Id] [varchar](50) NOT NULL,
	[ShipmentId] [varchar](50) NULL,
	[TypeId] [int] NULL,
	[Code] [varchar](50) NULL,
	[BookingNo] [varchar](50) NULL,
	[IssueDate] [datetime2](7) NULL,
	[MblTypeId] [varchar](50) NULL,
	[PolId] [varchar](50) NULL,
	[PodId] [varchar](50) NULL,
	[PlaceDeliveryId] [varchar](50) NULL,
	[FinalDestinationId] [varchar](50) NULL,
	[LoadingDate] [datetime2](7) NULL,
	[Vessel] [varchar](50) NULL,
	[VesselVoy] [varchar](50) NULL,
	[PaymentTermId] [varchar](50) NULL,
	[VendorId] [varchar](50) NULL,
	[Pic] [nvarchar](250) NULL,
	[ShipperDes] [nvarchar](500) NULL,
	[ConsigneeDes] [nvarchar](500) NULL,
	[ConsigneeId] [varchar](50) NULL,
	[ConsigneeIdText] [nvarchar](500) NULL,
	[NotifyPartyDes] [nvarchar](500) NULL,
	[NotifyPartyId] [varchar](50) NULL,
	[NotifyPartyIdText] [nvarchar](500) NULL,
	[RealShipperId] [varchar](50) NULL,
	[RealShipperIdText] [nvarchar](500) NULL,
	[RealShipperDes] [nvarchar](500) NULL,
	[RealConsigneeId] [varchar](50) NULL,
	[RealConsigneeIdText] [nvarchar](500) NULL,
	[RealConsigneeDes] [nvarchar](500) NULL,
	[Notes] [nvarchar](500) NULL,
	[ShippingMark] [nvarchar](500) NULL,
	[DetailsGoods] [nvarchar](500) NULL,
	[DeliveryTermId] [varchar](50) NULL,
	[ClauseId] [varchar](50) NULL,
	[ContainerText] [nvarchar](500) NULL,
	[GW] [money] NULL,
	[CBM] [money] NULL,
	[SeqKey] [int] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[Package] [nvarchar](500) NULL,
	[ProgressId] [int] NULL,
	[SITypeId] [int] NULL,
	[CustomerId] [varchar](50) NULL,
	[StatusId] [int] NULL,
	[FormatChat] [nvarchar](500) NULL,
	[VoucherTypeId] [int] NULL,
	[IsSend] [bit] NOT NULL,
	[HblTypeId] [varchar](50) NULL,
	[Quantity] [money] NULL,
	[Remark] [nvarchar](500) NULL,
	[UserReceiverId] [varchar](50) NULL,
	[UserApprovedIds] [varchar](50) NULL,
	[ServiceId] [int] NULL,
	[UserCreateId] [varchar](50) NULL,
	[UnitId] [varchar](50) NULL,
	[FeatureName] [nvarchar](500) NULL,
	[ReceiverIds] [varchar](250) NULL,
	[ShipmentFeature] [nvarchar](250) NULL,
 CONSTRAINT [PK_ShipmentSI] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShipmentTask]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShipmentTask](
	[Id] [varchar](50) NOT NULL,
	[ShipmentId] [varchar](50) NULL,
	[JobName] [nvarchar](250) NULL,
	[JobDate] [datetime2](7) NULL,
	[EstimateFinishDate] [datetime2](7) NULL,
	[FinishDate] [datetime2](7) NULL,
	[IsFinish] [bit] NOT NULL,
	[Notes] [nvarchar](250) NULL,
	[UserId] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[IsTracing] [bit] NOT NULL,
 CONSTRAINT [PK_ShipmentTask] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TableName]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TableName](
	[Id] [varchar](50) NOT NULL,
	[Name] [nvarchar](500) NULL,
	[Description] [nvarchar](500) NULL,
	[TableTrigger] [nvarchar](500) NULL,
	[Duplicate] [nvarchar](500) NULL,
	[TanentId] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_TableName] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tanent]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tanent](
	[Id] [varchar](50) NOT NULL,
	[Password] [nvarchar](250) NULL,
	[CompanyName] [nvarchar](250) NULL,
	[TaxCode] [nvarchar](250) NULL,
	[TanentCode] [nvarchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetimeoffset](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetimeoffset](7) NULL,
 CONSTRAINT [PK_Tanent] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaskNotification]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaskNotification](
	[Id] [varchar](50) NOT NULL,
	[Read] [bit] NOT NULL,
	[Icon] [varchar](150) NULL,
	[AssignedId] [varchar](50) NULL,
	[EntityId] [nvarchar](250) NULL,
	[Avatar] [nvarchar](2500) NULL,
	[FromName] [nvarchar](2500) NULL,
	[FeatureName] [nvarchar](500) NULL,
	[FeatureName2] [nvarchar](500) NULL,
	[FeatureName3] [nvarchar](500) NULL,
	[Title] [nvarchar](2500) NULL,
	[Title2] [nvarchar](2500) NULL,
	[Description] [nvarchar](2500) NULL,
	[Description2] [nvarchar](2500) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[RecordId] [varchar](50) NULL,
	[VoucherTypeId] [int] NULL,
 CONSTRAINT [PK_TaskNotification] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Terminal]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Terminal](
	[Id] [varchar](50) NOT NULL,
	[Code] [nvarchar](150) NULL,
	[Name] [nvarchar](400) NULL,
	[Address] [nvarchar](600) NULL,
	[IsLocal] [bit] NOT NULL,
	[CountryId] [varchar](50) NULL,
	[ZoneId] [varchar](50) NULL,
	[ServiceIds] [varchar](400) NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[SeqKey] [int] NULL,
	[CodeMn] [varchar](150) NULL,
 CONSTRAINT [PK_Terminal] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User](
	[Id] [varchar](50) NOT NULL,
	[Code] [nvarchar](250) NULL,
	[Email] [nvarchar](250) NULL,
	[Password] [nvarchar](250) NULL,
	[Salt] [nvarchar](250) NULL,
	[UserName] [nvarchar](250) NULL,
	[FullName] [nvarchar](250) NULL,
	[Address] [nvarchar](250) NULL,
	[Avatar] [nvarchar](250) NULL,
	[Ssn] [nvarchar](250) NULL,
	[PhoneNumber] [nvarchar](250) NULL,
	[TeamId] [varchar](50) NULL,
	[PartnerId] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[Dob] [datetime2](7) NULL,
	[GenderId] [varchar](50) NULL,
	[SsnDate] [datetime2](7) NULL,
	[TaxCode] [varchar](50) NULL,
	[SeqKey] [int] NULL,
	[LoginFailedCount] [int] NULL,
	[LastFailedLogin] [datetime2](7) NULL,
	[LastLogin] [datetime2](7) NULL,
	[Recover] [nvarchar](500) NULL,
	[JointDate] [datetime2](7) NULL,
	[RoleIds] [varchar](500) NULL,
	[RoleIdsText] [nvarchar](500) NULL,
	[DepartmentId] [varchar](50) NULL,
	[IdentityCardDate] [datetime2](7) NULL,
	[IdentityCard] [varchar](150) NULL,
	[PlaceIssue] [nvarchar](150) NULL,
	[NickName] [nvarchar](150) NULL,
	[Knowledge] [nvarchar](150) NULL,
	[TargetLocalCurr] [money] NULL,
	[TargetUSDCurr] [money] NULL,
	[TypeBonusId] [varchar](50) NULL,
	[SalaryAmount] [money] NULL,
	[SalaryCoefficient] [money] NULL,
	[InsuranceAmount] [money] NULL,
	[TypeContractId] [varchar](50) NULL,
	[Dependents] [int] NULL,
	[AccNumber] [varchar](50) NULL,
	[AccName] [nvarchar](255) NULL,
	[BankId] [varchar](50) NULL,
	[PassEmail] [nvarchar](255) NULL,
	[CompanyId] [varchar](50) NULL,
	[IsDepartment] [bit] NOT NULL,
	[IsTeam] [bit] NOT NULL,
	[PositionId] [varchar](50) NULL,
	[TypeId] [int] NULL,
	[MailServer] [varchar](250) NULL,
	[PortServer] [varchar](250) NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserLogin]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserLogin](
	[Id] [varchar](50) NOT NULL,
	[UserId] [varchar](50) NULL,
	[AccessToken] [varchar](1500) NULL,
	[RefreshToken] [varchar](1500) NULL,
	[AccessTokenExp] [datetime2](7) NULL,
	[RefreshTokenExp] [datetime2](7) NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[created_by] [nvarchar](255) NULL,
	[IpAddress] [nvarchar](255) NULL,
 CONSTRAINT [PK_UserLogin] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRole]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRole](
	[Id] [varchar](50) NOT NULL,
	[UserId] [varchar](50) NULL,
	[RoleId] [varchar](50) NULL,
	[Active] [bit] NOT NULL,
	[InsertedBy] [varchar](50) NULL,
	[InsertedDate] [datetimeoffset](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetimeoffset](7) NULL,
	[created_by] [nvarchar](255) NULL,
 CONSTRAINT [PK_UserRole] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserSetting]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserSetting](
	[Id] [varchar](50) NOT NULL,
	[FeatureId] [varchar](50) NULL,
	[ComponentId] [varchar](50) NULL,
	[UserId] [varchar](50) NULL,
	[Value] [nvarchar](max) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_UserSetting] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Voucher]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Voucher](
	[Id] [varchar](50) NOT NULL,
	[TypeId] [int] NULL,
	[VoucherTypeId] [int] NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[VoucherDate] [datetime2](7) NULL,
	[Code] [varchar](50) NULL,
	[DepartmentId] [varchar](50) NULL,
	[PositionId] [varchar](50) NULL,
	[ActionId] [int] NULL,
	[AutoProgressId] [int] NULL,
	[Amount] [money] NULL,
	[CurrencyId] [varchar](50) NULL,
	[PaymentMethodId] [varchar](50) NULL,
	[AmountText] [nvarchar](500) NULL,
	[AttachedFile] [nvarchar](500) NULL,
	[DeadlineDate] [datetime2](7) NULL,
	[Note] [nvarchar](500) NULL,
	[AdvanceAmount] [money] NULL,
	[SettlementAmount] [money] NULL,
	[RemainingAmount] [money] NULL,
	[UserId] [varchar](50) NULL,
	[UserCode] [varchar](50) NULL,
	[SeqKey] [int] NULL,
	[ParentId] [varchar](50) NULL,
	[VoucherId] [varchar](50) NULL,
	[UserViewIds] [nvarchar](500) NULL,
	[UserApprovedIds] [nvarchar](500) NULL,
	[ForwardId] [varchar](50) NULL,
	[StatusId] [int] NULL,
	[FormatChat] [nvarchar](500) NULL,
	[VoucherNo] [varchar](50) NULL,
	[IsSimple] [bit] NOT NULL,
	[CompanyId] [varchar](50) NULL,
	[Notes] [nvarchar](500) NULL,
	[Description] [nvarchar](2500) NULL,
	[CompanyAddress] [nvarchar](500) NULL,
	[Person] [nvarchar](500) NULL,
	[VendorId] [varchar](50) NULL,
	[VendorName] [nvarchar](500) NULL,
	[PartnerId] [varchar](50) NULL,
	[AccId] [varchar](50) NULL,
	[ExchangeRateINV] [money] NULL,
	[ExchangeRateINV2] [money] NULL,
	[ReferencesIds] [varchar](250) NULL,
	[ReportCodeId] [varchar](50) NOT NULL,
	[PayDay] [int] NULL,
	[DueDate] [datetime2](7) NULL,
	[IsPaid] [bit] NOT NULL,
	[PaidDate] [datetime2](7) NULL,
	[IsLock] [bit] NOT NULL,
	[IsInvoice] [bit] NOT NULL,
	[TotalAmountTax] [money] NULL,
	[TotalAmount] [money] NULL,
	[AmountTax] [money] NULL,
	[VatInv] [money] NULL,
	[EinvoiceLink] [nvarchar](500) NULL,
	[EinvoiceCode] [varchar](50) NULL,
	[InvoiceConfigId] [varchar](50) NULL,
	[InvoiceNo] [varchar](50) NULL,
	[SeriNo] [varchar](50) NULL,
	[InvoiceDate] [datetime2](7) NULL,
	[PostDate] [datetime2](7) NULL,
	[VendorIds] [varchar](50) NULL,
	[FromDate] [datetime2](7) NULL,
	[ToDate] [datetime2](7) NULL,
	[DateFieldId] [varchar](50) NULL,
	[InvoiceIds] [varchar](250) NULL,
	[ServiceIds] [varchar](250) NULL,
	[FileIds] [varchar](250) NULL,
	[Vat] [money] NULL,
	[ShipmentStatusId] [int] NULL,
	[DebitTypeId] [int] NULL,
	[TaxInvoiceId] [int] NULL,
	[ChargePaidId] [int] NULL,
	[FeeTypeId] [varchar](250) NULL,
	[DescriptionIds] [varchar](250) NULL,
	[HblIds] [varchar](250) NULL,
	[IsMultiple] [bit] NOT NULL,
	[VatAccId] [varchar](50) NULL,
	[Form] [varchar](50) NULL,
	[InvNotes] [nvarchar](500) NULL,
	[PaymentAccTypeId] [int] NULL,
	[DebtTypeId] [int] NULL,
	[DebtAccId] [varchar](50) NULL,
	[PaymentAccId] [varchar](50) NULL,
	[AdvId] [varchar](50) NULL,
	[IsDescription] [bit] NOT NULL,
	[ReId] [varchar](50) NULL,
	[InternalRefId] [varchar](50) NULL,
	[FeatureName] [nvarchar](500) NULL,
	[CurrencyCode] [varchar](50) NULL,
 CONSTRAINT [PK_Voucher] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[VoucherDetail]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[VoucherDetail](
	[Id] [varchar](50) NOT NULL,
	[VoucherId] [varchar](50) NULL,
	[TypeId] [int] NULL,
	[ShipmentId] [varchar](50) NULL,
	[FileId] [varchar](50) NULL,
	[VendorId] [varchar](50) NULL,
	[DescriptionId] [varchar](50) NULL,
	[DescriptionIdText] [nvarchar](500) NULL,
	[TotalAmountTax] [money] NULL,
	[TotalAmount] [money] NULL,
	[AmountTax] [money] NULL,
	[Amount] [money] NULL,
	[Quantity] [money] NULL,
	[UnitId] [varchar](50) NULL,
	[Vat] [money] NULL,
	[CurrencyId] [varchar](50) NULL,
	[CurrencyCode] [varchar](10) NULL,
	[Tax] [money] NULL,
	[Notes] [nvarchar](250) NULL,
	[Docs] [nvarchar](250) NULL,
	[IsObh] [bit] NOT NULL,
	[ObhId] [varchar](50) NULL,
	[IsNoDocs] [bit] NOT NULL,
	[ExchangeRateVND] [decimal](30, 24) NULL,
	[ExchangeRateUSD] [decimal](30, 24) NULL,
	[ExchangeRateINV] [decimal](30, 24) NULL,
	[IsContainer] [bit] NOT NULL,
	[IsCBM] [bit] NOT NULL,
	[IsFreight] [bit] NOT NULL,
	[IsLogistics] [bit] NOT NULL,
	[IsTrucking] [bit] NOT NULL,
	[IsKGS] [bit] NOT NULL,
	[IsGW] [bit] NOT NULL,
	[IsGrossWeight] [bit] NOT NULL,
	[IsAbs] [bit] NOT NULL,
	[Order] [int] NULL,
	[ExchangeRate] [money] NULL,
	[SettelementNo] [nvarchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
	[BasedId] [varchar](50) NULL,
	[PmTypeId] [varchar](50) NULL,
	[ShipmentInvoiceDetailId] [varchar](50) NULL,
	[ShipmentInvoiceId] [varchar](50) NULL,
	[ShipmentInvoiceCode] [varchar](50) NULL,
	[ShipmentInvoiceDate] [datetime2](7) NULL,
	[InvoiceNo] [varchar](50) NULL,
	[ParentId] [varchar](50) NULL,
	[InvoiceIssuerId] [varchar](50) NULL,
	[NoSubmit] [bit] NOT NULL,
	[IsLock] [bit] NOT NULL,
	[SaleId] [varchar](50) NULL,
	[DeadlineDate] [datetime2](7) NULL,
	[PartnerId] [varchar](50) NULL,
	[AdvanceRequestTypeId] [varchar](50) NULL,
	[InvoiceDate] [datetime2](7) NULL,
	[SeriNo] [varchar](50) NULL,
	[InvoiceId] [varchar](50) NULL,
	[ShipmentFeeId] [varchar](50) NULL,
	[DebitAccId] [varchar](50) NULL,
	[CreditAccId] [varchar](50) NULL,
	[ExAmount] [money] NULL,
	[ExAmountTax] [money] NULL,
	[VatAccId] [varchar](50) NULL,
	[AmountDifference] [money] NULL,
	[DifferenceAccId] [varchar](50) NULL,
	[OBHAccId] [varchar](50) NULL,
	[InvoiceConfigId] [varchar](50) NULL,
	[EinvoiceLink] [nvarchar](500) NULL,
	[EinvoiceCode] [nvarchar](500) NULL,
	[FileNo] [varchar](50) NULL,
	[HblNo] [varchar](50) NULL,
	[Form] [varchar](50) NULL,
	[ExTotalAmount] [money] NULL,
	[ExTotalAmountTax] [money] NULL,
	[PaymentRequestId] [varchar](50) NULL,
	[PaymentRequestDetailId] [varchar](50) NULL,
	[PaymentDate] [datetime2](7) NULL,
	[PaymentCode] [varchar](50) NULL,
	[IsPayment] [bit] NOT NULL,
	[IsInvoice] [bit] NOT NULL,
	[InvoiceDetailId] [varchar](50) NULL,
	[InvoiceCode] [varchar](50) NULL,
	[ServiceId] [int] NULL,
	[IsVat] [bit] NOT NULL,
	[ExchangeRateINV2] [money] NULL,
 CONSTRAINT [PK_VoucherDetail] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WebConfig]    Script Date: 9/15/2025 4:05:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WebConfig](
	[Id] [varchar](50) NOT NULL,
	[Key] [nvarchar](250) NULL,
	[Value] [nvarchar](250) NULL,
	[Active] [bit] NOT NULL,
	[InsertedDate] [datetime2](7) NULL,
	[InsertedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[UpdatedBy] [varchar](50) NULL,
 CONSTRAINT [PK_WebConfig] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AccountNo] ADD  CONSTRAINT [DF_AccountNo_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[AccountNo] ADD  CONSTRAINT [DF_AccountNo_IsLast]  DEFAULT ((0)) FOR [IsLast]
GO
ALTER TABLE [dbo].[AccountTransferSetup] ADD  CONSTRAINT [DF_AccountTransferSetup_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ApprovalConfig] ADD  CONSTRAINT [DF_ApprovalConfig_IsDepartment]  DEFAULT ((0)) FOR [IsDepartment]
GO
ALTER TABLE [dbo].[ApprovalConfig] ADD  CONSTRAINT [DF_ApprovalConfig_IsTeam]  DEFAULT ((0)) FOR [IsTeam]
GO
ALTER TABLE [dbo].[ApprovalConfig] ADD  CONSTRAINT [DF_ApprovalConfig_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Approvement] ADD  CONSTRAINT [DF_Approvement_IsEnd]  DEFAULT ((0)) FOR [IsEnd]
GO
ALTER TABLE [dbo].[Approvement] ADD  CONSTRAINT [DF_Approvement_Approved]  DEFAULT ((0)) FOR [Approved]
GO
ALTER TABLE [dbo].[Approvement] ADD  CONSTRAINT [DF_Approvement_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Asset] ADD  CONSTRAINT [DF_Asset_IsDisposal]  DEFAULT ((0)) FOR [IsDisposal]
GO
ALTER TABLE [dbo].[Asset] ADD  CONSTRAINT [DF_Asset_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Booking] ADD  CONSTRAINT [DF_Booking_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Booking] ADD  CONSTRAINT [DF_Booking_IsSend]  DEFAULT ((0)) FOR [IsSend]
GO
ALTER TABLE [dbo].[Booking] ADD  CONSTRAINT [DF_Booking_NoApproved]  DEFAULT ((0)) FOR [NoApproved]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_OtherFee]  DEFAULT ((0)) FOR [OtherFee]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsContainer]  DEFAULT ((0)) FOR [IsContainer]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsCBM]  DEFAULT ((0)) FOR [IsCBM]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsFreight]  DEFAULT ((0)) FOR [IsFreight]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsKGS]  DEFAULT ((0)) FOR [IsKGS]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsGW]  DEFAULT ((0)) FOR [IsGW]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsGrossWeight]  DEFAULT ((0)) FOR [IsGrossWeight]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsBuying]  DEFAULT ((0)) FOR [IsBuying]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsObh]  DEFAULT ((0)) FOR [IsObh]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsCM]  DEFAULT ((0)) FOR [IsCM]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsLogistics]  DEFAULT ((0)) FOR [IsLogistics]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsTrucking]  DEFAULT ((0)) FOR [IsTrucking]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsAdv]  DEFAULT ((0)) FOR [IsAdv]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsAdvCustomer]  DEFAULT ((0)) FOR [IsAdvCustomer]
GO
ALTER TABLE [dbo].[BookingDetail] ADD  CONSTRAINT [DF_BookingDetail_IsAdvProvider]  DEFAULT ((0)) FOR [IsAdvProvider]
GO
ALTER TABLE [dbo].[ChatEntity] ADD  CONSTRAINT [DF_ChatEntity_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Cluster] ADD  CONSTRAINT [DF_Cluster_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_CanSearch]  DEFAULT ((0)) FOR [CanSearch]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_CanCache]  DEFAULT ((0)) FOR [CanCache]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_ShowLabel]  DEFAULT ((0)) FOR [ShowLabel]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_Disabled]  DEFAULT ((0)) FOR [Disabled]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_Visibility]  DEFAULT ((0)) FOR [Visibility]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_Focus]  DEFAULT ((0)) FOR [Focus]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_CanAdd]  DEFAULT ((0)) FOR [CanAdd]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_IsPrivate]  DEFAULT ((0)) FOR [IsPrivate]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_IsRealtime]  DEFAULT ((0)) FOR [IsRealtime]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_TopEmpty]  DEFAULT ((0)) FOR [TopEmpty]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_IsCollapsible]  DEFAULT ((0)) FOR [IsCollapsible]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_FocusSearch]  DEFAULT ((0)) FOR [FocusSearch]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_IsSumary]  DEFAULT ((0)) FOR [IsSumary]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_ShowHotKey]  DEFAULT ((0)) FOR [ShowHotKey]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_UpperCase]  DEFAULT ((0)) FOR [UpperCase]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_LiteGrid]  DEFAULT ((0)) FOR [LiteGrid]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_ShowDatetimeField]  DEFAULT ((0)) FOR [ShowDatetimeField]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_ShowNull]  DEFAULT ((0)) FOR [ShowNull]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_AddDate]  DEFAULT ((0)) FOR [AddDate]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_FilterEq]  DEFAULT ((0)) FOR [FilterEq]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_FilterLocal]  DEFAULT ((0)) FOR [FilterLocal]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_HideGrid]  DEFAULT ((0)) FOR [HideGrid]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_HasFilter]  DEFAULT ((0)) FOR [HasFilter]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_Frozen]  DEFAULT ((0)) FOR [Frozen]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_Editable]  DEFAULT ((0)) FOR [Editable]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_BasicSearch]  DEFAULT ((0)) FOR [BasicSearch]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_IsTab]  DEFAULT ((0)) FOR [IsTab]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_IsVertialTab]  DEFAULT ((0)) FOR [IsVertialTab]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_Responsive]  DEFAULT ((0)) FOR [Responsive]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_IsDropDown]  DEFAULT ((0)) FOR [IsDropDown]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_DisplayBadge]  DEFAULT ((0)) FOR [DisplayBadge]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_IsDropDown1]  DEFAULT ((0)) FOR [IsMultiple]
GO
ALTER TABLE [dbo].[Component] ADD  CONSTRAINT [DF_Component_VirtualScroll]  DEFAULT ((0)) FOR [VirtualScroll]
GO
ALTER TABLE [dbo].[ComponentDefaultValue] ADD  CONSTRAINT [DF_ComponentDefaultValue_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ConfigUnique] ADD  CONSTRAINT [DF_ConfigUnique_IsSave]  DEFAULT ((0)) FOR [IsSave]
GO
ALTER TABLE [dbo].[ConfigUnique] ADD  CONSTRAINT [DF_ConfigUnique_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Conversation] ADD  CONSTRAINT [DF_Conversation_IsSend]  DEFAULT ((0)) FOR [IsSend]
GO
ALTER TABLE [dbo].[Conversation] ADD  CONSTRAINT [DF_Conversation_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ConversationDetail] ADD  CONSTRAINT [DF_ConversationDetail_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ConversationRead] ADD  CONSTRAINT [DF_ConversationRead_Read]  DEFAULT ((0)) FOR [Read]
GO
ALTER TABLE [dbo].[ConversationRead] ADD  CONSTRAINT [DF_ConversationRead_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[DefaultFee] ADD  CONSTRAINT [DF_DefaultFee_IsObh]  DEFAULT ((0)) FOR [IsObh]
GO
ALTER TABLE [dbo].[DefaultFee] ADD  CONSTRAINT [DF_DefaultFee_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[DefaultFee] ADD  CONSTRAINT [DF_DefaultFee_IsContainer]  DEFAULT ((0)) FOR [IsContainer]
GO
ALTER TABLE [dbo].[DefaultFee] ADD  CONSTRAINT [DF_DefaultFee_IsCBM]  DEFAULT ((0)) FOR [IsCBM]
GO
ALTER TABLE [dbo].[DefaultFee] ADD  CONSTRAINT [DF_DefaultFee_IsFreight]  DEFAULT ((0)) FOR [IsFreight]
GO
ALTER TABLE [dbo].[DefaultFee] ADD  CONSTRAINT [DF_DefaultFee_IsLogistics]  DEFAULT ((0)) FOR [IsLogistics]
GO
ALTER TABLE [dbo].[DefaultFee] ADD  CONSTRAINT [DF_DefaultFee_IsTrucking]  DEFAULT ((0)) FOR [IsTrucking]
GO
ALTER TABLE [dbo].[DefaultFee] ADD  CONSTRAINT [DF_DefaultFee_IsKGS]  DEFAULT ((0)) FOR [IsKGS]
GO
ALTER TABLE [dbo].[DefaultFee] ADD  CONSTRAINT [DF_DefaultFee_IsGW]  DEFAULT ((0)) FOR [IsGW]
GO
ALTER TABLE [dbo].[DefaultFee] ADD  CONSTRAINT [DF_DefaultFee_IsGrossWeight]  DEFAULT ((0)) FOR [IsGrossWeight]
GO
ALTER TABLE [dbo].[Dictionary] ADD  CONSTRAINT [DF_Dictionary_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[EntityAttached] ADD  CONSTRAINT [DF_EntityAttached_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[EntityContainer] ADD  CONSTRAINT [DF_BookingContainer_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[EntityContainer] ADD  CONSTRAINT [DF_EntityContainer_IsPartial]  DEFAULT ((0)) FOR [IsPartial]
GO
ALTER TABLE [dbo].[EntityDimension] ADD  CONSTRAINT [DF_InquiryDeminsion_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[EntityProcess] ADD  CONSTRAINT [DF_EntityProcess_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[EntityProcessDetail] ADD  CONSTRAINT [DF_EntityProcessDetail_IsAuto]  DEFAULT ((0)) FOR [IsAuto]
GO
ALTER TABLE [dbo].[EntityProcessDetail] ADD  CONSTRAINT [DF_EntityProcessDetail_IsTracing]  DEFAULT ((0)) FOR [IsTracing]
GO
ALTER TABLE [dbo].[EntityProcessDetail] ADD  CONSTRAINT [DF_EntityProcessDetail_IsDefaultShow]  DEFAULT ((0)) FOR [IsDefaultShow]
GO
ALTER TABLE [dbo].[EntityProcessDetail] ADD  CONSTRAINT [DF_EntityProcessDetail_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[EntityTransit] ADD  CONSTRAINT [DF_BookingTransit_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ExchangeRate] ADD  CONSTRAINT [DF_ExchangeRate_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ExchangeRateHistory] ADD  CONSTRAINT [DF_ExchangeRateHistory_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ExchangeRateHistoryDetail] ADD  CONSTRAINT [DF_ExchangeRateHistoryDetail_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_IsDevider]  DEFAULT ((0)) FOR [IsDevider]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_IsGroup]  DEFAULT ((0)) FOR [IsGroup]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_IsMenu]  DEFAULT ((0)) FOR [IsMenu]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_IsPublic]  DEFAULT ((0)) FOR [IsPublic]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_StartUp]  DEFAULT ((0)) FOR [StartUp]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_IsSystem]  DEFAULT ((0)) FOR [IsSystem]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_IgnoreEncode]  DEFAULT ((0)) FOR [IgnoreEncode]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_InheritParentFeature]  DEFAULT ((0)) FOR [InheritParentFeature]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_DeleteTemp]  DEFAULT ((0)) FOR [DeleteTemp]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_CustomNextCell]  DEFAULT ((0)) FOR [CustomNextCell]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_IsPortal1]  DEFAULT ((0)) FOR [LoadEntity]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_LoadEntity1]  DEFAULT ((0)) FOR [IsLock]
GO
ALTER TABLE [dbo].[Feature] ADD  CONSTRAINT [DF_Feature_IsFlow]  DEFAULT ((0)) FOR [IsFlow]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_CanRead]  DEFAULT ((0)) FOR [CanRead]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_CanReadAll]  DEFAULT ((0)) FOR [CanReadAll]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_CanWrite]  DEFAULT ((0)) FOR [CanWrite]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_CanWriteAll]  DEFAULT ((0)) FOR [CanWriteAll]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_CanDelete]  DEFAULT ((0)) FOR [CanDelete]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_CanDeleteAll]  DEFAULT ((0)) FOR [CanDeleteAll]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_CanCopy]  DEFAULT ((0)) FOR [CanCopy]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_CanCopyAll]  DEFAULT ((0)) FOR [CanCopyAll]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_CanDeactive]  DEFAULT ((0)) FOR [CanDeactivate]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_CanDeactiveAll]  DEFAULT ((0)) FOR [CanDeactivateAll]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_CanExport]  DEFAULT ((0)) FOR [CanExport]
GO
ALTER TABLE [dbo].[FeaturePolicy] ADD  CONSTRAINT [DF_FeaturePolicy_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[FeeType] ADD  CONSTRAINT [DF_FeeType_IsFreight]  DEFAULT ((0)) FOR [IsFreight]
GO
ALTER TABLE [dbo].[FeeType] ADD  CONSTRAINT [DF_FeeType_IsFreight1]  DEFAULT ((0)) FOR [IsObh]
GO
ALTER TABLE [dbo].[FeeType] ADD  CONSTRAINT [DF_FeeType_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[FeeType] ADD  CONSTRAINT [DF_FeeType_IsTrucking]  DEFAULT ((0)) FOR [IsTrucking]
GO
ALTER TABLE [dbo].[FeeType] ADD  CONSTRAINT [DF_FeeType_IsLogistics]  DEFAULT ((0)) FOR [IsLogistics]
GO
ALTER TABLE [dbo].[FeeType] ADD  CONSTRAINT [DF_FeeType_IsAdv]  DEFAULT ((0)) FOR [IsAdv]
GO
ALTER TABLE [dbo].[FeeType] ADD  CONSTRAINT [DF_FeeType_IsAdvCustomer]  DEFAULT ((0)) FOR [IsAdvCustomer]
GO
ALTER TABLE [dbo].[FeeType] ADD  CONSTRAINT [DF_FeeType_IsAdvProvider]  DEFAULT ((0)) FOR [IsAdvProvider]
GO
ALTER TABLE [dbo].[FeeType] ADD  CONSTRAINT [DF_FeeType_IsCom]  DEFAULT ((0)) FOR [IsCom]
GO
ALTER TABLE [dbo].[FileUpload] ADD  CONSTRAINT [DF_FileUpload_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[History] ADD  CONSTRAINT [DF_History_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Inquiry] ADD  CONSTRAINT [DF_Inquiry_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Inquiry] ADD  CONSTRAINT [DF_Inquiry_IsSimple]  DEFAULT ((0)) FOR [IsSimple]
GO
ALTER TABLE [dbo].[Inquiry] ADD  CONSTRAINT [DF_Inquiry_NoApproved]  DEFAULT ((0)) FOR [NoApproved]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_TypeId]  DEFAULT ((0)) FOR [OtherFee]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsApprove]  DEFAULT ((0)) FOR [IsApproved]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsDecline]  DEFAULT ((0)) FOR [IsDecline]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsObh]  DEFAULT ((0)) FOR [IsObh]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsFreight]  DEFAULT ((0)) FOR [IsFreight]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsContainer]  DEFAULT ((0)) FOR [IsContainer]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsCBM]  DEFAULT ((0)) FOR [IsCBM]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsKGS]  DEFAULT ((0)) FOR [IsKGS]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsGW]  DEFAULT ((0)) FOR [IsGW]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsSend]  DEFAULT ((0)) FOR [IsSend]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsGrossWeight]  DEFAULT ((0)) FOR [IsGrossWeight]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsLock]  DEFAULT ((0)) FOR [DisableRow]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsLogistics]  DEFAULT ((0)) FOR [IsLogistics]
GO
ALTER TABLE [dbo].[InquiryDetail] ADD  CONSTRAINT [DF_InquiryDetail_IsTrucking]  DEFAULT ((0)) FOR [IsTrucking]
GO
ALTER TABLE [dbo].[InvoiceConfig] ADD  CONSTRAINT [DF_InvoiceConfig_Multiple]  DEFAULT ((0)) FOR [IsMultiple]
GO
ALTER TABLE [dbo].[InvoiceConfig] ADD  CONSTRAINT [DF_InvoiceConfig_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[MarkupPricing] ADD  CONSTRAINT [DF_MarkupPricing_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[MasterData] ADD  CONSTRAINT [DF_MasterData_IsLocal]  DEFAULT ((0)) FOR [IsLocal]
GO
ALTER TABLE [dbo].[MasterData] ADD  CONSTRAINT [DF_MasterData_IsContainer]  DEFAULT ((0)) FOR [IsContainer]
GO
ALTER TABLE [dbo].[MasterData] ADD  CONSTRAINT [DF_MasterData_IsCBM]  DEFAULT ((0)) FOR [IsCBM]
GO
ALTER TABLE [dbo].[MasterData] ADD  CONSTRAINT [DF_MasterData_IsKGS]  DEFAULT ((0)) FOR [IsKGS]
GO
ALTER TABLE [dbo].[MasterData] ADD  CONSTRAINT [DF_MasterData_IsGW]  DEFAULT ((0)) FOR [IsGW]
GO
ALTER TABLE [dbo].[MasterData] ADD  CONSTRAINT [DF_MasterData_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Partner] ADD  CONSTRAINT [DF_Partner_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Partner] ADD  CONSTRAINT [DF_Partner_IsInternal]  DEFAULT ((0)) FOR [IsPublic]
GO
ALTER TABLE [dbo].[Partner] ADD  CONSTRAINT [DF_Partner_IsNoDebt]  DEFAULT ((0)) FOR [IsNoDebt]
GO
ALTER TABLE [dbo].[PartnerBankAccount] ADD  CONSTRAINT [DF_PartnerBankAccount_IsMain]  DEFAULT ((0)) FOR [IsMain]
GO
ALTER TABLE [dbo].[PartnerBankAccount] ADD  CONSTRAINT [DF_PartnerBankAccount_IsReceives]  DEFAULT ((0)) FOR [IsReceives]
GO
ALTER TABLE [dbo].[PartnerBankAccount] ADD  CONSTRAINT [DF_BankAccount_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[PartnerCare] ADD  CONSTRAINT [DF_CustomerCare_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[PartnerContact] ADD  CONSTRAINT [DF_PartnerContact_Active1]  DEFAULT ((0)) FOR [IsMain]
GO
ALTER TABLE [dbo].[PartnerContact] ADD  CONSTRAINT [DF_TerminalPartner_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[PartnerCreditPeriod] ADD  CONSTRAINT [DF_PartnerCreditPeriod_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[PlanEmail] ADD  CONSTRAINT [DF_PlanEmail_IsPause1]  DEFAULT ((0)) FOR [IsCompany]
GO
ALTER TABLE [dbo].[PlanEmail] ADD  CONSTRAINT [DF_PlanEmail_IsPause]  DEFAULT ((0)) FOR [IsPause]
GO
ALTER TABLE [dbo].[PlanEmail] ADD  CONSTRAINT [DF_PlanEmail_IsRun]  DEFAULT ((0)) FOR [IsStart]
GO
ALTER TABLE [dbo].[PlanEmail] ADD  CONSTRAINT [DF_PlanEmail_IsToId]  DEFAULT ((0)) FOR [IsToId]
GO
ALTER TABLE [dbo].[PlanEmail] ADD  CONSTRAINT [DF_EmailTemplate_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[PlanEmailDetail] ADD  CONSTRAINT [DF_PlanEmailDetail_IsPause]  DEFAULT ((0)) FOR [IsPause]
GO
ALTER TABLE [dbo].[PlanEmailDetail] ADD  CONSTRAINT [DF_PlanEmailDetail_IsStart]  DEFAULT ((0)) FOR [IsStart]
GO
ALTER TABLE [dbo].[PlanEmailDetail] ADD  CONSTRAINT [DF_PlanEmailDetail_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Pricing] ADD  CONSTRAINT [DF_Pricing_IsPublic]  DEFAULT ((0)) FOR [IsPublic]
GO
ALTER TABLE [dbo].[Pricing] ADD  CONSTRAINT [DF_Pricing_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Pricing] ADD  CONSTRAINT [DF_Pricing_IsLock]  DEFAULT ((0)) FOR [IsLock]
GO
ALTER TABLE [dbo].[PricingDetail] ADD  CONSTRAINT [DF_PricingDetail_IsObh]  DEFAULT ((0)) FOR [IsObh]
GO
ALTER TABLE [dbo].[PricingDetail] ADD  CONSTRAINT [DF_PricingDetail_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[PricingDetail] ADD  CONSTRAINT [DF_PricingDetail_IsFreight]  DEFAULT ((0)) FOR [IsFreight]
GO
ALTER TABLE [dbo].[PricingDetail] ADD  CONSTRAINT [DF_PricingDetail_IsContainer]  DEFAULT ((0)) FOR [IsContainer]
GO
ALTER TABLE [dbo].[PricingDetail] ADD  CONSTRAINT [DF_PricingDetail_IsCBM]  DEFAULT ((0)) FOR [IsCBM]
GO
ALTER TABLE [dbo].[PricingDetail] ADD  CONSTRAINT [DF_PricingDetail_IsKGS]  DEFAULT ((0)) FOR [IsKGS]
GO
ALTER TABLE [dbo].[PricingDetail] ADD  CONSTRAINT [DF_PricingDetail_IsKGS1]  DEFAULT ((0)) FOR [IsGW]
GO
ALTER TABLE [dbo].[PricingDetail] ADD  CONSTRAINT [DF_PricingDetail_IsGrossWeight]  DEFAULT ((0)) FOR [IsGrossWeight]
GO
ALTER TABLE [dbo].[PricingDetail] ADD  CONSTRAINT [DF_PricingDetail_IsLogistics]  DEFAULT ((0)) FOR [IsLogistics]
GO
ALTER TABLE [dbo].[PricingDetail] ADD  CONSTRAINT [DF_PricingDetail_IsTrucking]  DEFAULT ((0)) FOR [IsTrucking]
GO
ALTER TABLE [dbo].[Role] ADD  CONSTRAINT [DF_Role_Hidden]  DEFAULT ((0)) FOR [Hidden]
GO
ALTER TABLE [dbo].[Role] ADD  CONSTRAINT [DF_Role_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[SaleFunction] ADD  CONSTRAINT [DF_SaleFunction_Active1]  DEFAULT ((0)) FOR [IsYes]
GO
ALTER TABLE [dbo].[SaleFunction] ADD  CONSTRAINT [DF_SaleFuntion_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Services] ADD  CONSTRAINT [DF_Services_IsPublicInTenant]  DEFAULT ((0)) FOR [IsPublicInTenant]
GO
ALTER TABLE [dbo].[Services] ADD  CONSTRAINT [DF_Services_Annonymous]  DEFAULT ((0)) FOR [Annonymous]
GO
ALTER TABLE [dbo].[Services] ADD  CONSTRAINT [DF_Services_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Shipment] ADD  CONSTRAINT [DF_Shipment_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Shipment] ADD  CONSTRAINT [DF_Shipment_IsDirectMaster]  DEFAULT ((0)) FOR [IsDirectMaster]
GO
ALTER TABLE [dbo].[Shipment] ADD  CONSTRAINT [DF_Shipment_NoApproved]  DEFAULT ((0)) FOR [NoApproved]
GO
ALTER TABLE [dbo].[Shipment] ADD  CONSTRAINT [DF_Shipment_LockHblNo]  DEFAULT ((0)) FOR [IsLockHblNo]
GO
ALTER TABLE [dbo].[Shipment] ADD  CONSTRAINT [DF_Shipment_IsComplete]  DEFAULT ((0)) FOR [IsComplete]
GO
ALTER TABLE [dbo].[ShipmentDO] ADD  CONSTRAINT [DF_ShipmentDO_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsObh]  DEFAULT ((0)) FOR [IsObh]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsAuto]  DEFAULT ((0)) FOR [IsManual]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_NoDocs]  DEFAULT ((0)) FOR [IsNoDocs]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsContainer]  DEFAULT ((0)) FOR [IsContainer]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsCBM]  DEFAULT ((0)) FOR [IsCBM]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsFreight]  DEFAULT ((0)) FOR [IsFreight]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsLogistics]  DEFAULT ((0)) FOR [IsLogistics]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsTrucking]  DEFAULT ((0)) FOR [IsTrucking]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsCom]  DEFAULT ((0)) FOR [IsCom]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsKGS]  DEFAULT ((0)) FOR [IsKGS]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsGW]  DEFAULT ((0)) FOR [IsGW]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsGrossWeight]  DEFAULT ((0)) FOR [IsGrossWeight]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsAbs]  DEFAULT ((0)) FOR [IsAbs]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsAn]  DEFAULT ((0)) FOR [IsAn]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_NoSubmit]  DEFAULT ((0)) FOR [NoSubmit]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsLock]  DEFAULT ((0)) FOR [IsLock]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsAdv]  DEFAULT ((0)) FOR [IsAdv]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsAdvCustomer]  DEFAULT ((0)) FOR [IsAdvCustomer]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsAdvProvider]  DEFAULT ((0)) FOR [IsAdvProvider]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsPayment]  DEFAULT ((0)) FOR [IsPayment]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsInvoice]  DEFAULT ((0)) FOR [IsInvoice]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsDebt]  DEFAULT ((0)) FOR [IsDebtAcc]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsPaid]  DEFAULT ((0)) FOR [IsPaid]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsPaymentAcc]  DEFAULT ((0)) FOR [IsPaymentAcc]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_LockHblNo]  DEFAULT ((0)) FOR [LockHblNo]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsComplete]  DEFAULT ((0)) FOR [IsComplete]
GO
ALTER TABLE [dbo].[ShipmentFee] ADD  CONSTRAINT [DF_ShipmentFee_IsLockExchange]  DEFAULT ((0)) FOR [IsLockExchange]
GO
ALTER TABLE [dbo].[ShipmentFreight] ADD  CONSTRAINT [DF_ShipmentFreight_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ShipmentInvoice] ADD  CONSTRAINT [DF_ShipmentInvoice_SendPartner]  DEFAULT ((0)) FOR [IsSendPartner]
GO
ALTER TABLE [dbo].[ShipmentInvoice] ADD  CONSTRAINT [DF_ShipmentInvoice_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ShipmentInvoice] ADD  CONSTRAINT [DF_ShipmentInvoice_IsRevenue]  DEFAULT ((0)) FOR [IsRevenue]
GO
ALTER TABLE [dbo].[ShipmentInvoice] ADD  CONSTRAINT [DF_ShipmentInvoice_IsPaid]  DEFAULT ((0)) FOR [IsPaid]
GO
ALTER TABLE [dbo].[ShipmentInvoice] ADD  CONSTRAINT [DF_ShipmentInvoice_IsReceiptPayment]  DEFAULT ((0)) FOR [IsReceiptPayment]
GO
ALTER TABLE [dbo].[ShipmentInvoice] ADD  CONSTRAINT [DF_ShipmentInvoice_IsSend]  DEFAULT ((0)) FOR [IsSend]
GO
ALTER TABLE [dbo].[ShipmentInvoice] ADD  CONSTRAINT [DF_ShipmentInvoice_NoApproved]  DEFAULT ((0)) FOR [NoApproved]
GO
ALTER TABLE [dbo].[ShipmentInvoice] ADD  CONSTRAINT [DF_ShipmentInvoice_IsMultiple]  DEFAULT ((0)) FOR [IsMultiple]
GO
ALTER TABLE [dbo].[ShipmentInvoice] ADD  CONSTRAINT [DF_ShipmentInvoice_IsSendToPartner]  DEFAULT ((0)) FOR [IsSendToPartner]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_InvoiceDetail_IsObh]  DEFAULT ((0)) FOR [IsObh]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_InvoiceDetail_IsNoDocs]  DEFAULT ((0)) FOR [IsNoDocs]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_InvoiceDetail_IsContainer]  DEFAULT ((0)) FOR [IsContainer]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_InvoiceDetail_IsCBM]  DEFAULT ((0)) FOR [IsCBM]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_InvoiceDetail_IsFreight]  DEFAULT ((0)) FOR [IsFreight]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_InvoiceDetail_IsLogistics]  DEFAULT ((0)) FOR [IsLogistics]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_InvoiceDetail_IsTrucking]  DEFAULT ((0)) FOR [IsTrucking]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_InvoiceDetail_IsKGS]  DEFAULT ((0)) FOR [IsKGS]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_InvoiceDetail_IsGW]  DEFAULT ((0)) FOR [IsGW]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_InvoiceDetail_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_InvoiceDetail_IsLock]  DEFAULT ((0)) FOR [IsLock]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_ShipmentInvoiceDetail_IsPayment]  DEFAULT ((0)) FOR [IsPayment]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_ShipmentInvoiceDetail_IsManual]  DEFAULT ((0)) FOR [IsManual]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_ShipmentInvoiceDetail_IsGrossWeight]  DEFAULT ((0)) FOR [IsGrossWeight]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_ShipmentInvoiceDetail_NoSubmit]  DEFAULT ((0)) FOR [NoSubmit]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_ShipmentInvoiceDetail_IsAdv]  DEFAULT ((0)) FOR [IsAdv]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_ShipmentInvoiceDetail_IsAdvCustomer]  DEFAULT ((0)) FOR [IsAdvCustomer]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_ShipmentInvoiceDetail_IsAdvProvider]  DEFAULT ((0)) FOR [IsAdvProvider]
GO
ALTER TABLE [dbo].[ShipmentInvoiceDetail] ADD  CONSTRAINT [DF_ShipmentInvoiceDetail_IsInvoice]  DEFAULT ((0)) FOR [IsInvoice]
GO
ALTER TABLE [dbo].[ShipmentProcess] ADD  CONSTRAINT [DF_ShipmentProcessId_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ShipmentProcessDetail] ADD  CONSTRAINT [DF_ShipmentProcess_IsTracing]  DEFAULT ((0)) FOR [IsTracing]
GO
ALTER TABLE [dbo].[ShipmentProcessDetail] ADD  CONSTRAINT [DF_ShipmentProcess_IsDefault]  DEFAULT ((0)) FOR [IsDefault]
GO
ALTER TABLE [dbo].[ShipmentProcessDetail] ADD  CONSTRAINT [DF_ShipmentProcess_IsLockShipment]  DEFAULT ((0)) FOR [IsLockShipment]
GO
ALTER TABLE [dbo].[ShipmentProcessDetail] ADD  CONSTRAINT [DF_ShipmentProcess_IsCompleteShipment]  DEFAULT ((0)) FOR [IsCompleteShipment]
GO
ALTER TABLE [dbo].[ShipmentProcessDetail] ADD  CONSTRAINT [DF_ShipmentProcess_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ShipmentSI] ADD  CONSTRAINT [DF_ShipmentSI_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ShipmentSI] ADD  CONSTRAINT [DF_ShipmentSI_IsSend]  DEFAULT ((0)) FOR [IsSend]
GO
ALTER TABLE [dbo].[ShipmentTask] ADD  CONSTRAINT [DF_ShipmentTask_IsFinish]  DEFAULT ((0)) FOR [IsFinish]
GO
ALTER TABLE [dbo].[ShipmentTask] ADD  CONSTRAINT [DF_ShipmentTask_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[ShipmentTask] ADD  CONSTRAINT [DEFAULT_ShipmentTask_IsTracing]  DEFAULT ((0)) FOR [IsTracing]
GO
ALTER TABLE [dbo].[TableName] ADD  CONSTRAINT [DF_TableName_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Tanent] ADD  CONSTRAINT [DF_Tanent_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[TaskNotification] ADD  CONSTRAINT [DF_TaskNotification_Read]  DEFAULT ((0)) FOR [Read]
GO
ALTER TABLE [dbo].[TaskNotification] ADD  CONSTRAINT [DF_TaskNotification_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Terminal] ADD  CONSTRAINT [DF_Terminal_IsLocal]  DEFAULT ((0)) FOR [IsLocal]
GO
ALTER TABLE [dbo].[Terminal] ADD  CONSTRAINT [DF_Terminal_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_IsDepartment]  DEFAULT ((0)) FOR [IsDepartment]
GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_IsDepartment1]  DEFAULT ((0)) FOR [IsTeam]
GO
ALTER TABLE [dbo].[UserLogin] ADD  CONSTRAINT [DF_UserLogin_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[UserRole] ADD  CONSTRAINT [DF_UserRole_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[UserSetting] ADD  CONSTRAINT [DF_UserSetting_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Voucher] ADD  CONSTRAINT [DF_Voucher_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[Voucher] ADD  CONSTRAINT [DF_Voucher_IsSimple]  DEFAULT ((0)) FOR [IsSimple]
GO
ALTER TABLE [dbo].[Voucher] ADD  CONSTRAINT [DF_Voucher_IsGL]  DEFAULT ((0)) FOR [ReportCodeId]
GO
ALTER TABLE [dbo].[Voucher] ADD  CONSTRAINT [DF_Voucher_IsPaid]  DEFAULT ((0)) FOR [IsPaid]
GO
ALTER TABLE [dbo].[Voucher] ADD  CONSTRAINT [DF_Voucher_IsLock]  DEFAULT ((0)) FOR [IsLock]
GO
ALTER TABLE [dbo].[Voucher] ADD  CONSTRAINT [DF_Voucher_IsInvoice]  DEFAULT ((0)) FOR [IsInvoice]
GO
ALTER TABLE [dbo].[Voucher] ADD  CONSTRAINT [DF_Voucher_IsMultiple]  DEFAULT ((0)) FOR [IsMultiple]
GO
ALTER TABLE [dbo].[Voucher] ADD  CONSTRAINT [DF_Voucher_IsDescription]  DEFAULT ((0)) FOR [IsDescription]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsObh]  DEFAULT ((0)) FOR [IsObh]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsNoDocs]  DEFAULT ((0)) FOR [IsNoDocs]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsContainer]  DEFAULT ((0)) FOR [IsContainer]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsCBM]  DEFAULT ((0)) FOR [IsCBM]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsFreight]  DEFAULT ((0)) FOR [IsFreight]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsLogistics]  DEFAULT ((0)) FOR [IsLogistics]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsTrucking]  DEFAULT ((0)) FOR [IsTrucking]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsKGS]  DEFAULT ((0)) FOR [IsKGS]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsGW]  DEFAULT ((0)) FOR [IsGW]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsGrossWeight]  DEFAULT ((0)) FOR [IsGrossWeight]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsAbs]  DEFAULT ((0)) FOR [IsAbs]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_Active_1]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_NoSubmit]  DEFAULT ((0)) FOR [NoSubmit]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsLock]  DEFAULT ((0)) FOR [IsLock]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsPayment]  DEFAULT ((0)) FOR [IsPayment]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsInvoice]  DEFAULT ((0)) FOR [IsInvoice]
GO
ALTER TABLE [dbo].[VoucherDetail] ADD  CONSTRAINT [DF_VoucherDetail_IsVat]  DEFAULT ((0)) FOR [IsVat]
GO
ALTER TABLE [dbo].[WebConfig] ADD  CONSTRAINT [DF_WebConfig_Active]  DEFAULT ((0)) FOR [Active]
GO
