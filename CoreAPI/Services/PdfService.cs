using Core.Models;
using CoreAPI.BgService;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace CoreAPI.Services
{
    public class PdfService
    {
        public async Task<string> CreteHtml(string comId, object data)
        {
            var component = await BgExt.ReadDsAs<Component>($"SELECT * FROM [Component] where Id = {comId}");
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(component.Template);
            foreach (var item in document.DocumentNode.ChildNodes)
            {

            }
            return string.Empty;
        }

        private void ReplaceNode(HtmlNode htmlNode, List<Component> components, Feature feature)
        {
            var curlyBraceRegex = new Regex(@"\{(.+?)\}");
            var dollarCurlyBraceRegex = new Regex(@"\${(.+?)\}");
            var matches = curlyBraceRegex.Matches(htmlNode.InnerHtml);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var valueWithinCurlyBraces = match.Groups[1].Value;
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(valueWithinCurlyBraces);
                    string plainText = htmlDoc.DocumentNode.InnerText;

                }
            }
            foreach (var item in htmlNode.ChildNodes)
            {
                ReplaceNode(item, components, feature);
            }
        }
    }
}
