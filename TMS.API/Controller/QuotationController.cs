using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Slugify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class QuotationController : TMSController<Quotation>
    {
        public QuotationController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
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
            return await base.CreateAsync(entity);
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
