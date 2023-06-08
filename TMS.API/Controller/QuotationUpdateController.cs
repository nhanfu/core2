using Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class QuotationUpdateController : TMSController<QuotationUpdate>
    {
        public QuotationUpdateController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }

        public List<int> GetRegionPath(IEnumerable<MasterData> regions)
        {
            if (regions is null)
            {
                return new List<int>();
            }
            var pickupPaths = regions.SelectMany(x => x.Path.Split("\\")
            .Select(x =>
            {
                var parsed = int.TryParse(x, out int res);
                return parsed ? (int?)res : null;
            }).Where(x => x.HasValue).Select(x => x.Value).Union(new List<int> { x.Id }));
            return pickupPaths.Distinct().ToList();
        }

        [HttpPost("api/QuotationUpdate/QuotationUpdate")]
        public async Task<bool> QuotationUpdate([FromBody] QuotationUpdate entity)
        {
            SetAuditInfo(entity);
            db.Add(entity);
            await db.SaveChangesAsync();
            var regionIds = new List<int>();
            if (entity.RegionIds != null && entity.RegionIds.Any())
            {
                if (entity.TypeRegionId == 26418)
                {
                    var regions = await db.MasterData.Where(x => entity.RegionIds.Contains(x.Id)).ToListAsync();
                    regionIds = GetRegionPath(regions);
                }
                else if (entity.TypeRegionId == 26420)
                {
                    var regions = await db.MasterData.Where(x => entity.RegionIds.Contains(x.Id)).ToListAsync();
                    foreach (var item in regions)
                    {
                        var ids = await db.MasterData.AsNoTracking().Where(x => x.Path.Contains(item.Path + $"{item.Id}\\")).Select(x => x.Id).ToListAsync();
                        regionIds.AddRange(ids);
                        regionIds.Add(item.Id);
                    }
                }
                else
                {
                    regionIds = entity.RegionIds.Select(x => x.Value).ToList();
                }
            }
            var cmd = $@";WITH cte AS
                            (
                               SELECT *,
                                     ROW_NUMBER() OVER (PARTITION BY PackingId,BossId,ContainerTypeId,LocationId,RouteId,RegionId ORDER BY StartDate DESC) AS rn
                               FROM Quotation
                               where TypeId = {entity.TypeId}
                               and ExportListId = {VendorId}";
            if (!entity.Support)
            {
                cmd = $@" and Support = 0";
            }
            cmd = $@" and StartDate < '{entity.StartDate:yyyy-MM-dd}'";
            if (entity.ContainerId != null)
            {
                cmd += $@" and ContainerId = {entity.ContainerId}";
            }
            if (entity.PackingIds != null && entity.PackingIds.Any())
            {
                cmd += $@" and PackingId in ({entity.PackingIds.Combine()})";
            }
            if (regionIds != null && regionIds.Any())
            {
                cmd += $@" and RegionId in ({regionIds.Combine()})";
            }
            if (entity.Packing1Ids != null && entity.Packing1Ids.Any())
            {
                cmd += $@" and PackingId not in ({entity.Packing1Ids.Combine()})";
            }
            var unitPrice1 = entity.UnitPrice1 ?? entity.UnitPrice;
            var unitPrice3 = entity.UnitPrice3 ?? entity.UnitPrice;
            cmd += $@")";
            if (entity.Packing1Ids != null && entity.Packing1Ids.Any() || entity.PackingIds != null && entity.PackingIds.Any())
            {
                cmd += $@";WITH cte1 AS
                            (
                               SELECT *,
                                     ROW_NUMBER() OVER (PARTITION BY PackingId,BossId,ContainerTypeId,LocationId,RouteId,RegionId ORDER BY StartDate DESC) AS rn1
                               FROM Quotation
                               where TypeId = {entity.TypeId}
                               and ExportListId = {VendorId}";
                if (!entity.Support)
                {
                    cmd = $@" and Support = 0";
                }
                cmd = $@" and StartDate < '{entity.StartDate:yyyy-MM-dd}'";
                if (entity.ContainerId != null)
                {
                    cmd += $@" and ContainerId = {entity.ContainerId}";
                }
                if (regionIds != null && regionIds.Any())
                {
                    cmd += $@" and RegionId in ({regionIds.Combine()})";
                }
                if (entity.Packing1Ids != null && entity.Packing1Ids.Any())
                {
                    cmd += $@" and PackingId in ({entity.Packing1Ids.Combine()})";
                }
                cmd += $@")";
            }
            if (!entity.Support)
            {
                cmd += $@";WITH cte2 AS
                            (
                               SELECT *,
                                     ROW_NUMBER() OVER (PARTITION BY PackingId,BossId,ContainerTypeId,LocationId,RouteId,RegionId ORDER BY StartDate DESC) AS rn1
                               FROM Quotation
                               where TypeId = {entity.TypeId}
                               and ExportListId = {VendorId}";
                if (!entity.Support)
                {
                    cmd = $@" and Support = 1";
                }
                cmd = $@" and StartDate < '{entity.StartDate:yyyy-MM-dd}'";
                if (entity.ContainerId != null)
                {
                    cmd += $@" and ContainerId = {entity.ContainerId}";
                }
                if (regionIds != null && regionIds.Any())
                {
                    cmd += $@" and RegionId in ({regionIds.Combine()})";
                }
                if (entity.PackingIds != null && entity.PackingIds.Any())
                {
                    cmd += $@" and PackingId in ({entity.PackingIds.Combine()})";
                }
                cmd += $@")";
            }
            cmd += $@"insert into Quotation(BranchId
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
                           ,[ProvinceId]
                           ,[ExportListId])
                SELECT[BranchId]
            ,[TypeId]
            ,[RouteId]
            ,[ContainerTypeId]
            ,[PackingId]
            ,[BossId]
            ,[LocationId]
            ,[PolicyTypeId]
            ,case when[UnitPrice] > 0 then case when {(entity.IsAdd ? 1 : 0)} = 1 then[UnitPrice] + {entity.UnitPrice} else [UnitPrice] - {entity.UnitPrice}
                end else [UnitPrice] end
            ,isnull(case when[UnitPrice1] > 0 then case when {entity.TypeId} = 7592 or {entity.TypeId} = 7593 or {entity.TypeId} = 7594 or {entity.TypeId} = 7596 then case when {(entity.IsAdd ? 1 : 0)} = 1 then[UnitPrice1] + {unitPrice1} else [UnitPrice1] - {unitPrice1}
                end else [UnitPrice1] end end,0)
            ,UnitPrice2
            ,'{entity.StartDate:yyyy-MM-dd}' as [StartDate]
            ,[Note]
            ,[Active]
            ,GETDATE() as InsertedDate
            ,{UserId}
            ,{entity.Id}
            ,isnull(case when[UnitPrice3] > 0 then case when {entity.TypeId} = 7592 then case when {(entity.IsAdd ? 1 : 0)} = 1 then[UnitPrice3] + {unitPrice3} else [UnitPrice3] - {unitPrice3}
                end else [UnitPrice3] end end,0)
            ,[ParentId]
            ,[IsParent]
            ,[RegionId]
            ,[DistrictId]
            ,[ProvinceId]
            ,{VendorId}
                FROM cte
                            WHERE rn = 1";


            if (entity.Packing1Ids != null && entity.Packing1Ids.Any() || entity.PackingIds != null && entity.PackingIds.Any())
            {
                cmd += $@"insert into Quotation(BranchId
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
            			   ,[ProvinceId]
                           ,[ExportListId])
                SELECT [BranchId]
            ,[TypeId]
            ,[RouteId]
            ,[ContainerTypeId]
            ,[PackingId]
            ,[BossId]
            ,[LocationId]
            ,[PolicyTypeId]
            ,case when [UnitPrice] > 0 then case when {(entity.IsAdd ? 1 : 0)} = 1 then [UnitPrice] else [UnitPrice] end else [UnitPrice] end
            ,isnull(case when [UnitPrice1] > 0 then case when {entity.TypeId} = 7592 or {entity.TypeId} = 7593 or {entity.TypeId} = 7594 or {entity.TypeId} = 7596 then case when {(entity.IsAdd ? 1 : 0)} = 1 then [UnitPrice1] else [UnitPrice1] end else [UnitPrice1] end end,0)
            ,UnitPrice2
            ,'{entity.StartDate:yyyy-MM-dd}' as [StartDate]
            ,[Note]
            ,[Active]
            ,GETDATE() as InsertedDate
            ,{UserId}
            ,{entity.Id}
            ,isnull(case when [UnitPrice3] > 0 then case when {entity.TypeId} = 7592 then case when {(entity.IsAdd ? 1 : 0)} = 1 then [UnitPrice3] else [UnitPrice3] end else [UnitPrice3] end end,0)
            ,[ParentId]
            ,[IsParent]
            ,[RegionId]
            ,[DistrictId]
            ,[ProvinceId]
            ,{VendorId}
                            FROM cte1
                            WHERE rn1 = 1";
            }

            if (!entity.Support)
            {
                cmd += $@"insert into Quotation(BranchId
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
            			   ,[ProvinceId]
                           ,[ExportListId])
                SELECT [BranchId]
            ,[TypeId]
            ,[RouteId]
            ,[ContainerTypeId]
            ,[PackingId]
            ,[BossId]
            ,[LocationId]
            ,[PolicyTypeId]
            ,case when [UnitPrice] > 0 then case when {(entity.IsAdd ? 1 : 0)} = 1 then [UnitPrice] else [UnitPrice] end else [UnitPrice] end
            ,isnull(case when [UnitPrice1] > 0 then case when {entity.TypeId} = 7592 or {entity.TypeId} = 7593 or {entity.TypeId} = 7594 or {entity.TypeId} = 7596 then case when {(entity.IsAdd ? 1 : 0)} = 1 then [UnitPrice1] else [UnitPrice1] end else [UnitPrice1] end end,0)
            ,UnitPrice2
            ,'{entity.StartDate:yyyy-MM-dd}' as [StartDate]
            ,[Note]
            ,[Active]
            ,GETDATE() as InsertedDate
            ,{UserId}
            ,{entity.Id}
            ,isnull(case when [UnitPrice3] > 0 then case when {entity.TypeId} = 7592 then case when {(entity.IsAdd ? 1 : 0)} = 1 then [UnitPrice3] else [UnitPrice3] end else [UnitPrice3] end end,0)
            ,[ParentId]
            ,[IsParent]
            ,[RegionId]
            ,[DistrictId]
            ,[ProvinceId]
            ,{VendorId}
                            FROM cte2
                            WHERE rn2 = 1";
            }
            await db.Database.ExecuteSqlRawAsync(cmd);
            return true;
        }
    }
}
