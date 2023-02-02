using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Slugify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class MasterDataController : TMSController<MasterData>
    {
        public MasterDataController(TMSContext context, IHttpContextAccessor httpContextAccessor)
            : base(context, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<MasterData>> PatchAsync([FromQuery] ODataQueryOptions<MasterData> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            MasterData entity = default;
            MasterData oldEntity = default;
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            if (id != null && id.TryParseInt() > 0)
            {
                var idInt = id.TryParseInt() ?? 0;
                entity = await db.Set<MasterData>().FindAsync(idInt);
                oldEntity = await db.MasterData.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
            }
            else
            {
                entity = await GetEntityByOdataOptions(options);
                oldEntity = await GetEntityByOdataOptions(options);
            }
            await CheckDuplicatesSettingsTrainSchedule(entity);
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.Description)))
            {
                var masterDataDB = await db.MasterData.Where(x => x.ParentId == entity.ParentId && x.Description.ToLower() == entity.Description.ToLower() && (x.Id != id.TryParseInt())).FirstOrDefaultAsync();
                if (masterDataDB != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
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
            await UpdateTreeNodeAsync(entity, null);
            return entity;
        }

        [AllowAnonymous]
        public override Task<OdataResult<MasterData>> Get(ODataQueryOptions<MasterData> options)
        {
            return ApplyQuery(options, db.MasterData);
        }

        public override async Task<ActionResult<MasterData>> UpdateAsync([FromBody] MasterData entity, string reasonOfChange = "")
        {
            var masterDataDB = await db.MasterData.Where(x => x.ParentId == entity.ParentId && x.Description.ToLower() == entity.Description.ToLower() && (x.Id != entity.Id)).FirstOrDefaultAsync();
            if (masterDataDB != null)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await UpdateTreeNodeAsync(entity, reasonOfChange);
        }

        public override async Task<ActionResult<MasterData>> CreateAsync([FromBody] MasterData entity)
        {
            await CheckDuplicatesSettingsTrainSchedule(entity);
            var masterDataDB = await db.MasterData.Where(x => x.ParentId == entity.ParentId && x.Description.ToLower() == entity.Description.ToLower()).FirstOrDefaultAsync();
            if (masterDataDB != null)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
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
            var ms = await db.MasterData.OrderByDescending(x=>x.Id).ToListAsync();
            foreach (var item in ms)
            {
                await UpdateTreeNodeAsync(item);
            }
            return Ok(true);
        }

        [HttpPost("api/MasterData/ImportExpenseType")]
        public async Task<List<MasterData>> ImportExpenseType([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportMasterDataVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "")
                {
                    continue;
                }
                var name = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var masterData = new ImportMasterDataVM()
                {
                    Name = ConvertTextVn(name),
                    NameEn = ConvertTextEn(name)
                };
                SetAuditInfo(masterData);
                list.Add(masterData);
            }
            var listExpenseCodes = list.Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var rs = await db.MasterData.ToListAsync();
            var expenseDB = rs.Where(x => listExpenseCodes.Contains(ConvertTextEn(x.Name)) && x.ParentId == 7577).ToDictionary(x => ConvertTextEn(x.Name));
            foreach (var item in list)
            {
                var expense = expenseDB.Count == 0 ? null : expenseDB.GetValueOrDefault(item.NameEn);
                if (expense is null)
                {
                    expense = new MasterData()
                    {
                        Name = item.Name,
                        Description = item.Name,
                        ParentId = 7577, //ExpenseType
                        Path = @"\7577\",
                        Level = 1
                    };
                    SetAuditInfo(expense);
                    db.Add(expense);
                    await db.SaveChangesAsync();
                    expenseDB.Add(ConvertTextEn(expense.Name), expense);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/MasterData/ImportExpensePrice")]
        public async Task<List<MasterData>> ImportExpensePrice([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportMasterDataVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var name = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var masterData = new ImportMasterDataVM()
                {
                    Name = ConvertTextVn(name),
                    NameEn = ConvertTextEn(name),
                    Enum = worksheet.Cells[row, 3].Value?.ToString().Trim()
                };
                SetAuditInfo(masterData);
                list.Add(masterData);
            }
            var listExpenseCodes = list.Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var rs = await db.MasterData.ToListAsync();
            var expenseDB = rs.Where(x => listExpenseCodes.Contains(ConvertTextEn(x.Name)) && x.ParentId == 7577).ToDictionary(x => ConvertTextEn(x.Name));
            foreach (var item in list)
            {
                var expense = expenseDB.Count == 0 ? null : expenseDB.GetValueOrDefault(item.NameEn);
                if (expense is null)
                {
                    expense = new MasterData()
                    {
                        Name = item.Name,
                        Description = item.Name,
                        Enum = int.Parse(item.Enum),
                        ParentId = 7577, //ExpenseType
                        Path = @"\7577\",
                        Level = 1
                    };
                    SetAuditInfo(expense);
                    db.Add(expense);
                    await db.SaveChangesAsync();
                    expenseDB.Add(ConvertTextEn(expense.Name), expense);
                }
                else
                {
                    expense.Enum = int.Parse(item.Enum);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/MasterData/ImportCommodityType")]
        public async Task<List<MasterData>> ImportCommodityType([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportCommodityVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var description = worksheet.Cells[row, 3].Value?.ToString().Trim();
                list.Add(new ImportCommodityVM()
                {
                    Type = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    Name = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    Description = ConvertTextVn(description),
                    DescriptionEn = ConvertTextEn(description),
                    User = worksheet.Cells[row, 11].Value?.ToString().Trim()
                });
            }
            var listCommodityTypeCodes = list.Where(x => x.Type == "1").Select(x => x.DescriptionEn).Where(x => x != null && x != "").Distinct().ToList();
            var listCommodityCodes = list.Where(x => x.Type != "1").Select(x => x.DescriptionEn).Where(x => x != null && x != "").Distinct().ToList();
            var rs = await db.MasterData.ToListAsync();
            var commodityTypeDB = rs.Where(x => listCommodityTypeCodes.Contains(ConvertTextEn(x.Description)) && x.ParentId == 7651).ToDictionary(x => ConvertTextEn(x.Description));
            var commodityDB = rs.Where(x => listCommodityCodes.Contains(ConvertTextEn(x.Description))).ToDictionary(x => ConvertTextEn(x.Description));
            var currcurrentParentId = 0;
            var listUserCodes = list.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            foreach (var item in list)
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
                if (item.Type == "1")
                {
                    var masterData = commodityTypeDB.Count == 0 ? null : commodityTypeDB.GetValueOrDefault(item.DescriptionEn);
                    if (masterData is null)
                    {
                        masterData = new MasterData()
                        {
                            Name = item.Name,
                            Description = item.Description,
                            ParentId = 7651, //CommodityType
                            Path = @"\7651\",
                            Level = 1,
                            Active = true,
                            InsertedDate = DateTime.Now
                        };
                        db.Add(masterData);
                        await db.SaveChangesAsync();
                        commodityTypeDB.Add(ConvertTextEn(masterData.Description), masterData);
                    }
                    if (user is not null)
                    {
                        masterData.InsertedBy = user.Id;
                    }
                    currcurrentParentId = masterData.Id;
                }
                else
                {
                    var masterData = commodityDB.Count == 0 ? null : commodityDB.GetValueOrDefault(item.DescriptionEn);
                    if (masterData is null)
                    {
                        masterData = new MasterData()
                        {
                            Name = item.Name,
                            Description = item.Description,
                            ParentId = currcurrentParentId, //Commodity
                            Path = @"\7651\" + currcurrentParentId + @"\",
                            Level = 2,
                            Active = true,
                            InsertedDate = DateTime.Now
                        };
                        db.Add(masterData);
                        commodityDB.Add(ConvertTextEn(masterData.Description), masterData);
                    }
                    if (user is not null)
                    {
                        masterData.InsertedBy = user.Id;
                    }
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/MasterData/ImportContainerType")]
        public async Task<List<MasterData>> ImportContainerType([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportMasterDataVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == ""))
                {
                    continue;
                }
                var description = worksheet.Cells[row, 1].Value?.ToString().Trim();
                var masterData = new ImportMasterDataVM()
                {
                    Name = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    Description = ConvertTextVn(description),
                    DescriptionEn = ConvertTextEn(description),
                    User = worksheet.Cells[row, 4].Value?.ToString().Trim()
                };
                list.Add(masterData);
            }
            var listContainerCodes = list.Select(x => x.DescriptionEn).Where(x => x != null && x != "").Distinct().ToList();
            var rs = await db.MasterData.ToListAsync();
            var containerDB = rs.Where(x => listContainerCodes.Contains(ConvertTextEn(x.Description))).ToDictionary(x => ConvertTextEn(x.Description));
            var listUserCodes = list.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            foreach (var item in list)
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
                var container = containerDB.Count == 0 ? null : containerDB.GetValueOrDefault(item.DescriptionEn);
                if (container is null)
                {
                    container = new MasterData()
                    {
                        Name = item.Name,
                        Description = item.Description,
                        ParentId = 7565, //ExpenseType
                        Path = @"\7565\",
                        Level = 1,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(container);
                    await db.SaveChangesAsync();
                    containerDB.Add(ConvertTextEn(container.Description), container);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/MasterData/ImportShipBrand")]
        public async Task<List<Vendor>> ImportShipBrand([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
                var description = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var vendor = new ImportVendorVM()
                {
                    Code = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    Name = ConvertTextVn(description),
                    NameEn = ConvertTextEn(description),
                    User = worksheet.Cells[row, 5].Value?.ToString().Trim()
                };
                listVendor.Add(vendor);
            }
            var listVendorName = listVendor.Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var rs = await db.Vendor.ToListAsync();
            var vendorDB = rs.Where(x => listVendorName.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var vendorServiceDB = await db.VendorService.Where(x => x.ServiceId == 7634).ToDictionaryAsync(x => x.VendorId);
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
                        TypeId = 7552,
                        Active = true,
                        InsertedDate = DateTime.Now
                    };
                    vendor.VendorService.Add(new VendorService()
                    {
                        ServiceId = 7634, //Hãng tàu
                        Active = true,
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
                            ServiceId = 7634, //Hãng tàu
                            Active = true,
                            InsertedDate = DateTime.Now
                        });
                    }
                }
                if (user is not null)
                {
                    vendor.InsertedBy = user.Id;
                    vendor.VendorService.Select(x => x.InsertedBy = user.Id);
                }
                else
                {
                    vendor.InsertedBy = 1;
                    vendor.VendorService.Select(x => x.InsertedBy = 1);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/MasterData/ImportQuotationType")]
        public async Task<List<MasterData>> ImportQuotationType([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportMasterDataVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if (worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "")
                {
                    continue;
                }
                var description = worksheet.Cells[row, 1].Value?.ToString().Trim();
                var masterData = new ImportMasterDataVM()
                {
                    Name = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    Description = ConvertTextVn(description),
                    DescriptionEn = ConvertTextEn(description),
                    User = worksheet.Cells[row, 2].Value?.ToString().Trim()
                };
                list.Add(masterData);
            }
            var listQuotationCodes = list.Select(x => x.DescriptionEn).Where(x => x != null && x != "").Distinct().ToList();
            var rs = await db.MasterData.ToListAsync();
            var quotationDB = rs.Where(x => listQuotationCodes.Contains(ConvertTextEn(x.Description)) && x.ParentId == 11466).ToDictionary(x => ConvertTextEn(x.Description));
            var listUserCodes = list.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            foreach (var item in list)
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
                var quotation = quotationDB.Count == 0 ? null : quotationDB.GetValueOrDefault(item.DescriptionEn);
                if (quotation is null)
                {
                    quotation = new MasterData()
                    {
                        Name = item.Name,
                        Description = item.Description,
                        ParentId = 11466, //Policy Type
                        Path = @"\11466\",
                        Level = 1,
                        Active = true,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(quotation);
                    await db.SaveChangesAsync();
                    quotationDB.Add(ConvertTextEn(quotation.Description), quotation);
                }
                if (user is not null)
                {
                    quotation.InsertedBy = user.Id;
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/MasterData/ImportExcelAccount")]
        public async Task<List<MasterData>> ImportExcelAccount([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportMasterDataVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if (worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "" &&
                    worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "")
                {
                    continue;
                }
                var masterData = new ImportMasterDataVM()
                {
                    Level = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    Code = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    Name = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                    Description = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                };
                list.Add(masterData);
            }
            var accountParent = 0;
            foreach (var item in list)
            {
                if (item.Code != "" && item.Name != "")
                {
                    if (item.Level == "1")
                    {
                        var account = new MasterData()
                        {
                            Code = item.Code,
                            Name = item.Name,
                            Description = item.Description,
                            ParentId = 23991,
                            Path = @"\23991\",
                            Level = 1,
                            Active = true,
                            InsertedBy = 197,
                            InsertedDate = DateTime.Now
                        };
                        db.Add(account);
                        await db.SaveChangesAsync();
                        accountParent = account.Id;
                    }
                    else
                    {
                        var account = new MasterData()
                        {
                            Code = item.Code,
                            Name = item.Name,
                            Description = item.Description,
                            ParentId = accountParent,
                            Path = @"\23991\" + accountParent + @"\",
                            Level = 2,
                            Active = true,
                            InsertedBy = 197,
                            InsertedDate = DateTime.Now
                        };
                        db.Add(account);
                    }
                }
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

        [HttpGet("api/[Controller]/UserClick")]
        public virtual async Task<OdataResult<MasterData>> UserClick(ODataQueryOptions<MasterData> options)
        {
            var sql = $@"select UserClick.Click,MasterData.Id,MasterData.Name,MasterData.Description,MasterData.ParentId,MasterData.Path,MasterData.Additional,MasterData.[Order],MasterData.Enum,MasterData.[Level],MasterData.Active,MasterData.InsertedBy,MasterData.InsertedDate,MasterData.UpdatedDate,MasterData.UpdatedBy,MasterData.InterDesc,MasterData.CostCenterId,MasterData.Code
                                                from [{typeof(MasterData).Name}]
                                                left join UserClick
                                                on [{typeof(MasterData).Name}].Id = UserClick.RecordId
                                                and UserClick.EntityId = 1067
                                                and UserClick.UserId = " + UserId + @" where 1=1 ";
            var data = db.MasterData.FromSqlRaw(sql);
            return await ApplyQuery(options, data);
        }
    }
}
