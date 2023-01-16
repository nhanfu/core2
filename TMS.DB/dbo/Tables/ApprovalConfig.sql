CREATE TABLE [dbo].[ApprovalConfig] (
    [Id]           INT             IDENTITY (1, 1) NOT NULL,
    [Level]        INT             NOT NULL,
    [Description]  NVARCHAR (50)   NULL,
    [UserId]       INT             NULL,
    [RoleId]       INT             NULL,
    [DataSource]   NVARCHAR (200)  NULL,
    [EntityId]     INT             NOT NULL,
    [WorkflowId]   INT             NULL,
    [MinAmount]    DECIMAL (20, 5) NOT NULL,
    [MaxAmount]    DECIMAL (20, 5) NULL,
    [Active]       BIT             NOT NULL,
    [InsertedDate] DATETIME2 (7)   NOT NULL,
    [InsertedBy]   INT             NOT NULL,
    [UpdatedDate]  DATETIME2 (7)   NULL,
    [UpdatedBy]    INT             NULL,
    [TanentId]     INT             NOT NULL,
    CONSTRAINT [PK_ApprovalConfig] PRIMARY KEY CLUSTERED ([Id] ASC)
);

