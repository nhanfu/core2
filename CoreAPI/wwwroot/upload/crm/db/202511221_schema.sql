create database crm
go
use crm
go

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
ALTER TABLE [dbo].[Role] ADD  CONSTRAINT [DF_Role_Hidden]  DEFAULT ((0)) FOR [Hidden]
GO
ALTER TABLE [dbo].[Role] ADD  CONSTRAINT [DF_Role_Active]  DEFAULT ((0)) FOR [Active]
GO
ALTER TABLE [dbo].[TaskNotification] ADD  CONSTRAINT [DF_TaskNotification_Read]  DEFAULT ((0)) FOR [Read]
GO
ALTER TABLE [dbo].[TaskNotification] ADD  CONSTRAINT [DF_TaskNotification_Active]  DEFAULT ((0)) FOR [Active]
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
