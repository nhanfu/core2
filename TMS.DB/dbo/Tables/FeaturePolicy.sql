CREATE TABLE [dbo].[FeaturePolicy] (
    [Id]                     INT           IDENTITY (1, 1) NOT NULL,
    [FeatureId]              INT           NULL,
    [RoleId]                 INT           NULL,
    [CanRead]                BIT           NOT NULL,
    [CanWrite]               BIT           NOT NULL,
    [CanDelete]              BIT           NOT NULL,
    [Active]                 BIT           NOT NULL,
    [InsertedDate]           DATETIME2 (7) NOT NULL,
    [InsertedBy]             INT           NOT NULL,
    [UpdatedDate]            DATETIME2 (7) NULL,
    [UpdatedBy]              INT           NULL,
    [CanDeactivate]          BIT           CONSTRAINT [DF__FeaturePo__CanDe__4A03EDD9] DEFAULT ((0)) NOT NULL,
    [LockDeleteAfterCreated] INT           NULL,
    [LockUpdateAfterCreated] INT           NULL,
    [EntityId]               INT           NULL,
    [RecordId]               INT           NOT NULL,
    [UserId]                 INT           NULL,
    [CanShare]               BIT           DEFAULT ((0)) NOT NULL,
    [TanentId]               INT           NOT NULL,
    CONSTRAINT [PK_FeaturePolicy] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_FeaturePolicy_Feature] FOREIGN KEY ([FeatureId]) REFERENCES [dbo].[Feature] ([Id]),
    CONSTRAINT [FK_FeaturePolicy_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Role] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_Feature]
    ON [dbo].[FeaturePolicy]([FeatureId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Record]
    ON [dbo].[FeaturePolicy]([EntityId] ASC, [RecordId] ASC);

