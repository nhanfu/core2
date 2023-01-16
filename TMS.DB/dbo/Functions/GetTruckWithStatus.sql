use tms_demo

drop function [dbo].[GetTruckWithStatus]
go

CREATE FUNCTION [dbo].[GetTruckWithStatus] (
	@orderDate datetime2,
	@vendorId int,
	@isTanent bit
)
returns table 
return
	SELECT ISNULL(TruckCount.CountByMonth, 0) as CountByMonth, [t].[Id], CASE
		WHEN [t].[Anchored] = CAST(1 AS bit) THEN 1
		WHEN [onDateStatus].[Id] IS NOT NULL THEN 2
		ELSE 3
	END as FreightStateId, CASE
		WHEN [t].[Anchored] = CAST(1 AS bit) THEN N'Neo'
		WHEN [onDateStatus].[Id] IS NOT NULL THEN N'Đã phân chuyến'
		ELSE N'Trống'
	END as [Status], CASE
		WHEN [LastPark].[Id] IS NULL THEN NULL
		WHEN [LastPark].[ParkingId] is not null THEN [LastPark].[ParkingId]
		WHEN [LastPark].[ContainerMovingTypeId] = 2696 THEN [LastPark].[EmptyContFromId]
		ELSE [LastPark].[ToId]
	END as LastEndTerminalId, [t].[TruckPlate], [t].[TruckTypeId], [t].[DriverId], [t].[Driver2Id], [t].[Driver3Id], [t].[BoughtDate], [t].[Active], 
	[t].[ActiveDate], [t].[AdvancePaid], [t].[Anchored], [t].[BankId], [t].[BoxTypeId], [t].[BranchId], [t].[Color], [t].[CurrentOdo], [t].[ExchangeRate],
	[t].[ExpiredDate], [t].[FuelTypeId], [t].[Image], [t].[InitOdoKm], [t].[InsertedBy], [t].[InsertedDate], [t].[InUse], [t].[IsInternal], [t].[IsRent],
	[t].[KmPerLit], [t].[Lat], [t].[Long], [t].[MaintenanceEnd], [t].[MaintenancePeriod], [t].[MaintenanceStart], [t].[MaxCBM], [t].[MaxWeight], 
	[t].[Model], [t].[MonthlyPaidAmount], [t].[NextMaintenanceDate], [t].[Note], [t].[OdoKmExpiry], [t].[OdoUpdate], [t].[OdoUpdateDate], 
	[t].[TotalPaid], [t].[TotalPrice], [t].[TrailerId], [t].[UpdatedBy], [t].[UpdatedDate], [t].[VendorId], [t].[Vin], [t].[Year],
	[t].CargoHeight, [t].CargoLength, [t].CargoWidth, [t].CreditAccId, [t].CurrencyId, [t].DebitAccId, [t].DepartmentId, [t].DepreciationCoefficient, 
	[t].DepreciationCreditAccId, [t].DepreciationDebitAccId, [t].DepreciationMethodId, [t].InitOdoMile, [t].OdoMileExpiry, [t].PlateRenewal,
	[t].RentCreditAccId, [t].RentDebitAccId, [t].RoleId, t.[LitPer100Km]
	FROM [Truck] AS [t]
	outer apply (
		SELECT top 1 *
		FROM [CoordinationDetail] AS [c]
		WHERE c.TruckId = [t].Id and [c].[FreightStateId] >= 3 and [c].[FreightStateId] not in (15,16) and [OrderbyDate] < @orderDate
		order by OrderbyDate desc
	) AS [LastPark]
	outer apply (
		SELECT top 1 *
		FROM [CoordinationDetail] AS [onDateStatus]
		WHERE [t].[Id] = [onDateStatus].[TruckId] and CONVERT(date, onDateStatus.[OrderbyDate]) = CONVERT(date, @orderDate)
		order by OrderbyDate desc
	) AS [onDateStatus]
	outer apply (
		SELECT COUNT(*) AS CountByMonth FROM CoordinationDetail
		WHERE [t].Id = TruckId and datepart(year, [OrderbyDate]) = datepart(year, @orderDate)
			AND datepart(month, [OrderbyDate]) = datepart(month, @orderDate)
		GROUP BY TruckId
	) AS TruckCount
	WHERE [t].[Active] = 1 AND [t].[InUse] = 1 AND ([t].[VendorId] = @vendorId OR @isTanent = 1 and [t].[IsInternal] = 1)
