CREATE TABLE [dbo].[TerminalRoute] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [RouteId]      INT           NULL,
    [TerminalId]   INT           NULL,
    [Active]       BIT           NOT NULL,
    [InsertedDate] DATETIME2 (7) NOT NULL,
    [InsertedBy]   INT           NOT NULL,
    [UpdatedDate]  DATETIME2 (7) NULL,
    [UpdatedBy]    INT           NULL,
    [TanentId]     INT           NOT NULL,
    CONSTRAINT [PK_TerminalRoute] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TerminalRoute_Route] FOREIGN KEY ([RouteId]) REFERENCES [dbo].[Route] ([Id])
);

