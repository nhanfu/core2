CREATE TABLE [dbo].[IncomeTax] (
    [Id]            INT             IDENTITY (1, 1) NOT NULL,
    [IsEnterprise]  BIT             NOT NULL,
    [Min]           DECIMAL (20, 5) NOT NULL,
    [Max]           DECIMAL (20, 5) NULL,
    [Percentage]    DECIMAL (4, 2)  NOT NULL,
    [EffectiveDate] DATETIME2 (7)   NULL,
    [ExpiredDate]   DATETIME2 (7)   NULL,
    [Active]        BIT             NOT NULL,
    [InsertedDate]  DATETIME2 (7)   NOT NULL,
    [InsertedBy]    INT             NOT NULL,
    [UpdatedDate]   DATETIME2 (7)   NULL,
    [UpdatedBy]     INT             NULL,
    [TanentId]      INT             NULL,
    CONSTRAINT [PK_IncomeTax] PRIMARY KEY CLUSTERED ([Id] ASC)
);

