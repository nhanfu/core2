using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using CoreAPI.BgService;
using CoreAPI.Models;
using Elsa.Common.Entities;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace CoreAPI.Services
{
    public class SendMailService
    {
        public async Task<(string, string)[]> ReadTemplate(PlanEmail planEmail, string conn)
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

        public async Task ExecuteEmailPlan(string planId, string conn, string webRootPath)
        {
            var query1 = $"select top 1 * from [{nameof(PlanEmail)}] where Id = '{planId}'";
            var plans = await BgExt.ReadDsAsArr<PlanEmail>(query1, conn);
            var plan = plans.FirstOrDefault();
            if (plan != null)
            {
                var query2 = $"select top 1 * from [{nameof(Component)}] where Id = '{plan.ComponentId}'";
                var query3 = $"select top 1 * from [{nameof(Feature)}] where Id = '{plan.FeatureId}'";
                plan.Component = (await BgExt.ReadDsAsArr<Component>(query2, conn)).FirstOrDefault();
                plan.Feature = (await BgExt.ReadDsAsArr<Feature>(query3, conn)).FirstOrDefault();
                var templates = await ReadTemplate(plan, conn);
                foreach (var item in templates)
                {
                    var email = new EmailVM
                    {
                        ToAddresses = new List<string>() { item.Item1 },
                        Subject = plan.SubjectMail.IsNullOrWhiteSpace() ? plan.Name : plan.SubjectMail,
                        Body = item.Item2
                    };
                    var query = $"select top 1 * from [User] m where Id = '{plan.UserId}'";
                    var user = (await BgExt.ReadDsAsArr<User>(query, conn)).FirstOrDefault();
                    var server = "smtp.gmail.com";
                    await email.SendMailAsync(plan.FromName ?? user.FullName, plan.FromEmail ?? user.Email, plan.PassEmail ?? user.PassEmail, server, 587, false, webRootPath);
                }
                plan.LastStartDate = DateTime.Now;
                switch (plan.ReminderSettingId)
                {
                    case 1: // Daily
                        plan.NextStartDate = plan.NextStartDate.Value.AddDays(1);
                        break;
                    case 2: // Weekly
                        plan.NextStartDate = plan.NextStartDate.Value.AddDays(7);
                        break;
                    case 3: // Monthly
                        plan.NextStartDate = plan.NextStartDate.Value.AddMonths(1);
                        break;
                    case 4: // Yearly
                        plan.NextStartDate = plan.NextStartDate.Value.AddYears(1);
                        break;
                    default:
                        break;
                }
                var patch = plan.MapToPatch();
                await BgExt.SavePatch2(patch, conn);
            }
        }
    }
}
