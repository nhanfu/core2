CREATE TABLE [dbo].[UserLogin] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [UserId]       INT           NOT NULL,
    [IpAddress]    VARCHAR (50)  NULL,
    [SignInDate]   DATETIME2 (7) NULL,
    [RefreshToken] NVARCHAR (50) NULL,
    [ExpiredDate]  DATETIME2 (7) NULL,
    [TanentId]     INT           NOT NULL,
    CONSTRAINT [PK_UserLogin] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_SignInDate]
    ON [dbo].[UserLogin]([SignInDate] DESC);

