using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Text.RegularExpressions;
using TMS.API.Models;
using TMS.API.ViewModels;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class TransportationContractController : TMSController<TransportationContract>
    {
        public TransportationContractController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [HttpPost("api/TransportationContract/ImportExcel")]
        public async Task<List<TransportationContract>> ImportExcel([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportTransportationContract>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == ""))
                {
                    continue;
                }
                var boss = worksheet.Cells[row, 5].Value?.ToString().Trim();
                var transportationContract = new ImportTransportationContract()
                {
                    Code = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                    ContractName = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                    ContractNo = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                    BossText = ConvertTextVn(boss),
                    BossTextEn = ConvertTextEn(boss),
                    CompanyName = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    UserText = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                    StartDate = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                    EndDate = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                    SignDate = worksheet.Cells[row, 10].Value?.ToString().Trim().Trim(),
                    TotalPrice = worksheet.Cells[row, 11].Value?.ToString(),
                    Notes = worksheet.Cells[row, 12].Value?.ToString().Trim(),
                    User = worksheet.Cells[row, 13].Value?.ToString().Trim()
                };
                list.Add(transportationContract);
            }
            var listBossCodes = list.Select(x => x.BossTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var bossDB = rsVendor.Where(x => listBossCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7551).ToDictionary(x => ConvertTextEn(x.Name));
            var listSaleCodes = list.Select(x => x.UserText).Where(x => x != null && x != "").Distinct().ToList();
            var saleDB = await db.User.Where(x => listSaleCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            var listUserCodes = list.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            var listTCCodes = list.Select(x => x.Code).Where(x => x != null && x != "").Distinct().ToList();
            var transportationContractDB = await db.TransportationContract.Where(x => listTCCodes.Contains(x.Code)).ToDictionaryAsync(x => x.Code.ToLower());
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
                User sale = null;
                if (item.UserText != null && item.UserText != "")
                {
                    sale = saleDB.Count == 0 ? null : saleDB.GetValueOrDefault(item.UserText.ToLower());
                }
                if (sale is null && item.UserText != null && item.UserText != "")
                {
                    sale = new User()
                    {
                        FullName = item.UserText,
                        UserName = item.UserText,
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
                    saleDB.Add(sale.UserName.ToLower(), sale);
                }
                Vendor boss = null;
                if (item.BossText != null && item.BossText != "")
                {
                    boss = bossDB.Count == 0 ? null : bossDB.GetValueOrDefault(item.BossTextEn);
                }
                if (boss is null && item.BossText != null && item.BossText != "")
                {
                    boss = new Vendor()
                    {
                        Name = item.BossText,
                        TypeId = 7551,
                        IsContract = false,
                        ReturnRate = 0,
                        UserId = sale.Id,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(boss);
                    await db.SaveChangesAsync();
                    bossDB.Add(ConvertTextEn(boss.Name), boss);
                }
                TransportationContract transportationContract = null;
                if (item.Code != null && item.Code != "")
                {
                    transportationContract = transportationContractDB.Count == 0 ? null : transportationContractDB.GetValueOrDefault(item.Code.ToLower());
                }
                if (transportationContract is null && item.Code != null && item.Code != "")
                {
                    transportationContract = new TransportationContract()
                    {
                        Code = item.Code,
                        ContractName = item.ContractName,
                        ContractNo = item.ContractNo,
                        BossId = boss is null ? null : boss.Id,
                        CompanyName = item.CompanyName,
                        UserId = sale is null ? null : sale.Id,
                        StartDate = (item.StartDate != null && item.StartDate != "") ? DateTime.ParseExact(item.StartDate, "dd/MM/yyyy", CultureInfo.InvariantCulture) : null,
                        EndDate = (item.EndDate != null && item.EndDate != "") ? DateTime.ParseExact(item.EndDate, "dd/MM/yyyy", CultureInfo.InvariantCulture) : null,
                        SignDate = (item.SignDate != null && item.SignDate != "") ? DateTime.ParseExact(item.SignDate, "dd/MM/yyyy", CultureInfo.InvariantCulture) : null,
                        TotalPrice = (item.TotalPrice != null && item.TotalPrice != "") ? decimal.Parse(item.TotalPrice) : 0,
                        Notes = item.Notes,
                        Active = true,
                        InsertedBy = user.Id,
                        InsertedDate = DateTime.Now
                    };
                    transportationContractDB.Add(transportationContract.Code.ToLower(), transportationContract);
                    db.Add(transportationContract);
                }
                else
                {
                    transportationContract.ContractName = item.ContractName;
                    transportationContract.ContractNo = item.ContractNo;
                    transportationContract.BossId = boss is null ? null : boss.Id;
                    transportationContract.CompanyName = item.CompanyName;
                    transportationContract.UserId = sale is null ? null : sale.Id;
                    transportationContract.StartDate = (item.StartDate != null && item.StartDate != "") ? DateTime.ParseExact(item.StartDate, "dd/MM/yyyy", CultureInfo.InvariantCulture) : null;
                    transportationContract.EndDate = (item.EndDate != null && item.EndDate != "") ? DateTime.ParseExact(item.EndDate, "dd/MM/yyyy", CultureInfo.InvariantCulture) : null;
                    transportationContract.SignDate = (item.SignDate != null && item.SignDate != "") ? DateTime.ParseExact(item.SignDate, "dd/MM/yyyy", CultureInfo.InvariantCulture) : null;
                    transportationContract.TotalPrice = (item.TotalPrice != null && item.TotalPrice != "") ? decimal.Parse(item.TotalPrice) : 0;
                    transportationContract.Notes = item.Notes;
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        [HttpGet("api/[Controller]/GetByRole")]
        public async Task<OdataResult<TransportationContract>> UserClick(ODataQueryOptions<TransportationContract> options)
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(TransportationContract).Name}]
                    where 1 = 1";
            if (AllRoleIds.Contains(10) || AllRoleIds.Contains(9))
            {
                sql += @$" and (UserId = {UserId} or InsertedBy = {UserId})";
            }
            var data = db.TransportationContract.FromSqlRaw(sql);
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
