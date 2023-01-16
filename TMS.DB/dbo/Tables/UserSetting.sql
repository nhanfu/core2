CREATE TABLE [dbo].[UserSetting] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [RoleId]       INT            NULL,
    [UserId]       INT            NULL,
    [Name]         NVARCHAR (MAX) NULL,
    [Value]        NVARCHAR (MAX) NULL,
    [ParentId]     INT            NULL,
    [Path]         VARCHAR (200)  NULL,
    [Description]  NVARCHAR (200) NULL,
    [Active]       BIT            NOT NULL,
    [InsertedDate] DATETIME2 (7)  NOT NULL,
    [InsertedBy]   INT            NOT NULL,
    [UpdatedDate]  DATETIME2 (7)  NULL,
    [UpdatedBy]    INT            NULL,
    [TanentId]     INT            NOT NULL,
    CONSTRAINT [PK_UserSetting] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserSetting_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Role] ([Id])
);

