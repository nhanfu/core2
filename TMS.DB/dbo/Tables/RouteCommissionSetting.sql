CREATE TABLE [dbo].[RouteCommissionSetting] (
    [Id]                   INT             IDENTITY (1, 1) NOT NULL,
    [UserId]               INT             NULL,
    [FromId]               INT             NULL,
    [ToId]                 INT             NULL,
    [CommissionAmount]     DECIMAL (20, 5) NOT NULL,
    [CommissionPercentage] DECIMAL (4, 2)  NOT NULL,
    [EffectiveDate]        DATETIME2 (7)   NULL,
    [ExpiredDate]          DATETIME2 (7)   NULL,
    [Active]               BIT             NOT NULL,
    [InsertedDate]         DATETIME2 (7)   NOT NULL,
    [InsertedBy]           INT             NOT NULL,
    [UpdatedDate]          DATETIME2 (7)   NULL,
    [UpdatedBy]            INT             NULL,
    [TruckId]              INT             NULL,
    [TanentId]             INT             NOT NULL,
    CONSTRAINT [PK_SettingPayslip] PRIMARY KEY CLUSTERED ([Id] ASC)
);

