using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class BookingController : TMSController<Booking>
    {
        public BookingController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        [HttpGet("api/[Controller]/BookingNonExpire")]
        public Task<OdataResult<Booking>> BookingNonExpire(ODataQueryOptions<Booking> options)
        {
            var query = db.Booking.Where(x => x.BookingExpired == null || x.BookingExpired > DateTime.Now);
            return ApplyQuery(options, query);
        }

        protected override IQueryable<Booking> GetQuery()
        {
            var query = base.GetQuery();
            if (AllRoleIds.Contains(25))
            {
                query = query.Where(x => x.BranchId == VendorId);
            }
            return query;
        }

        public override Task<OdataResult<Booking>> Get(ODataQueryOptions<Booking> options)
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(Booking).Name}]
                    where 1 = 1";
            if (AllRoleIds.Contains(25) || AllRoleIds.Contains(27))
            {
                sql += @$" and BranchId = {VendorId}";
            }
            var query = db.Booking.FromSqlRaw(sql);
            return ApplyQuery(options, query, sql: sql);
        }

        public override async Task<ActionResult<Booking>> CreateAsync([FromBody] Booking entity)
        {
            if (entity.ShipId != null && entity.Trip != null && entity.StartShip != null && entity.BookingNo != null)
            {
                var check = await db.Booking.FirstOrDefaultAsync(x => x.ShipId == entity.ShipId && x.Trip == entity.Trip && x.StartShip.Value.Date == entity.StartShip.Value.Date && x.BookingNo == entity.BookingNo && x.BranchId == entity.BranchId);
                if (check != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            return await base.CreateAsync(entity);
        }

        public override async Task<ActionResult<Booking>> UpdateAsync([FromBody] Booking entity, string reasonOfChange = "")
        {
            var check = await db.Booking.FirstOrDefaultAsync(x => x.ShipId == entity.ShipId && x.Trip == entity.Trip && x.StartShip.Value.Date == entity.StartShip.Value.Date && x.Id != entity.Id && x.BookingNo == entity.BookingNo && x.BranchId == entity.BranchId);
            if (check != null)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await base.UpdateAsync(entity, reasonOfChange);
        }

        public override async Task<ActionResult<Booking>> PatchAsync([FromQuery] ODataQueryOptions<Booking> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            Booking entity = default;
            Booking oldEntity = default;
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            if (id != null && id.TryParseInt() > 0)
            {
                var idInt = id.TryParseInt() ?? 0;
                entity = await db.Set<Booking>().FindAsync(idInt);
                oldEntity = await db.Booking.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
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
            if (patch.Changes.Any(x => (x.Field == nameof(oldEntity.ShipId)
            || x.Field == nameof(oldEntity.Trip)
            || x.Field == nameof(oldEntity.StartShip)
            || x.Field == nameof(oldEntity.StartShip)
            || x.Field == nameof(oldEntity.BookingNo)) && !x.Value.IsNullOrWhiteSpace()))
            {
                var check = await db.Booking.FirstOrDefaultAsync(x => x.ShipId == entity.ShipId && x.Trip == entity.Trip && x.StartShip.Value.Date == entity.StartShip.Value.Date && x.Id != entity.Id && x.BookingNo == entity.BookingNo && x.BranchId == entity.BranchId && x.BookingNo != null && x.BookingNo != "");
                if (check != null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            await db.SaveChangesAsync();
            await db.Entry(entity).ReloadAsync();
            return entity;
        }

        [HttpPost("api/Booking/ImportExcel")]
        public async Task<List<Booking>> ImportExcel([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportBooking>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 4].Value == null || worksheet.Cells[row, 4].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 6].Value == null || worksheet.Cells[row, 6].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 8].Value == null || worksheet.Cells[row, 8].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 20].Value == null || worksheet.Cells[row, 20].Value?.ToString() == ""))
                {
                    continue;
                }
                var brandShip = worksheet.Cells[row, 1].Value?.ToString().Trim();
                var line = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var ship = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var pickupEmpty = worksheet.Cells[row, 7].Value?.ToString().Trim();
                var portLoading = worksheet.Cells[row, 8].Value?.ToString().Trim();
                var packingMethod = worksheet.Cells[row, 16].Value?.ToString().Trim();
                var booking = new ImportBooking()
                {
                    BrandShipText = ConvertTextVn(brandShip),
                    BrandShipTextEn = ConvertTextEn(brandShip),
                    LineText = ConvertTextVn(line),
                    LineTextEn = ConvertTextEn(line),
                    ShipText = ConvertTextVn(ship),
                    ShipTextEn = ConvertTextEn(ship),
                    Trip = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                    BookingNo = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                    StartShip = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    PickupEmptyText = ConvertTextVn(pickupEmpty),
                    PickupEmptyTextEn = ConvertTextEn(pickupEmpty),
                    PortLoadingText = ConvertTextVn(portLoading),
                    PortLoadingTextEn = ConvertTextEn(portLoading),
                    Teus20 = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                    Teus40 = worksheet.Cells[row, 10].Value?.ToString().Trim(),
                    Teus20Using = worksheet.Cells[row, 11].Value?.ToString().Trim(),
                    Teus40Using = worksheet.Cells[row, 12].Value?.ToString().Trim(),
                    Teus20Remain = worksheet.Cells[row, 13].Value?.ToString().Trim(),
                    Teus40Remain = worksheet.Cells[row, 14].Value?.ToString().Trim(),
                    Note1 = worksheet.Cells[row, 15].Value?.ToString().Trim(),
                    PackingMethodText = ConvertTextVn(packingMethod),
                    PackingMethodTextEn = ConvertTextEn(packingMethod),
                    Note = worksheet.Cells[row, 17].Value?.ToString().Trim(),
                    User = worksheet.Cells[row, 20].Value?.ToString().Trim()
                };
                list.Add(booking);
            }
            var listBrandShipCodes = list.Select(x => x.BrandShipTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var brandShipDB = rsVendor.Where(x => listBrandShipCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7552).ToDictionary(x => ConvertTextEn(x.Name));
            var listLineCodes = list.Select(x => x.LineTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var lineDB = rsVendor.Where(x => listLineCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7552).ToDictionary(x => ConvertTextEn(x.Name));
            var listShipCodes = list.Select(x => x.ShipTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsShip = await db.Ship.ToListAsync();
            var shipDB = rsShip.Where(x => listShipCodes.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var rsBrandShip = await db.BrandShip.ToListAsync();
            var brandShipNewDB = rsBrandShip.ToDictionary(x => $"{x.BranchId}-{x.ShipId}");
            var listPickupEmptyCodes = list.Select(x => x.PickupEmptyTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsLocation = await db.Location.ToListAsync();
            var pickupEmptyDB = rsLocation.Where(x => listPickupEmptyCodes.Contains(ConvertTextEn(x.Description))).ToDictionary(x => ConvertTextEn(x.Description));
            var listPortLoadingCodes = list.Select(x => x.PortLoadingTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var portLoadingDB = rsLocation.Where(x => listPortLoadingCodes.Contains(ConvertTextEn(x.Description))).ToDictionary(x => ConvertTextEn(x.Description));
            var listPackingMethodCodes = list.Select(x => x.PackingMethodTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsMasterData = await db.MasterData.ToListAsync();
            var packingMethodDB = rsMasterData.Where(x => listPackingMethodCodes.Contains(ConvertTextEn(x.Name)) && x.ParentId == 7574).ToDictionary(x => ConvertTextEn(x.Name));
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
                        IsContract = false,
                        ReturnRate = 0,
                        UserId = 78,
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
                Vendor line = null;
                if (item.LineText != null && item.LineText != "")
                {
                    line = lineDB.Count == 0 ? null : lineDB.GetValueOrDefault(item.LineTextEn);
                }
                if (line is null && item.LineText != null && item.LineText != "")
                {
                    line = new Vendor()
                    {
                        Name = item.LineText,
                        TypeId = 7552,
                        IsContract = false,
                        ReturnRate = 0,
                        UserId = 78,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    line.VendorService.Add(new VendorService()
                    {
                        ServiceId = 7588,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    });
                    db.Add(line);
                    await db.SaveChangesAsync();
                    lineDB.Add(ConvertTextEn(line.Name), line);
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
                        BrandShipId = brandShip.Id,
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
                TMS.API.Models.Location pickupEmpty = null;
                if (item.PickupEmptyText != null && item.PickupEmptyText != "")
                {
                    pickupEmpty = pickupEmptyDB.Count == 0 ? null : pickupEmptyDB.GetValueOrDefault(item.PickupEmptyTextEn);
                }
                if (pickupEmpty is null && item.PickupEmptyText != null && item.PickupEmptyText != "")
                {
                    pickupEmpty = new TMS.API.Models.Location()
                    {
                        Description = item.PickupEmptyText,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    pickupEmpty.LocationService.Add(new LocationService()
                    {
                        ServiceId = 7585,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    });
                    db.Add(pickupEmpty);
                    await db.SaveChangesAsync();
                    pickupEmptyDB.Add(ConvertTextEn(pickupEmpty.Description), pickupEmpty);
                }
                TMS.API.Models.Location portLoading = null;
                if (item.PortLoadingText != null && item.PortLoadingText != "")
                {
                    portLoading = portLoadingDB.Count == 0 ? null : portLoadingDB.GetValueOrDefault(item.PortLoadingTextEn);
                }
                if (portLoading is null && item.PortLoadingText != null && item.PortLoadingText != "")
                {
                    portLoading = new TMS.API.Models.Location()
                    {
                        Description = item.PortLoadingText,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    portLoading.LocationService.Add(new LocationService()
                    {
                        ServiceId = 7583,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    });
                    db.Add(portLoading);
                    await db.SaveChangesAsync();
                    portLoadingDB.Add(ConvertTextEn(portLoading.Description), portLoading);
                }
                MasterData packingMethod = null;
                if (item.PackingMethodText != null && item.PackingMethodText != "")
                {
                    packingMethod = packingMethodDB.Count == 0 ? null : packingMethodDB.GetValueOrDefault(item.PackingMethodTextEn);
                }
                if (packingMethod is null && item.PackingMethodText != null && item.PackingMethodText != "")
                {
                    packingMethod = new MasterData()
                    {
                        Name = item.PackingMethodText,
                        ParentId = 7574,
                        Level = 1,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(packingMethod);
                    await db.SaveChangesAsync();
                    packingMethodDB.Add(ConvertTextEn(packingMethod.Name), packingMethod);
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
                Booking booking = new Booking()
                {
                    BrandShipId = brandShip is null ? null : brandShip.Id,
                    LineId = line is null ? null : line.Id,
                    ShipId = ship is null ? null : ship.Id,
                    Trip = item.Trip,
                    BookingNo = item.BookingNo,
                    StartShip = DateTime.Parse(item.StartShip),
                    PickupEmptyId = pickupEmpty is null ? null : pickupEmpty.Id,
                    PortLoadingId = portLoading is null ? null : portLoading.Id,
                    Teus20 = decimal.Parse(item.Teus20),
                    Teus40 = decimal.Parse(item.Teus40),
                    Teus20Using = decimal.Parse(item.Teus20Using),
                    Teus40Using = decimal.Parse(item.Teus40Using),
                    Teus20Remain = decimal.Parse(item.Teus20Remain),
                    Teus40Remain = decimal.Parse(item.Teus40Remain),
                    Note1 = item.Note1,
                    PackingMethodId = packingMethod is null ? null : packingMethod.Id,
                    Note = item.Note,
                    Active = true,
                    InsertedBy = user is null ? 1 : user.Id,
                    InsertedDate = DateTime.Now
                };
                db.Add(booking);
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
