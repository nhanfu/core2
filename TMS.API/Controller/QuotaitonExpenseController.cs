using Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text.RegularExpressions;
using TMS.API.Models;
using TMS.API.ViewModels;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class QuotationExpenseController : TMSController<QuotationExpense>
    {
        public QuotationExpenseController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [HttpPost("api/QuotationExpense/ImportExcel")]
        public async Task<List<QuotationExpense>> ImportExcel([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportQuotationExpenseVM>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == ""))
                {
                    continue;
                }
                var brandShip = worksheet.Cells[row, 1].Value?.ToString().Trim();
                var expenseType = worksheet.Cells[row, 2].Value?.ToString().Trim();
                var quotationExpense = new ImportQuotationExpenseVM()
                {
                    BrandShipText = ConvertTextVn(brandShip),
                    BrandShipTextEn = ConvertTextEn(brandShip),
                    ExpenseTypeText = ConvertTextVn(expenseType),
                    ExpenseTypeTextEn = ConvertTextEn(expenseType),
                    VSC = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                    VS20UnitPrice = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                    VS40UnitPrice = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                    DOUnitPrice = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                };
                list.Add(quotationExpense);
            }
            var listBrandShipCodes = list.Select(x => x.BrandShipTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsVendor = await db.Vendor.ToListAsync();
            var brandShipDB = rsVendor.Where(x => listBrandShipCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7552).ToDictionary(x => ConvertTextEn(x.Name));
            var listExpenseTypeCodes = list.Select(x => x.ExpenseTypeTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsMasterData = await db.MasterData.ToListAsync();
            var expenseTypeDB = rsMasterData.Where(x => listExpenseTypeCodes.Contains(ConvertTextEn(x.Name)) && x.ParentId == 11842).ToDictionary(x => ConvertTextEn(x.Name));
            foreach (var item in list)
            {
                var brandShip = brandShipDB.Count == 0 ? null : brandShipDB.GetValueOrDefault(item.BrandShipTextEn);
                var expenseType = expenseTypeDB.Count == 0 ? null : expenseTypeDB.GetValueOrDefault(item.ExpenseTypeTextEn);
                if (brandShip is null)
                {
                    brandShip = new Vendor()
                    {
                        Name = item.BrandShipText,
                        TypeId = 7552,
                        UserId = 78,
                        IsContract = false,
                        ReturnRate = 0,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    brandShip.VendorService.Add(new VendorService()
                    {
                        ServiceId = 7634,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    });
                    db.Add(brandShip);
                    await db.SaveChangesAsync();
                    brandShipDB.Add(ConvertTextEn(brandShip.Name), brandShip);
                }
                if (brandShip != null && expenseType != null)
                {
                    var quotation = new QuotationExpense()
                    {
                        BrandShipId = brandShip is null ? null : brandShip.Id,
                        ExpenseTypeId = expenseType is null ? null : expenseType.Id,
                        VSC = item.VSC,
                        VS20UnitPrice = item.VS20UnitPrice is null || item.VS20UnitPrice == "" ? 0 : decimal.Parse(item.VS20UnitPrice),
                        VS40UnitPrice = item.VS40UnitPrice is null || item.VS40UnitPrice == "" ? 0 : decimal.Parse(item.VS40UnitPrice),
                        DOUnitPrice = item.DOUnitPrice is null || item.DOUnitPrice == "" ? 0 : decimal.Parse(item.DOUnitPrice),
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(quotation);
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