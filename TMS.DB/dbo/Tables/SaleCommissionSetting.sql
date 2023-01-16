CREATE TABLE [dbo].[SaleCommissionSetting] (
    [Id]                   INT             IDENTITY (1, 1) NOT NULL,
    [SaleId]               INT             NULL,
    [MinTarget]            DECIMAL (20, 5) NOT NULL,
    [MaxTarget]            DECIMAL (20, 5) NULL,
    [CommissionPercentage] DECIMAL (4, 2)  NOT NULL,
    [CommissionAmount]     DECIMAL (20, 5) NOT NULL,
    [EffectiveDate]        DATETIME2 (7)   NULL,
    [ExpiredDate]          DATETIME2 (7)   NULL,
    [Active]               BIT             NOT NULL,
    [InsertedDate]         DATETIME2 (7)   NOT NULL,
    [InsertedBy]           INT             NOT NULL,
    [UpdatedDate]          DATETIME2 (7)   NULL,
    [UpdatedBy]            INT             NULL,
    [TanentId]             INT             NOT NULL,
    CONSTRAINT [PK_SaleSetting] PRIMARY KEY CLUSTERED ([Id] ASC)
);

