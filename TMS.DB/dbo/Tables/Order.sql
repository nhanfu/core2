CREATE TABLE [dbo].[Order] (
    [Id]                      INT             IDENTITY (1, 1) NOT NULL,
    [SendCoordinationDate]    DATETIME2 (7)   NULL,
    [IsContainer]             BIT             NOT NULL,
    [CustomerId]              INT             NULL,
    [ContractId]              INT             NULL,
    [ContactFirstName]        NVARCHAR (50)   NULL,
    [ContactLastName]         NVARCHAR (100)  NULL,
    [PhoneNumber]             VARCHAR (50)    NULL,
    [ContactSSN]              VARCHAR (50)    NULL,
    [ContactPassport]         VARCHAR (50)    NULL,
    [ContactAddress]          NVARCHAR (200)  NULL,
    [FromId]                  INT             NULL,
    [ToId]                    INT             NULL,
    [CustomerContactId]       INT             NULL,
    [ContainerTypeId]         INT             NULL,
    [TruckTypeId]             INT             NULL,
    [TotalContainer]          INT             NULL,
    [FreightStateId]          INT             NULL,
    [StatusId]                INT             CONSTRAINT [DF_Order_StatusId] DEFAULT ((0)) NOT NULL,
    [IsSend]                  BIT             CONSTRAINT [DF_Order_IsSend] DEFAULT ((0)) NOT NULL,
    [Deadline]                DATETIME2 (7)   NULL,
    [SaleId]                  INT             NOT NULL,
    [AccountableUserId]       INT             NULL,
    [AccountableDepartmentId] INT             NULL,
    [AdvancedPaid]            DECIMAL (20, 5) NOT NULL,
    [Paid]                    BIT             CONSTRAINT [DF_Order_Paid] DEFAULT ((0)) NOT NULL,
    [Vat]                     DECIMAL (4, 2)  NOT NULL,
    [TotalPriceBeforeTax]     DECIMAL (20, 5) NOT NULL,
    [TotalPriceAfterTax]      DECIMAL (20, 5) NOT NULL,
    [CurrencyId]              INT             NULL,
    [ExchangeRate]            DECIMAL (20, 5) NOT NULL,
    [FixedExchangeRate]       BIT             CONSTRAINT [DF_Order_FixedExchangeRate] DEFAULT ((0)) NOT NULL,
    [PaidDate]                DATETIME2 (7)   NULL,
    [ActualEndDate]           DATETIME2 (7)   NULL,
    [Note]                    NVARCHAR (2000) NULL,
    [Note1]                   NVARCHAR (2000) NULL,
    [Reported]                BIT             CONSTRAINT [DF__Order__Reported__51A50FA1] DEFAULT ((0)) NOT NULL,
    [TotalSurchargeBeforeTax] DECIMAL (20, 5) CONSTRAINT [DF__Order__TotalSurc__709E980D] DEFAULT ((0.0)) NOT NULL,
    [TotalSurchargeAfterTax]  DECIMAL (20, 5) CONSTRAINT [DF__Order__TotalSurc__7192BC46] DEFAULT ((0.0)) NOT NULL,
    [HaveToReceiveAfterTax]   DECIMAL (20, 5) CONSTRAINT [DF_Order_HaveToReceiveAfterTax] DEFAULT ((0)) NOT NULL,
    [HaveToReceiveBeforeTax]  DECIMAL (20, 5) CONSTRAINT [DF_Order_HaveToReceiveBeforeTax] DEFAULT ((0)) NOT NULL,
    [Mbl]                     VARCHAR (50)    NULL,
    [InvoiceNo]               VARCHAR (50)    NULL,
    [InvoiceDate]             DATETIME2 (7)   NULL,
    [Attachments]             NVARCHAR (MAX)  NULL,
    [Active]                  BIT             NOT NULL,
    [InsertedDate]            DATETIME2 (7)   NOT NULL,
    [InsertedBy]              INT             NOT NULL,
    [UpdatedDate]             DATETIME2 (7)   NULL,
    [UpdatedBy]               INT             NULL,
    [TanentId]                INT             NOT NULL,
    CONSTRAINT [PK_Order] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Order_Customer] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Vendor] ([Id]),
    CONSTRAINT [FK_Order_User_Accountable] FOREIGN KEY ([AccountableUserId]) REFERENCES [dbo].[User] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_Order_InsertedBy]
    ON [dbo].[Order]([InsertedBy] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Order_InsertedDate]
    ON [dbo].[Order]([InsertedDate] DESC);

