using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class QuotationController : TMSController<Quotation>
    {
        public QuotationController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<Quotation>> CreateAsync([FromBody] Quotation entity)
        {
            var check = await db.Quotation.AnyAsync(x => x.LocationId == entity.LocationId
            && x.RouteId == entity.RouteId
            && x.RegionId == entity.RegionId
            && x.StartDate == entity.StartDate
            && x.BranchId == entity.BranchId
            && x.PackingId == entity.PackingId
            && x.BossId == entity.BossId
            && x.TypeId == entity.TypeId
            && x.ContainerTypeId == entity.ContainerTypeId);
            if (check)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            var rs = await base.CreateAsync(entity);
            if (entity.PackingId != null &&
                entity.RegionId != null &&
                entity.TypeId != null &&
                entity.ContainerTypeId != null &&
                entity.StartDate != null)
            {
                var sql = @$" update Transportation set ClosingUnitPrice = (CASE
		                    WHEN Transportation.IsClampingFee = 1 THEN UnitPrice1
		                    else
		                     case WHEN (Transportation.IsClosingCustomer = 1 or Transportation.IsEmptyCombination = 1) and ${entity.UnitPrice3} > 0 THEN ${entity.UnitPrice3}
		                     ELSE (case when ${entity.UnitPrice} = 0 then null else ${entity.UnitPrice} end)
		                    END
		                    end)
		                    from Transportation
		                    join Location re on Transportation.ReceivedId = re.Id
		                    where ${entity.RegionId} = Transportation.ClosingId
		                    and ${entity.RegionId} = re.RegionId 
		                    and ${entity.TypeId} = 7592
		                    and ${entity.ContainerTypeId} = Transportation.ContainerTypeId 
		                    and Transportation.ClosingDate >= inserted.StartDate
		                    and (Transportation.ClosingUnitPrice is null or Transportation.ClosingUnitPrice = 0);";

                sql += @$" update Transportation set ClosingUnitPrice = isnull((CASE
		                    WHEN Transportation.IsClampingFee = 1 THEN UnitPrice1
		                    else
		                     case WHEN (Transportation.IsClosingCustomer = 1 or Transportation.IsEmptyCombination = 1) and ${entity.UnitPrice3} > 0 THEN ${entity.UnitPrice3}
		                     ELSE (case when ${entity.UnitPrice} = 0 then null else ${entity.UnitPrice} end)
		                    END
		                    end),Transportation.ClosingUnitPrice)
		                    from Transportation
		                    join Location re on Transportation.ReceivedId = re.Id
		                    where ${entity.RegionId} = Transportation.ClosingId
		                    and ${entity.RegionId} = re.RegionId
		                    and ${entity.LocationId} = Transportation.ReceivedId 
		                    and ${entity.BossId} = Transportation.BossId 
		                    and ${entity.TypeId} = 7592
		                    and ${entity.ContainerTypeId} = Transportation.ContainerTypeId 
		                    and Transportation.ClosingDate >= inserted.StartDate
		                    and (Transportation.ClosingUnitPrice is null or Transportation.ClosingUnitPrice = 0);";
                ExecSql(sql, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            };
            return rs;
        }

        public override async Task<ActionResult<Quotation>> UpdateAsync([FromBody] Quotation entity, string reasonOfChange = "")
        {
            var check = await db.Quotation.AnyAsync(x => x.LocationId == entity.LocationId
            && x.RouteId == entity.RouteId
            && x.RegionId == entity.RegionId
            && x.StartDate == entity.StartDate
            && x.BranchId == entity.BranchId
            && x.PackingId == entity.PackingId
            && x.BossId == entity.BossId
            && x.TypeId == entity.TypeId
            && x.Id != entity.Id
            && x.ContainerTypeId == entity.ContainerTypeId);
            if (check)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await base.UpdateAsync(entity, reasonOfChange);
        }
    }
}
