CREATE TABLE [dbo].[TerminalDelay] (
    [Id]              INT             IDENTITY (1, 1) NOT NULL,
    [TerminalId]      INT             NULL,
    [CommodityTypeId] INT             NULL,
    [SpecVolume]      DECIMAL (20, 5) NULL,
    [Loading]         INT             NULL,
    [Unloading]       INT             NULL,
    [Active]          BIT             NOT NULL,
    [InsertedDate]    DATETIME2 (7)   NOT NULL,
    [InsertedBy]      INT             NOT NULL,
    [UpdatedDate]     DATETIME2 (7)   NULL,
    [UpdatedBy]       INT             NULL,
    [TanentId]        INT             NOT NULL,
    CONSTRAINT [PK_TerminalDelay] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TerminalDelay_Terminal] FOREIGN KEY ([TerminalId]) REFERENCES [dbo].[Terminal] ([Id])
);

