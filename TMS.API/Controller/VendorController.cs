using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;
using NuGet.Versioning;
using OfficeOpenXml;
using System.Linq;
using TMS.API.Models;
using TMS.API.Services;
using TMS.API.ViewModels;
using Windows.Storage;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class VendorController : TMSController<Vendor>
    {
        private readonly VendorSvc _vendorSvc;

        public VendorController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor, VendorSvc vendorSvc) : base(context, entityService, httpContextAccessor)
        {
            _vendorSvc = vendorSvc;
        }

        protected override IQueryable<Vendor> GetQuery()
        {
            var rs = base.GetQuery();
            //Sale
            if (RoleIds.Contains(10))
            {
                rs =
                from vendor in db.Vendor
                from policy in db.FeaturePolicy
                    .Where(x => x.RecordId == vendor.Id && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                    .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                    .DefaultIfEmpty()
                where vendor.InsertedBy == _userSvc.UserId
                    || policy != null || vendor.Id == _userSvc.VendorId || vendor.UserId == _userSvc.UserId
                select vendor;
            }
            if (RoleIds.Contains(43) || RoleIds.Contains(17))
            {
                rs =
                    from vendor in db.Vendor
                    from policy in db.FeaturePolicy
                        .Where(x => x.RecordId == vendor.Id && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                        .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                        .DefaultIfEmpty()
                    where vendor.InsertedBy == _userSvc.UserId
                        || policy != null || vendor.Id == _userSvc.VendorId || vendor.UserId == _userSvc.UserId || vendor.UserId == 78
                    select vendor;
            }
            return rs;
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
                var deleteCommand = $"delete {nameof(VendorService)} where {nameof(VendorService.VendorId)} in ({string.Join(",", ids)});" +
                    $" delete {nameof(VendorContact)} where {nameof(VendorContact.BossId)} in ({string.Join(",", ids)});" +
                    $" delete {nameof(VendorLocation)} where {nameof(VendorLocation.VendorId)} in ({string.Join(",", ids)});" +
                    $" delete from [{typeof(Vendor).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteCommand);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet("api/[Controller]/GetHasValue")]
        public Task<OdataResult<Vendor>> UserClick(ODataQueryOptions<Vendor> options)
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(Vendor).Name}]
                    where Id in (select distinct BossId from [{typeof(TransportationPlan).Name}])";
            var data = db.Vendor.FromSqlRaw(sql);
            return ApplyQuery(options, data, sql: sql);
        }

        public override async Task<ActionResult<Vendor>> PatchAsync([FromQuery] ODataQueryOptions<Vendor> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            Vendor entity = default;
            Vendor oldEntity = default;
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            if (id != null && id.TryParseInt() > 0)
            {
                var idInt = id.TryParseInt() ?? 0;
                entity = await db.Set<Vendor>().FindAsync(idInt);
                oldEntity = await db.Vendor.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
            }
            else
            {
                entity = await GetEntityByOdataOptions(options);
                oldEntity = await GetEntityByOdataOptions(options);
            }
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.Name)))
            {
                if (oldEntity.Name != null && oldEntity.Name != "")
                {
                    var name = patch.Changes.FirstOrDefault(x => x.Field == nameof(oldEntity.Name));
                    var vendorDB = await db.Vendor.Where(x => x.Active && x.TypeId == entity.TypeId && x.NameSys.ToLower().Contains(entity.Name) && (x.Id != id.TryParseInt() || (int)entity.GetPropValue(IdField) <= 0)).FirstOrDefaultAsync();
                    if (vendorDB != null)
                    {
                        throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                    }
                }
            }
            patch.ApplyTo(entity);
            SetAuditInfo(entity);
            if (patch.Changes.Any(x => x.Field == nameof(entity.CompanyName)) && entity.TypeId == 23741)
            {
                var checkExist = db.Vendor.Where(x => x.CompanyName.Trim().ToLower() == entity.CompanyName.Trim().ToLower() && x.TypeId == 23741).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            if ((int)entity.GetPropValue(IdField) <= 0)
            {
                db.Add(entity);
            }
            await db.SaveChangesAsync();
            await db.Entry(entity).ReloadAsync();
            return entity;
        }

        public override async Task<ActionResult<Vendor>> CreateAsync([FromBody] Vendor entity)
        {
            if (entity.Name != null && entity.Name != "")
            {
                var vendorDB = await db.Vendor.Where(x => x.NameSys.ToLower() == entity.Name.ToLower() && x.TypeId == entity.TypeId).FirstOrDefaultAsync();
                if (vendorDB != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            if (entity.TypeId == 23741 && entity.CompanyName != null && entity.CompanyName != "")
            {
                var checkExist = db.Vendor.Where(x => x.CompanyName.Trim().ToLower() == entity.CompanyName.Trim().ToLower() && x.TypeId == 23741).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            return await base.CreateAsync(entity);
        }

        public override Task<ActionResult<Vendor>> UpdateAsync([FromBody] Vendor entity, string reasonOfChange = "")
        {
            if (entity.TypeId == 23741)
            {
                var checkExist = db.Vendor.Where(x => x.CompanyName.Trim().ToLower() == entity.CompanyName.Trim().ToLower() && x.TypeId == 23741).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            return base.UpdateAsync(entity, reasonOfChange);
        }

        [HttpPost("api/Vendor/ImportObject")]
        public async Task<List<Vendor>> ImportObject([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
        {
            var formFile = fileImport.FirstOrDefault();
            if (formFile == null || formFile.Length <= 0)
            {
                return null;
            }
            if (!Path.GetExtension(formFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            var path = GetUploadPath(formFile.FileName, host.WebRootPath);
            EnsureDirectoryExist(path);
            path = IncreaseFileName(path);
            using var stream = FileIO.Create(path);
            await formFile.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var currentSheet = package.Workbook.Worksheets;
            var worksheet = currentSheet.First();
            var noOfCol = worksheet.Dimension.End.Column;
            var noOfRow = worksheet.Dimension.End.Row;
            var list = new List<ImportObjectVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 5].Value == null || worksheet.Cells[row, 5].Value?.ToString() == ""))
                {
                    continue;
                }
                var newObject = new ImportObjectVM()
                {
                    Groupid = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    YearCreated = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    GroupSaleId = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                    Code = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                    CompanyName = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                    TaxCode = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    Address = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                    PhoneNumber = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                    Email = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                    StaffName = worksheet.Cells[row, 10].Value?.ToString().Trim(),
                    PositionName = worksheet.Cells[row, 11].Value?.ToString().Trim(),
                    ClassifyName = worksheet.Cells[row, 12].Value?.ToString().Trim(),
                    BankNo = worksheet.Cells[row, 13].Value?.ToString().Trim(),
                    BankName = worksheet.Cells[row, 14].Value?.ToString().Trim(),
                    CityName = worksheet.Cells[row, 15].Value?.ToString().Trim(),
                    DepartmentName = worksheet.Cells[row, 16].Value?.ToString().Trim(),
                    BranchName = worksheet.Cells[row, 17].Value?.ToString().Trim(),
                    VendorId = worksheet.Cells[row, 18].Value?.ToString().Trim(),
                    Notes = worksheet.Cells[row, 19].Value?.ToString().Trim(),
                    UserName = worksheet.Cells[row, 20].Value?.ToString().Trim(),
                };
                list.Add(newObject);
            }
            var listUserCodes = list.Select(x => x.UserName).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName) && x.Active).ToDictionaryAsync(x => x.UserName.Trim().ToLower());
            var listGroupCodes = list.Select(x => x.Groupid).Where(x => x != null && x != "").Distinct().ToList();
            var groupDB = await db.MasterData.Where(x => listGroupCodes.Contains(x.Name) && x.ParentId == 26394 && x.Active).ToDictionaryAsync(x => x.Name.Trim().ToLower());
            var listGroupSaleCodes = list.Select(x => x.GroupSaleId).Where(x => x != null && x != "").Distinct().ToList();
            var saleDB = await db.Sale.Where(x => listGroupSaleCodes.Contains(x.Name) && x.Active).ToDictionaryAsync(x => x.Name.Trim().ToLower());
            var listObjectCodes = list.Select(x => x.CompanyName).Where(x => x != null && x != "").Distinct().ToList();
            var objectDB = await db.Vendor.Where(x => listObjectCodes.Contains(x.CompanyName) && x.TypeId == 23741 && x.Active).ToDictionaryAsync(x => x.TaxCode != null && x.TaxCode != "" && string.IsNullOrWhiteSpace(x.TaxCode) == false ? x.CompanyName.Trim().ToLower() + "-" + x.TaxCode.Trim().ToLower() : x.CompanyName.Trim().ToLower() + "-NULL");
            foreach (var item in list)
            {
                User user = null;
                if (item.UserName != null && item.UserName != "")
                {
                    user = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.UserName.ToLower());
                }
                MasterData group = null;
                if (item.Groupid != null && item.Groupid != "")
                {
                    group = groupDB.Count == 0 ? null : groupDB.GetValueOrDefault(item.Groupid.ToLower());
                }
                Sale groupSale = null;
                if (item.GroupSaleId != null && item.GroupSaleId != "")
                {
                    groupSale = saleDB.Count == 0 ? null : saleDB.GetValueOrDefault(item.GroupSaleId.ToLower());
                }
                if (groupSale == null)
                {
                    groupSale = new Sale()
                    {
                        Name = item.GroupSaleId,
                        GroupId = group?.Id,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now.Date,
                    };
                    saleDB.Add(groupSale.Name.Trim().ToLower(), groupSale);
                    db.Add(groupSale);
                    await db.SaveChangesAsync();
                }
                Vendor objectOrigin = null;
                if (item.CompanyName != null && item.CompanyName != "")
                {
                    objectOrigin = objectDB.Count == 0 ? null : objectDB.GetValueOrDefault(item.TaxCode != null && item.TaxCode != "" && string.IsNullOrWhiteSpace(item.TaxCode) == false ? item.CompanyName.Trim().ToLower() + "-" + item.TaxCode.Trim().ToLower() : item.CompanyName.Trim().ToLower() + "-NULL");
                }
                int? branch = null;
                if (item.BranchName == "DNG")
                {
                    branch = 15983;
                }
                else if (item.BranchName == "HPG")
                {
                    branch = 7633;
                }
                else
                {
                    branch = 7632;
                }
                if (objectOrigin == null)
                {
                    objectOrigin = new Vendor()
                    {
                        GroupId = group.Id,
                        YearCreated = item.YearCreated != null && item.YearCreated != "" ? int.Parse(item.YearCreated) : 23,
                        GroupSaleId = groupSale.Id,
                        CompanyName = item.CompanyName,
                        TaxCode = item.TaxCode,
                        Address = item.Address,
                        PhoneNumberReport = item.PhoneNumber,
                        Email = item.Email,
                        StaffName = item.StaffName,
                        PositionName = item.PositionName,
                        ClassifyName = item.ClassifyName,
                        BankNo = item.BankNo,
                        BankName = item.BankName,
                        CityName = item.CityName,
                        DepartmentId = null,
                        BranchId = branch,
                        ParentVendorId = null,
                        Notes = item.Notes,
                        TypeId = 23741,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now.Date,
                    };
                    db.Add(objectOrigin);
                    objectDB.Add(item.TaxCode != null && item.TaxCode != "" && string.IsNullOrWhiteSpace(item.TaxCode) == false ? item.CompanyName.Trim().ToLower() + "-" + item.TaxCode.Trim().ToLower() : item.CompanyName.Trim().ToLower() + "-NULL", objectOrigin);
                }
                else
                {
                    objectOrigin.GroupId = group.Id;
                    objectOrigin.YearCreated = item.YearCreated != null && item.YearCreated != "" ? int.Parse(item.YearCreated) : 23;
                    objectOrigin.GroupSaleId = groupSale.Id;
                    objectOrigin.CompanyName = item.CompanyName;
                    objectOrigin.TaxCode = item.TaxCode;
                    objectOrigin.Address = item.Address;
                    objectOrigin.PhoneNumberReport = item.PhoneNumber;
                    objectOrigin.Email = item.Email;
                    objectOrigin.StaffName = item.StaffName;
                    objectOrigin.PositionName = item.PositionName;
                    objectOrigin.ClassifyName = item.ClassifyName;
                    objectOrigin.BankNo = item.BankNo;
                    objectOrigin.BankName = item.BankName;
                    objectOrigin.CityName = item.CityName;
                    objectOrigin.DepartmentId = null;
                    objectOrigin.BranchId = branch;
                    objectOrigin.ParentVendorId = null;
                    objectOrigin.Notes = item.Notes;
                    objectOrigin.Active = true;
                    objectOrigin.InsertedBy = user.Id;
                    objectOrigin.InsertedDate = DateTime.Now.Date;
                }
            }
            await db.SaveChangesAsync();
            return null;
        }
    }
}