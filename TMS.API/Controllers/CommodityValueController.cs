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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;
using Windows.UI.Xaml;
using FileIO = System.IO.File;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace TMS.API.Controllers
{
    public class CommodityValueController : TMSController<CommodityValue>
    {
        public CommodityValueController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<CommodityValue>> PatchAsync([FromQuery] ODataQueryOptions<CommodityValue> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            CommodityValue entity = default;
            CommodityValue oldEntity = default;
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            if (id != null && id.TryParseInt() > 0)
            {
                var idInt = id.TryParseInt() ?? 0;
                entity = await db.Set<CommodityValue>().FindAsync(idInt);
                oldEntity = await db.CommodityValue.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
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
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.BossId)
            || x.Field == nameof(oldEntity.CommodityId)
            || x.Field == nameof(oldEntity.ContainerId)))
            {
                var commodity = await db.MasterData.Where(x => x.ParentId != 7651 && x.Path.Contains(@"\7651\") && x.Description.Contains("Vỏ rỗng")).FirstOrDefaultAsync();
                var commodityValueDB = await db.CommodityValue.Where(x => x.Active == true && x.BossId == entity.BossId && x.CommodityId == entity.CommodityId && x.ContainerId == entity.ContainerId).FirstOrDefaultAsync();
                if (commodityValueDB != null || entity.CommodityId == commodity.Id)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.BossId) ||
            x.Field == nameof(oldEntity.IsWet) ||
            x.Field == nameof(oldEntity.IsBought) ||
            x.Field == nameof(oldEntity.SaleId) ||
            x.Field == nameof(oldEntity.CustomerTypeId) ||
            x.Field == nameof(oldEntity.StartDate) ||
            x.Field == nameof(oldEntity.ContainerId) ||
            x.Field == nameof(oldEntity.JourneyId) ||
            x.Field == nameof(oldEntity.CommodityId)) &&
            (oldEntity.BossId != entity.BossId) ||
            (oldEntity.IsBought != entity.IsBought) ||
            (oldEntity.SaleId != entity.SaleId) ||
            (oldEntity.CustomerTypeId != entity.CustomerTypeId) ||
            (oldEntity.StartDate != entity.StartDate) ||
            (oldEntity.CommodityId != entity.CommodityId) ||
            (oldEntity.ContainerId != entity.ContainerId) ||
            (oldEntity.JourneyId != entity.JourneyId) ||
            (oldEntity.IsWet != entity.IsWet))
            {
                var commodity = await db.MasterData.Where(x => x.ParentId != 7651 && x.Path.Contains(@"\7651\") && x.Description.Contains("Vỏ rỗng")).FirstOrDefaultAsync();
                if (oldEntity.CommodityId == commodity.Id)
                {
                    throw new ApiException("Không được cập nhật thông tin khác ngoài GTHH") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            if (patch.Changes.Any(x =>
            x.Field == nameof(oldEntity.StartDate) ||
            x.Field == nameof(oldEntity.IsWet) ||
            x.Field == nameof(oldEntity.IsBought) ||
            x.Field == nameof(oldEntity.CustomerTypeId) ||
            x.Field == nameof(oldEntity.Notes) ||
            x.Field == nameof(oldEntity.JourneyId)) &&
            (oldEntity.StartDate != entity.StartDate) ||
            (oldEntity.IsBought != entity.IsBought) ||
            (oldEntity.CustomerTypeId != entity.CustomerTypeId) ||
            (oldEntity.JourneyId != entity.JourneyId) ||
            (oldEntity.Notes != entity.Notes) ||
            (oldEntity.IsWet != entity.IsWet))
            {
                var commodity = await db.MasterData.Where(x => x.ParentId != 7651 && x.Path.Contains(@"\7651\") && x.Description.Contains("Vỏ rỗng")).FirstOrDefaultAsync();
                if (oldEntity.CommodityId == commodity.Id)
                {
                    throw new ApiException("Không được cập nhật thông tin khác ngoài GTHH") { StatusCode = HttpStatusCode.BadRequest };
                }
                var expenseTypes = await db.MasterData.Where(x => x.ParentId == 7577 && (x.Name.Contains("Bảo hiểm") || x.Name.Contains("BH SOC"))).ToListAsync();
                var expenseTypeCodes = expenseTypes.Select(x => x.Id).ToList();
                var expenseTypeDictionary = expenseTypes.ToDictionary(x => x.Id);
                var containerName = await db.MasterData.Where(x => x.Id == entity.ContainerId).FirstOrDefaultAsync();
                var containerTypes = await db.MasterData.Where(x => x.ParentId == 7565 && x.Description.Contains(containerName.Description)).ToListAsync();
                var containerTypeCodes = containerTypes.Select(x => x.Id).ToList();
                var expenses = await db.Expense.Where(x => expenseTypeCodes.Contains((int)x.ExpenseTypeId) && x.BossId == entity.BossId && x.CommodityId == entity.CommodityId && containerTypeCodes.Contains((int)x.ContainerTypeId) && x.IsPurchasedInsurance == false && (x.StartShip >= entity.StartDate || x.StartShip == null) && x.RequestChangeId == null).ToListAsync();
                var transportationIds = expenses.Select(x => x.TransportationId).Distinct().ToList();
                var transportation = await db.Transportation.Where(x => transportationIds.Contains(x.Id)).ToListAsync();
                var transportationPlanIds = transportation.Select(x => x.TransportationPlanId).Distinct().ToList();
                var transportationPlan = await db.TransportationPlan.Where(x => transportationPlanIds.Contains(x.Id)).ToListAsync();
                transportationPlan.ForEach(x =>
                {
                    x.CommodityValue = entity.TotalPrice;
                    x.IsWet = entity.IsWet;
                    x.IsBought = entity.IsBought;
                    x.JourneyId = entity.JourneyId;
                    x.CustomerTypeId = entity.CustomerTypeId;
                });
                foreach (var x in expenses)
                {
                    x.CommodityValue = entity.TotalPrice;
                    x.CustomerTypeId = entity.CustomerTypeId;
                    x.CommodityValueNotes = entity.Notes;
                    x.JourneyId = entity.JourneyId;
                    var expenseType = expenseTypeDictionary.GetValueOrDefault((int)x.ExpenseTypeId);
                    if (x.CommodityId != commodity.Id && expenseType.Name.Contains("BH SOC") == false)
                    {
                        x.IsWet = entity.IsWet;
                    }
                    if (x.CommodityId != commodity.Id && expenseType.Name.Contains("BH SOC") == false)
                    {
                        x.IsBought = entity.IsBought;
                    }
                    await CalcInsuranceFees(x, false);
                }
            }
            await db.SaveChangesAsync();
            await db.Entry(entity).ReloadAsync();
            RealTimeUpdate(entity);
            return entity;
        }

        private void RealTimeUpdate(CommodityValue entity)
        {
            var thead = new Thread(async () =>
            {
                try
                {
                    await _taskService.SendMessageAllUser(new WebSocketResponse<CommodityValue>
                    {
                        EntityId = _entitySvc.GetEntity(typeof(CommodityValue).Name).Id,
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

        private async Task CalcInsuranceFees(Expense expense, bool isSOC)
        {
            bool isSubRatio = false;
            if (((expense.IsWet || expense.SteamingTerms || expense.BreakTerms) && expense.IsBought == false) || (expense.IsBought && expense.IsWet))
            {
                isSubRatio = true;
            }
            InsuranceFeesRate insuranceFeesRateDB = null;
            if (expense.IsBought)
            {
                insuranceFeesRateDB = await db.InsuranceFeesRate.Where(x => x.TransportationTypeId == expense.TransportationTypeId && x.JourneyId == expense.JourneyId && x.IsBought == expense.IsBought && x.IsSOC == isSOC && x.IsSubRatio == isSubRatio).FirstOrDefaultAsync();
            }
            else
            {
                insuranceFeesRateDB = await db.InsuranceFeesRate.Where(x => x.TransportationTypeId == expense.TransportationTypeId && x.JourneyId == expense.JourneyId && x.IsBought == expense.IsBought && x.IsSOC == isSOC).FirstOrDefaultAsync();
            }
            if (insuranceFeesRateDB != null)
            {
                var getContainerType = await db.MasterData.Where(x => x.Id == expense.ContainerTypeId).FirstOrDefaultAsync();
                if (getContainerType != null && getContainerType.Description.ToLower().Contains("lạnh") && insuranceFeesRateDB.TransportationTypeId == 11673 && insuranceFeesRateDB.JourneyId == 12114)
                {
                    var insuranceFeesRateColdDB = await db.MasterData.Where(x => x.Id == 25391).FirstOrDefaultAsync();
                    expense.InsuranceFeeRate = insuranceFeesRateColdDB != null ? decimal.Parse(insuranceFeesRateColdDB.Name) : 0;
                }
                else
                {
                    expense.InsuranceFeeRate = insuranceFeesRateDB.Rate;
                }
                if (insuranceFeesRateDB.IsSubRatio && expense.IsBought == false)
                {
                    var extraInsuranceFeesRateDB = await db.MasterData.Where(x => x.Active == true && x.ParentId == 25374).ToListAsync();
                    extraInsuranceFeesRateDB.ForEach(x =>
                    {
                        var prop = expense.GetType().GetProperties().Where(y => y.Name == x.Name && bool.Parse(y.GetValue(expense, null).ToString())).FirstOrDefault();
                        if (prop != null)
                        {
                            expense.InsuranceFeeRate += decimal.Parse(x.Code);
                        }
                    });
                }
            }
            else
            {
                expense.InsuranceFeeRate = 0;
                expense.TotalPriceBeforeTax = 0;
                expense.TotalPriceAfterTax = 0;
            }
            if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == true)
            {
                CalcInsuranceFeeNoVAT(expense);
            }
            else if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == false)
            {
                CalcInsuranceFee(expense);
            }
        }

        private void CalcInsuranceFee(Expense expense)
        {
            expense.TotalPriceBeforeTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
            expense.TotalPriceAfterTax = expense.TotalPriceBeforeTax + Math.Round(expense.TotalPriceBeforeTax * expense.Vat / 100, 0);
        }

        private void CalcInsuranceFeeNoVAT(Expense expense)
        {
            expense.TotalPriceAfterTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
            expense.TotalPriceBeforeTax = Math.Round(expense.TotalPriceAfterTax / (decimal)1.1, 0);
        }

        [HttpPost("api/CommodityValue/ImportExcel")]
        public async Task<List<CommodityValue>> ImportExcel([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
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
            var list = new List<ImportCommodityValue>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 4].Value == null || worksheet.Cells[row, 4].Value?.ToString() == ""))
                {
                    continue;
                }
                var boss = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var commodity = worksheet.Cells[row, 4].Value?.ToString().Trim();
                var commodityValue = new ImportCommodityValue()
                {
                    SaleText = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    BossText = ConvertTextVn(boss),
                    BossTextEn = ConvertTextEn(boss),
                    CommodityText = ConvertTextVn(commodity),
                    CommodityTextEn = ConvertTextEn(commodity),
                    TotalPrice1 = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                    TotalPrice2 = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                    Notes = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                    IsWetText = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                    SteamingTerms = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                    BreakTerms = worksheet.Cells[row, 10].Value?.ToString().Trim(),
                    IsBoughtText = worksheet.Cells[row, 11].Value?.ToString().Trim(),
                    CustomerTypeText = worksheet.Cells[row, 12].Value?.ToString().Trim(),
                    JourneyText = worksheet.Cells[row, 13].Value?.ToString().Trim(),
                };
                list.Add(commodityValue);
            }
            var listSaleCodes = list.Select(x => x.SaleText).Where(x => x != null && x != "").Distinct().ToList();
            var listBossCodes = list.Select(x => x.BossTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listCommodityCodes = list.Select(x => x.CommodityTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsBoss = await db.Vendor.ToListAsync();
            var vendorDB = rsBoss.Where(x => listBossCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7551).ToDictionary(x => ConvertTextEn(x.Name));
            var rsCommodity = await db.MasterData.ToListAsync();
            var commodityDB = rsCommodity.Where(x => listCommodityCodes.Contains(ConvertTextEn(x.Description)) && x.Path.Contains(@"\7651\") && x.ParentId != 7651 && x.Description != "" && x.Description != null).ToDictionaryDistinct(x => ConvertTextEn(x.Description));
            var userDB = await db.User.Where(x => listSaleCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            var listJourneyCodes = list.Select(x => x.JourneyText).Where(x => x != null && x != "").Distinct().ToList();
            var listCustomerTypeCodes = list.Select(x => x.CustomerTypeText).Where(x => x != null && x != "").Distinct().ToList();
            var journeyDB = rsCommodity.Where(x => x.ParentId == 12095 && listJourneyCodes.Contains(x.Name)).ToDictionary(x => x.Name);
            var customerTypeDB = rsCommodity.Where(x => x.ParentId == 12085 && listCustomerTypeCodes.Contains(x.Description)).ToDictionary(x => x.Description);
            var endDate1 = new DateTime(DateTime.Now.Year, 7, 1);
            var endDate2 = new DateTime(DateTime.Now.Year, 12, 31);
            foreach(var item in list)
            {
                User user = null;
                if (item.SaleText != null && item.SaleText != "")
                {
                    user = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.SaleText.ToLower());
                }
                Vendor vendor = null;
                if (item.BossText != null && item.BossText != "")
                {
                    vendor = vendorDB.Count == 0 ? null : vendorDB.GetValueOrDefault(item.BossTextEn);
                }
                MasterData commodity = null;
                if (item.CommodityText != null && item.CommodityText != "")
                {
                    commodity = commodityDB.Count == 0 ? null : commodityDB.GetValueOrDefault(item.CommodityTextEn);
                }
                CommodityValue commodityValue = null;
                CommodityValue commodityValue2 = null;
                MasterData journey = null;
                if (item.JourneyText != null && item.JourneyText != "")
                {
                    journey = journeyDB.Count == 0 ? null : journeyDB.GetValueOrDefault(item.JourneyText);
                }
                MasterData customerType = null;
                if (item.CustomerTypeText != null && item.CustomerTypeText != "")
                {
                    customerType = customerTypeDB.Count == 0 ? null : customerTypeDB.GetValueOrDefault(item.CustomerTypeText);
                }
                if (commodity != null && vendor != null)
                {
                    if (item.TotalPrice1 != "x" && item.TotalPrice1 != "X")
                    {
                        commodityValue = new CommodityValue()
                        {
                            BossId = vendor.Id,
                            CommodityId = commodity.Id,
                            SaleId = user.Id,
                            TotalPrice = item.TotalPrice1 is null || item.TotalPrice1 == "" ? 0 : decimal.Parse(item.TotalPrice1),
                            ContainerId = 14910,
                            Notes = item.Notes,
                            JourneyId = journey is null ? null : journey.Id,
                            CustomerTypeId = customerType is null ? null : customerType.Id,
                            StartDate = DateTime.Parse("2023/01/01"),
                            EndDate = endDate1,
                            Active = true,
                            InsertedBy = 1,
                            InsertedDate = DateTime.Now.Date
                        };
                        if (item.IsWetText == "CÓ")
                        {
                            commodityValue.IsWet = true;
                        }
                        else if (item.IsWetText == "KHÔNG" || item.IsWetText == "" || item.IsWetText is null)
                        {
                            commodityValue.IsWet = false;
                        }
                        if (item.IsBoughtText == "CÓ")
                        {
                            commodityValue.IsBought = true;
                        }
                        else if (item.IsBoughtText == "KHÔNG" || item.IsBoughtText == "" || item.IsBoughtText is null)
                        {
                            commodityValue.IsBought = false;
                        }
                        if (item.SteamingTerms == "CÓ")
                        {
                            commodityValue.SteamingTerms = true;
                        }
                        else if (item.SteamingTerms == "KHÔNG" || item.SteamingTerms == "" || item.SteamingTerms is null)
                        {
                            commodityValue.SteamingTerms = false;
                        }
                        if (item.BreakTerms == "CÓ")
                        {
                            commodityValue.BreakTerms = true;
                        }
                        else if (item.BreakTerms == "KHÔNG" || item.BreakTerms == "" || item.BreakTerms is null)
                        {
                            commodityValue.BreakTerms = false;
                        }
                        db.Add(commodityValue);
                    }
                    if (item.TotalPrice2 != "x" && item.TotalPrice2 != "X")
                    {
                        commodityValue2 = new CommodityValue()
                        {
                            BossId = vendor.Id,
                            CommodityId = commodity.Id,
                            SaleId = user.Id,
                            TotalPrice = item.TotalPrice2 is null || item.TotalPrice2 == "" ? 0 : decimal.Parse(item.TotalPrice2),
                            ContainerId = 14909,
                            Notes = item.Notes,
                            JourneyId = journey is null ? null : journey.Id,
                            CustomerTypeId = customerType is null ? null : customerType.Id,
                            StartDate = DateTime.Parse("2023/01/01"),
                            EndDate = endDate1,
                            Active = true,
                            InsertedBy = 1,
                            InsertedDate = DateTime.Now.Date
                        };
                        if (item.IsWetText == "CÓ")
                        {
                            commodityValue2.IsWet = true;
                        }
                        else if (item.IsWetText == "KHÔNG")
                        {
                            commodityValue2.IsWet = false;
                        }
                        if (item.IsBoughtText == "CÓ")
                        {
                            commodityValue2.IsBought = true;
                        }
                        else if (item.IsBoughtText == "KHÔNG" || item.IsBoughtText == "" || item.IsBoughtText is null)
                        {
                            commodityValue2.IsBought = false;
                        }
                        if (item.SteamingTerms == "CÓ")
                        {
                            commodityValue2.SteamingTerms = true;
                        }
                        else if (item.SteamingTerms == "KHÔNG" || item.SteamingTerms == "" || item.SteamingTerms is null)
                        {
                            commodityValue2.SteamingTerms = false;
                        }
                        if (item.BreakTerms == "CÓ")
                        {
                            commodityValue2.BreakTerms = true;
                        }
                        else if (item.BreakTerms == "KHÔNG" || item.BreakTerms == "" || item.BreakTerms is null)
                        {
                            commodityValue2.BreakTerms = false;
                        }
                        db.Add(commodityValue2);
                    }
                }
            }
            await db.SaveChangesAsync();
            return null;
        }

        private decimal GetDefaultCommodityValue(MasterData commodity, string container)
        {
            var TotalPrice = 0;
            if (commodity.Description.Contains("Đá"))
            {
                TotalPrice = 110000000;
            }
            else if (commodity.Description.Contains("Gạch"))
            {
                TotalPrice = 180000000;
            }
            else if (commodity.Description.Contains("Vôi"))
            {
                TotalPrice = 180000000;
            }
            else if (commodity.Description.Contains("Ngói"))
            {
                TotalPrice = 180000000;
            }
            else if (commodity.Description.Contains("Vỏ rỗng") && container.Contains("Cont 20"))
            {
                TotalPrice = 40000000;
            }
            else if (commodity.Description.Contains("Vỏ rỗng") && container.Contains("Cont 40"))
            {
                TotalPrice = 60000000;
            }
            else if (commodity.Description.Contains("Vỏ rỗng"))
            {
                TotalPrice = 250000000;
            }
            else if (container.Contains("Cont 20"))
            {
                TotalPrice = 360000000;
            }
            else if (container.Contains("Cont 40"))
            {
                TotalPrice = 540000000;
            }
            return TotalPrice;
        }

        public static string ConvertTextEn(string text)
        {
            return (text is null || text == "") ? "" : Regex.Replace(text.ToLower().Trim(), @"\s+", " ");
        }

        public static string ConvertTextVn(string text)
        {
            return (text is null || text == "") ? "" : Regex.Replace(text.Trim(), @"\s+", " ");
        }
    }
}
