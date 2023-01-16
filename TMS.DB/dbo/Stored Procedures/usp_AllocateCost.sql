
create procedure [dbo].[usp_AllocateCost]
	@fromdate datetime2,
	@todate datetime2,
	@currencyId int,
	@exchangeRate decimal(20, 5),
	@ownerId int
as
begin
	delete AllocationDetail 
	from AllocationDetail as detail
	join Allocation as alloc on detail.AllocationId = alloc.Id
	where alloc.Active = 1 and (alloc.ToDate >= @fromdate or @todate >= alloc.FromDate)

	delete from Allocation
	where Active = 1 and (ToDate >= @fromdate or @todate >= FromDate)
	
	insert into Allocation (Active, CurrencyId, ExchangeRate, FromDate, InsertedBy, InsertedDate, ToDate, TotalCost, TotalRevenue)
	values (1, @currencyId, @exchangeRate, @fromdate, @ownerId, getdate(), @todate, 0, 0)
	
	declare @allocationId int = SCOPE_IDENTITY();
	
	select *
	into #FinishedCoor
	from CoordinationDetail cood
	where Active = 1 and 
	((cood.ContainerMovingTypeId = 2696 and cood.ActualContainerReturnDate >= @fromDate and cood.ActualContainerReturnDate < @todate) or
	(cood.ContainerMovingTypeId != 2696 and cood.ActualDeliveredDate >= @fromDate and cood.ActualDeliveredDate < @todate))

	declare @totalDistance decimal (20, 5) = 0;
	select @totalDistance = sum(Distance) from #FinishedCoor

	insert into AllocationDetail (Active, AllocationId, AllocationType, CoordinationDetailId, Cost, InsertedBy, InsertedDate, TruckId)
	select 1 as Active, @allocationId, 
		iif(surcharge.CollectOnBehalf = 1, N'Thu hộ ', '') + surchargeType.[Name] as AllocationType,
		coor.Id as CoordinationId, surcharge.PriceAfterTax * surcharge.ExchangeRate, @ownerId, getdate(), coor.TruckId
	from #FinishedCoor as coor
	join SurchargePayable surcharge on coor.Id = surcharge.CoordinationDetailId
	join SurchargeType surchargeType on surcharge.SurchargeTypeId = surchargeType.Id
	join PaybackPayment payback on surcharge.PaybackPaymentId = payback.Id
	where surcharge.IsPayable = 1 and payback.StatusId = 1 -- direct paid and payback was approved

	insert into AllocationDetail (Active, AllocationId, AllocationType, CoordinationDetailId, Cost, InsertedBy, InsertedDate, TruckId)
	select 1 as Active, @allocationId, truckPayable.PayableType as AllocationType, coor.Id,
		sum(truckPayable.PriceAfterTax * coor.Distance / @totalDistance +
			isnull(trailerPayable.PriceAfterTax * trailerPayable.ExchangeRate * coor.Distance / @totalDistance, 0)) as Cost,
		@ownerId, getdate(), coor.TruckId
	from #FinishedCoor coor
	join Truck truck on truck.Id = coor.TruckId
	join PayableDetail truckPayable on truck.Id = truckPayable.TruckId
	left join Truck trailer on trailer.Id = coor.TrailerId
	left join PayableDetail trailerPayable on trailer.Id = trailerPayable.TruckId
	where Cost > 0 
		and truckPayable.InvoiceDate >= @fromdate and truckPayable.InvoiceDate < @todate
		and (trailerPayable.InvoiceDate is null or trailerPayable.InvoiceDate >= @fromdate and trailerPayable.InvoiceDate < @todate)
	group by coor.Id, coor.TruckId, truckPayable.PayableType, trailerPayable.PayableType

	insert into AllocationDetail (Active, AllocationId, AllocationType, CoordinationDetailId, Cost, InsertedBy, InsertedDate, TruckId)
	select 1 as Active, @allocationId, N'Khấu hao' as AllocationType, coor.Id,
		sum(truckDepreciation.DepreciatedValue * truckDepreciation.ExchangeRate
			+ isnull(trailerDepreciation.DepreciatedValue, 0) * isnull(trailerDepreciation.ExchangeRate, 0)) as Cost,
		@ownerId, getdate(), coor.TruckId
	from #FinishedCoor coor
	join Truck truck on truck.Id = coor.TruckId
	join Depreciation truckDepreciation on truck.Id = truckDepreciation.TruckId
	left join Truck trailer on trailer.Id = coor.TrailerId
	left join Depreciation trailerDepreciation on trailer.Id = trailerDepreciation.TruckId
	where Cost > 0
		and truckDepreciation.InsertedDate >= @fromdate and truckDepreciation.InsertedDate < @todate
		and (trailerDepreciation.InsertedDate is null or trailerDepreciation.InsertedDate >= @fromdate and trailerDepreciation.InsertedDate < @todate)
	group by coor.Id, coor.TruckId, coor.TrailerId

	insert into AllocationDetail (Active, AllocationId, AllocationType, CoordinationDetailId, Cost, InsertedBy, InsertedDate, TruckId)
	select 1, @allocationId, N'Lương tài xế', coor.Id,
		sum((payslip.GrossSalary + payslip.Commission) * coor.Distance / @totalDistance) as Cost, @ownerId, getdate(), coor.TruckId
		
	from (
		select DriverId, sum(Distance) as Distance from (
			select DriverId, sum(Distance) as Distance from #FinishedCoor
			where DriverId is not null
			group by DriverId
			union
			select Driver2Id as DriverId, sum(Distance) as Distance from #FinishedCoor
			where Driver2Id is not null
			group by Driver2Id
			union
			select Driver3Id as DriverId, sum(Distance) as Distance from #FinishedCoor
			where Driver3Id is not null
			group by Driver3Id
		) as Distance
		group by DriverId
	) as distanceByDriver
	join PayslipDetail payslip on distanceByDriver.DriverId = payslip.UserId
	join [User] on payslip.UserId = [user].Id
	join #FinishedCoor coor on coor.DriverId = payslip.UserId or coor.Driver2Id = payslip.UserId or coor.Driver3Id = payslip.UserId
	where payslip.[Month] = datepart(month, @fromDate) and payslip.GrossSalary > 0 and payslip.Active = 1 and Cost > 0
	group by coor.Id, coor.TruckId

	insert into AllocationDetail (Active, AllocationId, AllocationType, CoordinationDetailId, Cost, InsertedBy, InsertedDate, TruckId)
	select 1, @allocationId, N'Hoa hồng sale', coor.Id, sum(payslip.Commission * [block].PriceAfterTax / orderDetail.TotalPriceAfterTax) as Cost,
		@ownerId, getdate(), coor.TruckId
	from #FinishedCoor coor
	join CoordinationDetailBlock coorBlock on coor.Id = coorBlock.CoordinationDetailId
	join [Block] on coorBlock.BlockId = [block].Id
	join OrderDetail orderDetail on [block].OrderDetailId = orderDetail.Id
	join [Order] [order] on orderDetail.OrderId = [order].Id
	join PayslipDetail payslip on [order].SaleId = payslip.UserId
	join [User] on payslip.UserId = [user].Id
    where payslip.Active = 1 and payslip.[Month] = datepart(Month, @fromDate)
	group by coor.Id, coor.TruckId

	insert into AllocationDetail (Active, AllocationId, AllocationType, CoordinationDetailId, Cost, InsertedBy, InsertedDate, TruckId)
	select 1, @allocationId, N'Hoa hồng điều độ', coor.Id, sum(payslip.Commission * coor.Distance / @totalDistance) as Cost,
		@ownerId, getdate(), coor.TruckId
	from #FinishedCoor coor
	join PayslipDetail payslip on coor.InsertedBy = payslip.UserId
	join [User] on payslip.UserId = [user].Id
    where payslip.Active = 1 and payslip.[Month] = datepart(Month, @fromDate)
	group by coor.Id, coor.TruckId

	update coor
	set coor.AllocatedCost = cost.Cost
	from CoordinationDetail coor
	join (
		select coor.Id, Sum(alloDetail.Cost) as Cost
		from #FinishedCoor coor
		join AllocationDetail alloDetail on coor.Id = alloDetail.CoordinationDetailId
		group by coor.Id
	) as cost on coor.Id = cost.Id


	select *
	into #FinishedCoorCost
	from (
	select coor.Id as Id, SUM(ord.TotalPriceAfterTax/ord.TotalContainer * ord.ExchangeRate) * SUM(coordb.Quantity) as PriceAfterTax, SUM(ord.TotalPriceBeforeTax/ord.TotalContainer * ord.ExchangeRate) * SUM(coordb.Quantity) as PriceBeforeTax
		from #FinishedCoor coor
		left join AllocationDetail alloDetail on coor.Id = alloDetail.CoordinationDetailId
		join CoordinationDetailBlock coordb on coordb.CoordinationDetailId = coor.Id
		join [Block] bl on coordb.BlockId = bl.Id
		join OrderDetail ord on ord.Id = bl.OrderDetailId
		group by coor.Id) as tb

	update coor
	set coor.Profit = cost.PriceAfterTax - coor.AllocatedCost, coor.PriceAfterTax = cost.PriceAfterTax
	from  CoordinationDetail coor
	join #FinishedCoorCost cost  on coor.Id = cost.Id

	update Allocation
	set TotalCost = (select isnull(sum(Cost), 0) from AllocationDetail where AllocationId = @allocationId)
	where Id = @allocationId

	update Allocation
	set TotalRevenue = (select isnull(sum(cost.PriceAfterTax), 0) from #FinishedCoor coor join CoordinationDetail cost  on coor.Id = cost.Id)
	where Id = @allocationId

	update Allocation
	set TotalPriceAfterTax = TotalRevenue -TotalCost
	where Id = @allocationId
	drop table #FinishedCoor
	drop table #FinishedCoorCost
end
