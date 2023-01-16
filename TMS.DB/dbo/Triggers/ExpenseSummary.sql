drop trigger ExpenseSummary
go

create trigger ExpenseSummary on SurchargePayable after insert, update, delete
as
begin
	SET NOCOUNT ON;
	update coorDetail set coorDetail.IsOppositeRoute = 
		(case when exists (
			select top 1 sur.Id
			from SurchargePayable as sur
			join SurchargeType surType on sur.SurchargeTypeId = surType.Id
			where surType.Name like 'Opposite%' and sur.CoordinationDetailId = coorDetail.Id
		)
		then 1
		else 0 end),
	coorDetail.SubSurchargeBeforeTax = isnull((select sum(PriceBeforeTax) from SurchargePayable where CoordinationDetailId = coorDetail.Id and VendorId != 65), 0),
	coorDetail.SubSurchargeAfterTax = isnull((select sum(PriceAfterTax) from SurchargePayable where CoordinationDetailId = coorDetail.Id and VendorId != 65), 0)
	from CoordinationDetail as coorDetail
	where coorDetail.Id in (select CoordinationDetailId from inserted union select CoordinationDetailId from deleted)
end