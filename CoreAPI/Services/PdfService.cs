using Core.Extensions;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using CoreAPI.BgService;
using HtmlAgilityPack;

namespace CoreAPI.Services
{
    public class PdfService
    {
        private readonly UserService _userService;
        public PdfService(UserService userService)
        {
            _userService = userService;
        }

        public async Task<string> CreateHtml(CreateHtmlVM createHtmlVM, string conn)
        {
            var component = await BgExt.ReadDsAs<Component>($"SELECT * FROM [Component] where Id = '{createHtmlVM.ComId}'", conn);
            var webConfigs = await BgExt.ReadDataSet($"SELECT * FROM [WebConfig]", conn);
            var components = await BgExt.ReadDsAsArr<Component>($"SELECT * FROM [Component] where Label is not null and Label != '' and FeatureId = '{component.FeatureId}' and ComponentGroupId is not null and ComponentType not in ('Button','Section','GridView')", conn);
            var gridPolicys = await BgExt.ReadDsAsArr<Component>($"SELECT * FROM [Component] where Label is not null and Label != '' and FeatureId = '{component.FeatureId}' and EntityId is not null and ComponentType not in ('Button','Section','GridView')", conn);
            var dirCom = components.DistinctBy(x => x.Label).ToDictionary(x => x.Label);
            var sql = component.Query;
            BindingDataExt.SetDefaultToken(createHtmlVM, _userService);
            if (webConfigs[0].Length > 0)
            {
                foreach (var item in webConfigs[0])
                {
                    createHtmlVM.Data.Add("C" + item["Key"], item["Value"]);
                }
            }
            if (!sql.IsNullOrWhiteSpace())
            {
                var sqlQuery = BindingDataExt.FormatString(sql, createHtmlVM.Data);
                var customData = await BgExt.ReadDataSet(sqlQuery, conn);
                foreach (var item in customData[0][0])
                {
                    if (createHtmlVM.Data.GetValueOrNull(item.Key) != null)
                    {
                        createHtmlVM.Data[item.Key] = item.Value;
                    }
                    else
                    {
                        createHtmlVM.Data.Add(item.Key, item.Value);
                    }
                }
                if (customData.Length > 1)
                {
                    int i = 0;
                    foreach (var row in customData.Skip(1).ToList())
                    {
                        createHtmlVM.Data.Add("c" + i, row);
                        i++;
                    }
                }
            }
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(component.Template);
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
    }
}
