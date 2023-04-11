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
    public class TeusController : TMSController<Teus>
    {
        public TeusController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<Teus>> CreateAsync([FromBody] Teus entity)
        {
            var check = await db.Teus.FirstOrDefaultAsync(x => x.BrandShipId == entity.BrandShipId 
            && x.ShipId == entity.ShipId 
            && x.Trip == entity.Trip
            && x.PortLoadingId == entity.PortLoadingId
            && x.StartShip.Value.Date == entity.StartShip.Value.Date);
            if (check != null)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await base.CreateAsync(entity);
        }

        public override async Task<ActionResult<Teus>> UpdateAsync([FromBody] Teus entity, string reasonOfChange = "")
        {
            var check = await db.Teus.FirstOrDefaultAsync(x => x.BrandShipId == entity.BrandShipId 
            && x.ShipId == entity.ShipId 
            && x.Trip == entity.Trip
            && x.PortLoadingId == entity.PortLoadingId
            && x.StartShip.Value.Date == entity.StartShip.Value.Date 
            && x.Id != entity.Id);
            if (check != null)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await base.UpdateAsync(entity, reasonOfChange);
        }

        protected override IQueryable<Teus> GetQuery()
        {
            var query = base.GetQuery();
            if (AllRoleIds.Contains(25))
            {
                query = query.Where(x => x.BranchId == VendorId);
            }
            return query;
        }

        public override Task<OdataResult<Teus>> Get(ODataQueryOptions<Teus> options)
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(Teus).Name}]
                    where 1 = 1";
            if (AllRoleIds.Contains(25) || AllRoleIds.Contains(27))
            {
                sql += @$" and BranchId = {VendorId}";
            }
            return ApplyQuery(options, GetQuery(), sql: sql);
        }

        [HttpGet("api/[Controller]/GetInBooking")]
        public Task<OdataResult<Teus>> GetInBooking([FromQuery] DateTime shipDate, ODataQueryOptions<Teus> options)
        {
            var query = GetQuery().Where(x => x.StartShip == shipDate);
            return ApplyQuery(options, query);
        }

        [HttpPost("api/Booking/ImportExcelTeus")]
        public async Task<List<Teus>> ImportExcelTeus([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportTeus>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 4].Value == null || worksheet.Cells[row, 4].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 5].Value == null || worksheet.Cells[row, 5].Value?.ToString() == ""))
                {
                    continue;
                }
                var brandShip = worksheet.Cells[row, 1].Value?.ToString().Trim();
                var ship = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var teus = new ImportTeus()
                {
                    BrandShipText = ConvertTextVn(brandShip),
                    BrandShipTextEn = ConvertTextEn(brandShip),
                    ShipText = ConvertTextVn(ship),
                    ShipTextEn = ConvertTextEn(ship),
                    Trip = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                    StartShip = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                    Teus20 = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    Teus40 = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                    Teus20Using = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                    Teus40Using = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                    Teus20Remain = worksheet.Cells[row, 10].Value?.ToString().Trim(),
                    Teus40Remain = worksheet.Cells[row, 11].Value?.ToString().Trim(),
                    User = worksheet.Cells[row, 15].Value?.ToString().Trim()
                };
                list.Add(teus);
            }
            var listBrandShipCodes = list.Select(x => x.BrandShipTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var brandShipDB = rsVendor.Where(x => listBrandShipCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7552).ToDictionary(x => ConvertTextEn(x.Name));
            var listShipCodes = list.Select(x => x.ShipTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsShip = await db.Ship.ToListAsync();
            var shipDB = rsShip.Where(x => listShipCodes.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var rsBrandShip = await db.BrandShip.ToListAsync();
            var brandShipNewDB = rsBrandShip.ToDictionary(x => $"{x.BranchId}-{x.ShipId}");
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
                Vendor brandShip = null;
                if (item.BrandShipText != null && item.BrandShipText != "")
                {
                    brandShip = brandShipDB.Count == 0 ? null : brandShipDB.GetValueOrDefault(item.BrandShipTextEn);
                }
                if (brandShip is null && item.BrandShipText != null && item.BrandShipText != "")
                {
                    brandShip = new Vendor()
                    {
                        Name = item.BrandShipText,
                        TypeId = 7552,
                        UserId = 78,
                        IsContract = false,
                        ReturnRate = 0,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    brandShip.VendorService.Add(new VendorService()
                    {
                        ServiceId = 7634,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    });
                    db.Add(brandShip);
                    await db.SaveChangesAsync();
                    brandShipDB.Add(ConvertTextEn(brandShip.Name), brandShip);
                }
                Ship ship = null;
                if (item.ShipText != null && item.ShipText != "")
                {
                    ship = shipDB.Count == 0 ? null : shipDB.GetValueOrDefault(item.ShipTextEn);
                }
                if (ship is null && item.ShipText != null && item.ShipText != "")
                {
                    ship = new Ship()
                    {
                        Name = item.ShipText,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(ship);
                    await db.SaveChangesAsync();
                    shipDB.Add(ConvertTextEn(ship.Name), ship);
                }
                BrandShip brandShipNew = null;
                if (ship.BrandShipId != brandShip.Id && brandShip != null && ship != null)
                {
                    brandShipNew = brandShipNewDB.Count == 0 ? null : brandShipNewDB.GetValueOrDefault($"{brandShip.Id}-{ship.Id}");
                    if (brandShipNew is null)
                    {
                        var brandship = new BrandShip()
                        {
                            BranchId = brandShip.Id,
                            ShipId = ship.Id,
                            Active = true,
                            InsertedBy = user is null ? 1 : user.Id,
                            InsertedDate = DateTime.Now
                        };
                        db.Add(brandship);
                        await db.SaveChangesAsync();
                        brandShipNewDB.Add($"{brandShipNew.BranchId}-{brandShipNew.ShipId}", brandShipNew);
                    }
                }
                if (item.Teus20 == null || item.Teus20 == "")
                {
                    item.Teus20 = "0";
                }
                if (item.Teus40 == null || item.Teus40 == "")
                {
                    item.Teus40 = "0";
                }
                if (item.Teus20Using == null || item.Teus20Using == "")
                {
                    item.Teus20Using = "0";
                }
                if (item.Teus40Using == null || item.Teus40Using == "")
                {
                    item.Teus40Using = "0";
                }
                if (item.Teus20Remain == null || item.Teus20Remain == "")
                {
                    item.Teus20Remain = "0";
                }
                if (item.Teus40Remain == null || item.Teus40Remain == "")
                {
                    item.Teus40Remain = "0";
                }
                Teus teus = new Teus()
                {
                    BrandShipId = brandShip is null ? null : brandShip.Id,
                    ShipId = ship is null ? null : ship.Id,
                    Trip = item.Trip,
                    StartShip = DateTime.Parse(item.StartShip),
                    Teus20 = decimal.Parse(item.Teus20),
                    Teus40 = decimal.Parse(item.Teus40),
                    Teus20Using = decimal.Parse(item.Teus20Using),
                    Teus40Using = decimal.Parse(item.Teus40Using),
                    Teus20Remain = decimal.Parse(item.Teus20Remain),
                    Teus40Remain = decimal.Parse(item.Teus40Remain),
                    Active = true,
                    InsertedBy = user is null ? 1 : user.Id,
                    InsertedDate = DateTime.Now
                };
                db.Add(teus);
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

        public override async Task<ActionResult<bool>> HardDeleteAsync([FromBody] List<int> ids)
        {
            var rs = await base.HardDeleteAsync(ids);
            if (rs.Value)
            {
                var bookings = await db.Booking.Where(x => ids.Contains(x.TeusId.Value) && x.Teus20Using == 0 && x.Teus40Using == 0).ToListAsync();
                db.RemoveRange(bookings);
                await db.SaveChangesAsync();
            }
            return rs;
        }
    }
}
