using Aspose.Cells;
using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class RevenueController : TMSController<Revenue>
    {
        public RevenueController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<Revenue>> PatchAsync([FromQuery] ODataQueryOptions<Revenue> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            Revenue entity = default;
            Revenue oldEntity = default;
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            if (id != null && id.TryParseInt() > 0)
            {
                var idInt = id.TryParseInt() ?? 0;
                entity = await db.Set<Revenue>().FindAsync(idInt);
                oldEntity = await db.Revenue.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
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
            if (patch.Changes.Any(x => x.Field == nameof(entity.LotNo)
                || x.Field == nameof(entity.LotDate)
                || x.Field == nameof(entity.Vat)
                || x.Field == nameof(entity.UnitPriceAfterTax)
                || x.Field == nameof(entity.UnitPriceBeforeTax)
                || x.Field == nameof(entity.ReceivedPrice)
                || x.Field == nameof(entity.CollectOnBehaftPrice)
                || x.Field == nameof(entity.NotePayment)
                || x.Field == nameof(entity.VendorVatId)))
            {
                var tran = await db.Transportation.Where(x => x.Id == entity.TransportationId).FirstOrDefaultAsync();
                if (tran != null && tran.IsSubmit)
                {
                    throw new ApiException("DSVC này đã được khóa. Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            await db.SaveChangesAsync();
            await db.Entry(entity).ReloadAsync();
            RealTimeUpdate(entity);
            return entity;
        }

        private void RealTimeUpdate(Revenue entity)
        {
            var thead = new Thread(async () =>
            {
                try
                {
                    await _taskService.SendMessageAllUser(new WebSocketResponse<Revenue>
                    {
                        EntityId = _entitySvc.GetEntity(typeof(Revenue).Name).Id,
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

        public override Task<ActionResult<Revenue>> CreateAsync([FromBody] Revenue entity)
        {
            if (entity.TransportationId is null)
            {
                throw new ApiException("Vui lòng chọn cont cần nhập") { StatusCode = HttpStatusCode.BadRequest };
            }
            return base.CreateAsync(entity);
        }
    }
}
