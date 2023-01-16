CREATE TABLE [dbo].[Depreciation] (
    [Id]               INT             IDENTITY (1, 1) NOT NULL,
    [AssetId]          INT             NULL,
    [EntityId]         INT             NULL,
    [MethodId]         INT             NULL,
    [SupplierId]       INT             NULL,
    [TruckId]          INT             NULL,
    [UsedMonth]        INT             NOT NULL,
    [RestMonth]        INT             NOT NULL,
    [UsedKm]           DECIMAL (20, 5) NOT NULL,
    [RestKm]           DECIMAL (20, 5) NOT NULL,
    [DepreciatedValue] DECIMAL (20, 5) NOT NULL,
    [RestValue]        DECIMAL (20, 5) CONSTRAINT [DF__Depreciat__RestV__7A672E12] DEFAULT ((0)) NOT NULL,
    [CurrencyId]       INT             NULL,
    [ExchangeRate]     DECIMAL (20, 5) NOT NULL,
    [Active]           BIT             NOT NULL,
    [InsertedDate]     DATETIME2 (7)   NOT NULL,
    [InsertedBy]       INT             NOT NULL,
    [UpdatedDate]      DATETIME2 (7)   NULL,
    [UpdatedBy]        INT             NULL,
    [TanentId]         INT             NOT NULL,
    CONSTRAINT [PK_Depreciation] PRIMARY KEY CLUSTERED ([Id] ASC)
);

