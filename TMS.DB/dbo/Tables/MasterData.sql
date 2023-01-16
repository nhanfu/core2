CREATE TABLE [dbo].[MasterData] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [Name]         NVARCHAR (50)  NOT NULL,
    [Description]  NVARCHAR (200) NULL,
    [ParentId]     INT            NULL,
    [Path]         VARCHAR (100)  NULL,
    [Additional]   NVARCHAR (200) NULL,
    [Order]        INT            NULL,
    [Enum]         INT            NULL,
    [Active]       BIT            NOT NULL,
    [InsertedDate] DATETIME2 (7)  NOT NULL,
    [InsertedBy]   INT            NOT NULL,
    [UpdatedDate]  DATETIME2 (7)  NULL,
    [UpdatedBy]    INT            NULL,
    [TanentId]     INT            NULL,
    CONSTRAINT [PK_MasterData] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_MasterData_MasterData] FOREIGN KEY ([ParentId]) REFERENCES [dbo].[MasterData] ([Id])
);

