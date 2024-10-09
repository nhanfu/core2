using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using CoreAPI.BgService;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace CoreAPI.Services
{
    public class PdfService
    {
        public async Task<string> CreateHtml(CreateHtmlVM createHtmlVM, string conn)
        {
            var component = await BgExt.ReadDsAs<Component>($"SELECT * FROM [Component] where Id = '{createHtmlVM.ComId}'", conn);
            var components = await BgExt.ReadDsAsArr<Component>($"SELECT * FROM [Component] where Label is not null and Label != '' and FeatureId = '{component.FeatureId}' and ComponentGroupId is not null and ComponentType not in ('Button','Section','GridView')", conn);
            var dirCom = components.DistinctBy(x => x.Label).ToDictionary(x => x.Label);
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(component.Template);
            foreach (var item in document.DocumentNode.ChildNodes)
            {
                ReplaceNode(createHtmlVM, item, dirCom);
            }
            return document.DocumentNode.InnerHtml;
        }

        private string FormatString(string html, Dictionary<string, object> data)
        {
            var dollarCurlyBraceRegex = new Regex(@"\{(.+?)\}");
            var matches = dollarCurlyBraceRegex.Matches(html);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var valueWithinCurlyBraces = match.Groups[1].Value;
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(valueWithinCurlyBraces);
                    string plainText = htmlDoc.DocumentNode.InnerText;
                    html = html.Replace($"{{{valueWithinCurlyBraces}}}", data.GetValueOrDefault(plainText)?.ToString());
                }
            }
            return html;
        }

        private void ReplaceNode(CreateHtmlVM createHtmlVM, HtmlNode htmlNode, Dictionary<string, Component> dirCom)
        {
            var dollarCurlyBraceRegex = new Regex(@"\${(.+?)\}");
            var matches = dollarCurlyBraceRegex.Matches(htmlNode.InnerHtml);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var valueWithinCurlyBraces = match.Groups[1].Value;
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(valueWithinCurlyBraces);
                    string plainText = htmlDoc.DocumentNode.InnerText;
                    var curentComponent = dirCom.GetValueOrDefault(plainText);
                    if (curentComponent != null)
                    {
                        var currentData = createHtmlVM.Data[curentComponent.FieldName];
                        switch (curentComponent.ComponentType)
                        {
                            case "Datepicker":
                                currentData = currentData is null ? "" : Convert.ToDateTime(currentData).ToString("dd/MM/yyyy");
                                break;
                            case "Number":
                                currentData = currentData is null ? "" : Convert.ToDecimal(currentData).ToString(curentComponent.FormatData ?? "N0");
                                break;
                            case "Dropdown":
                                var displayField = curentComponent.FieldName;
                                var containId = curentComponent.FieldName.EndsWith("Id");
                                if (containId)
                                {
                                    displayField = displayField.Substring(0, displayField.Length - 2);
                                }
                                else
                                {
                                    displayField = displayField + "MasterData";
                                }
                                currentData = FormatString(curentComponent.FormatData, JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(createHtmlVM.Data.GetValueOrDefault(displayField))));
                                break;
                            default:
                                break;
                        }
                        htmlNode.InnerHtml = htmlNode.InnerHtml.Replace($"${{{valueWithinCurlyBraces}}}", currentData?.ToString() ?? string.Empty);
                    }
                    else
                    {
                        if (plainText.Contains("."))
                        {
                            var fields = plainText.Split(".");
                            var mainObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(createHtmlVM.Data[fields[0]]));
                            var currentData = mainObject[fields[1]];
                            htmlNode.InnerHtml = htmlNode.InnerHtml.Replace($"${{{valueWithinCurlyBraces}}}", currentData?.ToString() ?? string.Empty);
                        }
                        else
                        {
                            var currentData = createHtmlVM.Data[plainText];
                            htmlNode.InnerHtml = htmlNode.InnerHtml.Replace($"${{{valueWithinCurlyBraces}}}", currentData?.ToString() ?? string.Empty);
                        }
                    }
                }
            }
            foreach (var item in htmlNode.ChildNodes)
            {
                ReplaceNode(createHtmlVM, item, dirCom);
            }
        }
    }
}
