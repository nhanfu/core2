using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.Services;
using FileIO = System.IO.File;
using System.IO;
using TMS.API.ViewModels;
using Core.Exceptions;
using Core.Enums;
using Core.ViewModels;
using System.Text.RegularExpressions;
using Slugify;
using NuGet.Versioning;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace TMS.API.Controllers
{
    public class VendorController : TMSController<Vendor>
    {
        private readonly VendorSvc _vendorSvc;

        public VendorController(TMSContext context, IHttpContextAccessor httpContextAccessor, VendorSvc vendorSvc) : base(context, httpContextAccessor)
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
            if ((int)entity.GetPropValue(IdField) <= 0)
            {
                db.Add(entity);
            }
            await db.SaveChangesAsync();
            await db.Entry(entity).ReloadAsync();
            RealTimeUpdate(entity);
            return entity;
        }

        private void RealTimeUpdate(Vendor entity)
        {
            var thead = new Thread(async () =>
            {
                try
                {
                    await _taskService.SendMessageAllUser(new WebSocketResponse<Vendor>
                    {
                        EntityId = _entitySvc.GetEntity(typeof(Vendor).Name).Id,
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

        public override async Task<ActionResult<Vendor>> CreateAsync([FromBody] Vendor entity)
        {
            if (entity.Name != null && entity.Name != "")
            {
                var vendorDB = await db.Vendor.Where(x => x.NameSys.ToLower() == entity.Name.ToLower()).FirstOrDefaultAsync();
                if (vendorDB != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            return await base.CreateAsync(entity);
        }

        [HttpPost("api/Vendor/ImportLine")]
        public async Task<List<Vendor>> ImportLine([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var listVendor = new List<ImportVendorVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var name = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var vendor = new ImportVendorVM()
                {
                    Code = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    NameEn = ConvertTextEn(name),
                    Name = ConvertTextVn(name),
                    User = worksheet.Cells[row, 4].Value?.ToString().Trim()
                };
                listVendor.Add(vendor);
            }
            var listVendorNames = listVendor.Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var vendorDB = rsVendor.Where(x => listVendorNames.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var vendorServiceDB = await db.VendorService.Where(x => x.ServiceId == 7588).ToDictionaryAsync(x => x.VendorId);
            var listUserCodes = listVendor.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            foreach (var item in listVendor)
            {
                User user = null;
                if (item.User != null && item.User != "")
                {
                    user = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.User.ToLower());
                }
                if (user is null && item.User != null && item.User != "")
                {
                    user = new User()
                    {
                        FullName = item.User,
                        UserName = item.User,
                        VendorId = 65,
                        HasVerifiedEmail = true,
                        GenderId = 390,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    user.Salt = _userSvc.GenerateRandomToken();
                    var randomPassword = "123";
                    user.Password = _userSvc.GetHash(UserUtils.sHA256, randomPassword + user.Salt);
                    db.Add(user);
                    await db.SaveChangesAsync();
                    userDB.Add(user.UserName.ToLower(), user);
                }
                var vendor = vendorDB.Count == 0 ? null : vendorDB.GetValueOrDefault(item.NameEn);
                if (vendor is null)
                {
                    vendor = new Vendor()
                    {
                        Code = item.Code,
                        Name = item.Name,
                        UserId = 78,
                        TypeId = 7552, //Partner
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    vendor.VendorService.Add(new VendorService()
                    {
                        ServiceId = 7588, //Line
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    });
                    db.Add(vendor);
                    await db.SaveChangesAsync();
                    vendorDB.Add(ConvertTextEn(vendor.Name), vendor);
                }
                else
                {

                    var service = vendorServiceDB.Count == 0 ? null : vendorServiceDB.GetValueOrDefault(vendor.Id);
                    if (service is null)
                    {
                        vendor.VendorService.Add(new VendorService()
                        {
                            ServiceId = 7588, //Line
                            Active = true,
                            InsertedBy = user.Id,
                            InsertedDate = DateTime.Now
                        });
                    }
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Vendor/ImportVendorLocation")]
        public async Task<List<Vendor>> ImportVendorLocation([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var listVendorVM = new List<ImportVendorLocationVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 4].Value == null || worksheet.Cells[row, 4].Value?.ToString() == ""))
                {
                    continue;
                }
                var name = worksheet.Cells[row, 3].Value?.ToString().Trim();
                listVendorVM.Add(new ImportVendorLocationVM()
                {
                    Region = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    Code = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    Name = ConvertTextVn(name),
                    NameEn = ConvertTextEn(name),
                    User = worksheet.Cells[row, 4].Value?.ToString().Trim()
                });
            }
            var listVendorNames = listVendorVM.Where(x => x.Region != "1").Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var listMasterDataCodes = listVendorVM.Where(x => x.Region == "1").Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var vendorDB = rsVendor.Where(x => listVendorNames.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var rsMasterData = await db.MasterData.ToListAsync();
            var masterDataDB = rsMasterData.Where(x => listMasterDataCodes.Contains(ConvertTextEn(x.Description)) && x.ParentId == 7569).ToDictionary(x => ConvertTextEn(x.Description));
            var vendorServiceDB = await db.VendorService.Where(x => x.ServiceId == 7572).ToDictionaryAsync(x => x.VendorId);
            var vendorServiceDB2 = await db.VendorService.Where(x => x.ServiceId == 7573).ToDictionaryAsync(x => x.VendorId);
            var currentMasterData = new MasterData();
            var listUserCodes = listVendorVM.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            foreach (var item in listVendorVM)
            {
                User user = null;
                if (item.User != null && item.User != "")
                {
                    user = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.User.ToLower());
                }
                if (user is null && item.User != null && item.User != "")
                {
                    user = new User()
                    {
                        FullName = item.User,
                        UserName = item.User,
                        VendorId = 65,
                        HasVerifiedEmail = true,
                        GenderId = 390,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    user.Salt = _userSvc.GenerateRandomToken();
                    var randomPassword = "123";
                    user.Password = _userSvc.GetHash(UserUtils.sHA256, randomPassword + user.Salt);
                    db.Add(user);
                    await db.SaveChangesAsync();
                    userDB.Add(user.UserName.ToLower(), user);
                }
                if (item.Region == "1")
                {
                    var masterData = masterDataDB.Count == 0 ? null : masterDataDB.GetValueOrDefault(item.NameEn);
                    if (masterData is null)
                    {
                        masterData = new MasterData()
                        {
                            Name = ConvertTextVn(item.Name),
                            Description = ConvertTextVn(item.Name),
                            ParentId = 7569, //Region
                            Path = @"/7569/",
                            Code = item.Code,
                            Active = true,
                            InsertedBy = user.Id,
                            InsertedDate = DateTime.Now
                        };
                        db.Add(masterData);
                        await db.SaveChangesAsync();
                        masterDataDB.Add(ConvertTextEn(masterData.Description), masterData);
                    }
                    currentMasterData = masterData;
                }
                else
                {
                    Vendor vendor = null;
                    if (item.Code != null && item.Code != "")
                    {
                        vendor = vendorDB.Count == 0 ? null : vendorDB.GetValueOrDefault(item.NameEn);
                    }
                    if (vendor is null && item.Code != null && item.Code != "")
                    {
                        vendor = new Vendor()
                        {
                            Code = item.Code,
                            Name = item.Name,
                            UserId = 78,
                            RegionId = currentMasterData.Id,
                            TypeId = 7552,
                            Active = true,
                            InsertedBy = user.Id,
                            InsertedDate = DateTime.Now
                        };
                        vendor.VendorService.Add(new VendorService()
                        {
                            ServiceId = 7572, //Địa điểm đóng hàng
                            Active = true,
                            InsertedBy = user.Id,
                            InsertedDate = DateTime.Now
                        });
                        vendor.VendorService.Add(new VendorService()
                        {
                            ServiceId = 7573, //Địa điểm trả hàng
                            Active = true,
                            InsertedBy = user.Id,
                            InsertedDate = DateTime.Now
                        });
                        db.Add(vendor);
                        await db.SaveChangesAsync();
                        if (vendor.Code != null)
                        { vendorDB.Add(ConvertTextEn(vendor.Name), vendor); }
                    }
                    else
                    {
                        if (vendor is not null)
                        {
                            var service = vendorServiceDB.Count == 0 ? null : vendorServiceDB.GetValueOrDefault(vendor.Id);
                            if (service is null)
                            {
                                vendor.VendorService.Add(new VendorService()
                                {
                                    ServiceId = 7572, //Địa điểm đóng hàng
                                    Active = true,
                                    InsertedBy = user.Id,
                                    InsertedDate = DateTime.Now
                                });
                            }
                            var service2 = vendorServiceDB2.Count == 0 ? null : vendorServiceDB2.GetValueOrDefault(vendor.Id);
                            if (service2 is null)
                            {
                                vendor.VendorService.Add(new VendorService()
                                {
                                    ServiceId = 7573, //Địa điểm trả hàng
                                    Active = true,
                                    InsertedBy = user.Id,
                                    InsertedDate = DateTime.Now
                                });
                            }
                        }
                    }
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Vendor/ImportList")]
        public async Task<List<Vendor>> ImportList([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var listVendor = new List<ImportVendorVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var name = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var vendor = new ImportVendorVM()
                {
                    Code = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    Name = ConvertTextVn(name),
                    NameEn = ConvertTextEn(name),
                    User = worksheet.Cells[row, 4].Value?.ToString().Trim()
                };
                listVendor.Add(vendor);
            }
            var listVendorCodes = listVendor.Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var vendorDB = rsVendor.Where(x => listVendorCodes.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var vendorServiceDB = await db.VendorService.Where(x => x.ServiceId == 11033).ToDictionaryAsync(x => x.VendorId);
            var listUserCodes = listVendor.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            foreach (var item in listVendor)
            {
                User user = null;
                if (item.User != null && item.User != "")
                {
                    user = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.User.ToLower());
                }
                if (user is null && item.User != null && item.User != "")
                {
                    user = new User()
                    {
                        FullName = item.User,
                        UserName = item.User,
                        VendorId = 65,
                        HasVerifiedEmail = true,
                        GenderId = 390,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    user.Salt = _userSvc.GenerateRandomToken();
                    var randomPassword = "123";
                    user.Password = _userSvc.GetHash(UserUtils.sHA256, randomPassword + user.Salt);
                    db.Add(user);
                    await db.SaveChangesAsync();
                    userDB.Add(user.UserName.ToLower(), user);
                }
                var vendor = vendorDB.Count == 0 ? null : vendorDB.GetValueOrDefault(item.NameEn);
                if (vendor is null)
                {
                    vendor = new Vendor()
                    {
                        Code = item.Code,
                        Name = item.Name,
                        UserId = 78,
                        TypeId = 7552, //Partner
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    vendor.VendorService.Add(new VendorService()
                    {
                        ServiceId = 11033, //List xuất
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    });
                    db.Add(vendor);
                    await db.SaveChangesAsync();
                    vendorDB.Add(ConvertTextEn(vendor.Name), vendor);
                }
                else
                {
                    var service = vendorServiceDB.Count == 0 ? null : vendorServiceDB.GetValueOrDefault(vendor.Id);
                    if (service is null)
                    {
                        vendor.VendorService.Add(new VendorService()
                        {
                            ServiceId = 11033, //List xuất
                            Active = true,
                            InsertedBy = user.Id,
                            InsertedDate = DateTime.Now
                        });
                    }
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Vendor/CompareVendor")]
        public async Task<bool> CompareVendor([FromBody] Vendor entity)
        {
            if (entity.ParentVendorId is null)
            {
                return false;
            }
            else
            {
                var sql = $"update [{nameof(TransportationPlan)}] set {nameof(TransportationPlan.BossId)} = {entity.ParentVendorId} where {nameof(TransportationPlan.BossId)} = {entity.Id} ";
                sql += $"update [{nameof(Transportation)}] set {nameof(Transportation.BossId)} = {entity.ParentVendorId} where {nameof(Transportation.BossId)} = {entity.Id} ";
                sql += $"update [{nameof(TransportationContract)}] set {nameof(TransportationContract.BossId)} = {entity.ParentVendorId} where {nameof(TransportationContract.BossId)} = {entity.Id} ";
                sql += $"update [{nameof(Quotation)}] set {nameof(TransportationPlan.BossId)} = {entity.ParentVendorId} where {nameof(Quotation.BossId)} = {entity.Id} ";
                sql += $"update [{nameof(TransportationPlan)}] set {nameof(TransportationPlan.BossId)} = {entity.ParentVendorId} where {nameof(TransportationPlan.BossId)} = {entity.Id} ";
                sql += $"update [{nameof(Expense)}] set {nameof(Expense.BossId)} = {entity.ParentVendorId} where {nameof(Expense.BossId)} = {entity.Id} ";
                await ExeNonQuery(_serviceProvider, _config, sql, "Default");
                return true;
            }
        }

        [HttpPost("api/Vendor/ImportVendor")]
        public async Task<List<Vendor>> ImportVendor([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var listVendorVM = new List<ImportVendorVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 6].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 4].Value == null || worksheet.Cells[row, 6].Value?.ToString() == ""))
                {
                    continue;
                }
                var name = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var commodity = worksheet.Cells[row, 3].Value?.ToString().Trim();
                listVendorVM.Add(new ImportVendorVM()
                {
                    Name = ConvertTextVn(name),
                    NameEn = ConvertTextEn(name),
                    CommodityText = ConvertTextVn(commodity),
                    CommodityTextEn = ConvertTextEn(commodity),
                    UserName = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                    Code = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                    CompanyName = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    TaxCode = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                    Address = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                    User = worksheet.Cells[row, 10].Value?.ToString().Trim()
                });
            }
            var listVendorCodes = listVendorVM.Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var listCommodityCodes = listVendorVM.Select(x => x.CommodityTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listSaleCodes = listVendorVM.Select(x => x.UserName).Where(x => x != null && x != "").Distinct().ToList();
            var listUserCodes = listVendorVM.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var vendorDB = rsVendor.Where(x => listVendorCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7551).ToDictionary(x => ConvertTextEn(x.Name));
            var rsMasterData = await db.MasterData.ToListAsync();
            var commodityDB = rsMasterData.Where(x => listCommodityCodes.Contains(ConvertTextEn(x.Description)) && x.Path.Contains(@"\7651\") && x.ParentId != 7651).ToDictionary(x => ConvertTextEn(x.Description));
            var userDB = await db.User.Where(x => listSaleCodes.Contains(x.UserName) || listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            foreach (var item in listVendorVM)
            {
                User user = null;
                if (item.User != null && item.User != "")
                {
                    user = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.User.ToLower());
                }
                if (user is null && item.User != null && item.User != "")
                {
                    user = new User()
                    {
                        FullName = item.User,
                        UserName = item.User,
                        VendorId = 65,
                        HasVerifiedEmail = true,
                        GenderId = 390,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    user.Salt = _userSvc.GenerateRandomToken();
                    var randomPassword = "123";
                    user.Password = _userSvc.GetHash(UserUtils.sHA256, randomPassword + user.Salt);
                    db.Add(user);
                    await db.SaveChangesAsync();
                    userDB.Add(user.UserName.ToLower(), user);
                }
                User sale = null;
                if (item.UserName != null && item.UserName != "")
                {
                    sale = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.UserName.ToLower());
                }
                if (sale is null && item.UserName != null && item.UserName != "")
                {
                    sale = new User()
                    {
                        FullName = item.UserName,
                        UserName = item.UserName,
                        VendorId = 65,
                        HasVerifiedEmail = true,
                        GenderId = 390,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    sale.Salt = _userSvc.GenerateRandomToken();
                    var randomPassword = "123";
                    sale.Password = _userSvc.GetHash(UserUtils.sHA256, randomPassword + sale.Salt);
                    db.Add(sale);
                    await db.SaveChangesAsync();
                    userDB.Add(sale.UserName.ToLower(), sale);
                }
                var vendor = vendorDB.Count == 0 ? null : vendorDB.GetValueOrDefault(item.NameEn);
                MasterData commodity = null;
                if (item.CommodityText != null && item.CommodityText != "")
                {
                    commodity = commodityDB.Count == 0 ? null : commodityDB.GetValueOrDefault(item.CommodityTextEn);
                }
                if (commodity is null && item.CommodityText != null && item.CommodityText != "")
                {
                    commodity = new MasterData()
                    {
                        Name = item.CommodityText,
                        Description = item.CommodityText,
                        ParentId = 15004,
                        Path = @"\7651\15004\",
                        Level = 2,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(commodity);
                    await db.SaveChangesAsync();
                    commodityDB.Add(ConvertTextEn(commodity.Description), commodity);
                }
                if (vendor is null)
                {
                    vendor = new Vendor()
                    {
                        Name = item.Name,
                        Code = item.Code,
                        CompanyName = item.CompanyName,
                        TaxCode = item.TaxCode,
                        Address = item.Address,
                        TypeId = 7551,
                        UserId = sale.Id,
                        ReturnRate = 0,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(vendor);
                    vendorDB.Add(ConvertTextEn(vendor.Name), vendor);
                }
                if (item.CommodityText != null && item.CommodityText != "")
                {
                    vendor.CommodityId = commodity.Id;
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Vendor/ImportGetOrder")]
        public async Task<List<Vendor>> ImportGetOrder([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var listVendor = new List<ImportVendorVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == ""))
                {
                    continue;
                }
                var name = worksheet.Cells[row, 1].Value?.ToString().Trim();
                var vendor = new ImportVendorVM()
                {
                    UserName = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    NameEn = ConvertTextEn(name),
                    Name = ConvertTextVn(name),
                    PhoneNumber = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                    TaxCode = worksheet.Cells[row, 4].Value?.ToString().Trim()
                };
                listVendor.Add(vendor);
            }
            var listVendorNames = listVendor.Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var vendorDB = rsVendor.Where(x => listVendorNames.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var vendorServiceDB = await db.VendorService.Where(x => x.ServiceId == 11839).ToDictionaryAsync(x => x.VendorId);
            var listUserCodes = listVendor.Select(x => x.UserName).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            foreach (var item in listVendor)
            {
                User user = null;
                if (item.UserName != null && item.UserName != "")
                {
                    user = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.UserName.ToLower());
                }
                if (user is null && item.UserName != null && item.UserName != "")
                {
                    user = new User()
                    {
                        FullName = item.UserName,
                        UserName = item.UserName,
                        VendorId = 65,
                        HasVerifiedEmail = true,
                        GenderId = 390,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    user.Salt = _userSvc.GenerateRandomToken();
                    var randomPassword = "123";
                    user.Password = _userSvc.GetHash(UserUtils.sHA256, randomPassword + user.Salt);
                    db.Add(user);
                    await db.SaveChangesAsync();
                    userDB.Add(user.UserName.ToLower(), user);
                }
                var vendor = vendorDB.Count == 0 ? null : vendorDB.GetValueOrDefault(item.NameEn);
                if (vendor is null)
                {
                    vendor = new Vendor()
                    {
                        Name = item.Name,
                        UserId = user.Id,
                        PhoneNumber = item.PhoneNumber,
                        TaxCode = item.TaxCode,
                        TypeId = 7552, //Partner
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    vendor.VendorService.Add(new VendorService()
                    {
                        ServiceId = 11839, //ĐV nhận lệnh
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    });
                    db.Add(vendor);
                    await db.SaveChangesAsync();
                    vendorDB.Add(ConvertTextEn(vendor.Name), vendor);
                }
                else
                {
                    var service = vendorServiceDB.Count == 0 ? null : vendorServiceDB.GetValueOrDefault(vendor.Id);
                    if (service is null)
                    {
                        vendor.VendorService.Add(new VendorService()
                        {
                            ServiceId = 11839, //ĐV nhận lệnh
                            Active = true,
                            InsertedBy = 1,
                            InsertedDate = DateTime.Now
                        });
                    }
                }
            }
            await db.SaveChangesAsync();
            return null;
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
            var listVendor = new List<ImportObjectVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == ""))
                {
                    continue;
                }
                var vendor = new ImportObjectVM()
                {
                    Code = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    CompanyName = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                    TaxCode = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                    Address = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                    PhoneNumber = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    Email = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    StaffName = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                    PositionName = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                    ClassifyName = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                    BankNo = worksheet.Cells[row, 10].Value?.ToString().Trim(),
                    BankName = worksheet.Cells[row, 11].Value?.ToString().Trim(),
                    CityName = worksheet.Cells[row, 12].Value?.ToString().Trim(),
                    DepartmentText = worksheet.Cells[row, 13].Value?.ToString().Trim(),
                    BranchText = worksheet.Cells[row, 14].Value?.ToString().Trim(),
                    SaleText = worksheet.Cells[row, 15].Value?.ToString().Trim(),
                    Description = worksheet.Cells[row, 16].Value?.ToString().Trim()
                };
                listVendor.Add(vendor);
            }
            var listDepartment = listVendor.Select(x => x.DepartmentText).Where(x => x != null && x != "").Distinct().ToList();
            var listBranch = listVendor.Select(x => x.BranchText).Where(x => x != null && x != "").Distinct().ToList();
            var listSale = listVendor.Select(x => x.SaleText).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listSale.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            var masterDateDB = await db.MasterData.Where(x => listDepartment.Contains(x.Name) && x.ParentId == 24944).ToDictionaryAsync(x => x.Name.ToLower());
            var masterDateDB1 = await db.MasterData.Where(x => listBranch.Contains(x.Name) && x.ParentId == 7631).ToDictionaryAsync(x => x.Name.ToLower());
            foreach (var item in listVendor)
            {
                User user = null;
                if (item.SaleText != null && item.SaleText != "")
                {
                    user = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.SaleText.ToLower());
                }
                MasterData department = null;
                if (item.DepartmentText != null && item.DepartmentText != "")
                {
                    department = masterDateDB.Count == 0 ? null : masterDateDB.GetValueOrDefault(item.DepartmentText.ToLower());
                }
                MasterData branch = null;
                if (item.BranchText != null && item.BranchText != "")
                {
                    branch = masterDateDB1.Count == 0 ? null : masterDateDB1.GetValueOrDefault(item.BranchText.ToLower());
                }
                var vendor = new Vendor()
                {
                    Code = item.Code,
                    CompanyName = item.CompanyName,
                    TaxCode = item.TaxCode,
                    Address = item.Address,
                    PhoneNumber = item.PhoneNumber,
                    Email = item.Email,
                    StaffName = item.StaffName,
                    PositionName = item.PositionName,
                    ClassifyName = item.ClassifyName,
                    BankNo = item.BankNo,
                    BankName = item.BankName,
                    CityName = item.CityName,
                    DepartmentId = department is null ? null : department.Id,
                    BranchId = branch is null ? null : branch.Id,
                    SaleId = user is null ? 1 : user.Id,
                    Description = item.Description,
                    TypeId = 23741,
                    Active = true,
                    InsertedBy = 1,
                    InsertedDate = DateTime.Now
                };
                db.Add(vendor);
            }
            await db.SaveChangesAsync();
            return null;
        }

        public static string ConvertTextEn(string text)
        {
            return text is null || text == "" ? "" : Regex.Replace(text.ToLower().Trim(), @"\s+", " ");
        }

        public static string ConvertTextVn(string text)
        {
            return text is null || text == "" ? "" : Regex.Replace(text.Trim(), @"\s+", " ");
        }
    }
}