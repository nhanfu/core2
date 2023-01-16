CREATE TABLE [dbo].[ExchangeRate] (
    [Id]               INT             IDENTITY (1, 1) NOT NULL,
    [FromCurrencyId]   INT             NULL,
    [ToCurrencyId]     INT             NULL,
    [ExchangeRate]     DECIMAL (20, 5) NOT NULL,
    [ImageUrl]         NVARCHAR (200)  NULL,
    [BuyCash]          DECIMAL (20, 5) NOT NULL,
    [SellCash]         DECIMAL (20, 5) NOT NULL,
    [BuyBankTransfer]  DECIMAL (20, 5) NOT NULL,
    [SellBankTransfer] DECIMAL (20, 5) NOT NULL,
    [Active]           BIT             NOT NULL,
    [InsertedDate]     DATETIME2 (7)   NOT NULL,
    [InsertedBy]       INT             NOT NULL,
    [UpdatedDate]      DATETIME2 (7)   NULL,
    [UpdatedBy]        INT             NULL,
    [TanentId]         INT             NOT NULL,
    CONSTRAINT [PK_ExchangeRate] PRIMARY KEY CLUSTERED ([Id] ASC)
);

