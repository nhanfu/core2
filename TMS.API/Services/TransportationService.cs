using Core.Extensions;
using Core.ViewModels;
using TMS.API.Models;

namespace TMS.API.Services
{
    public class TransportationService
    {
        public string Transportation_BetAmount(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.IsBet) || x.Field == nameof(Transportation.BetAmount)))
            {
                return null;
            }
            return @$"update Transportation set BetAmount = case when t.Cont40 = 1 then 2000000 else 1000000 end
		            from Transportation t
		            where t.IsBet = 1 and (t.BetAmount is null or t.BetAmount = 0) and t.Id = {Id}

		            update Transportation set BetAmount = 0
		            from Transportation t
		            where t.IsBet = 0 and t.BetAmount <> 0 and t.Id = {Id};";
        }

        public string Transportation_CombinationFee(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.BrandShipId) || x.Field == nameof(Transportation.StartShip)
            || x.Field == nameof(Transportation.IsEmptyCombination) || x.Field == nameof(Transportation.IsClosingCustomer)))
            {
                return null;
            }
            return @$"update Transportation set
					CombinationFee = (select top 1  CASE
					WHEN (Transportation.IsEmptyCombination = 1 or Transportation.IsClosingCustomer = 1) THEN UnitPrice
					ELSE null
					END as UnitPrice
					from Quotation
					where 
					TypeId = 12071
					and PackingId = Transportation.BrandShipId 
					and StartDate <= Transportation.StartShip order by StartDate desc)
					from Transportation
					where Transportation.StartShip is not null
		            and Transportation.Id = {Id};";
        }

        public string Transportation_Cont20_40(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ContainerTypeId)))
            {
                return null;
            }
            return @$"update t set Cont20 = case when m.Enum = 1 then 1 else 0 end,
					Cont40 = case when m.Enum = 2 then 1 else 0 end
					from Transportation t
					left join MasterData m on t.ContainerTypeId = m.Id
					where t.Id = {Id};";
        }

        public string Transportation_Dem(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.DemDate)
            || x.Field == nameof(Transportation.ReturnDate)
            || x.Field == nameof(Transportation.ShipDate)))
            {
                return null;
            }
            return @$"update Transportation set Dem = (case when DATEDIFF(DAY,Transportation.DemDate,Transportation.ReturnDate) <= 0 then null else DATEDIFF(DAY,Transportation.DemDate,Transportation.ReturnDate) end)
						from Transportation
						where (Transportation.DemDate is not null and Transportation.ShipDate is not null)
						and Transportation.Id = {Id};";
        }

        public string Transportation_DemDate(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.DemDate)
            || x.Field == nameof(Transportation.ShipDate)
            || x.Field == nameof(Transportation.ContainerTypeId)))
            {
                return null;
            }
            if (patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ShipDate) || x.Field == nameof(Transportation.ContainerTypeId)))
            {
                return @$"update Transportation set DemDate = DATEADD(day,(select top 1 [Day] from SettingTransportation where RouteId = t.RouteId and BranchShipId = isnull(t.LineId,t.BrandShipId) and StartDate <= t.ShipDate order by StartDate desc)-1,t.ShipDate)
						from Transportation t
						join MasterData on MasterData.Id = t.ContainerTypeId
						where MasterData.Description not like N'%tank%'
						and t.Id = {Id};";
            }
            else
            {
                return @$"";
            }
        }

        public string Transportation_ExportListId(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.InsertedBy)))
            {
                return null;
            }
            return @$"update Transportation set ExportListId = Ex.VendorId
						from Transportation
						left join [User] as Ex on Transportation.TransportationBy = Ex.Id
						where Transportation.ExportListId is null
						and Transportation.Id = {Id};";
        }

        public string Transportation_IsSplitBill(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ReturnId)
            || x.Field == nameof(Transportation.SplitBill)))
            {
                return null;
            }
            return @$"update Transportation set IsSplitBill = 
					(case when (l.Description is not null and l.Description like N'%Tách Bill Cho Khách%') 
					or (Transportation.SplitBill is not null and trim(Transportation.SplitBill) <> N'') then 1 else 0 end)
					from Transportation
					left join Location l on Transportation.ReturnId = l.Id
					where Transportation.Id = {Id};";
        }

        public string Transportation_LandingFee(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ContainerTypeId)
            || x.Field == nameof(Transportation.IsLanding)
            || x.Field == nameof(Transportation.PortLoadingId)
            || x.Field == nameof(Transportation.ClosingDate)
            || x.Field == nameof(Transportation.LandingFee)))
            {
                return null;
            }
            if (patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.IsLanding)))
            {
                return @$"update Transportation set LandingFee = (select top 1 CASE
					WHEN Transportation.IsLanding = 1 THEN UnitPrice1
					ELSE UnitPrice
					END as UnitPrice from Quotation 
					where 
					TypeId = 7596
					and ContainerTypeId = Transportation.ContainerTypeId 
					and LocationId = Transportation.PortLoadingId 
					and StartDate <= Transportation.ClosingDate order by StartDate desc)
					from Transportation
					where Transportation.Id = {Id};";
            }
            else
            {
                return @$"update Transportation set LandingFee = case when Transportation.LandingFee is null  then  (select top 1 CASE
					WHEN Transportation.IsLanding = 1 THEN UnitPrice1
					ELSE UnitPrice
					END as UnitPrice from Quotation 
					where 
					TypeId = 7596
					and ContainerTypeId = Transportation.ContainerTypeId 
					and LocationId = Transportation.PortLoadingId 
					and StartDate <= Transportation.ClosingDate order by StartDate desc) else Transportation.LandingFee end
					from Transportation
					where Transportation.LandingFee is null and Transportation.Id = {Id};";
            }
        }

        public string Transportation_LiftFee(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ContainerTypeId)
            || x.Field == nameof(Transportation.PickupEmptyId)
            || x.Field == nameof(Transportation.ClosingDate)
            || x.Field == nameof(Transportation.IsEmptyLift)
            || x.Field == nameof(Transportation.LiftFee)))
            {
                return null;
            }
            if (patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.IsEmptyLift)))
            {
                return @$"update Transportation set LiftFee = (select top 1 CASE
					WHEN Transportation.IsEmptyLift = 1 THEN UnitPrice1
					ELSE UnitPrice
					END as UnitPrice from Quotation 
					where 
					TypeId = 7594
					and ContainerTypeId = Transportation.ContainerTypeId 
					and LocationId = Transportation.PickupEmptyId 
					and StartDate <= Transportation.ClosingDate order by StartDate desc)
					from Transportation
					where Transportation.Id = {Id};";
            }
            else
            {
                return @$"update Transportation set LiftFee = case when
					Transportation.LiftFee is null
					then (select top 1 CASE
					WHEN Transportation.IsEmptyLift = 1 THEN UnitPrice1
					ELSE UnitPrice
					END as UnitPrice from Quotation 
					where 
					TypeId = 7594
					and ContainerTypeId = Transportation.ContainerTypeId 
					and LocationId = Transportation.PickupEmptyId 
					and StartDate <= Transportation.ClosingDate order by StartDate desc) else Transportation.LiftFee end
					from Transportation
					where Transportation.LiftFee is null
					and Transportation.Id = {Id};";
            }
        }

        public string Transportation_MonthText(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ClosingDate)))
            {
                return null;
            }
            return @$"update Transportation set MonthText = CAST(MONTH(t.ClosingDate)as nvarchar(50)) + '%2F' + CAST(Year(t.ClosingDate)as nvarchar(50)),
					YearText = CAST(Year(t.ClosingDate) as nvarchar(50))
					from Transportation t
					where t.Id = {Id};";
        }

        public string Transportation_Note4(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.BossId)
            || x.Field == nameof(Transportation.RouteId)
            || x.Field == nameof(Transportation.ExportListId)))
            {
                return null;
            }
            return @$"update Transportation set Note4 = Vendor.Name, 
                    IsHost= (case when (Ex.RouteId = Transportation.RouteId or Route.Name like N'%Đường%') then 1 else 0 end)
					,IsBooking =(case when Route.Name like N'%Đường%' then 0 else 1 end)
					from Transportation
					left join Vendor as Ex on Transportation.ExportListId = Ex.Id
					left join Route on Transportation.RouteId = Route.Id
					left join Vendor on Vendor.Id = Transportation.BossId
					where Transportation.Id = {Id};
                    update Transportation set 
                    BookingId = null, 
                    ShipId = null,
					PolicyId = null,
					BrandShipId = null,
					Trip = null,
					LineId = null,
					StartShip = null,
					PortLoadingId = null,
					PickupEmptyId = null,
					ShipUnitPrice = null,
					ShipPrice = null,
                    ShipPolicyPrice = null
					from Transportation
					where Transportation.Id = {Id} and IsBooking = 0;";
        }

        public string Transportation_ReturnClosingFee(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ContainerTypeId)
            || x.Field == nameof(Transportation.IsClosingEmptyFee)
            || x.Field == nameof(Transportation.ReturnEmptyId)
            || x.Field == nameof(Transportation.ShipDate)
            || x.Field == nameof(Transportation.ReturnClosingFee)))
            {
                return null;
            }
            if (patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.IsClosingEmptyFee)))
            {
                return @$"update Transportation set ReturnClosingFee = (case when Transportation.ISIncluded = 1 then 0 else  (select top 1 CASE
					WHEN Transportation.IsClosingEmptyFee = 1 THEN UnitPrice1
					ELSE UnitPrice
					END as UnitPrice from Quotation 
					where 
					TypeId = 7594
					and ContainerTypeId = Transportation.ContainerTypeId 
					and LocationId = Transportation.ReturnEmptyId 
					and (StartDate <= Transportation.ShipDate or Transportation.ShipDate is null) order by StartDate desc) end)
					from Transportation
					where Transportation.ShipDate is not null and Transportation.Id = {Id};";
            }
            else
            {
                return @$"update Transportation set ReturnClosingFee = case when Transportation.ReturnClosingFee is null then (case when Transportation.ISIncluded = 1 then 0 else  (select top 1 CASE
					WHEN Transportation.IsClosingEmptyFee = 1 THEN UnitPrice1
					ELSE UnitPrice
					END as UnitPrice from Quotation 
					where 
					TypeId = 7594
					and ContainerTypeId = Transportation.ContainerTypeId 
					and LocationId = Transportation.ReturnEmptyId 
					and (StartDate <= Transportation.ShipDate or Transportation.ShipDate is null) order by StartDate desc) end) else Transportation.ReturnClosingFee end
					from Transportation
					where Transportation.ShipDate is not null
					and Transportation.Id = {Id};";
            }
        }

        public string Transportation_ReturnDate(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.SplitBill)
            || x.Field == nameof(Transportation.ReturnDate)
            || x.Field == nameof(Transportation.ShipDate)))
            {
                return null;
            }
            return @$"update Transportation set ReturnDate = Transportation.ShipDate, ReturnId = r.BranchId
					from Transportation
					join [Route] r on r.Id = Transportation.RouteId
					where Transportation.SplitBill is not null 
					and Transportation.SplitBill <> '' 
					and Transportation.SplitBill <> N'bill riêng'
					and Transportation.ShipDate is not null
					and Transportation.ReturnDate is null
					and Transportation.Id = {Id}

					update Transportation set IsSplitBill = 
					(case when (l.Description is not null and l.Description like N'%Tách Bill Cho Khách%') 
					or (Transportation.SplitBill is not null and trim(Transportation.SplitBill) <> N'') then 1 else 0 end)
					from Transportation
					left join Location l on Transportation.ReturnId = l.Id
					where Transportation.Id = {Id};";
        }

        public string Transportation_ReturnLiftFee(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ContainerTypeId)
            || x.Field == nameof(Transportation.IsLiftFee)
            || x.Field == nameof(Transportation.PortLiftId)
            || x.Field == nameof(Transportation.ShipDate)
            || x.Field == nameof(Transportation.ReturnLiftFee)))
            {
                return null;
            }
            if (patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.IsLiftFee)))
            {
                return @$"update Transportation set ReturnLiftFee = (select top 1 CASE
					WHEN Transportation.IsLiftFee = 1 THEN UnitPrice1
					ELSE UnitPrice
					END as UnitPrice from Quotation 
					where TypeId = 7596
					and ContainerTypeId = Transportation.ContainerTypeId 
					and LocationId = Transportation.PortLiftId 
					and (StartDate <= Transportation.ShipDate or Transportation.ShipDate is null) order by StartDate desc)
					from Transportation
					where Transportation.ShipDate is not null and Transportation.Id = {Id};";
            }
            else
            {
                return @$"update Transportation set ReturnLiftFee = case when Transportation.ReturnLiftFee is null then  (select top 1 CASE
					WHEN Transportation.IsLiftFee = 1 THEN UnitPrice1
					ELSE UnitPrice
					END as UnitPrice from Quotation 
					where TypeId = 7596
					and ContainerTypeId = Transportation.ContainerTypeId 
					and LocationId = Transportation.PortLiftId 
					and (StartDate <= Transportation.ShipDate or Transportation.ShipDate is null) order by StartDate desc) else Transportation.ReturnLiftFee end
					from Transportation
					where Transportation.ShipDate is not null
					and Transportation.Id = {Id};";
            }
        }

        public string Transportation_ReturnNotes(PatchUpdate patchUpdate, int Id)
        {
            return @$"update Transportation set ReturnNotes = 
					(CASe when (Transportation.ReturnNotes is null) and Transportation.IsNote = 1  then Transportation.Note2 else Transportation.ReturnNotes end)
					,[FreeText] = (CASe when (Transportation.[FreeText] is null)  then (select top 1 [FreeText] from Transportation t where t.BossId = Transportation.BossId and t.ReturnId = Transportation.ReturnId and t.[FreeText] is not null and t.[FreeText] != N'' order by t.ReturnDate desc) else Transportation.[FreeText] end)
					,[FreeText1] = (CASe when (Transportation.[FreeText1] is null)  then (select top 1 [FreeText1] from Transportation t where t.BossId = Transportation.BossId and t.ReturnId = Transportation.ReturnId and t.[FreeText1] is not null and t.[FreeText1] != N'' order by t.ReturnDate desc) else Transportation.[FreeText1] end)
					from Transportation
					where (Transportation.ReturnNotes is null
					or Transportation.[FreeText] is null
					or Transportation.[FreeText1] is null)
					and Transportation.Id = {Id};";
        }

        public string Transportation_ReturnVs(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.BrandShipId)
            || x.Field == nameof(Transportation.LevelId)
            || x.Field == nameof(Transportation.StartShip)
            || x.Field == nameof(Transportation.ReturnVs)
            || x.Field == nameof(Transportation.RouteId)))
            {
                return null;
            }
            var check = patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.LevelId));
            if (!check)
            {
                return @$"update Transportation set ReturnVs = case when Transportation.LevelId is null then null
		            else (select top 1 case when Transportation.Cont20 > 0 then VS20UnitPrice else VS40UnitPrice end
		            from QuotationExpense
		            where 
		            BrandShipId = Transportation.BrandShipId
		            and ExpenseTypeId = Transportation.LevelId 
		            and (RouteId = Transportation.RouteId or RouteId is null)
		            and StartDate <= Transportation.StartShip order by StartDate desc) end
		            from Transportation
		            where (Transportation.ReturnVs = 0 or Transportation.ReturnVs is null)
		            and Transportation.StartShip is not null
					and Transportation.Id = {Id};";
            }
            else
            {
                return @$"update Transportation set ReturnVs = case when Transportation.LevelId is null then null
		            else (select top 1 case when Transportation.Cont20 > 0 then VS20UnitPrice else VS40UnitPrice end
		            from QuotationExpense
		            where 
		            BrandShipId = Transportation.BrandShipId
		            and ExpenseTypeId = Transportation.LevelId 
		            and (RouteId = Transportation.RouteId or RouteId is null)
		            and StartDate <= Transportation.StartShip order by StartDate desc) end
		            from Transportation
		            where Transportation.StartShip is not null
					and Transportation.Id = {Id};";
            }
        }

        public string Transportation_ShellDate(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.LeftDate)
            || x.Field == nameof(Transportation.ReturnVs)))
            {
                return null;
            }
            return @$"update Transportation set ShellDate = (case when (DATEDIFF(DAY,Transportation.LeftDate,Transportation.ClosingCont) -2) <= 0 then null else DATEDIFF(DAY,Transportation.LeftDate,Transportation.ClosingCont) - 2 end)
		            from Transportation
		            where Transportation.Id = {Id};";
        }

        public string Transportation_ShipUnitPriceQuotation(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.RouteId)
            || x.Field == nameof(Transportation.ContainerTypeId)
            || x.Field == nameof(Transportation.LineId)
            || x.Field == nameof(Transportation.BrandShipId)
            || x.Field == nameof(Transportation.StartShip)
            || x.Field == nameof(Transportation.ShipUnitPriceQuotation)
            || x.Field == nameof(Transportation.BookingId)))
            {
                return null;
            }
            return @$"update Transportation set ShipUnitPriceQuotation =  
		            isnull((select top 1 UnitPrice
		            from Quotation
		            where 
		            TypeId = 7598
		            and RouteId = Transportation.RouteId 
		            and ContainerTypeId = Transportation.ContainerTypeId 
		            and PackingId =ISNULL(Transportation.LineId,Transportation.BrandShipId)
		            and CONVERT(DATE, Quotation.StartDate) <= CONVERT(DATE, Transportation.StartShip) order by StartDate desc),
		            case when Transportation.Cont20 = 1 then
		            (select top 1 UnitPrice
		            from Quotation
		            where 
		            TypeId = 7598
		            and RouteId = Transportation.RouteId 
		            and ContainerTypeId = 14910
		            and PackingId = ISNULL(Transportation.LineId,Transportation.BrandShipId)
		            and CONVERT(DATE, Quotation.StartDate) <= CONVERT(DATE, Transportation.StartShip) order by StartDate desc)
		            when Transportation.Cont40 = 1 then
		            (select top 1 UnitPrice
		            from Quotation
		            where 
		            TypeId = 7598
		            and RouteId = Transportation.RouteId 
		            and ContainerTypeId = 14909 
		            and PackingId = ISNULL(Transportation.LineId,Transportation.BrandShipId)
		            and CONVERT(DATE, Quotation.StartDate) <= CONVERT(DATE, Transportation.StartShip) order by StartDate desc)
		            end)
		            from Transportation
		            where Transportation.Id = {Id};

                    update Transportation set ShipPrice =  ShipUnitPriceQuotation - isnull(ShipPolicyPrice,0)
                    from Transportation
		            where Transportation.Id = {Id};";
        }

        public string Transportation_VendorLocation(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.BossId)
            || x.Field == nameof(Transportation.ReturnId)))
            {
                return null;
            }
            return @$"declare @vendorId int = null
	                declare @insertedby int = null
	                declare @locationId int = null
	                declare @exportListId int = null
	                select top 1 @vendorId = BossId, @locationId = ReturnId, @insertedby = InsertedBy, @exportListId = ExportListReturnId from Transportation where  Transportation.Id = {Id}
	                if(@vendorId is not null and @locationId is not null and exists (select Id from Location where Id = @locationId))
	                begin
		                if(not exists (select Id from VendorLocation where VendorId=@vendorId and LocationId = @locationId and TypeId = 2 and ExportListId = @exportListId))
		                begin
			                Insert into VendorLocation(VendorId,LocationId,InsertedBy,Active,InsertedDate,TypeId,ExportListId)
			                values(@vendorId,@locationId,@insertedby,1,GETDATE(),2, @exportListId)
		                end
	                end;";
        }

        public string Transportation_BetFee(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ReturnId)
            || x.Field == nameof(Transportation.CompanyId)))
            {
                return null;
            }
            if (patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ClosingUnitPrice)))
            {
                return null;
            }
            return @$"update Transportation set BetFee = (case when l.Description like N'%Giao Lệnh Tại HCM%' and t.Cont20 = 1 then 1000000
	                when l.Description like N'%Giao Lệnh Tại HCM%' and t.Cont40 = 1 then 2000000 end)
	                from Transportation t
	                left join [Location] l on l.Id = t.ReturnId
	                where t.Id = {Id};";
        }

        public string Transportation_ClosingUnitPrice(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ClosingId)
            || x.Field == nameof(Transportation.BossId)
            || x.Field == nameof(Transportation.ContainerTypeId)
            || x.Field == nameof(Transportation.ReceivedId)
            || x.Field == nameof(Transportation.ClosingDate)
            || x.Field == nameof(Transportation.IsClampingFee)
            || x.Field == nameof(Transportation.IsClosingCustomer)
            || x.Field == nameof(Transportation.IsEmptyCombination)
            || x.Field == nameof(Transportation.ClosingUnitPrice)))
            {
                return null;
            }
            var update = patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ClosingUnitPrice) && !x.Value.IsNullOrWhiteSpace());
            if (!update)
            {
                return @$"
                        declare @startDate datetime2(7) = null;
declare @startDate1 datetime2(7) = null;
select  @startDate = (select top 1 StartDate
		                from Quotation 
		                where PackingId = t.ClosingId 
		                and TypeId = 7592
		                and BossId = t.BossId 
		                and ContainerTypeId = t.ContainerTypeId 
		                and LocationId = t.ReceivedId 
		                and StartDate <= t.ClosingDate order by StartDate desc),
                        @startDate1 = (select top 1 StartDate
		                from Quotation
		                where PackingId = t.ClosingId 
		                and TypeId = 7592
		                and BossId is null
		                and re.RegionId = RegionId
		                and ContainerTypeId = t.ContainerTypeId 
		                and LocationId is null 
		                and StartDate <= t.ClosingDate order by StartDate desc)
                        from Transportation t
						join Location re on t.ReceivedId = re.Id
						where t.Id = {Id};
if(@startDate >= @startDate1 and @startDate is not null and @startDate1 is not null)
begin
							update Transportation set ClosingUnitPrice = 
							(select top 1 CASE
							WHEN t.IsClampingFee = 1 THEN UnitPrice1
							else
							 case WHEN (t.IsClosingCustomer = 1 or t.IsEmptyCombination = 1) and UnitPrice3 > 0 THEN UnitPrice3
							 ELSE (case when UnitPrice = 0 then null else UnitPrice end)
							END
							end 
							from Quotation 
							where PackingId = t.ClosingId 
							and TypeId = 7592
							and BossId = t.BossId 
							and ContainerTypeId = t.ContainerTypeId 
							and LocationId = t.ReceivedId 
							and StartDate <= t.ClosingDate order by StartDate desc)
							from Transportation t
							where t.Id = {Id};
end
if(@startDate < @startDate1 and @startDate is not null and @startDate1 is not null)
begin
							update Transportation set ClosingUnitPrice = (select top 1 CASE
							WHEN t.IsClampingFee = 1 THEN UnitPrice1
							else
							 case WHEN (t.IsClosingCustomer = 1 or t.IsEmptyCombination = 1) and UnitPrice3 > 0 THEN UnitPrice3
							 ELSE (case when UnitPrice = 0 then null else UnitPrice end)
							END
							end 
							from Quotation 
							where PackingId = t.ClosingId 
							and TypeId = 7592
							and BossId is null
							and re.RegionId = RegionId
							and ContainerTypeId = t.ContainerTypeId 
							and LocationId is null 
							and StartDate <= t.ClosingDate order by StartDate desc)
							from Transportation t
							join Location re on t.ReceivedId = re.Id
							where t.Id = {Id};
end
if(@startDate is not null and @startDate1 is null)
begin
							update Transportation set ClosingUnitPrice = 
							(select top 1 CASE
							WHEN t.IsClampingFee = 1 THEN UnitPrice1
							else
							 case WHEN (t.IsClosingCustomer = 1 or t.IsEmptyCombination = 1) and UnitPrice3 > 0 THEN UnitPrice3
							 ELSE (case when UnitPrice = 0 then null else UnitPrice end)
							END
							end 
							from Quotation 
							where PackingId = t.ClosingId 
							and TypeId = 7592
							and BossId = t.BossId 
							and ContainerTypeId = t.ContainerTypeId 
							and LocationId = t.ReceivedId 
							and StartDate <= t.ClosingDate order by StartDate desc)
							from Transportation t
							where t.Id = {Id};
end
if(@startDate1 is not null and @startDate is null)
begin
							update Transportation set ClosingUnitPrice = (select top 1 CASE
							WHEN t.IsClampingFee = 1 THEN UnitPrice1
							else
							 case WHEN (t.IsClosingCustomer = 1 or t.IsEmptyCombination = 1) and UnitPrice3 > 0 THEN UnitPrice3
							 ELSE (case when UnitPrice = 0 then null else UnitPrice end)
							END
							end 
							from Quotation 
							where PackingId = t.ClosingId 
							and TypeId = 7592
							and BossId is null
							and re.RegionId = RegionId
							and ContainerTypeId = t.ContainerTypeId 
							and LocationId is null 
							and StartDate <= t.ClosingDate order by StartDate desc)
							from Transportation t
							join Location re on t.ReceivedId = re.Id
							where t.Id = {Id};
end
                        ";
            }
            else
            {
                return "";
            }
        }

        public string Transportation_ReturnUnitPrice(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ReturnVendorId)
            || x.Field == nameof(Transportation.ReturnId)
            || x.Field == nameof(Transportation.ReturnDate)
            || x.Field == nameof(Transportation.ContainerTypeId)
            || x.Field == nameof(Transportation.IsClampingReturnFee)
            || x.Field == nameof(Transportation.ReturnUnitPrice)))
            {
                return null;
            }
            var update = patchUpdate.Changes.Any(x => (x.Field == nameof(Transportation.ReturnUnitPrice) && !x.Value.IsNullOrWhiteSpace()) || x.Field == nameof(Transportation.IsClampingReturnFee));
            if (!update)
            {
                return @$"
                        declare @startDate5 datetime2(7) = null;
declare @startDate6 datetime2(7) = null;
select  @startDate5 = (select top 1 StartDate
		                from Quotation 
		                where BossId = t.BossId 
							and TypeId = 7593
							and PackingId = t.ReturnVendorId 
							and ContainerTypeId = t.ContainerTypeId 
							and LocationId = t.ReturnId 
							and StartDate <= t.ReturnDate order by StartDate desc),
                        @startDate6 = (select top 1 StartDate
		                from Quotation
		                where BossId is null
							and TypeId = 7593
							and re.RegionId = RegionId
							and PackingId = t.ReturnVendorId 
							and ContainerTypeId = t.ContainerTypeId 
							and LocationId is null
							and StartDate <= t.ReturnDate order by StartDate desc)
                        from Transportation t
						join Location re on t.ReturnId = re.Id
						where t.Id = {Id};
if(@startDate5 >= @startDate6 and @startDate5 is not null and @startDate6 is not null)
begin
							update Transportation set ReturnUnitPrice = 
								(select top 1 CASE
								WHEN t.IsClampingReturnFee = 1 THEN UnitPrice1
								ELSE UnitPrice
								END as UnitPrice from Quotation 
								where BossId = t.BossId 
								and TypeId = 7593
								and PackingId = t.ReturnVendorId 
								and ContainerTypeId = t.ContainerTypeId 
								and LocationId = t.ReturnId 
								and StartDate <= t.ReturnDate order by StartDate desc)
							from Transportation t
							where t.Id = {Id};
end
if(@startDate5 < @startDate6 and @startDate5 is not null and @startDate6 is not null)
begin
							update Transportation set ReturnUnitPrice = (select top 1 CASE
								WHEN t.IsClampingReturnFee = 1 THEN UnitPrice1
								ELSE UnitPrice
								END as UnitPrice from Quotation 
								where BossId is null
								and TypeId = 7593
								and PackingId = t.ReturnVendorId 
								and re.RegionId = RegionId
								and ContainerTypeId = t.ContainerTypeId 
								and LocationId is null
								and StartDate <= t.ReturnDate order by StartDate desc)
							from Transportation t
							join Location re on t.ReturnId = re.Id
							where t.Id = {Id};
end
if(@startDate5 is not null and @startDate6 is null)
begin
							update Transportation set ReturnUnitPrice = 
								(select top 1 CASE
								WHEN t.IsClampingReturnFee = 1 THEN UnitPrice1
								ELSE UnitPrice
								END as UnitPrice from Quotation 
								where BossId = t.BossId 
								and TypeId = 7593
								and PackingId = t.ReturnVendorId 
								and ContainerTypeId = t.ContainerTypeId 
								and LocationId = t.ReturnId 
								and StartDate <= t.ReturnDate order by StartDate desc)
							from Transportation t
							where t.Id = {Id};
end
if(@startDate6 is not null and @startDate5 is null)
begin
							update Transportation set ReturnUnitPrice = (select top 1 CASE
								WHEN t.IsClampingReturnFee = 1 THEN UnitPrice1
								ELSE UnitPrice
								END as UnitPrice from Quotation 
								where BossId is null
								and TypeId = 7593
								and PackingId = t.ReturnVendorId 
								and re.RegionId = RegionId
								and ContainerTypeId = t.ContainerTypeId 
								and LocationId is null
								and StartDate <= t.ReturnDate order by StartDate desc)
							from Transportation t
							join Location re on t.ReturnId = re.Id
							where t.Id = {Id};
end
                        ";
            }
            else
            {
                return "";
            }
        }

        public string Transportation_Expense(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ContainerNo)
            || x.Field == nameof(Transportation.SealNo)
            || x.Field == nameof(Transportation.CommodityId)
            || x.Field == nameof(Transportation.MonthText)
            || x.Field == nameof(Transportation.YearText)
            || x.Field == nameof(Transportation.RouteId)
            || x.Field == nameof(Transportation.BrandShipId)))
            {
                return null;
            }
            return @$"update e set BossId = t.BossId,
					ContainerNo = t.ContainerNo,
					SealNo = t.SealNo,
					CommodityId = t.CommodityId,
					MonthText = t.MonthText,
					YearText = t.YearText,
					RouteId = t.RouteId,
					BrandShipId = t.BrandShipId
					from Expense e
					JOIN Transportation t ON t.Id = e.TransportationId
					where e.ExpenseTypeId not in (15981, 15939)
	                and e.TransportationId = {Id};";
        }
    }
}
