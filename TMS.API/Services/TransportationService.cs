using Core.Extensions;
using Core.ViewModels;
using TMS.API.Models;
using TMS.API.Websocket;

namespace TMS.API.Services
{
    public class TransportationService
    {
        private readonly TMSContext db;
        private readonly UserService _userService;
        public TransportationService(UserService userService, TMSContext db)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

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
		            where t.IsBet = 0 and t.BetAmount <> 0 and t.Id = {Id}";
        }

        public string Transportation_BetFee(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ReturnId) || x.Field == nameof(Transportation.CompanyId)
            || x.Field == nameof(Transportation.BetFee) || x.Field == nameof(Transportation.ReturnClosingFee)
            || x.Field == nameof(Transportation.ReturnLiftFeeReport) || x.Field == nameof(Transportation.CustomerReturnFee)))
            {
                return null;
            }
            return @$"update Transportation set BetFee = isnull(t.BetFee,case when l.Description like N'%Giao Lệnh Tại HCM%' and t.Cont20 = 1 then 1000000
		            when l.Description like N'%Giao Lệnh Tại HCM%' and t.Cont40 = 1 then 2000000 end)
		            from Transportation t
		            left join [Location] l on l.Id = t.ReturnId
		            where Id = {Id}";
        }

        public string Transportation_ClosingCombinationUnitPrice(PatchUpdate patchUpdate, int Id)
        {
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ReturnId) || x.Field == nameof(Transportation.CompanyId)
            || x.Field == nameof(Transportation.BetFee) || x.Field == nameof(Transportation.ReturnClosingFee)
            || x.Field == nameof(Transportation.ReturnLiftFeeReport) || x.Field == nameof(Transportation.CustomerReturnFee)))
            {
                return null;
            }
            return @$"update Transportation set ClosingCombinationUnitPrice = 
		            case when Transportation.ClosingCombinationUnitPrice = 0 
		            or Transportation.ClosingCombinationUnitPrice is null 
		            or Transportation.ClosingPercent > 0 
		            or Transportation.ClosingPercent > 0 
		            then (Transportation.ClosingUnitPrice * (case when  Transportation.ClosingPercent = 0 then 100 else isnull(Transportation.ClosingPercent,100) end))/100
		            when  Transportation.ClosingPercent is null then Transportation.ClosingCombinationUnitPrice
		            else Transportation.ClosingCombinationUnitPrice end,
		            ShipPrice = case when Transportation.ShipPolicyPrice <> deleted.ShipPolicyPrice 
		            then Transportation.ShipUnitPriceQuotation 
		            - isnull(Transportation.ShipPolicyPrice,0) 
		            else 
		            ISNULL(Transportation.ShipPrice,Transportation.ShipUnitPriceQuotation 
		            - isnull(Transportation.ShipPolicyPrice,0)) end
		            from Transportation
		            where Transportation.Id = {Id}";
        }
    }
}
