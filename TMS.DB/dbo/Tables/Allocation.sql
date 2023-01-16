CREATE TABLE [dbo].[Allocation] (
    [Id]                 INT             IDENTITY (1, 1) NOT NULL,
    [FromDate]           DATETIME2 (7)   NOT NULL,
    [ToDate]             DATETIME2 (7)   NOT NULL,
    [TotalCost]          DECIMAL (20, 5) CONSTRAINT [DF_Allocation_Cost] DEFAULT ((0)) NOT NULL,
    [TotalRevenue]       DECIMAL (20, 5) CONSTRAINT [DF_Allocation_Revenue] DEFAULT ((0)) NOT NULL,
    [TotalPriceAfterTax] DECIMAL (20, 5) CONSTRAINT [DF_Allocation_TotalRevenue1] DEFAULT ((0)) NOT NULL,
    [CurrencyId]         INT             NULL,
    [ExchangeRate]       DECIMAL (20, 5) CONSTRAINT [DF_Allocation2_ExchangeRate] DEFAULT ((1)) NOT NULL,
    [Active]             BIT             NOT NULL,
    [InsertedDate]       DATETIME2 (7)   NOT NULL,
    [InsertedBy]         INT             NOT NULL,
    [UpdatedDate]        DATETIME2 (7)   NULL,
    [UpdatedBy]          INT             NULL,
    [TanentId]           INT             NOT NULL,
    CONSTRAINT [PK_Allocation] PRIMARY KEY CLUSTERED ([Id] ASC)
);

