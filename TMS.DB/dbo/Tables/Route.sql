CREATE TABLE [dbo].[Route] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [IsContainer]  BIT            CONSTRAINT [DF_Route_IsContainer] DEFAULT ((0)) NOT NULL,
    [Name]         NVARCHAR (50)  NULL,
    [Description]  NVARCHAR (100) NULL,
    [Active]       BIT            NOT NULL,
    [InsertedDate] DATETIME2 (7)  NOT NULL,
    [InsertedBy]   INT            NOT NULL,
    [UpdatedDate]  DATETIME2 (7)  NULL,
    [UpdatedBy]    INT            NULL,
    [TanentId]     INT            NOT NULL,
    CONSTRAINT [PK_Route] PRIMARY KEY CLUSTERED ([Id] ASC)
);

