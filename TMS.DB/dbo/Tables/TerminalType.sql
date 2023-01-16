CREATE TABLE [dbo].[TerminalType] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [TerminalId]   INT           NULL,
    [TypeId]       INT           NULL,
    [Active]       BIT           NOT NULL,
    [InsertedDate] DATETIME2 (7) NOT NULL,
    [InsertedBy]   INT           NOT NULL,
    [UpdatedDate]  DATETIME2 (7) NULL,
    [UpdatedBy]    INT           NULL,
    [TanentId]     INT           NOT NULL,
    CONSTRAINT [PK_TerminalType] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TerminalType_Terminal] FOREIGN KEY ([TerminalId]) REFERENCES [dbo].[Terminal] ([Id])
);

