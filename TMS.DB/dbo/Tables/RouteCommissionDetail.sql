CREATE TABLE [dbo].[RouteCommissionDetail] (
    [Id]                   INT             IDENTITY (1, 1) NOT NULL,
    [RouteCommissionId]    INT             NOT NULL,
    [MinKm]                DECIMAL (20, 5) NOT NULL,
    [MaxKm]                DECIMAL (20, 5) NULL,
    [CommissionAmount]     DECIMAL (20, 5) NULL,
    [CommissionPercentage] DECIMAL (20, 5) NULL,
    [Active]               BIT             NOT NULL,
    [InsertedDate]         DATETIME2 (7)   NOT NULL,
    [InsertedBy]           INT             NOT NULL,
    [UpdatedDate]          DATETIME2 (7)   NULL,
    [UpdatedBy]            INT             NULL,
    [TanentId]             INT             NOT NULL,
    CONSTRAINT [PK_RouteCommissionDetail] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RouteCommissionDetail_RouteCommissionSetting] FOREIGN KEY ([RouteCommissionId]) REFERENCES [dbo].[RouteCommissionSetting] ([Id])
);

