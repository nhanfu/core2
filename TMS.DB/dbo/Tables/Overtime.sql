CREATE TABLE [dbo].[Overtime] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [Day]          INT            NOT NULL,
    [Month]        INT            NOT NULL,
    [Year]         INT            NOT NULL,
    [UserId]       INT            NOT NULL,
    [IsNight]      BIT            NOT NULL,
    [IsWeekend]    BIT            NOT NULL,
    [IsHoliday]    BIT            NOT NULL,
    [FromTime]     VARCHAR (10)   NOT NULL,
    [ToTime]       VARCHAR (10)   NOT NULL,
    [Hour]         DECIMAL (4, 2) NOT NULL,
    [Active]       BIT            NOT NULL,
    [InsertedDate] DATETIME2 (7)  NOT NULL,
    [InsertedBy]   INT            NOT NULL,
    [UpdatedDate]  DATETIME2 (7)  NULL,
    [UpdatedBy]    INT            NULL,
    [TanentId]     INT            NOT NULL,
    CONSTRAINT [PK_Overtime] PRIMARY KEY CLUSTERED ([Id] ASC)
);

