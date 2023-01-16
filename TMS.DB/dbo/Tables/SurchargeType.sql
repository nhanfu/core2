CREATE TABLE [dbo].[SurchargeType] (
    [Id]              INT             IDENTITY (1, 1) NOT NULL,
    [Name]            NVARCHAR (50)   NOT NULL,
    [Vat]             DECIMAL (20, 2) NOT NULL,
    [PriceTypeId]     INT             NULL,
    [Description]     NVARCHAR (200)  NULL,
    [CurrencyId]      INT             NULL,
    [Active]          BIT             NOT NULL,
    [InsertedDate]    DATETIME2 (7)   NOT NULL,
    [InsertedBy]      INT             NOT NULL,
    [UpdatedDate]     DATETIME2 (7)   NULL,
    [UpdatedBy]       INT             NULL,
    [CollectOnBehalf] BIT             CONSTRAINT [DF__Surcharge__IsSel__0C1BC9F9] DEFAULT ((0)) NOT NULL,
    [TanentId]        INT             NOT NULL,
    CONSTRAINT [PK_SurchargeType] PRIMARY KEY CLUSTERED ([Id] ASC)
);

