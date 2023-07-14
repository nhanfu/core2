using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;
using NuGet.Versioning;
using OfficeOpenXml;
using System.Linq;
using TMS.API.Models;
using TMS.API.Services;
using TMS.API.ViewModels;
using Windows.Storage;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class VendorController : TMSController<Vendor>
    {
        private readonly VendorSvc _vendorSvc;

        public VendorController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor, VendorSvc vendorSvc) : base(context, entityService, httpContextAccessor)
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
                        || policy != null || vendor.Id == _userSvc.VendorId || vendor.UserId == _userSvc.UserId || vendor.UserId == 78 || vendor.IsSeft
                    select vendor;
            }
            return rs;
        }

        public override async Task<ActionResult<Vendor>> CreateAsync([FromBody] Vendor entity)
        {
            if (entity.Name != null && entity.Name != "" && entity.TypeId != 23741)
            {
                var vendorDB = await db.Vendor.Where(x => x.NameSys.ToLower() == entity.Name.ToLower() && x.TypeId == entity.TypeId).FirstOrDefaultAsync();
                if (vendorDB != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            if (entity.TypeId == 23741 && entity.CompanyName != null && entity.CompanyName != "")
            {
                var checkExist = await db.Vendor.Where(x => x.CompanyName.Trim().ToLower() == entity.CompanyName.Trim().ToLower() && x.TypeId == 23741).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            return await base.CreateAsync(entity);
        }

        public override Task<ActionResult<Vendor>> UpdateAsync([FromBody] Vendor entity, string reasonOfChange = "")
        {
            if (entity.TypeId == 23741)
            {
                var checkExist = db.Vendor.Where(x => x.CompanyName.Trim().ToLower() == entity.CompanyName.Trim().ToLower() && x.TypeId == 23741).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            return base.UpdateAsync(entity, reasonOfChange);
        }


        [HttpPost("api/Vendor/BackToSale")]
        public async Task<bool> BackToSale([FromBody] User entity, [FromQuery] DateTime? dateTime)
        {
            if (dateTime is null)
            {
                return false;
            }
            var sql = $" Update Vendor set IsSeft = 1 where UserId = {entity.Id};";
            sql += $" Update TransportationPlan set User2Id = {entity.Id},UserId = 78 where UserId = {entity.Id} and ClosingDate >= '{dateTime:yyyy-MM-dd}';";
            sql += $" Update Transportation set User2Id = {entity.Id},UserId = 78 where UserId = {entity.Id} and ClosingDate >= '{dateTime:yyyy-MM-dd}';";
            await ExecSql(sql, "disable trigger all on TransportationPlan;disable trigger all on Transportation;", $"enable trigger all on TransportationPlan;enable trigger all on Transportation;");
            return true;
        }

        [HttpPost("api/Vendor/ReturnToSale")]
        public async Task<bool> ReturnToSale([FromBody] User entity, [FromQuery] DateTime? dateTime)
        {
            if (dateTime is null)
            {
                return false;
            }
            var sql = $" Update Vendor set IsSeft = 0 where UserId = {entity.Id};";
            sql += $" Update TransportationPlan set User2Id = {entity.Id},UserId = {entity.Id} where BossId in (select Id from VendorId where UserId = {entity.Id}) and ClosingDate >= '{dateTime:yyyy-MM-dd}';";
            sql += $" Update Transportation set User2Id = {entity.Id},UserId = {entity.Id} where BossId in (select Id from VendorId where UserId = {entity.Id}) and ClosingDate >= '{dateTime:yyyy-MM-dd}';";
            await ExecSql(sql, "disable trigger all on TransportationPlan;disable trigger all on Transportation;", $"enable trigger all on TransportationPlan;enable trigger all on Transportation;");
            return true;
        }
    }
}