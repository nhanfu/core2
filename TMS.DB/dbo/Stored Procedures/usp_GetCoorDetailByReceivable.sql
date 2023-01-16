

create procedure [dbo].[usp_GetCoorDetailByReceivable]
	@receivableId int
as
begin
	select cd.Id, cd.TruckId, cd.DriverId, cd.Driver2Id, cd.Driver3Id, cd.PortSupplierId, cd.ContSupplierId, cd.CoordinationId, 
	cd.EstimatedStartDate, cd.EstimatedEndDate, cd.ActualStartDate, cd.StartTerminalId, cd.FinishedTerminalId, cd.StartOdo, cd.EndOdo, 
	cd.AllocatedCost, cd.TrailerId, cd.ActualTrailerPickupDate, cd.EstimatedTrailerStartDate, cd.ActualTrailerStartDate, cd.EstimatedTrailerReturnDate, 
	cd.ActualTrailerReturnDate, cd.TrailerSavingCount, cd.TrailerSavingSurcharge, cd.AddressGetTrailer, cd.AddressReturnTrailer, cd.ContainerId, 
	cd.Container2Id, cd.EstimatedContainerPickupDate, cd.EstimatedContainer2PickupDate, cd.ActualContainerPickupDate, cd.ActualContainer2PickupDate, 
	cd.EstimatedContainerReturnDate, cd.EstimatedContainer2ReturnDate, cd.ActualContainerComeDate, cd.ActualContainerReturnDate, 
	cd.ActualContainer2ReturnDate, cd.AddressGetCont, cd.AddressGetCont2, cd.AddressReturnCont, cd.AddressReturnCont2, cd.SealNumbersCont,
	cd.SealNumbersCont2, cd.ClosingDate, cd.ActualEndDate, cd.FreightStateId, cd.Note, coorBlock.CurrencyId,  convert(decimal, 1) as ExchangeRate, cd.Cost, 
	cd.Profit, cd.Distance, cd.TrailerComeDate, cd.InTransitDate, cd.DeliveredDate, cd.CancelledDate, cd.GoodsPlaceDate, cd.GoodsTakenDate, 
	cd.DeliveryPlaceDate, cd.ActualTrailerComeDate, cd.Stuffing, cd.UnStuffing, cd.Attachments, cd.Active, cd.InsertedDate, cd.InsertedBy, 
	cd.UpdatedDate, cd.UpdatedBy, sum(coorBlock.PriceBeforeTax * ex.ExchangeRate) as PriceAfterTax
	from CoordinationDetail as cd
	join CoordinationDetailBlock as coorBlock on cd.Id = coorBlock.CoordinationDetailId
	join [Block] bl on bl.Id = coorBlock.BlockId
	join [OrderDetail] soDetail on bl.OrderDetailId = soDetail.Id
	join [Order] so on so.Id = soDetail.OrderId
	join ReceivableDetail recDetail on recDetail.RecordId = soDetail.Id and recDetail.EntityId = 2
	join Receivable rec on recDetail.ReceivableId = rec.Id
	outer apply GetExchangeRate(coorBlock.CurrencyId) as ex
	where rec.Id = @receivableId and (rec.CountFinishedOnly = 1 and cd.FreightStateId = 10 
		or rec.CountFinishedOnly = 0 and cd.FreightStateId > 0 and cd.FreightStateId <= 10)

	group by cd.Id, cd.TruckId, cd.DriverId, cd.Driver2Id, cd.Driver3Id, cd.PortSupplierId, cd.ContSupplierId, cd.CoordinationId, 
	cd.EstimatedStartDate, cd.EstimatedEndDate, cd.ActualStartDate, cd.StartTerminalId, cd.FinishedTerminalId, cd.StartOdo, cd.EndOdo, 
	cd.AllocatedCost, cd.TrailerId, cd.ActualTrailerPickupDate, cd.EstimatedTrailerStartDate, cd.ActualTrailerStartDate, cd.EstimatedTrailerReturnDate, 
	cd.ActualTrailerReturnDate, cd.TrailerSavingCount, cd.TrailerSavingSurcharge, cd.AddressGetTrailer, cd.AddressReturnTrailer, cd.ContainerId, 
	cd.Container2Id, cd.EstimatedContainerPickupDate, cd.EstimatedContainer2PickupDate, cd.ActualContainerPickupDate, cd.ActualContainer2PickupDate, 
	cd.EstimatedContainerReturnDate, cd.EstimatedContainer2ReturnDate, cd.ActualContainerComeDate, cd.ActualContainerReturnDate, 
	cd.ActualContainer2ReturnDate, cd.AddressGetCont, cd.AddressGetCont2, cd.AddressReturnCont, cd.AddressReturnCont2, cd.SealNumbersCont,
	cd.SealNumbersCont2, cd.ClosingDate, cd.ActualEndDate, cd.FreightStateId, cd.Note, cd.PriceAfterTax, coorBlock.CurrencyId, cd.Cost, 
	cd.Profit, cd.Distance, cd.TrailerComeDate, cd.InTransitDate, cd.DeliveredDate, cd.CancelledDate, cd.GoodsPlaceDate, cd.GoodsTakenDate, 
	cd.DeliveryPlaceDate, cd.ActualTrailerComeDate, cd.Stuffing, cd.UnStuffing, cd.Attachments, cd.Active, cd.InsertedDate, cd.InsertedBy, 
	cd.UpdatedDate, cd.UpdatedBy, coorBlock.CurrencyId
end
