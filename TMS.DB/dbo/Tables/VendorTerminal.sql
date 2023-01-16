CREATE TABLE [dbo].[VendorTerminal] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [VendorId]     INT           NULL,
    [TerminalId]   INT           NULL,
    [Active]       BIT           NOT NULL,
    [InsertedDate] DATETIME2 (7) NOT NULL,
    [InsertedBy]   INT           NOT NULL,
    [UpdatedDate]  DATETIME2 (7) NULL,
    [UpdatedBy]    INT           NULL,
    [TanentId]     INT           NOT NULL,
    CONSTRAINT [PK_VendorTerminal] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_VendorTerminal_Terminal] FOREIGN KEY ([TerminalId]) REFERENCES [dbo].[Terminal] ([Id]),
    CONSTRAINT [FK_VendorTerminal_Vendor] FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendor] ([Id])
);

