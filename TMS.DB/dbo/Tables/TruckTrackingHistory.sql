CREATE TABLE [dbo].[TruckTrackingHistory] (
    [Id]           BIGINT        IDENTITY (1, 1) NOT NULL,
    [TruckId]      INT           NOT NULL,
    [PackageId]    INT           NULL,
    [TruckPlate]   VARCHAR (20)  NULL,
    [Distance]     INT           NULL,
    [FreightState] INT           NULL,
    [GpsTime]      DATETIME2 (7) NULL,
    [Lat]          FLOAT (53)    NULL,
    [Long]         FLOAT (53)    NULL,
    [Speed]        FLOAT (53)    NULL,
    [X]            FLOAT (53)    NULL,
    [Y]            FLOAT (53)    NULL,
    [InsertedDate] DATETIME2 (7) NULL,
    [SysTime]      DATETIME2 (7) NULL,
    [Status]       INT           NULL,
    [Heading]      INT           NULL,
    [TanentId]     INT           NOT NULL,
    CONSTRAINT [PK_TruckTrackingHistory] PRIMARY KEY CLUSTERED ([Id] ASC)
);

