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

        public override async Task<ActionResult<bool>> HardDeleteAsync([FromBody] List<int> ids)
        {
            if (ids.Nothing())
            {
                return true;
            }
            ids = ids.Where(x => x > 0).ToList();
            if (ids.Nothing())
            {
                return true;
            }
            try
            {
                foreach (var id in ids)
                {
                    var revenue = await db.Revenue.Where(x => x.Id == id).FirstOrDefaultAsync();
                    db.Revenue.Remove(revenue);
                    await db.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                throw new ApiException("Không thể xóa dữ liệu!") { StatusCode = HttpStatusCode.BadRequest };
            }
        }

        [HttpPost("api/Revenue/UpdateRevenueSimultaneous")]
        public async Task<bool> UpdateRevenueSimultaneous([FromBody] List<Revenue> revenues)
        {
            if (revenues == null)
            {
                return false;
            }
            var cmd = $"Update [{nameof(Revenue)}] set ";
            var item = revenues.Where(x => x.Id <= 0).FirstOrDefault();
            if (item.IsLotNo)
            {
                cmd += $"{nameof(Revenue.LotNo)} = '{item.LotNo}',";
            }
            if (item.IsLotDate)
            {
                cmd += $"{nameof(Revenue.LotDate)} = '{item.LotDate.Value.ToString("yyyy-MM-dd")}',";
            }
            if (item.IsInvoinceNo)
            {
                cmd += $"{nameof(Revenue.InvoinceNo)}  = '{item.InvoinceNo}',";
            }
            if (item.IsInvoinceDate)
            {
                cmd += $"{nameof(Revenue.InvoinceDate)}  = '{item.InvoinceDate.Value.ToString("yyyy-MM-dd")}',";
            }
            if (item.IsUnitPriceBeforeTax)
            {
                cmd += $"{nameof(Revenue.UnitPriceBeforeTax)}  = '{item.UnitPriceBeforeTax}',";
            }
            if (item.IsUnitPriceAfterTax)
            {
                cmd += $"{nameof(Revenue.UnitPriceAfterTax)}  = '{item.UnitPriceAfterTax}',";
            }
            if (item.IsReceivedPrice)
            {
                cmd += $"{nameof(Revenue.ReceivedPrice)}  = '{item.ReceivedPrice}',";
            }
            if (item.IsCollectOnBehaftPrice)
            {
                cmd += $"{nameof(Revenue.CollectOnBehaftPrice)}  = '{item.CollectOnBehaftPrice}',";
            }
            if (item.IsVat)
            {
                cmd += $"{nameof(Revenue.Vat)}  = '{item.Vat}',";
            }
            if (item.IsTotalPriceBeforTax)
            {
                cmd += $"{nameof(Revenue.TotalPriceBeforTax)}   = '{item.TotalPriceBeforTax}',";
            }
            if (item.IsVatPrice)
            {
                cmd += $"{nameof(Revenue.VatPrice)}   = '{item.VatPrice}',";
            }
            if (item.IsTotalPrice)
            {
                cmd += $"{nameof(Revenue.TotalPrice)}   = '{item.TotalPrice}',";
            }
            if (item.IsNotePayment)
            {
                cmd += $"{nameof(Revenue.NotePayment)}   = '{item.NotePayment}',";
            }
            if (item.IsVendorVatId)
            {
                cmd += $"{nameof(Revenue.VendorVatId)}   = '{item.VendorVatId}',";
            }
            cmd = cmd.TrimEnd(',');
            revenues.Remove(item);
            var ids = revenues.Select(x => x.Id).ToList();
            cmd += $"where Id in ({ids.Combine()})";
            await db.Database.ExecuteSqlRawAsync(cmd);
            return true;
        }
    }
}
