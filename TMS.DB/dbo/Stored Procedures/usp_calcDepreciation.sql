
create procedure [dbo].[usp_calcDepreciation]
	@fromdate datetime2,
	@todate datetime2,
	@ownerId int
as
begin
	delete Depreciation 
	where Active = 1 and InsertedDate > @fromdate
end
