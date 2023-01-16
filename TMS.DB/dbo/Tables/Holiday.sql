CREATE TABLE [dbo].[Holiday] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [Name]         NVARCHAR (50) NOT NULL,
    [FromDate]     DATETIME2 (7) NULL,
    [ToDate]       DATETIME2 (7) NULL,
    [Active]       BIT           NOT NULL,
    [InsertedDate] DATETIME2 (7) NOT NULL,
    [InsertedBy]   INT           NOT NULL,
    [UpdatedDate]  DATETIME2 (7) NULL,
    [UpdatedBy]    INT           NULL,
    [TanentId]     INT           NOT NULL,
    CONSTRAINT [PK_Holiday] PRIMARY KEY CLUSTERED ([Id] ASC)
);

