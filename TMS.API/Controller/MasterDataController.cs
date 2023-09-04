using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Hangfire;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using TMS.API.Models;
using TMS.API.Services;
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
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            var idInt = id.TryParseInt() ?? 0;
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("Default")))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Transaction = transaction;
                        command.Connection = connection;
                        var updates = patch.Changes.Where(x => x.Field != IdField).ToList();
                        var update = updates.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
                        if (disableTrigger)
                        {
                            command.CommandText += $" DISABLE TRIGGER ALL ON [{typeof(MasterData).Name}];";
                        }
                        else
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{typeof(MasterData).Name}];";
                        }
                        command.CommandText += $" UPDATE [{typeof(MasterData).Name}] SET {update.Combine()} WHERE Id = {idInt};";
                        if (disableTrigger)
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{typeof(MasterData).Name}];";
                        }
                        foreach (var item in updates)
                        {
                            command.Parameters.AddWithValue($"@{item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                        }
                        command.ExecuteNonQuery();
                        transaction.Commit();
                        var entity = await ctx.Set<MasterData>().FindAsync(idInt);
                        if (!disableTrigger)
                        {
                            await ctx.Entry(entity).ReloadAsync();
                        }

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
                        BackgroundJob.Enqueue<TaskService>(x => x.SendMessageAllUserOtherMe(new WebSocketResponse<MasterData>
                        {
                            EntityId = _entitySvc.GetEntity(typeof(MasterData).Name).Id,
                            TypeId = 1,
                            Data = entity
                        }, UserId));
                        return entity;
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var entity = await ctx.Set<MasterData>().FindAsync(idInt);
                    return StatusCode(409, entity);
                }
            }
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
