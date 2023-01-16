CREATE TABLE [dbo].[Timebox] (
    [Id]           INT           NOT NULL,
    [TimeboxStart] VARCHAR (10)  NOT NULL,
    [TimeboxEnd]   VARCHAR (10)  NOT NULL,
    [Active]       BIT           NOT NULL,
    [InsertedDate] DATETIME2 (7) NOT NULL,
    [InsertedBy]   INT           NOT NULL,
    [UpdatedDate]  DATETIME2 (7) NULL,
    [UpdatedBy]    INT           NULL,
    [TanentId]     INT           NOT NULL,
    CONSTRAINT [PK_Timebox] PRIMARY KEY CLUSTERED ([Id] ASC)
);

