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
    public class RouteController : TMSController<Route>
    {
        public RouteController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        protected override IQueryable<Route> GetQuery()
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(Route).Name}]
                    where 1 = 1";
            if (AllRoleIds.Contains(25) || AllRoleIds.Contains(27))
            {
                sql += @$" and Route.Id in (select RouteId from UserRoute where UserRoute.UserId = {UserId})";
            }
            var data = db.Route.FromSqlRaw(sql);
            return data;
        }

        [HttpPost("api/Route/ImportRoute")]
        public async Task<List<Route>> ImportRoute([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var listRouteVM = new List<ImportRouteVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var name = worksheet.Cells[row, 3].Value?.ToString().Trim();
                listRouteVM.Add(new ImportRouteVM()
                {
                    Type = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    Code = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    Name = ConvertTextVn(name),
                    NameEn = ConvertTextEn(name),
                    User = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                });
            }
            var listVendorCodes = listRouteVM.Where(x => x.Type == "1").Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var listRouteCodes = listRouteVM.Where(x => x.Type != "1").Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var vendorDB = rsVendor.Where(x => listVendorCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7552).ToDictionary(x => ConvertTextEn(x.Name));
            var rsRoute = await db.Route.ToListAsync();
            var routeDB = rsRoute.Where(x => listRouteCodes.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var vendorServiceDB = await db.VendorService.Where(x => x.ServiceId == 11033).ToDictionaryAsync(x => x.VendorId);
            var currentVendor = new Vendor();
            var listUserCodes = listRouteVM.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            foreach (var item in listRouteVM)
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
                    currentVendor = vendor;
                }
                else
                {
                    var route = routeDB.Count == 0 ? null : routeDB.GetValueOrDefault(item.NameEn);
                    if (route is null)
                    {
                        route = new Route()
                        {
                            Code = item.Code,
                            Name = item.Name,
                            ParentId = currentVendor.Id,
                            Active = true,
                            InsertedBy = user.Id,
                            InsertedDate = DateTime.Now
                        };
                        db.Add(route);
                        routeDB.Add(ConvertTextEn(route.Name), route);
                    }
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpGet("api/[Controller]/UserClick")]
        public virtual async Task<OdataResult<Route>> UserClick(ODataQueryOptions<Route> options)
        {
            var sql = $@"select UserClick.Click,Route.Id,Route.ParentId,Route.Code,Route.Name,Route.Used,Route.Active,Route.InsertedBy,Route.InsertedDate,Route.UpdatedDate,Route.UpdatedBy
                                                from [{typeof(Route).Name}]
                                                left join UserClick
                                                on [{typeof(Route).Name}].Id = UserClick.RecordId
                                                and UserClick.EntityId = 5015
                                                and UserClick.UserId = " + UserId + @" where 1=1 ";
            if (AllRoleIds.Contains(25) || AllRoleIds.Contains(27))
            {
                sql += @$" and Route.Id in (select RouteId from UserRoute where UserRoute.UserId = {UserId})";
            }
            var data = db.Route.FromSqlRaw(sql);
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
