
create function GetExchangeRate(@fromCurrencyId int)
returns table
	return
	select top 1 * from ExchangeRate
		where FromCurrencyId = @fromCurrencyId and ToCurrencyId = 72
		order by InsertedDate desc
