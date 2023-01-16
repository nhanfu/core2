CREATE TABLE [dbo].[MonthlyPaid] (
    [Id]            INT             IDENTITY (1, 1) NOT NULL,
    [TruckId]       INT             NOT NULL,
    [CurrencyId]    INT             NULL,
    [ExchangeRate]  DECIMAL (20, 5) NOT NULL,
    [IsPaid]        BIT             NULL,
    [TotalPaid]     DECIMAL (20, 5) NOT NULL,
    [InvoiceNo]     VARCHAR (50)    NULL,
    [InvoiceDate]   DATETIME2 (7)   NOT NULL,
    [InvoiceForm]   NVARCHAR (30)   NULL,
    [Attachments]   NVARCHAR (200)  NULL,
    [CreditAccId]   INT             NULL,
    [DebitAccId]    INT             NULL,
    [PaymentTypeId] INT             NULL,
    [Active]        BIT             NOT NULL,
    [InsertedDate]  DATETIME2 (7)   NOT NULL,
    [InsertedBy]    INT             NOT NULL,
    [UpdatedDate]   DATETIME2 (7)   NULL,
    [UpdatedBy]     INT             NULL,
    [TanentId]      INT             NOT NULL,
    CONSTRAINT [PK_MonthlyPaid_1] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_MonthlyPaid_Truck] FOREIGN KEY ([TruckId]) REFERENCES [dbo].[Truck] ([Id])
);

