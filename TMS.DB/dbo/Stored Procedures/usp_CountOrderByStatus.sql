
create procedure usp_CountOrderByStatus
(
	@userId int,
	@status int
)
as
begin
	select SoId as Id, SoOwner.Id as UserId
	into #owner
	from (
		select so.Id as SoId, [user].Id
		from [Order] so
		join [User] on so.InsertedBy = [user].Id
		union
		select so.Id as SoId, [user].Id
		from [Order] so
		join [User] on so.InsertedBy = [user].Id
		join [User] as sup on [user].SupervisorId = [sup].Id
	) as SoOwner

	select distinct soOwner.Id
	into #SO
    from #owner as soOwner
	join UserRole as uRole on soOwner.UserId = uRole.UserId
	join [Role] on uRole.RoleId = [role].Id
	cross apply dbo.SplitStringToTable([Role].[Path], '\') as higherRole
	left join FeaturePolicy shared on shared.EntityId = 54 and soOwner.Id = shared.RecordId and shared.CanRead = 1
	where soOwner.UserId = @userId or shared.UserId = @userId
	or convert(int, higherRole.[data]) > 0

	select o.FreightStateId, Count(o.Id) as [Count] from [Order] o
	join #SO as so on o.Id = so.Id
	group by o.FreightStateId


	drop table #owner
	drop table #SO
end