
create function GetContractByUser(@userId int)
returns table
	return
	select top 1 con.*
		from [Contract] con
		where con.UserId = @userId and con.Active = 1
			and con.EffectiveDate < getdate() and (con.ExpiredDate is null or con.ExpiredDate > getdate())
		order by con.Id desc
