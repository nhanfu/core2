using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.Services;
using FileIO = System.IO.File;
using System.IO;
using TMS.API.ViewModels;
using Core.Exceptions;
using Core.Enums;
using Core.ViewModels;
using System.Text.RegularExpressions;
using Slugify;
using NuGet.Versioning;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace TMS.API.Controllers
{
    public class VendorController : TMSController<Vendor>
    {
        private readonly VendorSvc _vendorSvc;

        public VendorController(TMSContext context, IHttpContextAccessor httpContextAccessor, VendorSvc vendorSvc) : base(context, httpContextAccessor)
        {
            _vendorSvc = vendorSvc;
        }

        protected override IQueryable<Vendor> GetQuery()
        {
            var rs = base.GetQuery();
            //Sale
            if (RoleIds.Contains(10))
            {
                rs =
                from vendor in db.Vendor
                from policy in db.FeaturePolicy
                    .Where(x => x.RecordId == vendor.Id && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                    .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                    .DefaultIfEmpty()
                where vendor.InsertedBy == _userSvc.UserId
                    || policy != null || vendor.Id == _userSvc.VendorId || vendor.UserId == _userSvc.UserId
                select vendor;
            }
            if (RoleIds.Contains(43) || RoleIds.Contains(17))
            {
                rs =
                    from vendor in db.Vendor
                    from policy in db.FeaturePolicy
                        .Where(x => x.RecordId == vendor.Id && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                        .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                        .DefaultIfEmpty()
                    where vendor.InsertedBy == _userSvc.UserId
                        || policy != null || vendor.Id == _userSvc.VendorId || vendor.UserId == _userSvc.UserId || vendor.UserId == 78
                    select vendor;
            }
            return rs;
        }

        public override async Task<ActionResult<bool>> HardDeleteAsync([FromBody] List<int> ids)
        {
            if (ids.Nothing())
            {
                return false;
            }
            ids = ids.Where(x => x > 0).ToList();
            if (ids.Nothing())
            {
                return false;
            }
            try
            {
                var deleteCommand = $"delete {nameof(VendorService)} where {nameof(VendorService.VendorId)} in ({string.Join(",", ids)});" +
                    $" delete {nameof(VendorContact)} where {nameof(VendorContact.BossId)} in ({string.Join(",", ids)});" +
                    $" delete {nameof(VendorLocation)} where {nameof(VendorLocation.VendorId)} in ({string.Join(",", ids)});" +
                    $" delete from [{typeof(Vendor).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteCommand);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet("api/[Controller]/GetHasValue")]
        public Task<OdataResult<Vendor>> UserClick(ODataQueryOptions<Vendor> options)
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(Vendor).Name}]
                    where Id in (select distinct BossId from [{typeof(TransportationPlan).Name}])";
            var data = db.Vendor.FromSqlRaw(sql);
            return ApplyQuery(options, data, sql: sql);
        }

        public override async Task<ActionResult<Vendor>> PatchAsync([FromQuery] ODataQueryOptions<Vendor> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            Vendor entity = default;
            Vendor oldEntity = default;
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            if (id != null && id.TryParseInt() > 0)
            {
                var idInt = id.TryParseInt() ?? 0;
                entity = await db.Set<Vendor>().FindAsync(idInt);
                oldEntity = await db.Vendor.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
            }
            else
            {
                entity = await GetEntityByOdataOptions(options);
                oldEntity = await GetEntityByOdataOptions(options);
            }
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.Name)))
            {
                if (oldEntity.Name != null && oldEntity.Name != "")
                {
                    var name = patch.Changes.FirstOrDefault(x => x.Field == nameof(oldEntity.Name));
                    var vendorDB = await db.Vendor.Where(x => x.Active && x.TypeId == entity.TypeId && x.NameSys.ToLower().Contains(entity.Name) && (x.Id != id.TryParseInt() || (int)entity.GetPropValue(IdField) <= 0)).FirstOrDefaultAsync();
                    if (vendorDB != null)
                    {
                        throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                    }
                }
            }
            patch.ApplyTo(entity);
            SetAuditInfo(entity);
            if ((int)entity.GetPropValue(IdField) <= 0)
            {
                db.Add(entity);
            }
            await db.SaveChangesAsync();
            await db.Entry(entity).ReloadAsync();
            return entity;
        }

        public override async Task<ActionResult<Vendor>> CreateAsync([FromBody] Vendor entity)
        {
            if (entity.Name != null && entity.Name != "")
            {
                var vendorDB = await db.Vendor.Where(x => x.NameSys.ToLower() == entity.Name.ToLower() && x.TypeId == entity.TypeId).FirstOrDefaultAsync();
                if (vendorDB != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            return await base.CreateAsync(entity);
        }
    }
}