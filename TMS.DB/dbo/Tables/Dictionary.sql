CREATE TABLE [dbo].[Dictionary] (
    [Id]           INT            NOT NULL,
    [LangCode]     VARCHAR (50)   NULL,
    [Key]          NVARCHAR (500) NULL,
    [Value]        NVARCHAR (500) NULL,
    [Active]       BIT            NOT NULL,
    [InsertedBy]   INT            NOT NULL,
    [InsertedDate] DATETIME2 (7)  NOT NULL,
    [UpdatedBy]    INT            NULL,
    [UpdatedDate]  DATETIME2 (7)  NULL,
    [TanentId]     INT            NULL,
    CONSTRAINT [PK_Dictionary] PRIMARY KEY CLUSTERED ([Id] ASC)
);

