using Core.Enums;
using Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Exceptions;
using Core.Models;

namespace Core.Controllers
{
    public class VendorController : TMSController<Vendor>
    {
        public VendorController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<Vendor>> CreateAsync([FromBody] Vendor entity)
        {
            if (entity.Name != null && entity.Name != "" && entity.TypeId != 23741 .ToString())
            {
                var vendorDB = await db.Vendor.Where(x => x.NameSys.ToLower() == entity.Name.ToLower() && x.TypeId == entity.TypeId).FirstOrDefaultAsync();
                if (vendorDB != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            if (entity.TypeId == 23741 .ToString() && entity.CompanyName != null && entity.CompanyName != "")
            {
                var checkExist = await db.Vendor.Where(x => x.CompanyName.Trim().ToLower() == entity.CompanyName.Trim().ToLower() && x.TypeId == 23741 .ToString()).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            return await base.CreateAsync(entity);
        }

        public override Task<ActionResult<Vendor>> UpdateAsync([FromBody] Vendor entity, string reasonOfChange = "")
        {
            if (entity.TypeId == 23741 .ToString())
            {
                var checkExist = db.Vendor.Where(x => x.CompanyName.Trim().ToLower() == entity.CompanyName.Trim().ToLower() && x.TypeId == 23741 .ToString()).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            return base.UpdateAsync(entity, reasonOfChange);
        }
    }
}