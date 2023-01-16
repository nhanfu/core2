
CREATE function [dbo].[GetCountLate]
(	
	@fromDate datetime2(7) = null,
	@toDate datetime2(7) = null
)
returns table as return
(
		select Truck.*,CountByLate from Truck join (
				select COUNT(T1.Id) as CountByLate,T1.TruckId
				from CoordinationDetail T1 join Truck T2 on T1.TrailerId=T2.Id
				where ActualStartDate < OrderbyDate  
				and  T1.TruckId is not null
				and (T1.OrderbyDate>=@fromDate or @fromDate is null)
				and (T1.OrderbyDate<=@fromDate or @fromDate is null)
				group by T1.TruckId) as TruckCount on [Truck].Id = TruckCount.TruckId
)
