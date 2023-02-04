using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.AspNet.OData.Query;
using Core.ViewModels;
using Core.Extensions;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using Core.Exceptions;
using Core.Enums;
using PuppeteerSharp.Input;
using System.Text.RegularExpressions;

namespace TMS.API.Controllers
{
    public class BookingListController : TMSController<BookingList> 
    {
        public BookingListController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {

        }

        public override async Task<ActionResult<BookingList>> PatchAsync([FromQuery] ODataQueryOptions<BookingList> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            BookingList entity = default;
            BookingList oldEntity = default;
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            if (id != null && id.TryParseInt() > 0)
            {
                var idInt = id.TryParseInt() ?? 0;
                entity = await db.Set<BookingList>().FindAsync(idInt);
                oldEntity = await db.BookingList.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
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
            if (oldEntity.Submit && entity.Submit)
            {
                throw new ApiException("Danh sách book tàu này đã bị khóa !!!") { StatusCode = HttpStatusCode.BadRequest };
            }
            await db.SaveChangesAsync();
            await db.Entry(entity).ReloadAsync();
            RealTimeUpdate(entity);
            return entity;
        }

        private void RealTimeUpdate(BookingList entity)
        {
            var thead = new Thread(async () =>
            {
                try
                {
                    await _taskService.SendMessageAllUser(new WebSocketResponse<BookingList>
                    {
                        EntityId = _entitySvc.GetEntity(typeof(BookingList).Name).Id,
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

        [HttpPost("api/[Controller]/CreateAllBookingList")]
        public async Task<bool> CreateAllBookingList([FromBody] BookingList entity)
        {
            var query = db.Transportation.Where(x => x.BookingListId == null
            && x.BookingId != null).AsQueryable();
            var rs = await query.AsNoTracking().ToListAsync();
            var groupByPercentileQuery = (from tran in rs
                                          group tran by new
                                          {
                                              tran.ClosingDate.Value.Month,
                                              tran.ClosingDate.Value.Year,
                                              tran.RouteId,
                                              tran.BrandShipId,
                                              tran.ExportListId,
                                              tran.ShipId,
                                              tran.LineId,
                                              tran.SocId,
                                              tran.Trip,
                                              StartShip = tran.StartShip.Value.Date,
                                              tran.ContainerTypeId,
                                              tran.PolicyId,
                                              ShipUnitPrice = tran.ShipPrice ?? tran.ShipUnitPrice
                                          } into tranGroup
                                          select new BookingList()
                                          {
                                              Month = tranGroup.Key.Month,
                                              Year = tranGroup.Key.Year,
                                              ExportListId = tranGroup.Key.ExportListId,
                                              RouteId = tranGroup.Key.RouteId,
                                              BrandShipId = tranGroup.Key.BrandShipId,
                                              ShipId = tranGroup.Key.ShipId,
                                              LineId = tranGroup.Key.LineId,
                                              SocId = tranGroup.Key.SocId,
                                              Trip = tranGroup.Key.Trip,
                                              StartShip = tranGroup.Key.StartShip,
                                              ContainerTypeId = tranGroup.Key.ContainerTypeId,
                                              PolicyId = tranGroup.Key.PolicyId,
                                              Count = tranGroup.Count(),
                                              ShipUnitPrice = tranGroup.Key.ShipUnitPrice ?? 0,
                                              ShipPrice = tranGroup.Where(x => x.ShipUnitPrice != null).Sum(x => x.ShipUnitPrice.Value),
                                              ShipPolicyPrice = tranGroup.Where(x => x.ShipPolicyPrice != null).Sum(x => x.ShipPolicyPrice.Value),
                                              TransportationIds = tranGroup.Select(x => x.Id).Combine()
                                          }).ToList();
            SetAuditInfo(groupByPercentileQuery);
            groupByPercentileQuery.ForEach(x => { CalcTotalPriceAndTotalFee(x); });
            db.AddRange(groupByPercentileQuery);
            await db.SaveChangesAsync();
            foreach (var item in groupByPercentileQuery)
            {
                var trans = await db.Transportation.Where(x => item.TransportationIds.Contains(x.Id.ToString())).ToListAsync();
                trans.ForEach(x => x.BookingListId = item.Id);
                await db.SaveChangesAsync();
            }
            return true;
        }

        //[HttpPost("api/[Controller]/CreateBookingList")]
        //public async Task<bool> CreateBookingList([FromBody] BookingList entity)
        //{
        //    var query = db.Transportation.Where(x => x.BookingListId == null 
        //    && x.BookingId != null 
        //    && x.ClosingDate >= entity.FromDate 
        //    && x.ClosingDate <= entity.ToDate
        //    && x.RouteId == entity.RouteId
        //    && x.ShipId == entity.ShipId
        //    && x.Trip == entity.Trip).AsQueryable();
        //    var rs = await query.AsNoTracking().ToListAsync();
        //    var groupByPercentileQuery = (from tran in rs
        //                                  group tran by new
        //                                  {
        //                                      tran.ClosingDate.Value.Month,
        //                                      tran.ClosingDate.Value.Year,
        //                                      tran.RouteId,
        //                                      tran.BrandShipId,
        //                                      tran.ExportListId,
        //                                      tran.ShipId,
        //                                      tran.LineId,
        //                                      tran.SocId,
        //                                      tran.Trip,
        //                                      tran.StartShip,
        //                                      tran.ContainerTypeId,
        //                                      tran.PolicyId,
        //                                      ShipPrice = tran.ShipUnitPrice ?? tran.ShipPrice
        //                                  } into tranGroup
        //                                  select new BookingList()
        //                                  {
        //                                      Month = tranGroup.Key.Month,
        //                                      ExportListId = tranGroup.Key.ExportListId,
        //                                      Year = tranGroup.Key.Year,
        //                                      RouteId = tranGroup.Key.RouteId,
        //                                      BrandShipId = tranGroup.Key.BrandShipId,
        //                                      ShipId = tranGroup.Key.ShipId,
        //                                      LineId = tranGroup.Key.LineId,
        //                                      SocId = tranGroup.Key.SocId,
        //                                      Trip = tranGroup.Key.Trip,
        //                                      StartShip = tranGroup.Key.StartShip,
        //                                      ContainerTypeId = tranGroup.Key.ContainerTypeId,
        //                                      PolicyId = tranGroup.Key.PolicyId,
        //                                      Count = tranGroup.Count(),
        //                                      ShipUnitPrice = tranGroup.Key.ShipPrice ?? 0,
        //                                      ShipPrice = tranGroup.Where(x => x.ShipPrice != null).Sum(x => x.ShipPrice.Value),
        //                                      ShipPolicyPrice = tranGroup.Where(x => x.ShipPolicyPrice != null).Sum(x => x.ShipPolicyPrice.Value),
        //                                      TransportationIds = tranGroup.Select(x => x.Id).Combine()
        //                                  }).ToList();
        //    SetAuditInfo(groupByPercentileQuery);
        //    foreach (var item in groupByPercentileQuery.ToList())
        //    {
        //        var checkBookingList = await db.BookingList.Where(x =>
        //        x.Month == item.Month
        //        && x.Year == item.Year
        //        && x.RouteId == item.RouteId
        //        && x.ShipId == item.ShipId
        //        && x.LineId == item.LineId
        //        && x.BrandShipId == item.BrandShipId
        //        && x.Trip == item.Trip
        //        && x.StartShip == item.StartShip
        //        && x.ContainerTypeId == item.ContainerTypeId
        //        && x.SocId == item.SocId
        //        && x.ExportListId == item.ExportListId
        //        && x.PolicyId == item.PolicyId
        //        && x.ShipPrice == item.ShipPrice
        //        && x.Submit == false).FirstOrDefaultAsync();
        //        if (checkBookingList != null)
        //        {
        //            checkBookingList.Count++;
        //            checkBookingList.ShipPrice += item.ShipPrice;
        //            checkBookingList.ShipPolicyPrice += item.ShipPolicyPrice;
        //            CalcTotalPriceAndTotalFee(checkBookingList);
        //            groupByPercentileQuery.Remove(item);
        //        }
        //        else
        //        {
        //            CalcTotalPriceAndTotalFee(item);
        //        }
        //    }
        //    db.AddRange(groupByPercentileQuery);
        //    await db.SaveChangesAsync();
        //    StringBuilder sql = new StringBuilder();
        //    await db.Database.ExecuteSqlRawAsync("DISABLE Trigger [dbo].[tr_Transportation_UpdateTeus] on [dbo].[Transportation]");
        //    foreach (var item in groupByPercentileQuery)
        //    {
        //        sql.AppendLine(@$"update [{nameof(Transportation)}] set BookingListId = {item.Id} 
        //                   where MONTH(ClosingDate) = {item.Month}
        //                   and YEAR(ClosingDate) = {item.Year}
        //                   and RouteId {(item.RouteId is null ? "is null" : (" = " + item.RouteId))}
        //                   and BrandShipId {(item.BrandShipId is null ? "is null" : (" = " + item.BrandShipId))}
        //                   and ExportListId {(item.ExportListId is null ? "is null" : (" = " + item.ExportListId))}
        //                   and ShipId {(item.ShipId is null ? "is null" : (" = " + item.ShipId))}
        //                   and LineId {(item.LineId is null ? "is null" : (" = " + item.LineId))}
        //                   and StartShip {(item.StartShip is null ? "is null" : (" = '" + item.StartShip.Value.ToString("yyyy-MM-dd") + "'"))}
        //                   and ContainerTypeId {(item.ContainerTypeId is null ? "is null" : (" = " + item.ContainerTypeId))}
        //                   and isnull(ShipUnitPrice, ShipPrice) = {item.ShipUnitPrice}
        //                   and PolicyId {(item.PolicyId is null ? "is null" : (" = " + item.PolicyId))};");
        //    }
        //    await db.Database.ExecuteSqlRawAsync(sql.ToString());
        //    await db.Database.ExecuteSqlRawAsync("ENABLE Trigger [dbo].[tr_Transportation_UpdateTeus] on [dbo].[Transportation]");
        //    return true;
        //}

        [HttpPost("api/[Controller]/UpdateBookingList")]
        public async Task<bool> UpdateBookingList([FromBody] BookingList entity)
        {
            var rs = await db.Transportation.Where(x => x.StartShip.Value.Date >= entity.FromDate.Value.Date && x.StartShip.Value.Date <= entity.ToDate.Value.Date && x.BookingId != null).ToListAsync();
            if (rs == null)
            {
                return false;
            }
            var groupByPercentileQuery = (from tran in rs
                                          group tran by new
                                          {
                                              tran.ClosingDate.Value.Month,
                                              tran.ClosingDate.Value.Year,
                                              tran.RouteId,
                                              tran.BrandShipId,
                                              tran.ExportListId,
                                              tran.ShipId,
                                              tran.LineId,
                                              tran.SocId,
                                              tran.Trip,
                                              StartShip = tran.StartShip.Value.Date,
                                              tran.ContainerTypeId,
                                              tran.PolicyId,
                                              ShipUnitPrice = tran.ShipPrice ?? tran.ShipUnitPrice
                                          } into tranGroup
                                          select new BookingList()
                                          {
                                              Month = tranGroup.Key.Month,
                                              Year = tranGroup.Key.Year,
                                              ExportListId = tranGroup.Key.ExportListId,
                                              RouteId = tranGroup.Key.RouteId,
                                              BrandShipId = tranGroup.Key.BrandShipId,
                                              ShipId = tranGroup.Key.ShipId,
                                              LineId = tranGroup.Key.LineId,
                                              SocId = tranGroup.Key.SocId,
                                              Trip = tranGroup.Key.Trip,
                                              StartShip = tranGroup.Key.StartShip,
                                              ContainerTypeId = tranGroup.Key.ContainerTypeId,
                                              PolicyId = tranGroup.Key.PolicyId,
                                              Count = tranGroup.Count(),
                                              ShipUnitPrice = tranGroup.Key.ShipUnitPrice ?? 0,
                                              ShipPrice = tranGroup.Where(x => x.ShipUnitPrice != null).Sum(x => x.ShipUnitPrice.Value),
                                              ShipPolicyPrice = tranGroup.Where(x => x.ShipPolicyPrice != null).Sum(x => x.ShipPolicyPrice.Value),
                                              TransportationIds = tranGroup.Select(x => x.Id).Combine()
                                          }).ToList();
            SetAuditInfo(groupByPercentileQuery);
            groupByPercentileQuery.ForEach(x => { CalcTotalPriceAndTotalFee(x); });
            foreach (var item in groupByPercentileQuery)
            {
                var checkBookingList = await db.BookingList.Where(x =>
                x.Month == item.Month
                && x.Year == item.Year
                && x.RouteId == item.RouteId
                && x.ShipId == item.ShipId
                && x.LineId == item.LineId
                && x.BrandShipId == item.BrandShipId
                && x.Trip == item.Trip
                && x.StartShip == item.StartShip
                && x.ContainerTypeId == item.ContainerTypeId
                && x.SocId == item.SocId
                && x.ExportListId == item.ExportListId
                && x.PolicyId == item.PolicyId
                && x.ShipUnitPrice == item.ShipUnitPrice
                && x.Submit == false).FirstOrDefaultAsync();
                if (checkBookingList == null)
                {
                    db.Add(item);
                    await db.SaveChangesAsync();
                }
                else
                {
                    if (item.Count != checkBookingList.Count || item.ShipPrice != checkBookingList.ShipPrice || item.ShipPolicyPrice != checkBookingList.ShipPolicyPrice)
                    {
                        checkBookingList.Count = item.Count;
                        checkBookingList.ShipPrice = item.ShipPrice;
                        checkBookingList.ShipPolicyPrice = item.ShipPolicyPrice;
                        CalcTotalPriceAndTotalFee(checkBookingList);
                    }
                }
                var trans = await db.Transportation.Where(x => item.TransportationIds.Contains(x.Id.ToString())).ToListAsync();
                trans.ForEach(x => x.BookingListId = item.Id);
                await db.SaveChangesAsync();
            }
            return true;
        }

        public void CalcTotalPriceAndTotalFee(BookingList bookingList)
        {
            bookingList.TotalPrice = bookingList.ShipUnitPrice * bookingList.Count;
            bookingList.TotalFee = bookingList.TotalPrice + bookingList.OrtherFeePrice;
        }
    }
}
