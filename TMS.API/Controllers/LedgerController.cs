using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class LedgerController : TMSController<Ledger>
    {
        public LedgerController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<Ledger>> PatchAsync([FromQuery] ODataQueryOptions<Ledger> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            Ledger entity = default;
            Ledger oldEntity = default;
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            if (id != null && id.TryParseInt() > 0)
            {
                var idInt = id.TryParseInt() ?? 0;
                entity = await db.Set<Ledger>().FindAsync(idInt);
                oldEntity = await db.Ledger.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
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
            if (entity.InvoiceFormId == 16047 && entity.ParentId != null && entity.TypeId is null && !AllRoleIds.Contains(31) && !AllRoleIds.Contains(8))
            {
                var makeup = await db.Ledger.Where(x => x.ParentId == entity.ParentId && x.IsMakeUp).FirstOrDefaultAsync();
                if (makeup != null && entity.OriginPriceAfterTax == oldEntity.OriginPriceAfterTax + makeup.OriginPriceAfterTax)
                {
                    entity.OriginPriceAfterTax = oldEntity.OriginPriceAfterTax;
                    entity.OriginPriceBeforeTax = oldEntity.OriginPriceBeforeTax;
                    entity.OriginVatAmount = oldEntity.OriginVatAmount;
                }
            }
            await db.SaveChangesAsync();
            await db.Entry(entity).ReloadAsync();
            RealTimeUpdate(entity);
            return entity;
        }

        private void RealTimeUpdate(Ledger entity)
        {
            var thead = new Thread(async () =>
            {
                try
                {
                    await _taskService.SendMessageAllUser(new WebSocketResponse<Ledger>
                    {
                        EntityId = _entitySvc.GetEntity(typeof(Ledger).Name).Id,
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
                var deleteCommand = $"delete from [{typeof(Ledger).Name}] where ParentId in ({string.Join(",", ids)}); delete from [{typeof(Ledger).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteCommand);
                return true;
            }
            catch
            {
                throw new ApiException("Không thể xóa dữ liệu!") { StatusCode = HttpStatusCode.BadRequest };
            }
        }
    }
}
