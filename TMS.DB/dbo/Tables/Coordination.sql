CREATE TABLE [dbo].[Coordination] (
    [Id]           INT             IDENTITY (1, 1) NOT NULL,
    [TotalWeight]  DECIMAL (20, 5) NULL,
    [TotalVolume]  DECIMAL (20, 5) NULL,
    [Active]       BIT             NOT NULL,
    [InsertedDate] DATETIME2 (7)   NOT NULL,
    [InsertedBy]   INT             NOT NULL,
    [UpdatedDate]  DATETIME2 (7)   NULL,
    [UpdatedBy]    INT             NULL,
    [TanentId]     INT             NOT NULL,
    CONSTRAINT [PK_Coordination] PRIMARY KEY CLUSTERED ([Id] ASC)
);

