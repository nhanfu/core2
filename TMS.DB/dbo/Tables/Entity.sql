CREATE TABLE [dbo].[Entity] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [Name]         VARCHAR (50)   NOT NULL,
    [Description]  NVARCHAR (200) NULL,
    [Active]       BIT            NOT NULL,
    [InsertedDate] DATETIME2 (7)  NOT NULL,
    [InsertedBy]   INT            NOT NULL,
    [UpdatedDate]  DATETIME2 (7)  NULL,
    [UpdatedBy]    INT            NULL,
    [TanentId]     INT            NOT NULL,
    CONSTRAINT [PK_Entity] PRIMARY KEY CLUSTERED ([Id] ASC)
);

