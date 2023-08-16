using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text.RegularExpressions;
using TMS.API.Models;
using TMS.API.ViewModels;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class MasterDataController : TMSController<MasterData>
    {
        public MasterDataController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor)
            : base(context, entityService, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<MasterData>> PatchAsync([FromQuery] ODataQueryOptions<MasterData> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            MasterData entity = default;
            MasterData oldEntity = default;
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            if (id != null && id.TryParseInt() > 0)
            {
                var idInt = id.TryParseInt() ?? 0;
                entity = await db.Set<MasterData>().FindAsync(idInt);
                oldEntity = await db.MasterData.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
            }
            else
            {
                entity = await GetEntityByOdataOptions(options);
                oldEntity = await GetEntityByOdataOptions(options);
            }
            await CheckDuplicatesSettingsTrainSchedule(entity);
            var des = patch.Changes.FirstOrDefault(x => x.Field == nameof(oldEntity.Description));
            if (des.Value != null)
            {
                var masterDataDB = await db.MasterData.Where(x => x.ParentId == entity.ParentId && x.Description != null && x.Description.ToLower() == des.Value.ToLower() && (x.Id != id.TryParseInt())).FirstOrDefaultAsync();
                if (masterDataDB != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            patch.ApplyTo(entity);
            SetAuditInfo(entity);
            if (!entity.Path.IsNullOrWhiteSpace() && entity.Path.Contains(@"\7651\"))
            {
                var commodity = await db.MasterData.Where(x => x.Path.Contains(@"\7651\") && x.Description.Trim().ToLower() == des.Value.ToLower()).FirstOrDefaultAsync();
                if (commodity != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            await db.SaveChangesAsync();
            if (entity.ParentId != null)
            {
                var parentEntity = await db.MasterData.FirstOrDefaultAsync(x => x.Id == entity.ParentId);
                var pathParent = parentEntity.Path;
                entity.Path = @$"\{pathParent}\{entity.ParentId}\".Replace("/", @"\").Replace(@"\\", @"\");
            }
            else
            {
                entity.Path = null;
            }
            SetLevel(entity);
            if (entity.InverseParent.Any())
            {
                entity.InverseParent.ForEach(x =>
                {
                    x.Path = @$"\{entity.Path}\{x.ParentId}\".Replace("/", @"\").Replace(@"\\", @"\");
                });
            }
            await db.SaveChangesAsync();
            return entity;
        }

        [AllowAnonymous]
        public override Task<OdataResult<MasterData>> Get(ODataQueryOptions<MasterData> options)
        {
            return ApplyQuery(options, db.MasterData);
        }

        public override async Task<ActionResult<MasterData>> UpdateAsync([FromBody] MasterData entity, string reasonOfChange = "")
        {
            if (entity.ParentId != null)
            {
                var masterDataDB = await db.MasterData.Where(x => x.ParentId == entity.ParentId && x.Description.ToLower() == entity.Description.ToLower() && (x.Id != entity.Id)).FirstOrDefaultAsync();
                if (masterDataDB != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            if (entity.Path != null && entity.Path.Contains(@"\7651\"))
            {
                var commodity = await db.MasterData.Where(x => x.Path.Contains(@"\7651\") && x.Description.Trim().ToLower() == entity.Description.Trim().ToLower()).FirstOrDefaultAsync();
                if (commodity != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            return await UpdateTreeNodeAsync(entity, reasonOfChange);
        }

        public override async Task<ActionResult<MasterData>> CreateAsync([FromBody] MasterData entity)
        {
            if (entity.ParentId != null)
            {
                await CheckDuplicatesSettingsTrainSchedule(entity);
                var masterDataDB = await db.MasterData.Where(x => x.ParentId == entity.ParentId && x.Description.ToLower() == entity.Description.ToLower()).FirstOrDefaultAsync();
                if (masterDataDB != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            if (entity.Path != null && entity.Path.Contains(@"\7651\"))
            {
                var commodity = await db.MasterData.Where(x => x.Path.Contains(@"\7651\") && x.Description.Trim().ToLower() == entity.Description.Trim().ToLower()).FirstOrDefaultAsync();
                if (commodity != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            var rs = await base.CreateAsync(entity);
            if (entity.ParentId != null)
            {
                var parentEntity = await db.MasterData.FirstOrDefaultAsync(x => x.Id == entity.ParentId);
                var pathParent = parentEntity.Path;
                entity.Path = @$"\{pathParent}\{entity.ParentId}\".Replace("/", @"\").Replace(@"\\", @"\");
            }
            else
            {
                entity.Path = null;
            }
            SetLevel(entity);
            if (entity.InverseParent.Any())
            {
                entity.InverseParent.ForEach(x =>
                {
                    x.Path = @$"\{entity.Path}\{x.ParentId}\".Replace("/", @"\").Replace(@"\\", @"\");
                });
            }
            await db.SaveChangesAsync();
            return rs;
        }

        public async Task CheckDuplicatesSettingsTrainSchedule(MasterData masterData)
        {
            if (masterData.Name != null && masterData.Name != "" && (masterData.ParentId == 25219 || masterData.ParentId == 25220 || masterData.ParentId == 25221 || masterData.ParentId == 25222))
            {
                var masterDataDB = await db.MasterData.Where(x => (x.ParentId == 25219 || x.ParentId == 25220 || x.ParentId == 25221 || x.ParentId == 25222) && x.Name.Trim().ToLower() == masterData.Name.Trim().ToLower()).FirstOrDefaultAsync();
                if (masterDataDB != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
        }

        [HttpPost("api/MasterData/UpdatePath")]
        public async Task<IActionResult> UpdatePath()
        {
            var ms = await db.MasterData.OrderByDescending(x => x.Id).ToListAsync();
            foreach (var item in ms)
            {
                await UpdateTreeNodeAsync(item);
            }
            return Ok(true);
        }
    }
}
