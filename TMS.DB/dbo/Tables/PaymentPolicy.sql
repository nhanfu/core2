CREATE TABLE [dbo].[PaymentPolicy] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [PolicyId]     INT           NULL,
    [MaxApproval]  FLOAT (53)    NOT NULL,
    [Active]       BIT           NOT NULL,
    [InsertedDate] DATETIME2 (7) NOT NULL,
    [InsertedBy]   INT           NOT NULL,
    [UpdatedDate]  DATETIME2 (7) NULL,
    [UpdatedBy]    INT           NULL,
    [TanentId]     INT           NOT NULL,
    CONSTRAINT [PK_PaymentPolicy] PRIMARY KEY CLUSTERED ([Id] ASC)
);

