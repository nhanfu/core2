USE [softek_donga]
GO
/****** Object:  Trigger [dbo].[tr_Transportation_UpdateTeus]    Script Date: 11/10/2022 10:03:26 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER trigger [dbo].[tr_Transportation_UpdateTeus] on [dbo].[Transportation]
after update,Insert
as
begin
	update b set 
	b.Teus20Using = isnull((select COUNT(Id) from Transportation where BookingId = b.Id and Cont20 = 1),0),
	b.Teus40Using = isnull((select COUNT(Id) from Transportation where BookingId = b.Id and Cont40 = 1),0),
	b.Teus20Remain = b.Teus20 - isnull((select COUNT(Id) from Transportation where BookingId = b.Id and Cont20 = 1),0),
	b.Teus40Remain = b.Teus40 - isnull((select COUNT(Id) from Transportation where BookingId = b.Id and Cont40 = 1),0)
	from Booking b
	join deleted on b.Id = deleted.BookingId

	update b set 
	b.Teus20Using = isnull((select COUNT(Id) from Transportation where BookingId = b.Id and Cont20 = 1),0),
	b.Teus40Using = isnull((select COUNT(Id) from Transportation where BookingId = b.Id and Cont40 = 1),0),
	b.Teus20Remain = b.Teus20-isnull((select COUNT(Id) from Transportation where BookingId = b.Id and Cont20 = 1),0),
	b.Teus40Remain = b.Teus40-isnull((select COUNT(Id) from Transportation where BookingId = b.Id and Cont40 = 1),0)
	from Booking b
	join inserted on b.Id = inserted.BookingId


	update t set 
	t.Teus20Using = isnull((select COUNT(Id) from Transportation where  BrandShipId = t.BrandShipId and ShipId = t.ShipId and Trip = t.Trip and t.StartShip = t.StartShip  and Cont20 = 1),0),
	t.Teus40Using = isnull((select COUNT(Id) from Transportation where  BrandShipId = t.BrandShipId and ShipId = t.ShipId and Trip = t.Trip and t.StartShip = t.StartShip  and Cont40 = 1),0),
	t.Teus20Remain = t.Teus20-isnull((select COUNT(Id) from Transportation where  BrandShipId = t.BrandShipId and ShipId = t.ShipId and Trip = t.Trip and t.StartShip = t.StartShip  and Cont20 = 1),0),
	t.Teus40Remain = t.Teus40-isnull((select COUNT(Id) from Transportation where  BrandShipId = t.BrandShipId and ShipId = t.ShipId and Trip = t.Trip and t.StartShip = t.StartShip  and Cont40 = 1),0)
	from Teus t
	join deleted on t.BrandShipId = deleted.BrandShipId 
	and t.ShipId = deleted.ShipId
	and t.Trip = deleted.Trip
	and t.StartShip = deleted.StartShip

	update t set 
	t.Teus20Using = isnull((select COUNT(Id) from Transportation where  BrandShipId = t.BrandShipId and ShipId = t.ShipId and Trip = t.Trip and t.StartShip = t.StartShip  and Cont20 = 1),0),
	t.Teus40Using = isnull((select COUNT(Id) from Transportation where  BrandShipId = t.BrandShipId and ShipId = t.ShipId and Trip = t.Trip and t.StartShip = t.StartShip  and Cont40 = 1),0),
	t.Teus20Remain = t.Teus20-isnull((select COUNT(Id) from Transportation where BrandShipId = t.BrandShipId and ShipId = t.ShipId and Trip = t.Trip and t.StartShip = t.StartShip and Cont20 = 1),0),
	t.Teus40Remain = t.Teus40-isnull((select COUNT(Id) from Transportation where BrandShipId = t.BrandShipId and ShipId = t.ShipId and Trip = t.Trip and t.StartShip = t.StartShip  and Cont40 = 1),0)
	from Teus t
	join inserted on t.BrandShipId = inserted.BrandShipId 
	and t.ShipId = inserted.ShipId
	and t.Trip = inserted.Trip
	and t.StartShip = inserted.StartShip
	--Tính CVC đóng hàng
	update Transportation set ClosingUnitPrice =  isnull((select top 1 CASE
    WHEN inserted.IsClampingFee = 1 THEN UnitPrice1
    ELSE UnitPrice
	END as UnitPrice from Quotation 
	where PackingId = inserted.ClosingId 
	and TypeId = 7592
	and BossId = inserted.BossId 
	and ContainerTypeId = inserted.ContainerTypeId 
	and LocationId = inserted.ReceivedId 
	and StartDate <= inserted.ClosingDate order by StartDate desc),inserted.ClosingUnitPrice)
	,IsQuotation = isnull((select top 1 1 from Quotation 
	where PackingId = inserted.ClosingId 
	and TypeId = 7592
	and BossId = inserted.BossId 
	and ContainerTypeId = inserted.ContainerTypeId 
	and LocationId = inserted.ReceivedId 
	and StartDate <= inserted.ClosingDate order by StartDate desc),0)
	from Transportation
	join inserted on inserted.Id = Transportation.Id
	
	--Tính CVC trả hàng
	update Transportation set ReturnUnitPrice =  isnull((select top 1 CASE
    WHEN inserted.IsClampingReturnFee = 1 THEN UnitPrice1
    ELSE UnitPrice
	END as UnitPrice from Quotation 
	where BossId = inserted.BossId 
	and TypeId = 7593
	and PackingId = inserted.ReturnVendorId 
	and ContainerTypeId = inserted.ContainerTypeId 
	and LocationId = inserted.ReturnId 
	and StartDate <= inserted.ReturnDate order by StartDate desc),inserted.ReturnUnitPrice)
	,IsQuotationReturn = isnull((select top 1 1 from Quotation 
	where BossId = inserted.BossId 
	and TypeId = 7593
	and PackingId = inserted.ReturnVendorId 
	and ContainerTypeId = inserted.ContainerTypeId 
	and LocationId = inserted.ReturnId 
	and StartDate <= inserted.ReturnDate order by StartDate desc),0)
	from Transportation
	join inserted on inserted.Id = Transportation.Id
	-- Nâng đóng = hạ trả = rỗng
	--Tính Phí nâng (đóng hàng)
	update Transportation set LiftFee =  (select top 1 CASE
    WHEN inserted.IsEmptyLift = 1 THEN UnitPrice1
    ELSE UnitPrice
	END as UnitPrice from Quotation 
	where 
	TypeId = 7594
	and ContainerTypeId = inserted.ContainerTypeId 
	and LocationId = inserted.PickupEmptyId 
	and StartDate <= inserted.ClosingDate order by StartDate desc)
	from Transportation
	join inserted on inserted.Id = Transportation.Id
	--Tính Phí hạ (trả hàng)
	update Transportation set ReturnClosingFee =(case when Transportation.ISIncluded = 1 then 0 else  (select top 1 CASE
    WHEN inserted.IsClosingEmptyFee = 1 THEN UnitPrice1
    ELSE UnitPrice
	END as UnitPrice from Quotation 
	where 
	TypeId = 7594
	and ContainerTypeId = inserted.ContainerTypeId 
	and LocationId = inserted.ReturnEmptyId 
	and (StartDate <= inserted.ReturnDate or inserted.ReturnDate is null) order by StartDate desc) end)
	from Transportation
	join inserted on inserted.Id = Transportation.Id
	--Tính Phí nâng (Trả hàng)
	update Transportation set ReturnLiftFee =  (select top 1 CASE
    WHEN inserted.IsLiftFee = 1 THEN UnitPrice1
    ELSE UnitPrice
	END as UnitPrice from Quotation 
	where TypeId = 7596
	and ContainerTypeId = inserted.ContainerTypeId 
	and LocationId = inserted.PortLiftId 
	and (StartDate <= inserted.ReturnDate or inserted.ReturnDate is null) order by StartDate desc)
	from Transportation
	join inserted on inserted.Id = Transportation.Id
	--Tính Phí hạ (đóng hàng)
	update Transportation set LandingFee =  (select top 1 CASE
    WHEN inserted.IsLanding = 1 THEN UnitPrice1
    ELSE UnitPrice
	END as UnitPrice from Quotation 
	where 
	TypeId = 7596
	and ContainerTypeId = inserted.ContainerTypeId 
	and LocationId = inserted.PortLoadingId 
	and StartDate <= inserted.ClosingDate order by StartDate desc)
	from Transportation
	join inserted on inserted.Id = Transportation.Id
	--Tính Phí kết hợp
	update Transportation set
	CombinationFee =  (select top 1  CASE
	WHEN (inserted.IsEmptyCombination = 1 or inserted.IsClosingCustomer = 1) THEN UnitPrice
    ELSE null
	END as UnitPrice
	from Quotation
	where 
	TypeId = 12071
	and PackingId = inserted.BrandShipId 
	and StartDate <= inserted.ClosingDate order by StartDate desc)
	from Transportation
	join inserted on inserted.Id = Transportation.Id
	--Tính cước tàu
	update Transportation set ShipUnitPrice =  
	isnull((select top 1 UnitPrice
	from Quotation
	where 
	TypeId = 7598
	and RouteId = inserted.RouteId 
	and ContainerTypeId = inserted.ContainerTypeId 
	and PackingId = inserted.BrandShipId 
	and StartDate <= inserted.StartShip order by StartDate desc),
	case when Transportation.Cont20 = 1 then
	(select top 1 UnitPrice
	from Quotation
	where 
	TypeId = 7598
	and RouteId = inserted.RouteId 
	and ContainerTypeId = 14910
	and PackingId = inserted.BrandShipId 
	and StartDate <= inserted.StartShip order by StartDate desc)
	when Transportation.Cont40 = 1 then
	(select top 1 UnitPrice
	from Quotation
	where 
	TypeId = 7598
	and RouteId = inserted.RouteId 
	and ContainerTypeId = 14909 
	and PackingId = inserted.BrandShipId 
	and StartDate <= inserted.StartShip order by StartDate desc)
	end
	)
	from Transportation
	join inserted on inserted.Id = Transportation.Id
	--
	update Transportation set ClosingCombinationUnitPrice =  (Transportation.ClosingUnitPrice * (case when  Transportation.ClosingPercent = 0 then 100 else Transportation.ClosingPercent end))/100,
	ClosingPercent=(case when  Transportation.ClosingPercent = 0 then 100 else Transportation.ClosingPercent end),
	ShipPrice = Transportation.ShipUnitPrice - Transportation.ShipPolicyPrice
	from Transportation
	join inserted on inserted.Id = Transportation.Id

	-- Update Nơi trả rỗng rỗng kết hợp
	-- 93638 rỗng kết hợp không thu phí
	update Transportation set ReturnEmptyId = 93638, ReturnClosingFee = null
	from Transportation
	join inserted on Transportation.Id = inserted.EmptyCombinationId

	update Transportation set Note4 = Vendor.Name, IsHost= (case when (Ex.RouteId = inserted.RouteId or Route.Name like N'%Đường%') then 1 else 0 end)
	,IsBooking =(case when Route.Name like N'%Đường%' then 0 else 1 end)
	from Transportation
	join inserted on Transportation.Id = inserted.Id
	left join Vendor as Ex on Transportation.ExportListId = Ex.Id
	left join Route on Transportation.RouteId = Route.Id
	left join Vendor on Vendor.Id = inserted.BossId

	update tp set TotalContainerUsing = tb.c,
	TotalContainerRemain = TotalContainer-tb.c,
	IsTransportation = case when TotalContainer-tb.c = 0 then 1
	else 0 end
	from TransportationPlan tp
	join (
	select tpd.Id, COUNT(tr.Id) as c
	from TransportationPlan tpd
	left join Transportation tr on tpd.Id = tr.TransportationPlanId
	where tpd.Id in (select TransportationPlanId from inserted)
	group by tpd.Id) as tb on tp.Id = tb.Id
	Update e
	set e.ContainerNo = i.ContainerNo, e.SealNo = i.SealNo
	from Expense e join inserted i on e.TransportationId = i.Id
	--Update totalFee
	update Transportation set OrtherFeeInvoinceNo = isnull((
	select SUM(isnull(TotalPriceAfterTax,0))
	from Expense
	join MasterData on Expense.ExpenseTypeId = MasterData.Id
	where TransportationId = Transportation.Id and IsVat = 1 and IsCollectOnBehaft = 0 and IsReturn = 0
	and (MasterData.Additional is null or MasterData.Additional = '')
	),0),
	CollectOnBehaftInvoinceNoFee = isnull((
	select SUM(isnull(TotalPriceAfterTax,0))
	from Expense
	join MasterData on Expense.ExpenseTypeId = MasterData.Id
	where TransportationId = Transportation.Id and IsVat = 1 and IsCollectOnBehaft = 1 and IsReturn = 0
	and (MasterData.Additional is null or MasterData.Additional = '')
	),0),
	OrtherFee = isnull((
	select SUM(isnull(TotalPriceAfterTax,0))
	from Expense
	join MasterData on Expense.ExpenseTypeId = MasterData.Id
	where TransportationId = Transportation.Id and IsVat = 0 and IsCollectOnBehaft = 0 and IsReturn = 0
	and (MasterData.Additional is null or MasterData.Additional = '')
	),0),
	CollectOnBehaftFee = isnull((
	select SUM(isnull(TotalPriceAfterTax,0))
	from Expense
	join MasterData on Expense.ExpenseTypeId = MasterData.Id
	where TransportationId = Transportation.Id and IsVat = 0 and IsCollectOnBehaft = 1 and IsReturn = 0
	and (MasterData.Additional is null or MasterData.Additional = '')
	),0),
	--
	ReturnOrtherInvoinceFee = isnull((
	select SUM(isnull(TotalPriceAfterTax,0))
	from Expense
	join MasterData on Expense.ExpenseTypeId = MasterData.Id
	where TransportationId = Transportation.Id and IsVat = 1 and IsCollectOnBehaft = 0 and IsReturn = 1
	and (MasterData.Additional is null or MasterData.Additional = '')
	),0),
	ReturnCollectOnBehaftInvoinceFee = isnull((
	select SUM(isnull(TotalPriceAfterTax,0))
	from Expense
	join MasterData on Expense.ExpenseTypeId = MasterData.Id
	where TransportationId = Transportation.Id and IsVat = 1 and IsCollectOnBehaft = 1 and IsReturn = 1
	and (MasterData.Additional is null or MasterData.Additional = '')
	),0),
	ReturnOrtherFee = isnull((
	select SUM(isnull(TotalPriceAfterTax,0))
	from Expense
	join MasterData on Expense.ExpenseTypeId = MasterData.Id
	where TransportationId = Transportation.Id and IsVat = 0 and IsCollectOnBehaft = 0 and IsReturn = 1
	and (MasterData.Additional is null or MasterData.Additional = '')
	),0),
	ReturnCollectOnBehaftFee = isnull((
	select SUM(isnull(TotalPriceAfterTax,0))
	from Expense
	join MasterData on Expense.ExpenseTypeId = MasterData.Id
	where TransportationId = Transportation.Id and IsVat = 0 and IsCollectOnBehaft = 1 and IsReturn = 1
	and (MasterData.Additional is null or MasterData.Additional = '')
	),0),
	Cont20=(case when MasterData.Enum = 1 then 1
	else 0 end),
	Cont40=(case when MasterData.Enum = 2 then 1
	else 0 end)
	from Transportation
	join inserted on Transportation.Id = inserted.Id
	join MasterData on inserted.ContainerTypeId = MasterData.Id
	--update ReturnClosingFeeReport
	update Transportation set ReturnClosingFeeReport = Transportation.ReturnClosingFee
	from Transportation
	join inserted on Transportation.Id = inserted.Id
	--
	update Transportation set ReturnClosingFeeReport = (case when inserted.ReturnClosingFeeReport != deleted.ReturnClosingFeeReport then inserted.ReturnClosingFeeReport
	else deleted.ReturnClosingFeeReport end)
	from Transportation
	join deleted on Transportation.Id = deleted.Id
	join inserted on Transportation.Id = inserted.Id
end
