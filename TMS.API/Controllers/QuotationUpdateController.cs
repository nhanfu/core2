using Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class QuotationUpdateController : TMSController<QuotationUpdate>
    {
        public QuotationUpdateController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {

        }

        [HttpPost("api/QuotationUpdate/QuotationUpdate")]
        public async Task<bool> QuotationUpdate([FromBody] QuotationUpdate entity)
        {
            var cmd = $@";WITH cte AS
                        (
                           SELECT *,
                                 ROW_NUMBER() OVER (PARTITION BY PackingId,BossId,ContainerTypeId,LocationId,RouteId ORDER BY StartDate DESC) AS rn
                           FROM Quotation
                           where TypeId = {entity.TypeId}
                           and BranchId = {VendorId}
                           and StartDate < '{entity.StartDate:yyyy-MM-dd}'";
            if (entity.ContainerId != null)
            {
                cmd += $@" and ContainerId = {entity.ContainerId}";
            }
            if (entity.PackingIds != null && entity.PackingIds.Any())
            {
                cmd += $@" and PackingId in ({entity.PackingIds.Combine()})";
            }
            if (entity.Packing1Ids != null && entity.Packing1Ids.Any())
            {
                cmd += $@" and PackingId not in ({entity.Packing1Ids.Combine()})";
            }
            cmd += $@")
            insert into Quotation(BranchId
											   ,[TypeId]
											   ,[RouteId]
											   ,[ContainerTypeId]
											   ,[PackingId]
											   ,[BossId]
											   ,[LocationId]
											   ,[PolicyTypeId]
											   ,[UnitPrice]
											   ,[UnitPrice1]
											   ,[UnitPrice2]
											   ,[StartDate]
											   ,[Note]
											   ,[Active]
											   ,[InsertedDate]
											   ,[InsertedBy]
											   ,[QuotationUpdateId]
											   ,[UnitPrice3]
											   ,[ParentId]
											   ,[IsParent]
											   ,[RegionId]
											   ,[DistrictId]
											   ,[ProvinceId])
            SELECT [BranchId]
					   ,[TypeId]
					   ,[RouteId]
					   ,[ContainerTypeId]
					   ,[PackingId]
					   ,[BossId]
					   ,[LocationId]
					   ,[PolicyTypeId]
					   ,case when [UnitPrice] > 0 then case when {(entity.IsAdd ? 1 : 0)} = 1 then [UnitPrice] + {entity.UnitPrice} else [UnitPrice] - {entity.UnitPrice} end else [UnitPrice] end
					   ,case when [UnitPrice1] > 0 then case when {entity.TypeId} = 7592 or {entity.TypeId} = 7593 or {entity.TypeId} = 7594 or {entity.TypeId} = 7596 then case when {(entity.IsAdd ? 1 : 0)} = 1 then [UnitPrice1] + {entity.UnitPrice} else [UnitPrice1] - {entity.UnitPrice} end else [UnitPrice1] end end 
					   ,UnitPrice2
					   ,'{entity.StartDate:yyyy-MM-dd}' as [StartDate]
					   ,[Note]
					   ,[Active]
					   ,GETDATE() as InsertedDate
					   ,[InsertedBy]
					   ,[QuotationUpdateId]
					   ,case when [UnitPrice3] > 0 then case when {entity.TypeId} = 7592 then case when {(entity.IsAdd ? 1 : 0)} = 1 then [UnitPrice3] + {entity.UnitPrice} else [UnitPrice3] - {entity.UnitPrice} end else [UnitPrice3] end end
					   ,[ParentId]
					   ,[IsParent]
					   ,[RegionId]
					   ,[DistrictId]
					   ,[ProvinceId]
                        FROM cte
                        WHERE rn = 1";
            await db.Database.ExecuteSqlRawAsync(cmd);
            return true;
        }
    }
}
