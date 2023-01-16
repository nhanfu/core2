
alter or create procedure [usp_SyncOrderState]
	@coorIds varchar(1000)
as
begin
	select *
	into #ListCoorId
    from [dbo].[SplitStringToTable](@coorIds, ',')

	update od
	set FreightStateId = case
		when MinState = 2 then 4 
		when MinState = 5 then 5
		when MinState = 9 then 5
		when MinState = 11 then 7
		when MinState = 14 and MaxState = 14 then 7
		when MinState = 15 then 8
		else od.FreightStateId end
	from OrderDetail od
	cross apply (
		select od.Id, od.FreightStateId, Min(coor.FreightStateId) as MinState, Max(coor.FreightStateId) as MaxState
		from CoordinationDetail coor
		join OrderDetail od on coor.OrderDetailId = od.Id
		where coor.Id in (select [data] from #ListCoorId) and od.Id = OrderDetailId
		group by od.Id, od.FreightStateId
	) as shouldSync

	update so
	set FreightStateId = case when Moving = 1 then 5 else MinState end
	from [Order] as so
	cross apply (
	select Min(relatedOD.FreightStateId) as MinState, Max(case when relatedOD.FreightStateId = 5 then 1 else 0 end) as Moving
		from CoordinationDetail coor
		join OrderDetail od on coor.OrderDetailId = od.Id
		join OrderDetail relatedOD on relatedOD.OrderId = od.OrderId
		where coor.Id in (select [data] from #ListCoorId) and so.Id = relatedOD.OrderId
		group by relatedOD.OrderId
	) as shouldSync
end