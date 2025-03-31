using Core.Extensions;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using CoreAPI.BgService;
using DocumentFormat.OpenXml.Drawing.Charts;
using HtmlAgilityPack;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace CoreAPI.Services
{
    public class PdfService
    {
        private readonly UserService _userService;
        private readonly IWebHostEnvironment _host;
        private readonly IHttpContextAccessor _context;
        public PdfService(UserService userService, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _host = webHostEnvironment;
            _context = httpContextAccessor;
        }

        public async Task<string> CreateHtml(CreateHtmlVM createHtmlVM, string conn)
        {
            var component = await BgExt.ReadDsAs<Component>($"SELECT * FROM [Component] where Id = '{createHtmlVM.ComId}'", conn);
            var webConfigs = await BgExt.ReadDataSet($"SELECT * FROM [WebConfig]", conn);
            var myCompany = await BgExt.ReadDsAs<Partner>($"SELECT TOP 1 * FROM [Partner] where ServiceId = 5", conn);
            var components = await BgExt.ReadDsAsArr<Component>($"SELECT * FROM [Component] where Label is not null and Label != '' and FeatureId = '{component.FeatureId}' and ComponentGroupId is not null and ComponentType not in ('Button','Section','GridView')", conn);
            var gridPolicys = await BgExt.ReadDsAsArr<Component>($"SELECT * FROM [Component] where Label is not null and Label != '' and FeatureId = '{component.FeatureId}' and EntityId is not null and ComponentType not in ('Button','Section','GridView')", conn);
            var dirCom = components.DistinctBy(x => x.Label).ToDictionary(x => x.Label);
            var sql = component.Query;
            BindingDataExt.SetDefaultToken(createHtmlVM, _userService);
            if (webConfigs[0].Length > 0)
            {
                foreach (var item in webConfigs[0])
                {
                    createHtmlVM.Data["C" + item["Key"]] = item["Value"] ?? string.Empty;
                }
            }
            if (myCompany != null)
            {
                createHtmlVM.Data["CEmail"] = myCompany.Email ?? string.Empty;
                createHtmlVM.Data["CCompanyName"] = myCompany.CompanyName ?? string.Empty;
                createHtmlVM.Data["CAddress"] = myCompany.Address ?? string.Empty;
                createHtmlVM.Data["CPhoneNumber"] = myCompany.PhoneNumber ?? string.Empty;
                createHtmlVM.Data["CLogo"] = $"<img class=\"logo\" src=\"{myCompany.Logo ?? string.Empty}\" alt=\"\" width=\"100\" height=\"38\">";
            }
            if (!sql.IsNullOrWhiteSpace())
            {
                var sqlQuery = BindingDataExt.FormatString(sql, createHtmlVM.Data);
                var customData = await BgExt.ReadDataSet(sqlQuery, conn);
                foreach (var item in customData[0][0])
                {
                    createHtmlVM.Data[item.Key] = item.Value ?? string.Empty;
                }
                if (customData.Length > 1)
                {
                    int i = 0;
                    foreach (var row in customData.Skip(1).ToList())
                    {
                        createHtmlVM.Data["c" + i] = row;
                        i++;
                    }
                }
            }
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(component.Template);
            var headerHtml = $"<header class='header'>{myCompany.Header}</div>";
            var footerHtml = $"<footer class='footer'>{myCompany.Footer}</div>";
            var headerNode = HtmlNode.CreateNode(headerHtml);
            var footerNode = HtmlNode.CreateNode(footerHtml);
            if (component.ShowHotKey)
            {
                document.DocumentNode.PrependChild(headerNode);
            }
            foreach (var item in document.DocumentNode.ChildNodes)
            {
                BindingDataExt.ReplaceNode(createHtmlVM, item, dirCom);
            }
            foreach (var item in document.DocumentNode.ChildNodes)
            {
                BindingDataExt.ReplaceTableNode(createHtmlVM, item, dirCom);
            }
            foreach (var item in document.DocumentNode.ChildNodes)
            {
                BindingDataExt.ReplaceCTableNode(createHtmlVM, item, dirCom);
            }
            if (component.ShowHotKey)
            {
                document.DocumentNode.AppendChild(footerNode);
            }
            return document.DocumentNode.InnerHtml;
        }

        public async Task<string> HtmlToPdf(string html, string type)
        {
            var name = DateTime.Now.ToString("ddMMyyyyHHmm") + Guid.NewGuid() + ".pdf";
            var path = GetPdfPath(name, _host.WebRootPath, _userService.TenantCode, _userService.UserId);
            EnsureDirectoryExist(path);
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
            // Using statement with grouped declarations
            await using (var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true }))
            {
                await using (var page = await browser.NewPageAsync())
                {
                    try
                    {
                        await page.SetContentAsync(html);
                        await page.EvaluateExpressionHandleAsync("document.fonts.ready");
                        await page.EmulateMediaTypeAsync(MediaType.Screen);
                        var pdfOptions = new PdfOptions
                        {
                            PrintBackground = true,
                            Format = type == "A5" ? PaperFormat.A5 : PaperFormat.A4,
                            MarginOptions = new MarginOptions
                            {
                                Bottom = "10px",
                                Left = "20px",
                                Right = "20px",
                                Top = "10px",
                            }
                        };
                        await page.PdfAsync(path, pdfOptions);
                        var requestUrl = $"{_context.HttpContext.Request.Scheme}://{_context.HttpContext.Request.Host}";
                        return path.Replace(_host.WebRootPath, requestUrl).Replace("\\", "/");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error generating PDF: " + ex.Message, ex);
                    }
                }
            }
        }

        private string GetPdfPath(string fileName, string webRootPath, string tanentcode, string userid)
        {
            return Path.Combine(webRootPath, "pdf", tanentcode, DateTime.Now.ToString("MMyyyy"), $"U{userid:00000000}", fileName);
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
