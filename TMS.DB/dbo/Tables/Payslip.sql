CREATE TABLE [dbo].[Payslip] (
    [Id]              INT             IDENTITY (1, 1) NOT NULL,
    [TotalSalary]     DECIMAL (20, 5) NOT NULL,
    [FromDate]        DATETIME2 (7)   NOT NULL,
    [ToDate]          DATETIME2 (7)   NOT NULL,
    [Month]           INT             NOT NULL,
    [TotalWorkingDay] INT             NOT NULL,
    [Year]            INT             NOT NULL,
    [IsSend]          BIT             CONSTRAINT [DF_Payslip_IsSend] DEFAULT ((0)) NOT NULL,
    [IsSendPayslip]   BIT             CONSTRAINT [DF_Payslip_IsSendPayslip] DEFAULT ((0)) NOT NULL,
    [StatusId]        INT             NOT NULL,
    [PayslipStatusId] INT             CONSTRAINT [DF_Payslip_PayslipId] DEFAULT ((0)) NOT NULL,
    [Active]          BIT             NOT NULL,
    [InsertedDate]    DATETIME2 (7)   NOT NULL,
    [InsertedBy]      INT             NOT NULL,
    [UpdatedDate]     DATETIME2 (7)   NULL,
    [UpdatedBy]       INT             NULL,
    [TanentId]        INT             NOT NULL,
    CONSTRAINT [PK_Payslip] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Payslip_User] FOREIGN KEY ([InsertedBy]) REFERENCES [dbo].[User] ([Id])
);

