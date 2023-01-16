CREATE TABLE [dbo].[VendorService] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [VendorId]     INT           NULL,
    [ServiceId]    INT           NULL,
    [Active]       BIT           NOT NULL,
    [InsertedDate] DATETIME2 (7) NOT NULL,
    [InsertedBy]   INT           NOT NULL,
    [UpdatedDate]  DATETIME2 (7) NULL,
    [UpdatedBy]    INT           NULL,
    [TanentId]     INT           NOT NULL,
    CONSTRAINT [PK_VendorService] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_VendorService_ServiceType] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[MasterData] ([Id]),
    CONSTRAINT [FK_VendorService_Vendor] FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendor] ([Id])
);

