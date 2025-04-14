using Core.Extensions;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using CoreAPI.BgService;
using HtmlAgilityPack;
using System.Text;
using System.Text.Json;

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
            var headerHtml = $"<header class='header'>{myCompany.Header}</div>";
            var footerHtml = $"<footer class='footer'>{myCompany.Footer}</div>";
            var contentWrapper = HtmlNode.CreateNode($"<div class='content'>{component.Template}</div>");
            document.DocumentNode.InnerHtml = contentWrapper.OuterHtml;
            var headerNode = HtmlNode.CreateNode(headerHtml);
            var footerNode = HtmlNode.CreateNode(footerHtml);
            if (component.ShowHotKey)
            {
                document.DocumentNode.PrependChild(headerNode);
            }
            if (component.ShowHotKey)
            {
                document.DocumentNode.AppendChild(footerNode);
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
            return document.DocumentNode.InnerHtml;
        }

        public class PdfVM2
        {
            public string Html { get; set; }
            public string MarginTop { get; set; }
            public string MarginRight { get; set; }
            public string MarginLeft { get; set; }
            public string MarginBottom { get; set; }
            public bool Landscape { get; set; }
            public string Format { get; set; }
        }

        public async Task<string> HtmlToPdf(PdfVM vm)
        {
            var name = vm.FileName + Guid.NewGuid() + ".pdf";
            string directoryPath = Path.Combine(_host.WebRootPath, "upload", _userService.TenantCode, "pdf", $"U{_userService.UserId}");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            var path = Path.Combine(directoryPath, name);
            var _httpClient = new HttpClient();
            var requestBody = new PdfVM2()
            {
                Html = vm.Html,
                Format = vm.Type ?? "A4",
                MarginBottom = "0px",
                MarginLeft = "0px",
                MarginRight = "0px",
                MarginTop = "0px",
                Landscape = vm.Landscape,
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://cdn-tms.softek.com.vn/api/FileUpload/HtmlToPdf2");
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            var fileUrl = await response.Content.ReadAsStringAsync();
            var pdfBytes = await _httpClient.GetByteArrayAsync(fileUrl);
            await File.WriteAllBytesAsync(path, pdfBytes);
            var requestUrl = $"{_context.HttpContext.Request.Scheme}://{_context.HttpContext.Request.Host}";
            return path.Replace(_host.WebRootPath, requestUrl).Replace("\\", "/");
        }
    }
}
