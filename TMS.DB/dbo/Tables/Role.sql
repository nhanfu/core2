CREATE TABLE [dbo].[Role] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [VendorId]     INT            NULL,
    [RoleName]     NVARCHAR (50)  NOT NULL,
    [Description]  NVARCHAR (100) NULL,
    [ParentRoleId] INT            NULL,
    [CostCenterId] INT            NULL,
    [Level]        INT            NOT NULL,
    [Path]         VARCHAR (50)   NULL,
    [Active]       BIT            NOT NULL,
    [InsertedDate] DATETIME2 (7)  NOT NULL,
    [InsertedBy]   INT            NOT NULL,
    [UpdatedDate]  DATETIME2 (7)  NULL,
    [UpdatedBy]    INT            NULL,
    [TanentId]     INT            NOT NULL,
    CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Role_CostCenter] FOREIGN KEY ([CostCenterId]) REFERENCES [dbo].[MasterData] ([Id]),
    CONSTRAINT [FK_Role_ParentRole] FOREIGN KEY ([ParentRoleId]) REFERENCES [dbo].[Role] ([Id])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Role]
    ON [dbo].[Role]([RoleName] ASC);

