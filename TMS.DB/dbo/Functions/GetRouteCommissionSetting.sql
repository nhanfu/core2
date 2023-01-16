
create function GetRouteCommissionSetting(@truckId int, @fromId int, @toId int, @userId int)
returns table
as
	return
	select top 1 *
	from RouteCommissionSetting
	where Active = 1 and EffectiveDate < getdate() and (ExpiredDate is null or ExpiredDate > getdate())
		and FromId = @fromId and ToId = @toId
		and (UserId is null or UserId = @userId)
		and (TruckId is null or TruckId = @truckId)
	order by
		case when UserId is null then 1 else 0 end,
		case when TruckId is null then 1 else 0 end
