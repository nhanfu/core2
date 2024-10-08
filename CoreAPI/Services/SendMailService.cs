using Core.Extensions;
using Core.Models;
using CoreAPI.BgService;
using CoreAPI.Models;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace CoreAPI.Services
{
    public static class SendMailService
    {
        public static async Task<(string, string)[]> ReadTemplate(this PlanEmail planEmail, string conn)
        {
            var html = planEmail.Template;
            var sql = string.Empty;
            var now = DateTime.Now;
            switch (planEmail.ReminderSettingId)
            {
                case 1:
                    sql += $"SELECT * FROM [{planEmail.Feature.EntityId}] WHERE [{planEmail.Component.FieldName}] IS NOT NULL " +
                          $"SELECT * FROM COMPONENT WHERE FEATUREID = '{planEmail.FeatureId}' AND ComponentGroupId IS NOT NULL";
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                default:
                    break;
            }
            var datas = await BgExt.ReadDataSet(sql, conn);
            var components = datas.Length > 0 && datas[1].Length > 0 ? datas[1].Select(x => x.MapTo<Component>()).ToList() : null;
            var curlyBraceRegex = new Regex(@"\{(.+?)\}");
            var dollarCurlyBraceRegex = new Regex(@"\${(.+?)\}");
            var dirCom = components.DistinctBy(x => x.Label).ToDictionary(x => x.Label);
            var rs = datas[0].Select((data) =>
            {
                var email = data["Email"]?.ToString();
                var modifiedHtml = html;
                var matches = curlyBraceRegex.Matches(modifiedHtml);
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
                            var currentData = data[curentComponent.FieldName];
                            switch (curentComponent.ComponentType)
                            {
                                case "Datepicker":
                                    currentData = currentData is null ? "" : Convert.ToDateTime(currentData).ToString("dd/MM/yyyy");
                                    break;
                                case "Number":
                                    currentData = currentData is null ? "" : Convert.ToDecimal(currentData).ToString(curentComponent.FormatData ?? "N0");
                                    break;
                                default:
                                    break;
                            }
                            modifiedHtml = modifiedHtml.Replace($"{{{valueWithinCurlyBraces}}}", currentData?.ToString() ?? string.Empty);
                        }
                    }
                }
                return (email, modifiedHtml);
            }).ToArray();
            return rs;
        }
    }
}
