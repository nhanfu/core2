using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text.RegularExpressions;
using TMS.API.Models;
using TMS.API.ViewModels;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class LocationController : TMSController<Location>
    {
        public LocationController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }

        public override async Task<ActionResult<Location>> CreateAsync([FromBody] Location entity)
        {
            var check = await db.Location.FirstOrDefaultAsync(x => x.Description1 == entity.Description1 && x.Description2 == entity.Description2 && x.Description3 == entity.Description3);
            if (check != null)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            var listVendorContact = new List<VendorContact>();
            if (!entity.Description1.IsNullOrWhiteSpace())
            {
                var v1 = await db.Location.Include(x => x.VendorContact).FirstOrDefaultAsync(x => x.Name == entity.Description1);
                if (v1 != null)
                {
                    listVendorContact.AddRange(v1.VendorContact);
                }
            }
            if (!entity.Description2.IsNullOrWhiteSpace())
            {
                var v2 = await db.Location.Include(x => x.VendorContact).FirstOrDefaultAsync(x => x.Name == entity.Description2);
                if (v2 != null)
                {
                    listVendorContact.AddRange(v2.VendorContact);
                }
            }
            if (!entity.Description3.IsNullOrWhiteSpace())
            {
                var v3 = await db.Location.Include(x => x.VendorContact).FirstOrDefaultAsync(x => x.Name == entity.Description3);
                if (v3 != null)
                {
                    listVendorContact.AddRange(v3.VendorContact);
                }
            }
            listVendorContact.ForEach(x =>
            {
                x.Id = 0;
                x.LocationId = null;
            });
            entity.VendorContact = listVendorContact;
            return await base.CreateAsync(entity);
        }

        public override async Task<ActionResult<Location>> UpdateAsync([FromBody] Location entity, string reasonOfChange = "")
        {
            var check = await db.Location.FirstOrDefaultAsync(x => x.Description1 == entity.Description1 && x.Description2 == entity.Description2 && x.Description3 == entity.Description3 && x.Id != entity.Id);
            if (check != null)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            if (entity.VendorContact.Nothing())
            {
                var listVendorContact = new List<VendorContact>();
                if (!entity.Description1.IsNullOrWhiteSpace())
                {
                    var v1 = await db.Location.Include(x => x.VendorContact).FirstOrDefaultAsync(x => x.Name == entity.Description1);
                    if (v1 != null)
                    {
                        listVendorContact.AddRange(v1.VendorContact);
                    }
                }
                if (!entity.Description2.IsNullOrWhiteSpace())
                {
                    var v2 = await db.Location.Include(x => x.VendorContact).FirstOrDefaultAsync(x => x.Name == entity.Description2);
                    if (v2 != null)
                    {
                        listVendorContact.AddRange(v2.VendorContact);
                    }
                }
                if (!entity.Description3.IsNullOrWhiteSpace())
                {
                    var v3 = await db.Location.Include(x => x.VendorContact).FirstOrDefaultAsync(x => x.Name == entity.Description3);
                    if (v3 != null)
                    {
                        listVendorContact.AddRange(v3.VendorContact);
                    }
                }
                listVendorContact.ForEach(x =>
                {
                    x.Id = 0;
                    x.LocationId = null;
                });
                entity.VendorContact = listVendorContact;
            }
            return await base.UpdateAsync(entity, reasonOfChange);
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
            var check = await db.TransportationPlan.AnyAsync(x => ids.Contains(x.ReceivedId.Value));
            if (check)
            {
                return false;
            }
            try
            {
                var deleteCommand = $"delete VendorContact where LocationId in ({string.Join(",", ids)}); delete VendorLocation where LocationId in ({string.Join(",", ids)}); delete LocationService where LocationId in ({string.Join(",", ids)}); delete from [{typeof(Location).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteCommand);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet("api/[Controller]/UserClick")]
        public virtual async Task<OdataResult<Location>> UserClick(ODataQueryOptions<Location> options)
        {
            var data = ctx.Set<Location>().FromSqlRaw($@"select UserClick.Click,Location.Id,Location.RegionId,Location.BranchId,Location.Name,Location.Description,Location.DescriptionEn,Location.Long,Location.Lat,Location.Active,Location.InsertedBy,Location.InsertedDate,Location.UpdatedDate,Location.UpdatedBy from [{typeof(Location).Name}]
                                                left join UserClick
                                                on [{typeof(Location).Name}].Id = UserClick.RecordId
                                                and UserClick.EntityId = 5018
                                                and UserClick.UserId = " + UserId + @"");
            return await ApplyQuery(options, data);
        }

        [HttpPost("api/Location/ImportLocation")]
        public async Task<List<Location>> ImportLocation([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var listImportExcel = new List<ImportLocationVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 4].Value == null || worksheet.Cells[row, 4].Value?.ToString() == ""))
                {
                    continue;
                }
                var region = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var description = worksheet.Cells[row, 4].Value?.ToString().Trim();
                var importLocation = new ImportLocationVM()
                {
                    Name = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                    RegionText = ConvertTextVn(region),
                    RegionTextEn = ConvertTextEn(region),
                    Description = ConvertTextVn(description),
                    DescriptionEn = ConvertTextEn(description),
                    User = worksheet.Cells[row, 5].Value?.ToString().Trim()
                };
                listImportExcel.Add(importLocation);
            }
            var listRegionIds = listImportExcel.Select(x => x.RegionTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listLocationDescription = listImportExcel.Select(x => x.DescriptionEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsRegion = await db.MasterData.ToListAsync();
            var regionDB = rsRegion.Where(x => x.ParentId == 7569 && listRegionIds.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name)); // 7569 = Region
            var rsLocation = await db.Location.ToListAsync();
            var locationDB = rsLocation.Where(x => listLocationDescription.Contains(ConvertTextEn(x.Description))).ToDictionary(x => ConvertTextEn(x.Description));
            var listUserCodes = listImportExcel.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            foreach (var item in listImportExcel)
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
                MasterData region = null;
                if (item.RegionText != null && item.RegionText != "")
                {
                    region = regionDB.Count == 0 ? null : regionDB.GetValueOrDefault(item.RegionTextEn);
                }
                if (region is null && item.RegionText != null && item.RegionText != "")
                {
                    region = new MasterData()
                    {
                        Name = item.RegionText,
                        Description = item.RegionText,
                        ParentId = 7569, //Region
                        Path = @"\7569\",
                        Level = 1,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(region);
                    await db.SaveChangesAsync();
                    regionDB.Add(ConvertTextEn(region.Name), region);
                }
                else if (region is not null && item.RegionText != null && item.RegionText != "")
                {
                    region.Description = item.RegionText;
                }
                var location = locationDB.Count == 0 ? null : locationDB.GetValueOrDefault(item.DescriptionEn);
                if (location is null)
                {
                    location = new Location()
                    {
                        Name = item.Name,
                        Description = item.Description,
                        Active = true,
                        InsertedDate = DateTime.Now
                    };
                    if (region is not null)
                    {
                        location.RegionId = region.Id;
                    }
                    location.LocationService.Add(new LocationService()
                    {
                        ServiceId = 7581, //Địa điểm đóng hàng
                        Active = true,
                        InsertedDate = DateTime.Now
                    });
                    if (user is not null)
                    {
                        location.InsertedBy = user.Id;
                        location.LocationService.Select(x => x.InsertedBy = user.Id);
                    }
                    else
                    {
                        location.InsertedBy = 1;
                        location.LocationService.Select(x => x.InsertedBy = 1);
                    }
                    locationDB.Add(ConvertTextEn(location.Description), location);
                    db.Add(location);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Location/ImportPortLowerliftGoods")]
        public async Task<List<Location>> ImportPortLowerliftGoods([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var listImportExcel = new List<ImportLocationVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var description = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var importLocation = new ImportLocationVM()
                {
                    Type = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    Name = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    Description = ConvertTextVn(description),
                    DescriptionEn = ConvertTextEn(description),
                };
                listImportExcel.Add(importLocation);
            }
            var listRegionCodes = listImportExcel.Where(x => x.Type == "1").Select(x => x.DescriptionEn).Where(x => x != null && x != "").Distinct().ToList();
            var listLocationName = listImportExcel.Where(x => x.Type != "1").Select(x => x.DescriptionEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsRegion = await db.MasterData.ToListAsync();
            var masterDataDB = rsRegion.Where(x => listRegionCodes.Select(y => y.Replace("khu-vuc", "").Trim()).Contains(ConvertTextEn(x.Description)) && x.ParentId == 7569).ToDictionary(x => ConvertTextEn(x.Description));
            var locationServiceDB = await db.LocationService.Where(x => x.ServiceId == 7583).ToDictionaryAsync(x => x.LocationId);
            var rsLocation = await db.Location.ToListAsync();
            var locationDB = rsLocation.Where(x => listLocationName.Contains(ConvertTextEn(x.Description))).ToDictionary(x => ConvertTextEn(x.Description));
            MasterData currentMasterData = null;
            foreach (var item in listImportExcel)
            {
                if (item.Type == "1")
                {
                    var masterData = masterDataDB.Count == 0 ? null : masterDataDB.GetValueOrDefault(item.DescriptionEn.Replace("khu-vuc", "").Trim());
                    if (masterData is null)
                    {
                        masterData = new MasterData()
                        {
                            Name = item.Description.Replace("Khu vực", "").Trim(),
                            Description = item.Description.Replace("Khu vực", "").Trim(),
                            ParentId = 7569, //Region
                            Path = @"/7569/",
                            Code = item.Name,
                            Active = true,
                            InsertedBy = 1,
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
                    var location = locationDB.Count == 0 ? null : locationDB.GetValueOrDefault(item.DescriptionEn);
                    if (location is null)
                    {
                        location = new Location()
                        {
                            Name = item.Name,
                            Description = item.Description,
                            RegionId = currentMasterData.Id,
                            Active = true,
                            InsertedBy = 1,
                            InsertedDate = DateTime.Now
                        };
                        location.LocationService.Add(new LocationService()
                        {
                            ServiceId = 7583, //Cảng nâng hạ hàng
                            Active = true,
                            InsertedBy = 1,
                            InsertedDate = DateTime.Now
                        });
                        locationDB.Add(ConvertTextEn(location.Description), location);
                        db.Add(location);
                        await db.SaveChangesAsync();
                    }
                    else
                    {
                        var service = locationServiceDB.Count == 0 ? null : locationServiceDB.GetValueOrDefault(location.Id);
                        if (service is null)
                        {
                            location.LocationService.Add(new LocationService()
                            {
                                ServiceId = 7583, //Cảng nâng hạ hàng
                                Active = true,
                                InsertedBy = 1,
                                InsertedDate = DateTime.Now
                            });
                        }
                    }
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Location/ImportPortLowerliftHollow")]
        public async Task<List<Location>> ImportPortLowerliftHollow([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var listImportExcel = new List<ImportLocationVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var description = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var importLocation = new ImportLocationVM()
                {
                    Type = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    Name = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    Description = ConvertTextVn(description),
                    DescriptionEn = ConvertTextEn(description),
                };
                listImportExcel.Add(importLocation);
            }
            var listRegionCodes = listImportExcel.Where(x => x.Type == "1").Select(x => x.DescriptionEn).Where(x => x != null && x != "").Distinct().ToList();
            var listLocationName = listImportExcel.Where(x => x.Type != "1").Select(x => x.DescriptionEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsRegion = await db.MasterData.ToListAsync();
            var masterDataDB = rsRegion.Where(x => listRegionCodes.Select(y => y.Replace("khu-vuc", "").Trim()).Contains(ConvertTextEn(x.Description)) && x.ParentId == 7569).ToDictionary(x => ConvertTextEn(x.Description));
            var locationServiceDB = await db.LocationService.Where(x => x.ServiceId == 7585).ToDictionaryAsync(x => x.LocationId);
            var rsLocation = await db.Location.ToListAsync();
            var locationDB = rsLocation.Where(x => listLocationName.Contains(ConvertTextEn(x.Description))).ToDictionary(x => ConvertTextEn(x.Description));
            MasterData currentMasterData = null;
            foreach (var item in listImportExcel)
            {
                if (item.Type == "1")
                {
                    var masterData = masterDataDB.Count == 0 ? null : masterDataDB.GetValueOrDefault(item.DescriptionEn.Replace("khu-vuc", "").Trim());
                    if (masterData is null)
                    {
                        masterData = new MasterData()
                        {
                            Name = item.Description.Replace("Khu vực", "").Trim(),
                            Description = item.Description.Replace("Khu vực", "").Trim(),
                            ParentId = 7569, //Region
                            Path = @"/7569/",
                            Code = item.Name,
                            Active = true,
                            InsertedBy = 1,
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
                    var location = locationDB.Count == 0 ? null : locationDB.GetValueOrDefault(item.DescriptionEn);
                    if (location is null)
                    {
                        location = new Location()
                        {
                            Name = item.Name,
                            Description = item.Description,
                            RegionId = currentMasterData.Id,
                            Active = true,
                            InsertedBy = 1,
                            InsertedDate = DateTime.Now
                        };
                        location.LocationService.Add(new LocationService()
                        {
                            ServiceId = 7585, //Nơi lấy trả rỗng
                            Active = true,
                            InsertedBy = 1,
                            InsertedDate = DateTime.Now
                        });
                        locationDB.Add(ConvertTextEn(location.Description), location);
                        db.Add(location);
                        await db.SaveChangesAsync();
                    }
                    else
                    {
                        var service = locationServiceDB.Count == 0 ? null : locationServiceDB.GetValueOrDefault(location.Id);
                        if (service is null)
                        {
                            location.LocationService.Add(new LocationService()
                            {
                                ServiceId = 7585, //Nơi lấy trả rỗng
                                Active = true,
                                InsertedBy = 1,
                                InsertedDate = DateTime.Now
                            });
                        }
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
    }
}

