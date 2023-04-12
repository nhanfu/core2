using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class RevenueController : TMSController<Revenue>
    {
        public RevenueController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<Revenue>> PatchAsync([FromQuery] ODataQueryOptions<Revenue> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            var idInt = id.TryParseInt() ?? 0;
            var entity = await db.Revenue.FindAsync(idInt);
            var tran = await db.Transportation.Where(x => x.Id == entity.TransportationId).FirstOrDefaultAsync();
            if (tran != null && tran.IsLocked)
            {
                throw new ApiException("DSVC này đã được khóa. Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.InvoinceNo)
                || x.Field == nameof(entity.InvoinceDate)
                || x.Field == nameof(entity.Vat)
                || x.Field == nameof(entity.TotalPriceBeforTax)
                || x.Field == nameof(entity.VatPrice)
                || x.Field == nameof(entity.TotalPrice)
                || x.Field == nameof(entity.VendorVatId)))
            {
                if (tran != null && tran.IsLockedRevenue)
                {
                    throw new ApiException("Cont này đã được khóa doanh thu.") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            var oldEntity = await db.Revenue.Where(x => x.Id == entity.Id).FirstOrDefaultAsync();
            if (patch.Changes.Any(x => x.Field == nameof(entity.Name)
                || x.Field == nameof(entity.LotNo)
                || x.Field == nameof(entity.LotDate)
                || x.Field == nameof(entity.UnitPriceAfterTax)
                || x.Field == nameof(entity.UnitPriceBeforeTax)
                || x.Field == nameof(entity.ReceivedPrice)
                || x.Field == nameof(entity.CollectOnBehaftPrice)
                || x.Field == nameof(entity.NotePayment)
                || x.Field == nameof(entity.Note)
                || x.Field == nameof(entity.RevenueAdjustment)))
            {
                if (tran != null && tran.IsSubmit)
                {
                    throw new ApiException("DT này đã được khóa kế toán. Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.InvoinceNo)
                || x.Field == nameof(entity.InvoinceDate)
                || x.Field == nameof(entity.Vat)
                || x.Field == nameof(entity.TotalPriceBeforTax)
                || x.Field == nameof(entity.VatPrice)
                || x.Field == nameof(entity.TotalPrice)))
            {
                if (RoleIds.Where(x => x == 46 || x == 8).Any() == false)
                {
                    if (patch.Changes.Any(x => x.Field == nameof(entity.Vat)
                    || x.Field == nameof(entity.VatPrice)
                    || x.Field == nameof(entity.TotalPriceBeforTax)
                    || x.Field == nameof(entity.TotalPrice)) && RoleIds.Where(x => x == 34 || x == 8).Any())
                    {
                        if (entity.UserUpdate2 != null && entity.UserUpdate2 != 0)
                        {
                            patch.Changes = patch.Changes.Where(x => x.Field != nameof(entity.Vat)
                            && x.Field != nameof(entity.VatPrice)
                            && x.Field != nameof(entity.TotalPriceBeforTax)
                            && x.Field != nameof(entity.TotalPrice)).ToList();
                        }
                    }
                    if (patch.Changes.Any(x => x.Field == nameof(entity.InvoinceNo)
                    || x.Field == nameof(entity.InvoinceDate)) && RoleIds.Where(x => x == 34 || x == 8).Any())
                    {
                        patch.Changes = patch.Changes.Where(x => x.Field != nameof(entity.InvoinceNo)
                            && x.Field != nameof(entity.InvoinceDate)).ToList();
                    }
                }
                else
                {
                    if (entity.UserUpdate2 != null && entity.UserUpdate2 != 0 && entity.UserUpdate2 != UserId)
                    {
                        throw new ApiException("Bạn không có quyền chỉnh sửa dữ liệu của user khác.") { StatusCode = HttpStatusCode.BadRequest };
                    }
                    else
                    {
                        entity.UserUpdate2 = UserId;
                    }
                }
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.Name)
                || x.Field == nameof(entity.LotNo)
                || x.Field == nameof(entity.LotDate)
                || x.Field == nameof(entity.UnitPriceBeforeTax)
                || x.Field == nameof(entity.UnitPriceAfterTax)
                || x.Field == nameof(entity.ReceivedPrice)
                || x.Field == nameof(entity.CollectOnBehaftPrice)
                || x.Field == nameof(entity.NotePayment)
                || x.Field == nameof(entity.RevenueAdjustment)
                || x.Field == nameof(entity.Note)))
            {
                if (RoleIds.Where(x => x == 34 || x == 8).Any() == false)
                {
                    throw new ApiException("Bạn không có quyền chỉnh sửa dữ liệu của cột này.") { StatusCode = HttpStatusCode.BadRequest };
                }
                else
                {
                    if (entity.UserUpdate1 != null && entity.UserUpdate1 != 0 && entity.UserUpdate1 != UserId)
                    {
                        throw new ApiException("Bạn không có quyền chỉnh sửa dữ liệu của user khác.") { StatusCode = HttpStatusCode.BadRequest };
                    }
                    else
                    {
                        entity.UserUpdate1 = UserId;
                    }
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
                        patch.Changes.Add(new PatchUpdateDetail() { Field = nameof(Revenue.UserUpdate1), Value = entity.UserUpdate1.ToString() });
                        patch.Changes.Add(new PatchUpdateDetail() { Field = nameof(Revenue.UserUpdate2), Value = entity.UserUpdate2.ToString() });
                        var updates = patch.Changes.Where(x => x.Field != IdField).ToList();
                        var update = updates.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
                        if (disableTrigger)
                        {
                            command.CommandText += $" DISABLE TRIGGER ALL ON [{nameof(Revenue)}];";
                        }
                        else
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(Revenue)}];";
                        }
                        command.CommandText += $" UPDATE [{nameof(Revenue)}] SET {update.Combine()} WHERE Id = {idInt};";
                        //
                        if (disableTrigger)
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(Revenue)}];";
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

        public async override  Task<ActionResult<Revenue>> CreateAsync([FromBody] Revenue entity)
        {
            if (entity.TransportationId is null)
            {
                throw new ApiException("Vui lòng chọn cont cần nhập") { StatusCode = HttpStatusCode.BadRequest };
            }
            if ((entity.InvoinceNo != null ||
                entity.InvoinceDate != null ||
                (entity.Vat != null && entity.Vat != 0) ||
                (entity.TotalPriceBeforTax != null && entity.TotalPriceBeforTax != 0) ||
                (entity.VatPrice != null && entity.VatPrice != 0) ||
                (entity.TotalPrice != null && entity.TotalPrice != 0)) && RoleIds.Where(x => x == 46 || x == 8).Any() == false)
            {
                throw new ApiException("Bạn không có quyền nhập dữ liệu của cột này.") { StatusCode = HttpStatusCode.BadRequest };
            }
            if ((entity.Name != null ||
                entity.LotNo != null ||
                entity.LotDate != null ||
                (entity.UnitPriceBeforeTax != null && entity.UnitPriceBeforeTax != 0) ||
                (entity.UnitPriceAfterTax != null && entity.UnitPriceAfterTax != 0) ||
                (entity.ReceivedPrice != null && entity.ReceivedPrice != 0) ||
                (entity.CollectOnBehaftPrice != null && entity.CollectOnBehaftPrice != 0) ||
                (entity.RevenueAdjustment != null && entity.RevenueAdjustment != 0) ||
                entity.NotePayment != null ||
                entity.Note != null) && RoleIds.Where(x => x == 34 || x == 8).Any() == false)
            {
                throw new ApiException("Bạn không có quyền nhập dữ liệu của cột này.") { StatusCode = HttpStatusCode.BadRequest };
            }
            var tran = await db.Transportation.Where(x => x.Id == entity.TransportationId).FirstOrDefaultAsync();
            if (tran != null && tran.IsLockedRevenue &&
                (entity.InvoinceNo != null ||
                entity.InvoinceDate != null ||
                (entity.Vat != null && entity.Vat != 0) ||
                (entity.TotalPriceBeforTax != null && entity.TotalPriceBeforTax != 0) ||
                (entity.VatPrice != null && entity.VatPrice != 0) ||
                (entity.TotalPrice != null && entity.TotalPrice != 0) ||
                entity.VendorVatId != null))
            {
                throw new ApiException("Cont này đã được khóa doanh thu.") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await base.CreateAsync(entity);
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
                var revenues = await db.Revenue.Where(x => ids.Contains(x.Id)).ToListAsync();
                var tranIds = revenues.Select(x => x.TransportationId).ToList();
                var trans = await db.Transportation.Where(x => tranIds.Contains(x.Id)).ToListAsync();
                foreach (var id in ids)
                {
                    var revenue = await db.Revenue.Where(x => x.Id == id).FirstOrDefaultAsync();
                    db.Revenue.Remove(revenue);
                    await db.SaveChangesAsync();
                }
                var revenueDB = await db.Revenue.Where(x => tranIds.Contains(x.TransportationId)).ToListAsync();
                foreach (var item in trans)
                {
                    var countRevenue = revenueDB.Where(x => x.TransportationId == item.Id).ToList();
                    if (countRevenue.Count <= 0)
                    {
                        item.UnitPriceBeforeTax = 0;
                        item.UnitPriceAfterTax = 0;
                        item.ReceivedPrice = 0;
                        item.CollectOnBehaftPrice = 0;
                        item.TotalPriceBeforTax = 0;
                        item.Vat = 0;
                        item.VatPrice = 0;
                        item.TotalPrice = 0;
                        item.RevenueAdjustment = 0;
                        item.Name = null;
                        item.LotNo = null;
                        item.NoteLotDate = null;
                        item.InvoinceNo = null;
                        item.NoteInvoinceDate = null;
                        item.NoteVat = null;
                        item.NotePayment = null;
                        item.NoteVendorVatId = null;
                        item.Note = null;
                        item.LotDate = null;
                        item.InvoinceDate = null;
                    }
                }
                await db.SaveChangesAsync();
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
                cmd += $"{nameof(Revenue.LotNo)} = " + (item.LotNo != null ? $"'{item.LotNo}'" : "NULL") + ",";
            }
            if (item.IsLotDate)
            {
                cmd += $"{nameof(Revenue.LotDate)} = " + (item.LotDate != null ? $"'{item.LotDate.Value.ToString("yyyy-MM-dd")}'" : "NULL") + ",";
            }
            if (item.IsInvoinceNo)
            {
                cmd += $"{nameof(Revenue.InvoinceNo)} = " + (item.InvoinceNo != null ? $"'{item.InvoinceNo}'" : "NULL") + ",";
            }
            if (item.IsInvoinceDate)
            {
                cmd += $"{nameof(Revenue.InvoinceDate)} = " + (item.InvoinceDate != null ? $"'{item.InvoinceDate.Value.ToString("yyyy-MM-dd")}'" : "NULL") + ",";
            }
            if (item.IsUnitPriceBeforeTax)
            {
                cmd += $"{nameof(Revenue.UnitPriceBeforeTax)} = " + (item.UnitPriceBeforeTax != null ? $"'{item.UnitPriceBeforeTax}'" : "NULL") + ",";
            }
            if (item.IsUnitPriceAfterTax)
            {
                cmd += $"{nameof(Revenue.UnitPriceAfterTax)} = " + (item.UnitPriceAfterTax != null ? $"'{item.UnitPriceAfterTax}'" : "NULL") + ",";
            }
            if (item.IsReceivedPrice)
            {
                cmd += $"{nameof(Revenue.ReceivedPrice)} = " + (item.ReceivedPrice != null ? $"'{item.ReceivedPrice}'" : "NULL") + ",";
            }
            if (item.IsCollectOnBehaftPrice)
            {
                cmd += $"{nameof(Revenue.CollectOnBehaftPrice)} = " + (item.CollectOnBehaftPrice != null ? $"'{item.CollectOnBehaftPrice}'" : "NULL") + ",";
            }
            if (item.IsVat)
            {
                cmd += $"{nameof(Revenue.Vat)} = " + (item.Vat != null ? $"'{item.Vat}'" : "NULL") + ",";
            }
            if (item.IsTotalPriceBeforTax)
            {
                cmd += $"{nameof(Revenue.TotalPriceBeforTax)} = " + (item.TotalPriceBeforTax != null ? $"'{item.TotalPriceBeforTax}'" : "NULL") + ",";
            }
            if (item.IsVatPrice)
            {
                cmd += $"{nameof(Revenue.VatPrice)} = " + (item.VatPrice != null ? $"'{item.VatPrice}'" : "NULL") + ",";
            }
            if (item.IsTotalPrice)
            {
                cmd += $"{nameof(Revenue.TotalPrice)} = " + (item.TotalPrice != null ? $"'{item.TotalPrice}'" : "NULL") + ",";
            }
            if (item.IsNotePayment)
            {
                cmd += $"{nameof(Revenue.NotePayment)} = " + (item.NotePayment != null ? $"N'{item.NotePayment}'" : "NULL") + ",";
            }
            if (item.IsVendorVatId)
            {
                cmd += $"{nameof(Revenue.VendorVatId)} = " + (item.VendorVatId != null ? $"'{item.VendorVatId}'" : "NULL") + ",";
            }
            if (item.IsLotNo ||
                item.IsLotDate ||
                item.IsUnitPriceAfterTax ||
                item.IsUnitPriceBeforeTax ||
                item.IsReceivedPrice ||
                item.IsCollectOnBehaftPrice ||
                item.IsNotePayment)
            {
                if (RoleIds.Where(x => x == 34 || x == 8).Any())
                {
                    cmd += $"{nameof(Revenue.UserUpdate1)} = {UserId},";
                }
            }
            if (item.IsVat ||
                item.IsVatPrice ||
                item.IsTotalPriceBeforTax ||
                item.IsTotalPrice ||
                item.IsInvoinceNo ||
                item.IsInvoinceDate)
            {
                if (RoleIds.Where(x => x == 46 || x == 8).Any())
                {
                    cmd += $"{nameof(Revenue.UserUpdate2)} = {UserId},";
                }
            }
            cmd = cmd.TrimEnd(',');
            revenues.Remove(item);
            var ids = revenues.Select(x => x.Id).ToList();
            cmd += $" where Id in ({ids.Combine()})";
            await db.Database.ExecuteSqlRawAsync(cmd);
            return true;
        }

        [HttpPost("api/Revenue/CreateRevenueSimultaneous")]
        public async Task<bool> CreateRevenueSimultaneous([FromBody] List<Revenue> revenues)
        {
            if (revenues == null)
            {
                return false;
            }
            await db.AddRangeAsync(revenues);
            await db.SaveChangesAsync();
            return true;
        }

        [HttpPost("api/Revenue/CreateRevenues")]
        public async Task<bool> CreateRevenues([FromBody] List<Transportation> transportations)
        {
            var revenues = await db.Revenue.Where(x => x.Active == true && x.TransportationId != null).ToListAsync();
            var ids = revenues.Select(x => x.TransportationId).Distinct().ToList();
            var trans = transportations.Where(x => x.Active == true && ids.All(y => y != x.Id)).ToList();
            if (trans.Count <= 0)
            {
                return false;
            }
            var cmd = "";
            foreach(var item in trans)
            {
                var boss = item.BossId == null ? "NULL" : item.BossId.ToString();
                var containerNo = item.ContainerNo == null ? "NULL" : $"'{item.ContainerNo}'";
                var sealNo = item.SealNo == null ? "NULL" : $"'{item.SealNo}'";
                var containerType = item.ContainerTypeId == null ? "NULL" : item.ContainerTypeId.ToString();
                var closingDate = item.ClosingDate == null ? "NULL" : $"'{item.ClosingDate.Value.ToString("yyyy-MM-dd")}'";
                cmd += $"INSERT INTO Revenue(TransportationId, Active, InsertedDate, InsertedBy, BossId, ContainerNo, SealNo, ContainerTypeId, ClosingDate) VALUES ({item.Id}, 1, '{DateTime.Now.Date.ToString("yyyy-MM-dd")}', 1, {boss}, {containerNo}, {sealNo}, {containerType}, {closingDate}) ";
            }
            await db.Database.ExecuteSqlRawAsync(cmd);
            return true;
        }
    }
}
