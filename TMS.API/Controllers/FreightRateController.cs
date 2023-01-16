using Core.Exceptions;
using Core.Enums;
using Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.AspNet.OData.Query;
using Core.ViewModels;

namespace TMS.API.Controllers
{
    public class FreightRateController : TMSController<FreightRate>
    {
        public FreightRateController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<FreightRate>> PatchAsync([FromQuery] ODataQueryOptions<FreightRate> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            FreightRate entity = default;
            FreightRate oldEntity = default;
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            if (id != null && id.TryParseInt() > 0)
            {
                var idInt = id.TryParseInt() ?? 0;
                entity = await db.Set<FreightRate>().FindAsync(idInt);
                oldEntity = await db.FreightRate.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
            }
            else
            {
                entity = await GetEntityByOdataOptions(options);
                oldEntity = await GetEntityByOdataOptions(options);
            }
            patch.ApplyTo(entity);
            SetAuditInfo(entity);
            if ((int)entity.GetPropValue(IdField) <= 0)
            {
                db.Add(entity);
            }
            if (oldEntity.IsClosing && entity.IsClosing)
            {
                throw new ApiException("Biểu giá CVC này đã được khóa. Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
            }
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.BossId)
            || x.Field == nameof(oldEntity.UserId) ||
            x.Field == nameof(oldEntity.TransportationTypeId) ||
            x.Field == nameof(oldEntity.RouteId) ||
            x.Field == nameof(oldEntity.ReceivedId) ||
            x.Field == nameof(oldEntity.ReturnId) ||
            x.Field == nameof(oldEntity.UnitPriceCont20) ||
            x.Field == nameof(oldEntity.UnitPriceCont40) ||
            x.Field == nameof(oldEntity.UnitPriceNoVatCont20) ||
            x.Field == nameof(oldEntity.UnitPriceNoVatCont40) ||
            x.Field == nameof(oldEntity.UnitPriceNoVatTon) ||
            x.Field == nameof(oldEntity.UnitPriceTon) ||
            x.Field == nameof(oldEntity.StartDate) ||
            x.Field == nameof(oldEntity.EndDate) ||
            x.Field == nameof(oldEntity.Notes)) &&
            (oldEntity.BossId != entity.BossId) ||
            (oldEntity.UserId != entity.UserId) ||
            (oldEntity.TransportationTypeId != entity.TransportationTypeId) ||
            (oldEntity.RouteId != entity.RouteId) ||
            (oldEntity.ReceivedId != entity.ReceivedId) ||
            (oldEntity.ReturnId != entity.ReturnId) ||
            (oldEntity.UnitPriceCont20 != entity.UnitPriceCont20) ||
            (oldEntity.UnitPriceCont40 != entity.UnitPriceCont40) ||
            (oldEntity.UnitPriceNoVatCont20 != entity.UnitPriceNoVatCont20) ||
            (oldEntity.UnitPriceNoVatCont40 != entity.UnitPriceNoVatCont40) ||
            (oldEntity.UnitPriceNoVatTon != entity.UnitPriceNoVatTon) ||
            (oldEntity.UnitPriceTon != entity.UnitPriceTon) ||
            (oldEntity.StartDate != entity.StartDate) ||
            (oldEntity.EndDate != entity.EndDate) ||
            (oldEntity.Notes != entity.Notes))
            {
                if (entity.IsChange)
                {
                    var newFreightRate = new FreightRate();
                    newFreightRate.CopyPropFrom(oldEntity);
                    newFreightRate.Id = 0;
                    newFreightRate.RequestChangeId = entity.Id;
                    db.FreightRate.Add(newFreightRate);
                }
            }
            await db.SaveChangesAsync();
            await db.Entry(entity).ReloadAsync();
            RealTimeUpdate(entity);
            return entity;
        }

        private void RealTimeUpdate(FreightRate entity)
        {
            var thead = new Thread(async () =>
            {
                try
                {
                    await _taskService.SendMessageAllUser(new WebSocketResponse<FreightRate>
                    {
                        EntityId = _entitySvc.GetEntity(typeof(FreightRate).Name).Id,
                        Data = entity
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("RealtimeUpdate error at {0}: {1} {2}", DateTimeOffset.Now, ex.Message, ex.StackTrace);
                }
            });
            thead.Start();
        }

        protected override IQueryable<FreightRate> GetQuery()
        {
            var rs = base.GetQuery();
            //Sale
            if (AllRoleIds.Contains(10))
            {
                //Biểu giá CVC
                rs =
                from fr in db.FreightRate
                from policy in db.FeaturePolicy
                    .Where(x => x.RecordId == fr.Id && x.EntityId == _entitySvc.GetEntity(nameof(FreightRate)).Id && x.CanRead)
                    .Where(x => x.InsertedBy == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                    .DefaultIfEmpty()
                where fr.InsertedBy == _userSvc.UserId
                    || policy != null
                select fr;
            }
            return rs;
        }

        [HttpPost("api/FreightRate/RequestUnLock")]
        public async Task RequestUnLock([FromBody] FreightRate freightRate)
        {
            var entityType = _entitySvc.GetEntity(typeof(FreightRate).Name);
            var approvalConfig = await db.ApprovalConfig.AsNoTracking().OrderBy(x => x.Level)
                .Where(x => x.Active && x.EntityId == entityType.Id).ToListAsync();
            if (approvalConfig.Nothing())
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
            if (matchApprovalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            await _taskService.SendMessageAllUser(new WebSocketResponse<FreightRate>
            {
                EntityId = _entitySvc.GetEntity(typeof(FreightRate).Name).Id,
                Data = freightRate
            });
            await db.SaveChangesAsync();
            if (approvalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var listUser = await (
                from user in db.User
                join userRole in db.UserRole on user.Id equals userRole.UserId
                join role in db.Role on userRole.RoleId equals role.Id
                where userRole.RoleId == matchApprovalConfig.RoleId
                select user
            ).ToListAsync();
            if (listUser.HasElement())
            {
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = listUser.Select(user => new TaskNotification
                {
                    Title = $"{currentUser.FullName}",
                    Description = $"Đã gửi yêu cầu mở khóa",
                    EntityId = _entitySvc.GetEntity(typeof(FreightRate).Name).Id,
                    RecordId = freightRate.Id,
                    Attachment = "fal fa-paper-plane",
                    AssignedId = user.Id,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now,
                });
                SetAuditInfo(tasks);
                db.AddRange(tasks);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(tasks);
            }
        }
    }
}
