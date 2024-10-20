using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using CoreAPI.BgService;
using CoreAPI.Models;
using Hangfire;
using HtmlAgilityPack;
using System.Linq;
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
            var emailField = $"SELECT * FROM COMPONENT WHERE ID = '{planEmail.EmailFieldId}'";
            var emailCom = await BgExt.ReadDsAs<Component>(emailField, conn);
            if (!planEmail.FeatureId.IsNullOrWhiteSpace())
            {
                sql = $"SELECT * FROM [{planEmail.Feature.EntityId}] WHERE {emailCom.FieldName} is not null; SELECT * FROM COMPONENT WHERE FEATUREID = '{planEmail.FeatureId}' AND ComponentGroupId IS NOT NULL";
            }
            if (!planEmail.ComponentId.IsNullOrWhiteSpace())
            {
                sql = $"SELECT * FROM [{planEmail.Feature.EntityId}] WHERE [{planEmail.Component.FieldName}] IS NOT NULL and {emailCom.FieldName} is not null; SELECT * FROM COMPONENT WHERE FEATUREID = '{planEmail.FeatureId}' AND ComponentGroupId IS NOT NULL";
            }
            var datas = await BgExt.ReadDataSet(sql, conn);
            if (datas[0].Length == 0)
            {
                return null;
            }
            var components = datas.Length > 0 && datas[1].Length > 0 ? datas[1].Select(x => x.MapTo<Component>()).ToList() : null;
            var curlyBraceRegex = new Regex(@"\{(.+?)\}");
            var dollarCurlyBraceRegex = new Regex(@"\${(.+?)\}");
            var dirCom = components.DistinctBy(x => x.Label).ToDictionary(x => x.Label);
            var tasks = datas[0].Select(async (data) =>
            {
                var sqlChild = planEmail.Sql;
                if (!sqlChild.IsNullOrWhiteSpace())
                {
                    var sqlQuery = BindingDataExt.FormatString(sqlChild, data);
                    var customData = await BgExt.ReadDataSet(sqlQuery, conn);
                    foreach (var item in customData[0][0])
                    {
                        if (data.GetValueOrNull(item.Key) != null)
                        {
                            data[item.Key] = item.Value;
                        }
                        else
                        {
                            data.Add(item.Key, item.Value);
                        }
                    }
                    if (customData.Length > 1)
                    {
                        int i = 0;
                        foreach (var row in customData.Skip(1).ToList())
                        {
                            data.Add("c" + i, row);
                            i++;
                        }
                    }
                }
                var createHtmlVM = new CreateHtmlVM()
                {
                    Data = data
                };
                var selectedData = Convert.ToDateTime(planEmail.ComponentId.IsNullOrWhiteSpace() ? planEmail.DailyDate : data[planEmail.Component.FieldName]);
                var email = data[emailCom.FieldName]?.ToString();
                var id = $"{planEmail.Id}{data["Id"]?.ToString()}";
                var modifiedHtml = html;
                var matches = curlyBraceRegex.Matches(modifiedHtml);
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(planEmail.Template);
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
                var newHtml = document.DocumentNode.InnerHtml;
                return (email, newHtml, selectedData, planEmail.ReminderSettingId, id);
            }).ToArray();
            return await Task.WhenAll(tasks);
        }

        public async Task ActionSendMail(string conn, string webRootPath, PlanEmail plan, PlanEmailDetail planDetail, (string, string, DateTime, int?, string) item)
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
            switch (plan.ReminderSettingId)
            {
                case 1:
                    planDetail.NextStartDate = planDetail.NextStartDate.Value.AddDays(1);
                    break;
                case 2:
                    planDetail.NextStartDate = planDetail.NextStartDate.Value.AddDays(7);
                    break;
                case 3:
                    planDetail.NextStartDate = planDetail.NextStartDate.Value.AddMonths(1);
                    break;
                case 4:
                    planDetail.NextStartDate = planDetail.NextStartDate.Value.AddYears(1);
                    break;
                default:
                    break;
            }
            planDetail.Id = planDetail.Id.Substring(1);
            var patch2 = planDetail.MapToPatch();
            await BgExt.SavePatch2(patch2, conn);
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
