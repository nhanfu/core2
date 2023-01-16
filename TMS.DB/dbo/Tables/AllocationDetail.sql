CREATE TABLE [dbo].[AllocationDetail] (
    [Id]                   INT             IDENTITY (1, 1) NOT NULL,
    [AllocationId]         INT             NOT NULL,
    [AllocationType]       NVARCHAR (100)  NOT NULL,
    [CoordinationDetailId] INT             NOT NULL,
    [TruckId]              INT             NULL,
    [Cost]                 DECIMAL (20, 5) NOT NULL,
    [Active]               BIT             NOT NULL,
    [InsertedDate]         DATETIME2 (7)   NOT NULL,
    [InsertedBy]           INT             NOT NULL,
    [UpdatedDate]          DATETIME2 (7)   NULL,
    [UpdatedBy]            INT             NULL,
    [TanentId]             INT             NOT NULL,
    CONSTRAINT [PK_AllocationDetail] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AllocationDetail_CoordinationDetail] FOREIGN KEY ([CoordinationDetailId]) REFERENCES [dbo].[CoordinationDetail] ([Id])
);

