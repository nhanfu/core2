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
using Microsoft.Extensions.Configuration;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;

namespace TMS.API.Controllers
{
    public class BookingListController : TMSController<BookingList> 
    {
        public BookingListController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<BookingList>> PatchAsync([FromQuery] ODataQueryOptions<BookingList> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            var idInt = id.TryParseInt() ?? 0;
            var entity = await db.BookingList.FindAsync(idInt); 
            if (patch.Changes.Any(x => x.Field == nameof(entity.Submit)) == false)
            {
                if (entity.Submit)
                {
                    throw new ApiException("Danh sách book tàu này đã bị khóa !!!") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
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
                            command.CommandText += $" DISABLE TRIGGER ALL ON [{nameof(BookingList)}];";
                        }
                        else
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(BookingList)}];";
                        }
                        command.CommandText += $" UPDATE [{nameof(BookingList)}] SET {update.Combine()} WHERE Id = {idInt};";
                        //
                        if (disableTrigger)
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(BookingList)}];";
                        }
                        foreach (var item in updates)
                        {
                            command.Parameters.AddWithValue($"@{item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                        }
                        command.ExecuteNonQuery();
                        transaction.Commit();
                        await db.Entry(entity).ReloadAsync();
                        return entity;
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return entity;
                }
            }
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
            var listAdd = new List<int>();
            var tranIds = groupByPercentileQuery.Select(x => x.TransportationIds).ToList().Combine();
            var bookingList = await db.BookingList.Where(x => x.Active && x.StartShip.Value.Date >= entity.FromDate.Value.Date && x.StartShip.Value.Date <= entity.ToDate.Value.Date).ToListAsync();
            foreach (var item in groupByPercentileQuery)
            {
                var checkBookingList = bookingList.Where(x =>
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
                && x.Active).FirstOrDefault();
                if (checkBookingList == null)
                {
                    bookingList.Add(item);
                    db.Add(item);
                    await db.SaveChangesAsync();
                    listAdd.Add(item.Id);
                }
                else
                {
                    if ((item.Count != checkBookingList.Count || item.ShipPrice != checkBookingList.ShipPrice || item.ShipPolicyPrice != checkBookingList.ShipPolicyPrice) && checkBookingList.Submit == false)
                    {
                        checkBookingList.Count = item.Count;
                        checkBookingList.ShipPrice = item.ShipPrice;
                        checkBookingList.ShipPolicyPrice = item.ShipPolicyPrice;
                    }
                    listAdd.Add(checkBookingList.Id);
                }
                var trans = rs.Where(x => item.TransportationIds.Contains(x.Id.ToString())).ToList();
                trans.ForEach(x => x.BookingListId = checkBookingList is null ? item.Id : checkBookingList.Id);
            }
            if (listAdd.Count > 0)
            {
                var bookingListSuperfluous = await db.BookingList.Where(x => x.Submit == false && x.StartShip.Value.Date >= entity.FromDate.Value.Date && x.StartShip.Value.Date <= entity.ToDate.Value.Date && !listAdd.Any(y => y == x.Id) && x.Active).ToListAsync();
                if (bookingListSuperfluous.Count > 0)
                {
                    bookingListSuperfluous.ForEach(x => { x.Active = false; });
                }
            }
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.SaveChangesAsync();
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
            return true;
        }
    }
}
