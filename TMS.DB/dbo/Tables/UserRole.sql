CREATE TABLE [dbo].[UserRole] (
    [Id]            INT           IDENTITY (1, 1) NOT NULL,
    [UserId]        INT           NOT NULL,
    [RoleId]        INT           NOT NULL,
    [Active]        BIT           NOT NULL,
    [EffectiveDate] DATETIME2 (7) NULL,
    [ExpiredDate]   DATETIME2 (7) NULL,
    [InsertedDate]  DATETIME2 (7) NOT NULL,
    [InsertedBy]    INT           NOT NULL,
    [UpdatedDate]   DATETIME2 (7) NULL,
    [UpdatedBy]     INT           NULL,
    [TanentId]      INT           NOT NULL,
    CONSTRAINT [PK_UserRole] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserRole_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Role] ([Id]),
    CONSTRAINT [FK_UserRole_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_UserRole_UserId]
    ON [dbo].[UserRole]([UserId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_UserRole_RoleId]
    ON [dbo].[UserRole]([RoleId] ASC);

