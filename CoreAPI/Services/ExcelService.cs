using ClosedXML.Excel;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using CoreAPI.BgService;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Pkcs;
using System.Text.RegularExpressions;
using FileIO = System.IO.File;

namespace CoreAPI.Services
{
    public class ExcelService
    {
        private readonly IWebHostEnvironment _host;
        private readonly IHttpContextAccessor _context;
        public ExcelService(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _host = webHostEnvironment;
            _context = httpContextAccessor;
        }

        public async Task<string> CreateExcelFile(CreateHtmlVM createHtmlVM, string connStr)
        {
            var component = await BgExt.ReadDsAs<Component>($"SELECT * FROM [Component] where Id = '{createHtmlVM.ComId}'", connStr);
            var sql = component.Query;
            if (!sql.IsNullOrWhiteSpace())
            {
                var sqlQuery = BindingDataExt.FormatString(sql, createHtmlVM.Data);
                var customData = await BgExt.ReadDataSet(sqlQuery, connStr);
                foreach (var item in customData[0][0])
                {
                    createHtmlVM.Data[item.Key] = item.Value ?? string.Empty;
                }
                if (customData.Length > 1)
                {
                    for (int i = 0; i < customData.Length - 1; i++)
                    {
                        createHtmlVM.Data["c" + i] = customData[i + 1];
                    }
                }
            }
            var path = Path.Combine(_host.WebRootPath, "excel", component.PlainText ?? "template" + ".xlsx");
            EnsureDirectoryExist(path);
            if (FileIO.Exists(path))
            {
                FileIO.Delete(path);
            }
            var templatePath = component.ExcelUrl;
            if (Uri.TryCreate(templatePath, UriKind.Absolute, out var templateUri) && templateUri.Scheme == Uri.UriSchemeHttps)
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(templatePath);
                    if (response.IsSuccessStatusCode)
                    {
                        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                        await response.Content.CopyToAsync(fileStream);
                    }
                    else
                    {
                        throw new Exception($"Failed to download template from {templatePath}. Status code: {response.StatusCode}");
                    }
                }
            }
            else if (FileIO.Exists(templatePath))
            {
                FileIO.Copy(templatePath, path, overwrite: true);
            }
            else
            {
                throw new FileNotFoundException($"Template file not found at {templatePath}");
            }
            var curlyBraceRegex = new Regex(@"\{(.+?)\}");
            var dollarCurlyBraceRegex = new Regex(@"\${(.+?)\}");
            using (var workbook = new XLWorkbook(path))
            {
                var worksheet = workbook.Worksheet(1);
                var lastDataIndex = 0;
                foreach (var row in worksheet.RowsUsed())
                {
                    var currentRow = -1;
                    foreach (var cell in row.CellsUsed())
                    {
                        var cellValue = cell.GetString();
                        if (dollarCurlyBraceRegex.IsMatch(cellValue))
                        {
                            currentRow = row.RowNumber();
                            break;
                        }
                    }
                    if (currentRow == row.RowNumber())
                    {
                        var mainJson = JsonConvert.DeserializeObject<Dictionary<string, object>[]>(JsonConvert.SerializeObject(createHtmlVM.Data["c" + lastDataIndex]));
                        foreach (var item in mainJson)
                        {
                            var newRow = worksheet.Row(currentRow).InsertRowsBelow(1).First();
                            foreach (var cell in row.CellsUsed())
                            {
                                var cellValue = cell.GetString();
                                var newValue = BindingDataExt.FormatString2(cellValue, item);
                                newRow.Cell(cell.Address.ColumnNumber).SetValue(newValue);
                                if (cell.HasFormula)
                                {
                                    newRow.Cell(cell.Address.ColumnNumber).FormulaA1 = cell.FormulaA1;
                                    newRow.Cell(cell.Address.ColumnNumber).FormulaReference = cell.FormulaReference;
                                    newRow.Cell(cell.Address.ColumnNumber).FormulaR1C1 = cell.FormulaR1C1;
                                }
                                newRow.Cell(cell.Address.ColumnNumber).Style = cell.Style;
                                newRow.Cell(cell.Address.ColumnNumber).Style.NumberFormat.Format = cell.Style.NumberFormat.Format;
                                if (cell.IsMerged())
                                {
                                    var mergedRange = cell.MergedRange();
                                    var firstAddress = mergedRange.FirstCell().Address;
                                    var lastAddress = mergedRange.LastCell().Address;
                                    worksheet.Range(firstAddress, lastAddress).Merge();
                                }
                            }
                        }
                        lastDataIndex++;
                        worksheet.Row(row.RowNumber()).Delete(); // Delete the original row after processing
                    }
                }
                foreach (var row in worksheet.RowsUsed())
                {
                    var hasConfig = false;
                    double maxRowHeight = 0;
                    foreach (var cell in row.CellsUsed())
                    {
                        var cellValue = cell.GetString();
                        var newValue = BindingDataExt.FormatString(cellValue, createHtmlVM.Data);
                        row.Cell(cell.Address.ColumnNumber).SetValue(newValue);
                        var text = cell.GetString();
                        var columnWidth = cell.WorksheetColumn().Width;
                        var fontSize = cell.Style.Font.FontSize;
                        double maxCharsPerLine = columnWidth * 1.2;
                        int textLength = text.Length;
                        double lines = Math.Ceiling(textLength / maxCharsPerLine);
                        double lineHeight = fontSize * 2.1; // Hệ số 1.5 để điều chỉnh độ cao dòng
                        double rowHeight = lines * lineHeight;
                        if (rowHeight > maxRowHeight)
                        {
                            maxRowHeight = rowHeight;
                        }
                        if (!hasConfig)
                        {
                            hasConfig = cellValue != newValue;
                        }
                    }
                    if (hasConfig)
                    {
                        row.Height = maxRowHeight;
                    }
                }
                workbook.SaveAs(path);
            }
            var requestUrl = $"{_context.HttpContext.Request.Scheme}://{_context.HttpContext.Request.Host}";
            return path.Replace(_host.WebRootPath, requestUrl).Replace("\\", "/");
        }


        private void EnsureDirectoryExist(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
