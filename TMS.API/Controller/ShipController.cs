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
    public class ShipController : TMSController<Ship>
    {
        public ShipController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [HttpPost("api/Ship/ImportShip")]
        public async Task<List<Ship>> ImportShip([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportShip>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 5].Value == null || worksheet.Cells[row, 5].Value?.ToString() == ""))
                {
                    continue;
                }
                var name = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var shipBrand = worksheet.Cells[row, 5].Value?.ToString().Trim();
                var ship = new ImportShip()
                {
                    Code = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    Name = ConvertTextVn(name),
                    NameEn = ConvertTextEn(name),
                    ShipBrandText = ConvertTextVn(shipBrand),
                    ShipBrandTextEn = ConvertTextEn(shipBrand),
                    User = worksheet.Cells[row, 6].Value?.ToString().Trim()
                };
                list.Add(ship);
            }
            var listShipName = list.Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var listVendorCodes = list.Select(x => x.ShipBrandTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsShip = await db.Ship.ToListAsync();
            var shipDB = rsShip.Where(x => listShipName.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name) + x.BrandShipId);
            var rsVendor = await db.Vendor.ToListAsync();
            var vendorDB = rsVendor.Where(x => listVendorCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7552).ToDictionary(x => ConvertTextEn(x.Name));
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
                var vendor = vendorDB.Count == 0 ? null : vendorDB.GetValueOrDefault(item.ShipBrandTextEn);
                if (vendor is null && item.ShipBrandText != null && item.ShipBrandText != "")
                {
                    vendor = new Vendor()
                    {
                        Name = item.ShipBrandText,
                        UserId = 78,
                        TypeId = 7552,
                        Active = true,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(vendor);
                    await db.SaveChangesAsync();
                    vendorDB.Add(ConvertTextEn(vendor.Name), vendor);
                }
                var ship = shipDB.Count == 0 ? null : shipDB.GetValueOrDefault(item.NameEn + vendor.Id);
                if (ship is null)
                {
                    ship = new Ship()
                    {
                        Code = item.Code,
                        Name = item.Name,
                        BrandShipId = vendor.Id,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(ship);
                    shipDB.Add(ConvertTextEn(ship.Name) + vendor.Id, ship);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Ship/ImportShipNew")]
        public async Task<List<Ship>> ImportShipNew([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportShip>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 5].Value == null || worksheet.Cells[row, 5].Value?.ToString() == ""))
                {
                    continue;
                }
                var name = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var shipBrand = worksheet.Cells[row, 5].Value?.ToString().Trim();
                var ship = new ImportShip()
                {
                    Level = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    Code = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    Name = ConvertTextVn(name),
                    NameEn = ConvertTextEn(name),
                    ShipBrandText = ConvertTextVn(shipBrand),
                    ShipBrandTextEn = ConvertTextEn(shipBrand),
                    User = worksheet.Cells[row, 6].Value?.ToString().Trim()
                };
                list.Add(ship);
            }
            var listShipName = list.Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var listVendorCodes = list.Select(x => x.ShipBrandTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsShip = await db.Ship.ToListAsync();
            var shipDB = rsShip.Where(x => listShipName.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var rsVendor = await db.Vendor.ToListAsync();
            var vendorDB = rsVendor.Where(x => listVendorCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7552).ToDictionary(x => ConvertTextEn(x.Name));
            var listUserCodes = list.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            foreach (var item in list.OrderByDescending(x => x.Level))
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
                var vendor = vendorDB.Count == 0 ? null : vendorDB.GetValueOrDefault(item.ShipBrandTextEn);
                if (vendor is null && item.ShipBrandText != null && item.ShipBrandText != "")
                {
                    vendor = new Vendor()
                    {
                        Name = item.ShipBrandText,
                        UserId = 78,
                        TypeId = 7552,
                        Active = true,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(vendor);
                    await db.SaveChangesAsync();
                    vendorDB.Add(ConvertTextEn(vendor.Name), vendor);
                }
                var ship = shipDB.Count == 0 ? null : shipDB.GetValueOrDefault(item.NameEn);
                if (ship is null)
                {
                    if (item.Level == "1")
                    {
                        ship = new Ship()
                        {
                            Code = item.Code,
                            Name = item.Name,
                            BrandShipId = vendor.Id,
                            Active = true,
                            InsertedBy = user.Id,
                            InsertedDate = DateTime.Now
                        };
                        db.Add(ship);
                        await db.SaveChangesAsync();
                        shipDB.Add(ConvertTextEn(ship.Name), ship);
                    }
                }
                else
                {
                    var brandShip = new BrandShip()
                    {
                        BranchId = vendor.Id,
                        ShipId = ship.Id,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(brandShip);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpGet("api/[Controller]/UserClick")]
        public virtual async Task<OdataResult<Ship>> UserClick(ODataQueryOptions<Ship> options)
        {
            var data = ctx.Set<Ship>().FromSqlRaw($@"select UserClick.Click,Ship.Id,Ship.BrandId,Ship.Code,Ship.OldCode,Ship.BrandShipId,Ship.Active,Ship.InsertedBy,Ship.InsertedDate,Ship.UpdatedDate,Ship.UpdatedBy,Ship.Name 
                                                from [{typeof(Ship).Name}]
                                                left join UserClick
                                                on [{typeof(Ship).Name}].Id = UserClick.RecordId
                                                and UserClick.EntityId = 5016
                                                and UserClick.UserId = " + UserId + @"");
            return await ApplyQuery(options, data);
        }

        [HttpGet("api/[Controller]/GetShipStart")]
        public virtual async Task<OdataResult<Ship>> GetShipStart(ODataQueryOptions<Ship> options)
        {
            var data = ctx.Set<Ship>().FromSqlRaw($@"select * from [{typeof(Ship).Name}] where Id in (select distinct ShipId from [{typeof(Transportation).Name}] where ShipDate is null)");
            return await ApplyQuery(options, data);
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
