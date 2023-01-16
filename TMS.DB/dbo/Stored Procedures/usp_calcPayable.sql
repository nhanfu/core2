create procedure [dbo].[usp_calcPayable]
	@fromdate datetime2,
	@todate datetime2,
	@dateType int,
	@ownerId int,
	@venderIds varchar(100)
as
begin
	declare @date datetime2(7) = getdate()
	select *
	into #ListVendor
    from [dbo].[SplitStringToTable](@venderIds, ',')

	declare @checkAll bit = 0
	set @checkAll = (select case when count(*) > 0 then 1 else 0 end from #ListVendor)

	insert into PayableDetail (TruckId,CoordinationDetailId, InvoiceNo, InvoiceDate, EntityId, RecordId, [Description],
		PriceBeforeTax, Vat, PriceAfterTax, CurrencyId, ExchangeRate, SupplierId, Attachments, 
		PayableType, Active, InsertedBy, InsertedDate)
	-- 2108: SurchargePayable
	select cood.TruckId,cood.Id, surcharge.InvoiceNo, surcharge.InvoiceDate, 2108, surcharge.Id, surcharge.[Description], 
		surcharge.PriceBeforeTax, surcharge.Vat, surcharge.PriceAfterTax, surcharge.CurrencyId, 
		iif([surcharge].FixedExchangeRate = 1, [surcharge].ExchangeRate, isnull(ex.ExchangeRate, 1)), surcharge.SupplierId, surcharge.Attachments,
		surchargeType.[Name], 1, @ownerId, @date
	from SurchargePayable surcharge
	join CoordinationDetail cood on cood.Id = surcharge.CoordinationDetailId
	join SurchargeType surchargeType on surcharge.SurchargeTypeId = surchargeType.Id 
	outer apply GetExchangeRate([surcharge].CurrencyId) as ex
	where surcharge.IsPayable = 1 and -- Công nợ
		((@dateType = 1 and cood.ReceivedDate >= @fromDate and cood.ReceivedDate < @todate) or
		(@dateType = 2 and cood.DeliveryDate >= @fromDate and cood.DeliveryDate < @todate) or
		(@dateType = 3 and cood.OrderbyDate >= @fromDate and cood.OrderbyDate < @todate))
		and surcharge.Active = 1 
		and surcharge.SurchargeTypeId is not null 
		and surcharge.StatusId = 1
		and (@checkAll = 0 or (@checkAll = 1 and surcharge.SupplierId in (select [data] from #ListVendor)))
	union

	select monthly.TruckId,null, InvoiceNo, InvoiceDate, 3125, monthly.Id, '', TotalPaid, 0, TotalPaid,
		CurrencyId,[monthly].ExchangeRate, VendorId, Attachments, N'Thuê/mua', 1, @ownerId, @date
	from MonthlyPaid monthly
	outer apply GetExchangeRate([monthly].CurrencyId) as ex
	where monthly.Active = 1 and monthly.InvoiceDate >= @fromdate and monthly.InvoiceDate < @todate
	and (@checkAll = 0 or (@checkAll = 1 and monthly.VendorId in (select [data] from #ListVendor)))
	union
	select par.TruckId,null, InvoiceNo, InvoiceDate, 3125, par.Id, '', Price, 0, Price,
		CurrencyId,par.ExchangeRate, SupplierId, Attachments, N'Phí bến bãi', 1, @ownerId, @date
	from ParkingFee par
	outer apply GetExchangeRate([par].CurrencyId) as ex
	where par.Active = 1 and par.InvoiceDate >= @fromdate and par.InvoiceDate < @todate and par.IsPayable = 1
	and (@checkAll = 0 or (@checkAll = 1 and par.SupplierId in (select [data] from #ListVendor)))

	insert into Payable (StatusId,IsManual, IsPaid, OpeningDebit, ClosingDebit, HaveToPay, Clearing, TotalPaid, FromDate, ToDate, SupplierId, ExchangeRate, CurrencyId, PriceBeforeTax, PriceAfterTax, Active, InsertedBy, InsertedDate)
	select 2,0, 0, 0, 0, 0, 0, 0, @fromdate, @todate, SupplierId, 1, CurrencyId, sum(PriceBeforeTax), sum(PriceAfterTax), 1, @ownerId, @date
	from PayableDetail
	group by SupplierId, CurrencyId
	update detail
	set detail.PayableId = payable.Id
	from PayableDetail detail
	join Payable payable on detail.SupplierId = payable.SupplierId and detail.CurrencyId = payable.CurrencyId
	where payable.FromDate = @fromdate and payable.ToDate = @todate

	-- calc opening debit
	Update pay
	set pay.OpeningDebit = isnull(closing.ClosingDebit, 0),
		pay.HaveToPay = pay.OpeningDebit + pay.PriceAfterTax - pay.Clearing,
		pay.ClosingDebit = pay.HaveToPay - pay.TotalPaid  
	from Payable pay
	left join (
		select top 1 * from Payable where Active = 1 and ToDate < @fromdate order by Id desc
	) as closing on pay.SupplierId = closing.SupplierId and pay.CurrencyId = closing.CurrencyId
end
