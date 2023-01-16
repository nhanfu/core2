CREATE TABLE [dbo].[VendorBranch] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [CustomerId]   INT            NULL,
    [VendorId]     INT            NULL,
    [RegionId]     INT            NULL,
    [Address]      NVARCHAR (200) NULL,
    [PhoneNumber]  VARCHAR (50)   NULL,
    [InsertedDate] DATETIME2 (7)  NOT NULL,
    [InsertedBy]   INT            NULL,
    [UpdatedDate]  DATETIME2 (7)  NULL,
    [UpdatedBy]    INT            NULL,
    [Active]       BIT            NOT NULL,
    [TanentId]     INT            NOT NULL,
    CONSTRAINT [PK_VendorBranch] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_VendorBranch_Vendor] FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendor] ([Id])
);

