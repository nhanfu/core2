using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Slugify;
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
    public class QuotationController : TMSController<Quotation>
    {
        public QuotationController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        [HttpPost("api/Quotation/ImportQuotation")]
        public async Task<List<Quotation>> ImportQuotation([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportQuotationVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 4].Value == null || worksheet.Cells[row, 4].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 5].Value == null || worksheet.Cells[row, 5].Value?.ToString() == ""))
                {
                    continue;
                }
                var route = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var brandShip = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var containerType = worksheet.Cells[row, 4].Value?.ToString().Trim();
                var quotation = new ImportQuotationVM()
                {
                    StartDate = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    RouteText = ConvertTextVn(route),
                    RouteTextEn = ConvertTextEn(route),
                    BrandShipText = ConvertTextVn(brandShip),
                    BrandShipTextEn = ConvertTextEn(brandShip),
                    ContainerTypeText = ConvertTextVn(containerType),
                    ContainerTypeTextEn = ConvertTextEn(containerType),
                    UnitPrice = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                    UnitPrice1 = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    UnitPrice2 = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                    Note = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                    User = worksheet.Cells[row, 9].Value?.ToString().Trim()
                };
                list.Add(quotation);
            }
            var listRouteCodes = list.Select(x => x.RouteTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listBrandShipCodes = list.Select(x => x.BrandShipTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listContainerCodes = list.Select(x => x.ContainerTypeTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsRoute = await db.Route.ToListAsync();
            var routeDB = rsRoute.Where(x => listRouteCodes.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var rsVendor = await db.Vendor.ToListAsync();
            var brandShipDB = rsVendor.Where(x => listBrandShipCodes.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var rsMasterData = await db.MasterData.ToListAsync();
            var containerDB = rsMasterData.Where(x => listContainerCodes.Contains(ConvertTextEn(x.Description)) && x.ParentId == 7565).ToDictionary(x => ConvertTextEn(x.Description));
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
                var route = routeDB.Count == 0 ? null : routeDB.GetValueOrDefault(item.RouteTextEn);
                var brandShip = brandShipDB.Count == 0 ? null : brandShipDB.GetValueOrDefault(item.BrandShipTextEn);
                var container = containerDB.Count == 0 ? null : containerDB.GetValueOrDefault(item.ContainerTypeTextEn);
                if (route is not null && brandShip is not null && container is not null)
                {
                    var quotation = new Quotation()
                    {
                        StartDate = DateTime.Parse(item.StartDate),
                        TypeId = 7598,
                        RouteId = route.Id,
                        ContainerTypeId = container.Id,
                        PackingId = brandShip.Id,
                        UnitPrice = decimal.Parse(item.UnitPrice),
                        UnitPrice1 = decimal.Parse(item.UnitPrice1),
                        UnitPrice2 = decimal.Parse(item.UnitPrice2),
                        Note = item.Note,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(quotation);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Quotation/ImportQuotationAdjustment")]
        public async Task<List<Quotation>> ImportQuotationAdjustment([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportQuotationVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 4].Value == null || worksheet.Cells[row, 4].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 5].Value == null || worksheet.Cells[row, 5].Value?.ToString() == ""))
                {
                    continue;
                }
                var route = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var brandShip = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var containerType = worksheet.Cells[row, 4].Value?.ToString().Trim();
                var policyType = worksheet.Cells[row, 5].Value?.ToString().Trim();
                var quotation = new ImportQuotationVM()
                {
                    StartDate = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    RouteText = ConvertTextVn(route),
                    RouteTextEn = ConvertTextEn(route),
                    BrandShipText = ConvertTextVn(brandShip),
                    BrandShipTextEn = ConvertTextEn(brandShip),
                    ContainerTypeText = ConvertTextVn(containerType),
                    ContainerTypeTextEn = ConvertTextEn(containerType),
                    PolicyTypeText = ConvertTextVn(policyType),
                    PolicyTypeTextEn = ConvertTextEn(policyType),
                    UnitPrice = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    User = worksheet.Cells[row, 7].Value?.ToString().Trim()
                };
                list.Add(quotation);
            }
            var listRouteCodes = list.Select(x => x.RouteTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listBrandShipCodes = list.Select(x => x.BrandShipTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listContainerCodes = list.Select(x => x.ContainerTypeTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listPolicyCodes = list.Select(x => x.PolicyTypeTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsRoute = await db.Route.ToListAsync();
            var routeDB = rsRoute.Where(x => listRouteCodes.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var rsVendor = await db.Vendor.ToListAsync();
            var brandShipDB = rsVendor.Where(x => listBrandShipCodes.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var rsMasterData = await db.MasterData.ToListAsync();
            var containerDB = rsMasterData.Where(x => listContainerCodes.Contains(ConvertTextEn(x.Description)) && x.ParentId == 7565).ToDictionary(x => ConvertTextEn(x.Description));
            var policyDB = rsMasterData.Where(x => listPolicyCodes.Contains(ConvertTextEn(x.Name)) && x.ParentId == 11466).ToDictionary(x => ConvertTextEn(x.Name));
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
                var route = routeDB.Count == 0 ? null : routeDB.GetValueOrDefault(item.RouteTextEn);
                var brandShip = brandShipDB.Count == 0 ? null : brandShipDB.GetValueOrDefault(item.BrandShipTextEn);
                var container = containerDB.Count == 0 ? null : containerDB.GetValueOrDefault(item.ContainerTypeTextEn);
                var policy = policyDB.Count == 0 ? null : policyDB.GetValueOrDefault(item.PolicyTypeTextEn);
                if (brandShip is null && item.BrandShipText != null && item.BrandShipText != "")
                {
                    brandShip = new Vendor()
                    {
                        Name = item.BrandShipText,
                        UserId = 78,
                        TypeId = 7552,
                        Active = true,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(brandShip);
                    await db.SaveChangesAsync();
                    brandShipDB.Add(ConvertTextEn(brandShip.Name), brandShip);
                }
                if (policy is null && item.PolicyTypeText != null && item.PolicyTypeText != "")
                {
                    policy = new MasterData()
                    {
                        Name = item.PolicyTypeText,
                        Description = item.PolicyTypeText,
                        ParentId = 11466, //Policy Type
                        Path = @"\11466\",
                        Level = 1,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(policy);
                    await db.SaveChangesAsync();
                    policyDB.Add(ConvertTextEn(policy.Name), policy);
                }
                if (route is not null && brandShip is not null && container is not null)
                {
                    var quotation = new Quotation()
                    {
                        TypeId = 11483,
                        RouteId = route.Id,
                        ContainerTypeId = container.Id,
                        PackingId = brandShip.Id,
                        PolicyTypeId = policy.Id,
                        UnitPrice1 = 0,
                        UnitPrice2 = 0,
                        Active = true,
                        InsertedDate = DateTime.Now
                    };
                    if (item.StartDate != null && item.StartDate != "")
                    {
                        quotation.StartDate = DateTime.Parse(item.StartDate);
                    }
                    if (item.UnitPrice != null && item.UnitPrice != "")
                    {
                        quotation.UnitPrice = decimal.Parse(item.UnitPrice);
                    }
                    else
                    {
                        quotation.UnitPrice = 0;
                    }
                    if (user is not null)
                    {
                        quotation.InsertedBy = user.Id;
                    }
                    else
                    {
                        quotation.InsertedBy = 1;
                    }
                    db.Add(quotation);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Quotation/ImportQuotationCVC")]
        public async Task<List<Quotation>> ImportQuotationCVC([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportQuotationVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 4].Value == null || worksheet.Cells[row, 4].Value?.ToString() == ""))
                {
                    continue;
                }
                var vendorLocation = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var vendor = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var containerType = worksheet.Cells[row, 4].Value?.ToString().Trim();
                var location = worksheet.Cells[row, 5].Value?.ToString().Trim();
                var quotation = new ImportQuotationVM()
                {
                    StartDate = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    VendorLocationText = ConvertTextVn(vendorLocation),
                    VendorLocationTextEn = ConvertTextEn(vendorLocation),
                    VendorText = ConvertTextVn(vendor),
                    VendorTextEn = ConvertTextEn(vendor),
                    ContainerTypeText = ConvertTextVn(containerType),
                    ContainerTypeTextEn = ConvertTextEn(containerType),
                    LocationText = ConvertTextVn(location),
                    LocationTextEn = ConvertTextEn(location),
                    UnitPrice = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    UnitPrice3 = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                    Note = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                    User = worksheet.Cells[row, 9].Value?.ToString().Trim()
                };
                list.Add(quotation);
            }
            var listVendorLocationCodes = list.Select(x => x.VendorLocationTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listVendorCodes = list.Select(x => x.VendorTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listContainerCodes = list.Select(x => x.ContainerTypeTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listLocationCodes = list.Select(x => x.LocationTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var vendorLocationDB = rsVendor.Where(x => listVendorLocationCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7552).ToDictionary(x => ConvertTextEn(x.Name));
            var vendorDB = rsVendor.Where(x => listVendorCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7551).ToDictionary(x => ConvertTextEn(x.Name));
            var rsMasterData = await db.MasterData.ToListAsync();
            var containerDB = rsMasterData.Where(x => listContainerCodes.Contains(ConvertTextEn(x.Description)) && x.ParentId == 7565).ToDictionary(x => ConvertTextEn(x.Description));
            var rsLocation = await db.Location.ToListAsync();
            var locationDB = rsLocation.Where(x => listLocationCodes.Contains(ConvertTextEn(x.Description))).ToDictionary(x => ConvertTextEn(x.Description));
            var listUserCodes = list.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            //var checkvendorLocationDB = await db.VendorLocation.Where(x => x.VendorId != null).ToDictionaryAsync(x => x.VendorId + x.LocationId);
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
                var vendorLocation = vendorLocationDB.Count == 0 ? null : vendorLocationDB.GetValueOrDefault(item.VendorLocationTextEn);
                var vendor = vendorDB.Count == 0 ? null : vendorDB.GetValueOrDefault(item.VendorTextEn);
                var container = containerDB.Count == 0 ? null : containerDB.GetValueOrDefault(item.ContainerTypeTextEn);
                Location location = null;
                if (item.LocationText != null && item.LocationText != "")
                {
                    location = locationDB.Count == 0 ? null : locationDB.GetValueOrDefault(item.LocationTextEn);
                }
                if (vendorLocation is null)
                {
                    vendorLocation = new Vendor()
                    {
                        Name = item.VendorLocationText,
                        TypeId = 7552,
                        UserId = 78,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    vendorLocation.VendorService.Add(new VendorService()
                    {
                        ServiceId = 7572, //Địa điểm đóng hàng
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    });
                    vendorLocation.VendorService.Add(new VendorService()
                    {
                        ServiceId = 7573, //Địa điểm trả hàng
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    });
                    db.Add(vendorLocation);
                    await db.SaveChangesAsync();
                    vendorLocationDB.Add(ConvertTextEn(vendorLocation.Name), vendorLocation);
                }
                if (vendor is null)
                {
                    vendor = new Vendor()
                    {
                        Name = item.VendorText,
                        TypeId = 7551,
                        UserId = 78,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(vendor);
                    await db.SaveChangesAsync();
                    vendorDB.Add(ConvertTextEn(vendor.Name), vendor);
                }
                if (location is null && item.LocationText != null && item.LocationText != "")
                {
                    location = new Location()
                    {
                        Description = item.LocationText,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    locationDB.Add(ConvertTextEn(location.Description), location);
                    db.Add(location);
                    await db.SaveChangesAsync();
                }
                //var vendorlocation = checkvendorLocationDB.Count == 0 ? null : checkvendorLocationDB.GetValueOrDefault(vendor.Id + location.Id);
                //if (vendorlocation is null)
                //{
                //    vendorlocation = new VendorLocation()
                //    {
                //        VendorId = vendor.Id,
                //        LocationId = location.Id,
                //        Active = true,
                //        InsertedBy = user.Id,
                //        InsertedDate = DateTime.Now
                //    };
                //    db.Add(vendorlocation);
                //    await db.SaveChangesAsync();
                //    checkvendorLocationDB.Add(vendorlocation.VendorId + vendorlocation.LocationId, vendorlocation);
                //}
                if (vendorLocation is not null && vendor is not null && container is not null && location is not null)
                {
                    var quotationP = new Quotation()
                    {
                        StartDate = DateTime.Parse(item.StartDate),
                        TypeId = 7592,
                        PackingId = vendorLocation.Id,
                        BossId = vendor.Id,
                        ContainerTypeId = container.Id,
                        LocationId = location.Id,
                        UnitPrice1 = 0,
                        UnitPrice2 = 0,
                        Note = item.Note,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    if (item.UnitPrice != null && item.UnitPrice != "")
                    {
                        quotationP.UnitPrice = decimal.Parse(item.UnitPrice);
                    }
                    else
                    {
                        quotationP.UnitPrice = 0;
                    }
                    if (item.UnitPrice3 != null && item.UnitPrice3 != "")
                    {
                        quotationP.UnitPrice3 = decimal.Parse(item.UnitPrice3);
                    }
                    else
                    {
                        quotationP.UnitPrice3 = 0;
                    }
                    db.Add(quotationP);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Quotation/ImportQuotationCVCReturns")]
        public async Task<List<Quotation>> ImportQuotationCVCReturns([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportQuotationVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 4].Value == null || worksheet.Cells[row, 4].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 5].Value == null || worksheet.Cells[row, 5].Value?.ToString() == ""))
                {
                    continue;
                }
                var vendorLocation = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var vendor = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var containerType = worksheet.Cells[row, 4].Value?.ToString().Trim();
                var location = worksheet.Cells[row, 5].Value?.ToString().Trim();
                var quotation = new ImportQuotationVM()
                {
                    StartDate = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    VendorLocationText = ConvertTextVn(vendorLocation),
                    VendorLocationTextEn = ConvertTextEn(vendorLocation),
                    VendorText = ConvertTextVn(vendor),
                    VendorTextEn = ConvertTextEn(vendor),
                    ContainerTypeText = ConvertTextVn(containerType),
                    ContainerTypeTextEn = ConvertTextEn(containerType),
                    LocationText = ConvertTextVn(location),
                    LocationTextEn = ConvertTextEn(location),
                    UnitPrice = worksheet.Cells[row, 6].Value?.ToString().Trim()//,
                    //User = worksheet.Cells[row, 8].Value?.ToString().Trim()
                };
                list.Add(quotation);
            }
            var listVendorLocationCodes = list.Select(x => x.VendorLocationTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listVendorCodes = list.Select(x => x.VendorTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listContainerCodes = list.Select(x => x.ContainerTypeTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listLocationCodes = list.Select(x => x.LocationTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var vendorLocationDB = rsVendor.Where(x => listVendorLocationCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7552).ToDictionary(x => ConvertTextEn(x.Name));
            var vendorDB = rsVendor.Where(x => listVendorCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7551).ToDictionary(x => ConvertTextEn(x.Name));
            var rsMasterData = await db.MasterData.ToListAsync();
            var containerDB = rsMasterData.Where(x => listContainerCodes.Contains(ConvertTextEn(x.Description)) && x.ParentId == 7565).ToDictionary(x => ConvertTextEn(x.Description));
            var rsLocation = await db.Location.ToListAsync();
            var locationDB = rsLocation.Where(x => listLocationCodes.Contains(ConvertTextEn(x.Description))).ToDictionary(x => ConvertTextEn(x.Description));
            //var listUserCodes = list.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            //var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            var checkvendorLocationDB = await db.VendorLocation.Where(x => x.VendorId != null).ToDictionaryAsync(x => x.VendorId.ToString() + x.LocationId.ToString());
            foreach (var item in list)
            {
                //User user = null;
                //if (item.User != null && item.User != "")
                //{
                //    user = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.User.ToLower());
                //}
                //if (user is null && item.User != null && item.User != "")
                //{
                //    user = new User()
                //    {
                //        FullName = item.User,
                //        UserName = item.User,
                //        VendorId = 65,
                //        HasVerifiedEmail = true,
                //        GenderId = 390,
                //        Active = true,
                //        InsertedBy = 1,
                //        InsertedDate = DateTime.Now
                //    };
                //    user.Salt = _userSvc.GenerateRandomToken();
                //    var randomPassword = "123";
                //    user.Password = _userSvc.GetHash(UserUtils.sHA256, randomPassword + user.Salt);
                //    db.Add(user);
                //    await db.SaveChangesAsync();
                //    userDB.Add(user.UserName.ToLower(), user);
                //}
                var vendorLocation = vendorLocationDB.Count == 0 ? null : vendorLocationDB.GetValueOrDefault(item.VendorLocationTextEn);
                var vendor = vendorDB.Count == 0 ? null : vendorDB.GetValueOrDefault(item.VendorTextEn);
                var container = containerDB.Count == 0 ? null : containerDB.GetValueOrDefault(item.ContainerTypeTextEn);
                Location location = null;
                if (item.LocationText != null && item.LocationText != "")
                {
                    location = locationDB.Count == 0 ? null : locationDB.GetValueOrDefault(item.LocationTextEn);
                }
                if (vendorLocation is null)
                {
                    vendorLocation = new Vendor()
                    {
                        Name = item.VendorLocationText,
                        TypeId = 7552,
                        Active = true,
                        InsertedBy = 1,//user.Id,
                        InsertedDate = DateTime.Now
                    };
                    vendorLocation.VendorService.Add(new VendorService()
                    {
                        ServiceId = 7572, //Địa điểm đóng hàng
                        Active = true,
                        InsertedBy = 1,//user.Id,
                        InsertedDate = DateTime.Now
                    });
                    vendorLocation.VendorService.Add(new VendorService()
                    {
                        ServiceId = 7573, //Địa điểm trả hàng
                        Active = true,
                        InsertedBy = 1,//user.Id,
                        InsertedDate = DateTime.Now
                    });
                    db.Add(vendorLocation);
                    await db.SaveChangesAsync();
                    vendorLocationDB.Add(ConvertTextEn(vendorLocation.Name), vendorLocation);
                }
                if (vendor is null)
                {
                    vendor = new Vendor()
                    {
                        Name = item.VendorText,
                        TypeId = 7551,
                        UserId = 78,
                        Active = true,
                        InsertedBy = 1,//user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(vendor);
                    await db.SaveChangesAsync();
                    vendorDB.Add(ConvertTextEn(vendor.Name), vendor);
                }
                if (location is null && item.LocationText != null && item.LocationText != "")
                {
                    location = new Location()
                    {
                        Description = item.LocationText,
                        Active = true,
                        InsertedBy = 1,//user.Id,
                        InsertedDate = DateTime.Now
                    };
                    locationDB.Add(ConvertTextEn(location.Description), location);
                    db.Add(location);
                    await db.SaveChangesAsync();
                }
                var vendorlocation = checkvendorLocationDB.Count == 0 ? null : checkvendorLocationDB.GetValueOrDefault(vendor.Id.ToString() + location.Id.ToString());
                if (vendorlocation is null)
                {
                    vendorlocation = new VendorLocation()
                    {
                        VendorId = vendor.Id,
                        LocationId = location.Id,
                        Active = true,
                        InsertedBy = 1,//user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(vendorlocation);
                    await db.SaveChangesAsync();
                    checkvendorLocationDB.Add(vendorlocation.VendorId.ToString() + vendorlocation.LocationId.ToString(), vendorlocation);
                }
                if (vendorLocation is not null && vendor is not null && container is not null && location is not null)
                {
                    var quotationR = new Quotation()
                    {
                        StartDate = DateTime.Parse(item.StartDate),
                        TypeId = 7593,
                        PackingId = vendorLocation.Id,
                        BossId = vendor.Id,
                        ContainerTypeId = container.Id,
                        LocationId = location.Id,
                        UnitPrice3 = 0,
                        UnitPrice2 = 0,
                        UnitPrice1 = 0,
                        Note = item.Note,
                        Active = true,
                        InsertedBy = 1,//user.Id,
                        InsertedDate = DateTime.Now
                    };
                    if (item.UnitPrice != null && item.UnitPrice != "")
                    {
                        quotationR.UnitPrice = decimal.Parse(item.UnitPrice);
                    }
                    else
                    {
                        quotationR.UnitPrice = 0;
                    }
                    db.Add(quotationR);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Quotation/ImportQuotationLiftLlowerGoods")]
        public async Task<List<Quotation>> ImportQuotationLiftLlowerGoods([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportQuotationVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var location = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var containerType = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var quotation = new ImportQuotationVM()
                {
                    StartDate = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    LocationText = ConvertTextVn(location),
                    LocationTextEn = ConvertTextEn(location),
                    ContainerTypeText = ConvertTextVn(containerType),
                    ContainerTypeTextEn = ConvertTextEn(containerType),
                    UnitPrice = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                    UnitPrice1 = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                    Note = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    User = worksheet.Cells[row, 7].Value?.ToString().Trim()
                };
                list.Add(quotation);
            }
            var listContainerCodes = list.Select(x => x.ContainerTypeTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listLocationCodes = list.Select(x => x.LocationTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsMasterData = await db.MasterData.ToListAsync();
            var containerDB = rsMasterData.Where(x => listContainerCodes.Contains(ConvertTextEn(x.Description)) && x.ParentId == 7565).ToDictionary(x => ConvertTextEn(x.Description));
            var rsLocation = await db.Location.ToListAsync();
            var locationDB = rsLocation.Where(x => listLocationCodes.Contains(ConvertTextEn(x.Description))).ToDictionary(x => ConvertTextEn(x.Description));
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
                var container = containerDB.Count == 0 ? null : containerDB.GetValueOrDefault(item.ContainerTypeTextEn);
                Location location = null;
                if (item.LocationText != null && item.LocationText != "")
                {
                    location = locationDB.Count == 0 ? null : locationDB.GetValueOrDefault(item.LocationTextEn);
                }
                if (location is null && item.LocationText != null && item.LocationText != "")
                {
                    location = new Location()
                    {
                        Description = item.LocationText,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    location.LocationService.Add(new LocationService()
                    {
                        ServiceId = 7583, //Địa điểm hạ hàng
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    });
                    locationDB.Add(ConvertTextEn(location.Description), location);
                    db.Add(location);
                    await db.SaveChangesAsync();
                }
                else
                {
                    location.LocationService.Add(new LocationService()
                    {
                        ServiceId = 7583, //Địa điểm hạ hàng
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    });
                }
                if (container is not null && location is not null)
                {
                    var quotationLi = new Quotation()
                    {
                        StartDate = DateTime.Parse(item.StartDate),
                        TypeId = 7596, //Nâng hàng
                        ContainerTypeId = container.Id,
                        LocationId = location.Id,
                        UnitPrice2 = 0,
                        Note = item.Note,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    if (item.UnitPrice != null && item.UnitPrice != "")
                    {
                        quotationLi.UnitPrice = decimal.Parse(item.UnitPrice);
                    }
                    else
                    {
                        quotationLi.UnitPrice = 0;
                    }
                    if (item.UnitPrice1 != null && item.UnitPrice1 != "")
                    {
                        quotationLi.UnitPrice1 = decimal.Parse(item.UnitPrice1);
                    }
                    else
                    {
                        quotationLi.UnitPrice1 = 0;
                    }
                    db.Add(quotationLi);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Quotation/ImportQuotationLiftLlowerHollow")]
        public async Task<List<Quotation>> ImportQuotationLiftLlowerHollow([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportQuotationVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var location = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var containerType = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var quotation = new ImportQuotationVM()
                {
                    StartDate = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    LocationText = ConvertTextVn(location),
                    LocationTextEn = ConvertTextEn(location),
                    ContainerTypeText = ConvertTextVn(containerType),
                    ContainerTypeTextEn = ConvertTextEn(containerType),
                    UnitPrice = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                    UnitPrice1 = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                    Note = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    User = worksheet.Cells[row, 7].Value?.ToString().Trim()
                };
                list.Add(quotation);
            }
            var listContainerCodes = list.Select(x => x.ContainerTypeTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listLocationCodes = list.Select(x => x.LocationTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsMasterData = await db.MasterData.ToListAsync();
            var containerDB = rsMasterData.Where(x => listContainerCodes.Contains(ConvertTextEn(x.Description)) && x.ParentId == 7565).ToDictionary(x => ConvertTextEn(x.Description));
            var rsLocation = await db.Location.ToListAsync();
            var locationDB = rsLocation.Where(x => listLocationCodes.Contains(ConvertTextEn(x.Description))).ToDictionary(x => ConvertTextEn(x.Description));
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
                MasterData container = null;
                if (item.ContainerTypeText != null && item.ContainerTypeText != "")
                {
                    container = containerDB.Count == 0 ? null : containerDB.GetValueOrDefault(item.ContainerTypeTextEn);
                }
                Location location = null;
                if (item.LocationText != null && item.LocationText != "")
                {
                    location = locationDB.Count == 0 ? null : locationDB.GetValueOrDefault(item.LocationTextEn);
                }
                if (location is null && item.LocationText != null && item.LocationText != "")
                {
                    location = new Location()
                    {
                        Description = item.LocationText,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    location.LocationService.Add(new LocationService()
                    {
                        ServiceId = 7585, //Địa điểm lấy rỗng
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    });
                    locationDB.Add(ConvertTextEn(location.Description), location);
                    db.Add(location);
                    await db.SaveChangesAsync();
                }
                else
                {
                    location.LocationService.Add(new LocationService()
                    {
                        ServiceId = 7585, //Địa điểm lấy rỗng
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    });
                }
                if (container is not null && location is not null)
                {
                    var quotationLi = new Quotation()
                    {
                        StartDate = DateTime.Parse(item.StartDate),
                        TypeId = 7594, //Nâng rỗng
                        ContainerTypeId = container.Id,
                        LocationId = location.Id,
                        UnitPrice2 = 0,
                        Note = item.Note,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    if (item.UnitPrice != null && item.UnitPrice != "")
                    {
                        quotationLi.UnitPrice = decimal.Parse(item.UnitPrice);
                    }
                    else
                    {
                        quotationLi.UnitPrice = 0;
                    }
                    if (item.UnitPrice1 != null && item.UnitPrice1 != "")
                    {
                        quotationLi.UnitPrice1 = decimal.Parse(item.UnitPrice1);
                    }
                    else
                    {
                        quotationLi.UnitPrice1 = 0;
                    }
                    db.Add(quotationLi);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpPost("api/Quotation/ImportQuotationCombination")]
        public async Task<List<Quotation>> ImportQuotationCombination([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportQuotationVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var brandShip = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var container = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var quotation = new ImportQuotationVM()
                {
                    StartDate = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    BrandShipText = ConvertTextVn(brandShip),
                    BrandShipTextEn = ConvertTextEn(brandShip),
                    ContainerTypeText = ConvertTextVn(container),
                    ContainerTypeTextEn = ConvertTextEn(container),
                    UnitPrice = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                };
                list.Add(quotation);
            }
            var listBrandShipCodes = list.Select(x => x.BrandShipTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var brandShipDB = rsVendor.Where(x => listBrandShipCodes.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var listContainerCodes = list.Select(x => x.ContainerTypeTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsMasterData = await db.MasterData.ToListAsync();
            var containerDB = rsMasterData.Where(x => listContainerCodes.Contains(ConvertTextEn(x.Description)) && x.ParentId == 7565).ToDictionary(x => ConvertTextEn(x.Description));
            foreach (var item in list)
            {
                var brandShip = brandShipDB.Count == 0 ? null : brandShipDB.GetValueOrDefault(item.BrandShipTextEn);
                var container = containerDB.Count == 0 ? null : containerDB.GetValueOrDefault(item.ContainerTypeTextEn);
                if (brandShip is not null && container is not null)
                {
                    var quotation = new Quotation()
                    {
                        StartDate = DateTime.Parse(item.StartDate),
                        PackingId = brandShip is null ? null : brandShip.Id,
                        ContainerTypeId = container is null ? null : container.Id,
                        TypeId = 12071,
                        UnitPrice1 = 0,
                        UnitPrice2 = 0,
                        Note = item.Note,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    if (item.UnitPrice != null && item.UnitPrice != "")
                    {
                        quotation.UnitPrice = decimal.Parse(item.UnitPrice);
                    }
                    else
                    {
                        quotation.UnitPrice = 0;
                    }
                    db.Add(quotation);
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpGet("api/[Controller]/GetCurrentQuotation")]
        public virtual async Task<OdataResult<Quotation>> GetCurrentQuotation(ODataQueryOptions<Quotation> options)
        {
            var data = ctx.Set<Quotation>().FromSqlRaw($@"select top 1 with ties
                            *
                            from Quotation
                            order by row_number() over (partition by BranchId,TypeId,RouteId,ContainerTypeId,PackingId,BossId,LocationId order by StartDate desc)");
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

        public override async Task<ActionResult<Quotation>> CreateAsync([FromBody] Quotation entity)
        {
            var check = await db.Quotation.AnyAsync(x => x.LocationId == entity.LocationId
            && x.RouteId == entity.RouteId
            && x.StartDate == entity.StartDate
            && x.BranchId == entity.BranchId
            && x.PackingId == entity.PackingId
            && x.BossId == entity.BossId
            && x.ContainerTypeId == entity.ContainerTypeId);
            if(check)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await base.CreateAsync(entity);
        }

        public override async Task<ActionResult<Quotation>> UpdateAsync([FromBody] Quotation entity, string reasonOfChange = "")
        {
            var check = await db.Quotation.AnyAsync(x => x.LocationId == entity.LocationId
            && x.RouteId == entity.RouteId
            && x.StartDate == entity.StartDate
            && x.BranchId == entity.BranchId
            && x.PackingId == entity.PackingId
            && x.BossId == entity.BossId
            && x.Id != entity.Id
            && x.ContainerTypeId == entity.ContainerTypeId);
            if (check)
            {
                throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await base.UpdateAsync(entity, reasonOfChange);
        }
    }
}
