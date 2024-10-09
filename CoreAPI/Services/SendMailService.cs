using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using CoreAPI.BgService;
using CoreAPI.Models;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace CoreAPI.Services
{
    public class SendMailService
    {
        public async Task<(string, string, DateTime, int?, string)[]> ReadTemplate(PlanEmail planEmail, string conn)
        {
            var html = planEmail.Template;
            var sql = string.Empty;
            var now = DateTime.Now;
            if (!planEmail.FeatureId.IsNullOrWhiteSpace() && !planEmail.ComponentId.IsNullOrWhiteSpace())
            {
                sql += $"SELECT * FROM [{planEmail.Feature.EntityId}] WHERE [{planEmail.Component.FieldName}] IS NOT NULL " +
                       $"SELECT * FROM COMPONENT WHERE FEATUREID = '{planEmail.FeatureId}' AND ComponentGroupId IS NOT NULL";
            }
            var datas = await BgExt.ReadDataSet(sql, conn);
            var components = datas.Length > 0 && datas[1].Length > 0 ? datas[1].Select(x => x.MapTo<Component>()).ToList() : null;
            var curlyBraceRegex = new Regex(@"\{(.+?)\}");
            var dollarCurlyBraceRegex = new Regex(@"\${(.+?)\}");
            var dirCom = components.DistinctBy(x => x.Label).ToDictionary(x => x.Label);
            var rs = datas[0].Select((data) =>
            {
                var selectedData = Convert.ToDateTime(data[planEmail.Component.FieldName]);
                var email = data["Email"]?.ToString();
                var id = data["Id"]?.ToString();
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
                return (email, modifiedHtml, selectedData, planEmail.ReminderSettingId, id);
            }).ToArray();
            return rs;
        }

        public async Task ExecuteEmailPlan(string planId, string conn, string webRootPath)
        {

        }

        public async Task ActionSendMail(string conn, string webRootPath, PlanEmail plan, (string, string, DateTime, int?, string) item)
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

        public async Task ActionSendMail(string conn, string webRootPath, PlanEmail plan)
        {
            var email = new EmailVM
            {
                ToAddresses = new List<string>() { plan.ToEmail },
                Subject = plan.SubjectMail.IsNullOrWhiteSpace() ? plan.Name : plan.SubjectMail,
                Body = plan.Template
            };
            var query = $"select top 1 * from [User] m where Id = '{plan.UserId}'";
            var user = (await BgExt.ReadDsAsArr<User>(query, conn)).FirstOrDefault();
            var server = "smtp.gmail.com";
            await email.SendMailAsync(plan.FromName ?? user.FullName, plan.FromEmail ?? user.Email, plan.PassEmail ?? user.PassEmail, server, 587, false, webRootPath);
        }
    }
}
